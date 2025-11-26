let isRecording = false;
let currentFeatureName = 'MyFeature';
let recordedActions = [];

// Initialize storage
chrome.storage.local.set({ isRecording: false, actionCount: 0 });

chrome.runtime.onMessage.addListener((request, sender, sendResponse) => {
    if (request.command === 'startRecording') {
        isRecording = true;
        currentFeatureName = request.featureName;
        recordedActions = [];

        chrome.storage.local.set({
            isRecording: true,
            featureName: currentFeatureName,
            actionCount: 0
        });

        // Inject content script into active tab
        chrome.tabs.query({ active: true, currentWindow: true }, (tabs) => {
            if (tabs[0]) {
                chrome.tabs.sendMessage(tabs[0].id, { command: 'start' });
            }
        });

        sendResponse({ status: 'started' });
    }
    else if (request.command === 'stopRecording') {
        isRecording = false;
        chrome.storage.local.set({ isRecording: false });

        // Tell content script to stop
        chrome.tabs.query({ active: true, currentWindow: true }, (tabs) => {
            if (tabs[0]) {
                try {
                    chrome.tabs.sendMessage(tabs[0].id, { command: 'stop' }).catch(err => {
                        console.log('Could not send stop message to tab (likely restricted or closed):', err);
                    });
                } catch (e) {
                    console.log('Error sending stop message:', e);
                }
            }
        });

        generateFiles();
        sendResponse({ status: 'stopped' });
    }
    else if (request.command === 'recordAction') {
        if (isRecording) {
            recordedActions.push(request.action);
            chrome.storage.local.set({ actionCount: recordedActions.length });

            // Notify popup if open
            chrome.runtime.sendMessage({
                type: 'actionRecorded',
                count: recordedActions.length
            }).catch(() => { }); // Ignore error if popup is closed
        }
    }

    return true;
});

function generateFiles() {
    const featureContent = generateFeatureFile(recordedActions, currentFeatureName);
    const stepsContent = generateStepsFile(recordedActions, currentFeatureName);

    // Download Feature File
    const featureBlob = new Blob([featureContent], { type: 'text/plain' });
    const featureUrl = URL.createObjectURL(featureBlob);

    chrome.downloads.download({
        url: featureUrl,
        filename: `${currentFeatureName}.feature`,
        saveAs: true
    }, () => {
        // Download Steps File after the first one initiates
        const stepsBlob = new Blob([stepsContent], { type: 'text/plain' });
        const stepsUrl = URL.createObjectURL(stepsBlob);

        chrome.downloads.download({
            url: stepsUrl,
            filename: `${currentFeatureName}Steps.cs`,
            saveAs: true
        });
    });
}

function generateFeatureFile(actions, featureName) {
    let content = `Feature: ${featureName}\n\n`;
    content += `  Scenario: Recorded Scenario\n`;

    // Simple deduplication logic could go here

    actions.forEach(action => {
        switch (action.type) {
            case 'navigate':
                content += `    Given I navigate to "${action.value}"\n`;
                break;
            case 'click':
                content += `    When I click the element with ${action.selector} "${action.selectorValue}"\n`;
                break;
            case 'type':
                content += `    When I type "${action.value}" into element with ${action.selector} "${action.selectorValue}"\n`;
                break;
            case 'enterkey':
                content += `    When I type "${action.value}" and press Enter in element with ${action.selector} "${action.selectorValue}"\n`;
                break;
        }
    });

    content += `    Then the page should be in the expected state\n`;
    return content;
}

function generateStepsFile(actions, featureName) {
    const className = `${featureName}Steps`;
    let content = `using System;
using TechTalk.SpecFlow;
using OpenQA.Selenium;
using System.Threading;

namespace SpecFlowTests.Steps
{
    [Binding]
    public class ${className}
    {
        private readonly IWebDriver _driver;

        public ${className}(IWebDriver driver)
        {
            _driver = driver;
        }

        [Then(@"the page should be in the expected state")]
        public void ThenThePageShouldBeInTheExpectedState()
        {
            Thread.Sleep(2000);
        }
`;

    // Generate step definitions
    const signatures = new Set();

    actions.forEach(action => {
        if (action.type === 'navigate') {
            if (!signatures.has('NavigateToUrl')) {
                content += `
        [Given(@"I navigate to ""(.*)""")]
        [When(@"I navigate to ""(.*)""")]
        public void NavigateToUrl(string url)
        {
            _driver.Navigate().GoToUrl(url);
            Thread.Sleep(1000);
        }
`;
                signatures.add('NavigateToUrl');
            }
        }
        else if (action.type === 'click') {
            if (!signatures.has('ClickElementWith')) {
                content += `
        [When(@"I click the element with (.*?) ""(.*?)""")]
        public void ClickElementWith(string selectorType, string selectorValue)
        {
            var element = GetElement(selectorType, selectorValue);
            element.Click();
            Thread.Sleep(500);
        }
`;
                signatures.add('ClickElementWith');
            }
        }
        else if (action.type === 'type') {
            if (!signatures.has('TypeIntoElement')) {
                content += `
        [When(@"I type ""(.*)"" into element with (.*?) ""(.*?)""")]
        public void TypeIntoElement(string text, string selectorType, string selectorValue)
        {
            var element = GetElement(selectorType, selectorValue);
            element.Clear();
            element.SendKeys(text);
            Thread.Sleep(300);
        }
`;
                signatures.add('TypeIntoElement');
            }
        }
        else if (action.type === 'enterkey') {
            if (!signatures.has('TypeAndEnter')) {
                content += `
        [When(@"I type ""(.*)"" and press Enter in element with (.*?) ""(.*?)""")]
        public void TypeAndEnter(string text, string selectorType, string selectorValue)
        {
            var element = GetElement(selectorType, selectorValue);
            element.Clear();
            element.SendKeys(text);
            element.SendKeys(Keys.Enter);
            Thread.Sleep(1000);
        }
`;
                signatures.add('TypeAndEnter');
            }
        }
    });

    // Add Helper Method
    content += `
        private IWebElement GetElement(string selectorType, string selectorValue)
        {
            By by;
            switch (selectorType.toLowerCase())
            {
                case "id": by = By.Id(selectorValue); break;
                case "cssselector": by = By.CssSelector(selectorValue); break;
                case "xpath": by = By.XPath(selectorValue); break;
                case "name": by = By.Name(selectorValue); break;
                default: by = By.CssSelector(selectorValue); break;
            }
            return _driver.FindElement(by);
        }
    }
}`;

    return content;
}
