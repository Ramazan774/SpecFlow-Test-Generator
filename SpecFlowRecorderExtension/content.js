(function () {
    let isRecording = false;

    chrome.runtime.onMessage.addListener((request, sender, sendResponse) => {
        if (request.command === 'start') {
            isRecording = true;
            attachListeners();
        } else if (request.command === 'stop') {
            isRecording = false;
            removeListeners();
        }
    });

    chrome.storage.local.get(['isRecording'], (result) => {
        if (result.isRecording) {
            isRecording = true;
            attachListeners();
        }
    });

    const inputValues = new Map();

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
            const dataAttrs = ['data-testid', 'data-test-id', 'data-test', 'data-qa'];
            for (const attr of dataAttrs) {
                if (el.hasAttribute(attr)) {
                    const val = el.getAttribute(attr);
                    const sel = `[${attr}="${val}"]`;
                    if (isUnique(sel)) return { type: 'CssSelector', value: sel };
                }
            }

            if (el.id && isUnique('#' + CSS.escape(el.id))) {
                return { type: 'Id', value: el.id };
            }

            if (el.name && isUnique(`[name="${el.name}"]`)) {
                return { type: 'Name', value: el.name };
            }

            if (['BUTTON', 'A', 'LABEL', 'SPAN', 'DIV', 'H1', 'H2', 'H3', 'H4', 'H5', 'H6'].includes(el.tagName)) {
                const text = el.innerText.trim();
                if (text && text.length < 50 && !text.includes('"') && !text.includes("'")) {
                    const xpath = `//${el.tagName.toLowerCase()}[normalize-space()="${text}"]`;
                    if (document.evaluate(`count(${xpath})`, document, null, XPathResult.NUMBER_TYPE, null).numberValue === 1) {
                        return { type: 'XPath', value: xpath };
                    }
                }
            }

            if (el.tagName === 'INPUT' || el.tagName === 'BUTTON') {
                let sibling = el.nextElementSibling;
                while (sibling) {
                    if (sibling.tagName === 'LABEL' && sibling.innerText.trim().length > 0) {
                        const text = sibling.innerText.trim();
                        if (!text.includes('"') && !text.includes("'")) {
                            const xpath = `//label[normalize-space()="${text}"]/preceding-sibling::${el.tagName.toLowerCase()}`;
                            if (document.evaluate(`count(${xpath})`, document, null, XPathResult.NUMBER_TYPE, null).numberValue === 1) {
                                return { type: 'XPath', value: xpath };
                            }
                        }
                    }
                    sibling = sibling.nextElementSibling;
                }

                const parent = el.parentElement;
                if (parent && ['LI', 'DIV', 'TR'].includes(parent.tagName)) {
                    const text = parent.innerText.trim();
                    if (text && text.length > 0 && text.length < 50 && !text.includes('"') && !text.includes("'")) {
                        const xpath = `//${parent.tagName.toLowerCase()}[contains(., "${text}")]//${el.tagName.toLowerCase()}`;
                        if (document.evaluate(`count(${xpath})`, document, null, XPathResult.NUMBER_TYPE, null).numberValue === 1) {
                            return { type: 'XPath', value: xpath };
                        }
                    }
                }
            }

            const placeholder = el.getAttribute('placeholder');
            if (placeholder && isUnique(`[placeholder="${placeholder}"]`)) {
                return { type: 'CssSelector', value: `[placeholder="${placeholder}"]` };
            }
            if (el.className && typeof el.className === 'string' && el.className.trim().length > 0) {
                const classes = el.className.split(/\s+/).filter(c => c);
                if (classes.length > 0) {
                    const classSel = '.' + classes.join('.');
                    if (isUnique(classSel)) return { type: 'CssSelector', value: classSel };
                }
            }

            const cssPath = getCssPath(el);
            if (cssPath && isUnique(cssPath)) {
                return { type: 'CssSelector', value: cssPath };
            }

            const absXPath = getXPath(el);
            if (absXPath) {
                return { type: 'XPath', value: absXPath };
            }

            return { type: 'TagName', value: el.tagName.toLowerCase() };
        }
        catch (e) {
            console.error('Error getting selector:', e);
            return { type: 'TagName', value: el.tagName ? el.tagName.toLowerCase() : 'unknown' };
        }
    }

    function findInputElement(el) {
        if (el.tagName === 'INPUT' || el.tagName === 'TEXTAREA' || el.tagName === 'SELECT') {
            return el;
        }
        const inputs = el.querySelectorAll('input, textarea, select');
        if (inputs && inputs.length > 0) return inputs[0];
        if (el.shadowRoot) {
            const shadowInputs = el.shadowRoot.querySelectorAll('input, textarea, select');
            if (shadowInputs && shadowInputs.length > 0) return shadowInputs[0];
        }
        return null;
    }

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

    function handleEvent(e) {
        if (!isRecording) return;

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
                elementType: target.type,
                url: window.location.href
            };

            if (e.type === 'change') {
                // Ignore change for now
            }
            else if (e.type === 'click') {
                action.value = inputValues.get(target) || null;
                chrome.runtime.sendMessage({ command: 'recordAction', action: action });
            }
            else if (e.type === 'keydown' && e.key === 'Enter') {
                action.type = 'enterkey';
                action.value = inputValues.get(target) || (inputEl ? inputEl.value : target.value);
                chrome.runtime.sendMessage({ command: 'recordAction', action: action });
            }
        }
        catch (error) {
            console.error('Event handling error:', error);
        }
    }

    function attachListeners() {
        document.addEventListener('click', handleEvent, { capture: true, passive: true });
        document.addEventListener('keydown', handleEvent, { capture: true, passive: true });
        console.log('SpecFlow Recorder: Listeners attached');

        // Record initial navigation
        chrome.runtime.sendMessage({
            command: 'recordAction',
            action: { type: 'navigate', value: window.location.href }
        });
    }

    function removeListeners() {
        document.removeEventListener('click', handleEvent, { capture: true });
        document.removeEventListener('keydown', handleEvent, { capture: true });
        console.log('SpecFlow Recorder: Listeners removed');
    }

})();
