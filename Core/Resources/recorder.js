(function () {
    // Avoid reinitializing if already done
    if (window.cdpRecorderListenersAttached) {
        return;
    }

    console.log('Attaching CDP recorder listeners');

    // Track input values
    const inputValues = new Map();

    // --- Selector Generation Logic ---

    function getCssPath(el) {
        if (!(el instanceof Element)) return;
        const path = [];
        while (el.nodeType === Node.ELEMENT_NODE) {
            let selector = el.nodeName.toLowerCase();
            if (el.id) {
                selector += '#' + CSS.escape(el.id);
                path.unshift(selector);
                break;
            } else {
                let sib = el, nth = 1;
                while (sib = sib.previousElementSibling) {
                    if (sib.nodeName.toLowerCase() == selector)
                        nth++;
                }
                if (nth != 1)
                    selector += ':nth-of-type(' + nth + ')';
            }
            path.unshift(selector);
            el = el.parentNode;
        }
        return path.join(' > ');
    }

    function getXPath(element) {
        if (element.id !== '')
            return 'id("' + element.id + '")';
        if (element === document.body)
            return element.tagName;

        var ix = 0;
        var siblings = element.parentNode.childNodes;
        for (var i = 0; i < siblings.length; i++) {
            var sibling = siblings[i];
            if (sibling === element)
                return getXPath(element.parentNode) + '/' + element.tagName + '[' + (ix + 1) + ']';
            if (sibling.nodeType === 1 && sibling.tagName === element.tagName)
                ix++;
        }
    }

    function isUnique(selector) {
        try {
            return document.querySelectorAll(selector).length === 1;
        } catch (e) { return false; }
    }

    function getBestSelector(el) {
        if (!el || !el.tagName) return null;

        try {
            // 1. Data Attributes (Best Practice)
            const dataAttrs = ['data-testid', 'data-test-id', 'data-test', 'data-qa'];
            for (const attr of dataAttrs) {
                if (el.hasAttribute(attr)) {
                    const val = el.getAttribute(attr);
                    const sel = `[${attr}="${val}"]`;
                    if (isUnique(sel)) return { type: 'CssSelector', value: sel };
                }
            }

            // 2. ID (if unique)
            if (el.id && isUnique('#' + CSS.escape(el.id))) {
                return { type: 'Id', value: el.id };
            }

            // 3. Name (for inputs)
            if (el.name && isUnique(`[name="${el.name}"]`)) {
                return { type: 'Name', value: el.name };
            }

            // 4. Text Content (for buttons, links, labels) - XPath
            if (['BUTTON', 'A', 'LABEL', 'SPAN', 'DIV', 'H1', 'H2', 'H3', 'H4', 'H5', 'H6'].includes(el.tagName)) {
                const text = el.innerText.trim();
                if (text && text.length < 50 && !text.includes('"') && !text.includes("'")) {
                    const xpath = `//${el.tagName.toLowerCase()}[normalize-space()="${text}"]`;
                    if (document.evaluate(`count(${xpath})`, document, null, XPathResult.NUMBER_TYPE, null).numberValue === 1) {
                        return { type: 'XPath', value: xpath };
                    }
                }
            }

            // 5. Contextual Text (Sibling/Parent) - Great for Todo lists
            // Example: <li ...><input type="checkbox"><label>Buy milk</label>...</li>
            // Target: input. We want to find it via "Buy milk"
            if (el.tagName === 'INPUT' || el.tagName === 'BUTTON') {
                // Check siblings for label
                let sibling = el.nextElementSibling;
                while (sibling) {
                    if (sibling.tagName === 'LABEL' && sibling.innerText.trim().length > 0) {
                        const text = sibling.innerText.trim();
                        if (!text.includes('"') && !text.includes("'")) {
                            // XPath: //label[text()="Buy milk"]/preceding-sibling::input
                            const xpath = `//label[normalize-space()="${text}"]/preceding-sibling::${el.tagName.toLowerCase()}`;
                            if (document.evaluate(`count(${xpath})`, document, null, XPathResult.NUMBER_TYPE, null).numberValue === 1) {
                                return { type: 'XPath', value: xpath };
                            }
                        }
                    }
                    sibling = sibling.nextElementSibling;
                }

                // Check parent for text (e.g. <li><input> Text </li>)
                const parent = el.parentElement;
                if (parent && ['LI', 'DIV', 'TR'].includes(parent.tagName)) {
                    const text = parent.innerText.trim();
                    if (text && text.length > 0 && text.length < 50 && !text.includes('"') && !text.includes("'")) {
                        // XPath: //li[contains(., "Buy milk")]//input
                        const xpath = `//${parent.tagName.toLowerCase()}[contains(., "${text}")]//${el.tagName.toLowerCase()}`;
                        if (document.evaluate(`count(${xpath})`, document, null, XPathResult.NUMBER_TYPE, null).numberValue === 1) {
                            return { type: 'XPath', value: xpath };
                        }
                    }
                }
            }

            // 6. Placeholder
            const placeholder = el.getAttribute('placeholder');
            if (placeholder && isUnique(`[placeholder="${placeholder}"]`)) {
                return { type: 'CssSelector', value: `[placeholder="${placeholder}"]` };
            }

            // 7. Class combination (heuristic)
            if (el.className && typeof el.className === 'string' && el.className.trim().length > 0) {
                const classes = el.className.split(/\s+/).filter(c => c);
                if (classes.length > 0) {
                    const classSel = '.' + classes.join('.');
                    if (isUnique(classSel)) return { type: 'CssSelector', value: classSel };
                }
            }

            // 8. Full CSS Path (Robust fallback)
            const cssPath = getCssPath(el);
            if (cssPath && isUnique(cssPath)) {
                return { type: 'CssSelector', value: cssPath };
            }

            // 9. XPath Absolute (Final fallback)
            const absXPath = getXPath(el);
            if (absXPath) {
                return { type: 'XPath', value: absXPath };
            }

            // Fallback to TagName (likely not unique but better than nothing)
            return { type: 'TagName', value: el.tagName.toLowerCase() };
        }
        catch (e) {
            console.error('Error getting selector:', e);
            return { type: 'TagName', value: el.tagName ? el.tagName.toLowerCase() : 'unknown' };
        }
    }

    // Find input element in container or shadow DOM
    function findInputElement(el) {
        if (el.tagName === 'INPUT' || el.tagName === 'TEXTAREA' || el.tagName === 'SELECT') {
            return el;
        }

        const inputs = el.querySelectorAll('input, textarea, select');
        if (inputs && inputs.length > 0) {
            return inputs[0];
        }

        if (el.shadowRoot) {
            const shadowInputs = el.shadowRoot.querySelectorAll('input, textarea, select');
            if (shadowInputs && shadowInputs.length > 0) {
                return shadowInputs[0];
            }
        }

        return null;
    }

    // Track input values as they change
    document.addEventListener('input', function (e) {
        const target = e.target;
        if (!target || !target.tagName) return;

        inputValues.set(target, target.value);

        let parent = target.parentElement;
        while (parent && parent !== document.body) {
            inputValues.set(parent, target.value);
            parent = parent.parentElement;
        }
    }, true);

    // Handle user actions
    function handleEvent(e) {
        const target = e.target;
        if (!target || !target.tagName || target.tagName === 'HTML' || target.tagName === 'BODY') {
            return;
        }

        try {
            const inputEl = findInputElement(target);
            const value = inputEl ? inputEl.value : (inputValues.get(target) || target.value);
            const selector = getBestSelector(target);

            if (!selector) return;

            let action = {
                type: e.type,
                selector: selector.type,
                selectorValue: selector.value,
                value: value,
                key: e.key,
                tagName: target.tagName,
                elementType: target.type
            };

            // Process different event types
            if (e.type === 'change') {
                window['sendActionToCSharp'](JSON.stringify(action));
            }
            else if (e.type === 'click') {
                action.value = inputValues.get(target) || null;
                window['sendActionToCSharp'](JSON.stringify(action));
            }
            else if (e.type === 'keydown' && e.key === 'Enter') {
                action.type = 'enterkey';
                action.value = inputValues.get(target) || (inputEl ? inputEl.value : target.value);
                window['sendActionToCSharp'](JSON.stringify(action));
            }
        }
        catch (error) {
            console.error('Event handling error:', error);
        }
    }

    // Add event listeners
    function attachListeners() {
        if (window.cdpRecorderListenersAttached) return;
        window.cdpRecorderListenersAttached = true;

        document.addEventListener('click', handleEvent, { capture: true, passive: true });
        document.addEventListener('change', handleEvent, { capture: true, passive: true });
        document.addEventListener('keydown', handleEvent, { capture: true, passive: true });
        console.log('CDP Recorder listeners attached');
    }

    // Attach immediately
    attachListeners();

    // And ensure it attaches if the document is still loading
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', attachListeners);
    }

    console.log('CDP Recorder initialized successfully');
})();
