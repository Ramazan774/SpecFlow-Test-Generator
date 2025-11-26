using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using TechTalk.SpecFlow;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SpecFlowTests.Steps
{
    [Binding]
    public class MyFeatureSteps
    {
        private readonly IWebDriver _driver;

    public MyFeatureSteps(IWebDriver driver)
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        
        // Set implicit wait to improve stability
        _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
    }

    [Given(@"I navigate to ""(.*)""")]
    [When(@"I navigate to ""(.*)""")]
    public void NavigateToUrl(string url)
    {
        Console.WriteLine($"Navigating to {url}");
        _driver.Navigate().GoToUrl(url);
        
        // Wait for page to load completely
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(20));
        _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
        
        // Additional wait to ensure UI is ready
        Thread.Sleep(1000);
    }

    [When(@"I click the element with (.*?) ""(.*?)""")]
    public void ClickElementWith(string selectorType, string selectorValue)
    {
        Console.WriteLine($"Clicking element with {selectorType}='{selectorValue}'");
        var element = WaitForElement(selectorType, selectorValue);
        
        try
        {
            // Scroll element into view for better reliability
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", element);
            Thread.Sleep(300);
            
            element.Click();
        }
        catch (ElementClickInterceptedException)
        {
            // Fallback to JavaScript click if regular click fails
            Console.WriteLine("Regular click failed, trying JavaScript click...");
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", element);
        }
        
        // Wait for UI to update after click
        Thread.Sleep(500);
    }

    [When(@"I click the (\d+)[st|nd|rd|th]* element with (.*?) ""(.*?)""")]
    public void ClickNthElementWith(int index, string selectorType, string selectorValue)
    {
        Console.WriteLine($"Clicking element #{index} with {selectorType}='{selectorValue}'");
        var elements = FindElements(selectorType, selectorValue);
        
        if (elements.Count <= index)
        {
            throw new NoSuchElementException($"Found {elements.Count} elements with {selectorType}='{selectorValue}' but index {index} is out of range");
        }
        
        var element = elements.ElementAt(index);
        
        try
        {
            // Scroll element into view for better reliability
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", element);
            Thread.Sleep(300);
            
            element.Click();
        }
        catch (ElementClickInterceptedException)
        {
            // Fallback to JavaScript click if regular click fails
            Console.WriteLine("Regular click failed, trying JavaScript click...");
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", element);
        }
        
        // Wait for UI to update after click
        Thread.Sleep(500);
    }

    [When(@"I type ""(.*)"" and press Enter in element with (.*?) ""(.*?)""")]
    [Given(@"I type ""(.*)"" and press Enter in element with (.*?) ""(.*?)""")]
    public void TypeAndEnter(string text, string selectorType, string selectorValue)
    {
        Console.WriteLine($"Typing '{text}' and pressing Enter in element with {selectorType}='{selectorValue}'");
        var element = WaitForElement(selectorType, selectorValue);
        
        try
        {
            // Scroll element into view
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", element);
            Thread.Sleep(300);
            
            element.Clear();
            element.SendKeys(text);
            Thread.Sleep(300); // Short pause before pressing Enter
            element.SendKeys(Keys.Enter);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Standard typing failed: {ex.Message}");
            Console.WriteLine("Trying JavaScript approach...");
            
            // Fallback to JavaScript for setting value and pressing Enter
            IJavaScriptExecutor js = (IJavaScriptExecutor)_driver;
            js.ExecuteScript("arguments[0].value = arguments[1];", element, text);
            js.ExecuteScript("arguments[0].dispatchEvent(new Event('change'));", element);
            js.ExecuteScript("arguments[0].dispatchEvent(new KeyboardEvent('keydown', {key: 'Enter', code: 'Enter', keyCode: 13}));", element);
        }
        
        // Wait for UI to update after pressing Enter
        Thread.Sleep(1000);
    }

    [When(@"I type ""(.*)"" and press Enter in the (\d+)[st|nd|rd|th]* element with (.*?) ""(.*?)""")]
    [Given(@"I type ""(.*)"" and press Enter in the (\d+)[st|nd|rd|th]* element with (.*?) ""(.*?)""")]
    public void TypeAndEnterNthElement(string text, int index, string selectorType, string selectorValue)
    {
        Console.WriteLine($"Typing '{text}' and pressing Enter in element #{index} with {selectorType}='{selectorValue}'");
        var elements = FindElements(selectorType, selectorValue);
        
        if (elements.Count <= index)
        {
            throw new NoSuchElementException($"Found {elements.Count} elements with {selectorType}='{selectorValue}' but index {index} is out of range");
        }
        
        var element = elements.ElementAt(index);
        
        try
        {
            // Scroll element into view
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", element);
            Thread.Sleep(300);
            
            element.Clear();
            element.SendKeys(text);
            Thread.Sleep(300); // Short pause before pressing Enter
            element.SendKeys(Keys.Enter);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Standard typing failed: {ex.Message}");
            Console.WriteLine("Trying JavaScript approach...");
            
            // Fallback to JavaScript for setting value and pressing Enter
            IJavaScriptExecutor js = (IJavaScriptExecutor)_driver;
            js.ExecuteScript("arguments[0].value = arguments[1];", element, text);
            js.ExecuteScript("arguments[0].dispatchEvent(new Event('change'));", element);
            js.ExecuteScript("arguments[0].dispatchEvent(new KeyboardEvent('keydown', {key: 'Enter', code: 'Enter', keyCode: 13}));", element);
        }
        
        // Wait for UI to update after pressing Enter
        Thread.Sleep(1000);
    }

    [When(@"I press Enter in element with (.*?) ""(.*?)""")]
    [Given(@"I press Enter in element with (.*?) ""(.*?)""")]
    public void PressEnterInElement(string selectorType, string selectorValue)
    {
        Console.WriteLine($"Pressing Enter in element with {selectorType}='{selectorValue}'");
        var element = WaitForElement(selectorType, selectorValue);
        
        try
        {
            element.SendKeys(Keys.Enter);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Standard Enter key failed: {ex.Message}");
            Console.WriteLine("Trying JavaScript approach...");
            
            // Fallback to JavaScript for pressing Enter
            IJavaScriptExecutor js = (IJavaScriptExecutor)_driver;
            js.ExecuteScript("arguments[0].dispatchEvent(new KeyboardEvent('keydown', {key: 'Enter', code: 'Enter', keyCode: 13}));", element);
        }
        
        // Wait for UI to update after pressing Enter
        Thread.Sleep(1000);
    }

    [Then(@"the page should be in the expected state")]
    public void ThenExpectedState()
    {
        Console.WriteLine("Verifying the expected page state");
        
        // Basic verification that page loaded successfully
        Assert.That(_driver.Title != null, "Page title should not be null");
        Assert.That(_driver.PageSource.Length > 0, "Page source should not be empty");
        
        // Wait to see the final state
        Thread.Sleep(2000);
    }

    [Then(@"I wait and print element info for (.*?) ""(.*?)""")]
    public void ThenIWaitAndPrintElementInfo(string selectorType, string selectorValue)
    {
        Console.WriteLine($"Diagnostic info for {selectorType}='{selectorValue}'");
        
        try
        {
            var elements = FindElements(selectorType, selectorValue);
            Console.WriteLine($"Found {elements.Count} matching elements");
            
            int index = 0;
            foreach (var element in elements)
            {
                Console.WriteLine($"Element #{index}: Tag={element.TagName}, Displayed={element.Displayed}, Enabled={element.Enabled}");
                index++;
            }
            
            // If no elements found, try to report what's on the page
            if (elements.Count == 0)
            {
                Console.WriteLine("Looking for similar elements on the page...");
                var allElements = _driver.FindElements(By.XPath("//*"));
                var relevantElements = new List<IWebElement>();
                
                foreach (var el in allElements)
                {
                    try {
                        string tagName = el.TagName?.ToLower() ?? "";
                        if (tagName == "input" || tagName == "button" || tagName == "a" || tagName == "select")
                        {
                            relevantElements.Add(el);
                        }
                    } catch { /* Ignore stale elements */ }
                }
                
                Console.WriteLine($"Found {relevantElements.Count} interactive elements on the page");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during diagnostics: {ex.Message}");
        }
    }

    [Then(@"I should see (\d+) elements? with (.*?) ""(.*?)""")]
    public void ThenIShouldSeeElements(int expectedCount, string selectorType, string selectorValue)
    {
        Console.WriteLine($"Checking for {expectedCount} elements with {selectorType}='{selectorValue}'");
        var elements = FindElements(selectorType, selectorValue);
        Assert.That(elements.Count, Is.EqualTo(expectedCount), $"Expected {expectedCount} elements but found {elements.Count}");
    }

    // Helper method to get By locator from string values
    private By GetBy(string selectorType, string selectorValue)
    {
        selectorValue = selectorValue ?? string.Empty;
        selectorType = selectorType ?? string.Empty;
        switch (selectorType.ToLowerInvariant().Trim())
        {
            case "id": return By.Id(selectorValue);
            case "name": return By.Name(selectorValue);
            case "classname": return By.ClassName(selectorValue);
            case "cssselector": return By.CssSelector(selectorValue);
            case "xpath": return By.XPath(selectorValue);
            case "linktext": return By.LinkText(selectorValue);
            case "partiallinktext": return By.PartialLinkText(selectorValue);
            case "tagname": return By.TagName(selectorValue);
            // Handle attribute selectors
            case "aria-label": return By.CssSelector($"[aria-label='{selectorValue}']");
            case "placeholder": return By.CssSelector($"[placeholder='{selectorValue}']");
            case "data-test-id": return By.CssSelector($"[data-test-id='{selectorValue}']");
            case "data-testid": return By.CssSelector($"[data-testid='{selectorValue}']");
            case "data-test": return By.CssSelector($"[data-test='{selectorValue}']");
            // Handle other attribute-based selectors
            default:
                // If selector type looks like an attribute, use it as an attribute selector
                if (!selectorType.Contains(" ") && !selectorType.Contains(">") && !selectorType.Contains("["))
                {
                    return By.CssSelector($"[{selectorType}='{selectorValue}']");
                }
                throw new ArgumentException($"Unsupported selector type provided: '{selectorType}'. Value was '{selectorValue}'.");
        }
    }

    // Helper method to find all elements matching a selector
    private IReadOnlyCollection<IWebElement> FindElements(string selectorType, string selectorValue, int timeoutSeconds = 10)
    {
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
        var by = GetBy(selectorType, selectorValue);
        
        try
        {
            Console.WriteLine($"Finding elements with {selectorType}='{selectorValue}'");
            var elements = wait.Until(driver => {
                var foundElements = driver.FindElements(by);
                return foundElements.Count > 0 ? foundElements : null;
            });
            
            Console.WriteLine($"Found {elements.Count} elements with {selectorType}='{selectorValue}'");
            return elements;
        }
        catch (WebDriverTimeoutException)
        {
            Console.WriteLine($"No elements found with {selectorType}='{selectorValue}'");
            return new List<IWebElement>();
        }
    }

    // Helper method to wait for an element to be present and visible
    private IWebElement WaitForElement(string selectorType, string selectorValue, int timeoutSeconds = 10)
    {
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
        var by = GetBy(selectorType, selectorValue);
        
        try
        {
            // First attempt with the provided selector
            Console.WriteLine($"Looking for element with {selectorType}='{selectorValue}'");
            var element = wait.Until(driver => {
                var foundElement = driver.FindElement(by);
                
                // Special handling for checkboxes/radios that might be hidden by custom UI
                if (!foundElement.Displayed && foundElement.TagName.ToLower() == "input" && 
                   (foundElement.GetAttribute("type") == "checkbox" || foundElement.GetAttribute("type") == "radio"))
                {
                    return foundElement;
                }
                
                return foundElement.Displayed ? foundElement : null;
            });
            
            // Check for multiple matches and warn if found
            var allMatches = _driver.FindElements(by);
            if (allMatches.Count > 1)
            {
                Console.WriteLine($"WARNING: Found {allMatches.Count} elements with {selectorType}='{selectorValue}'. Using the first match. For a specific element, use 'I click the Nth element with...' step");
            }
            
            return element;
        }
        catch (WebDriverTimeoutException)
        {
            Console.WriteLine($"Element not found with primary selector, trying fallbacks");
            
            // Try with CSS selector alternative if className was used
            if (selectorType.Equals("ClassName", StringComparison.OrdinalIgnoreCase))
            {
                try 
                {
                    Console.WriteLine($"Trying CSS selector alternative: .{selectorValue}");
                    var cssSelector = By.CssSelector("." + selectorValue);
                    var element = _driver.FindElement(cssSelector);
                    if (element.Displayed)
                    {
                        Console.WriteLine("Found element with CSS selector");
                        return element;
                    }
                }
                catch { /* Continue to next approach */ }
            }
            
            // Try simple JavaScript approach as a last resort
            try
            {
                IJavaScriptExecutor js = (IJavaScriptExecutor)_driver;
                Console.WriteLine("Trying JavaScript to find element");
                
                // Simple script to try class name or attribute
                if (selectorType.Equals("ClassName", StringComparison.OrdinalIgnoreCase))
                {
                    var elements = js.ExecuteScript(
                        "return document.getElementsByClassName(arguments[0])", selectorValue) as IReadOnlyCollection<IWebElement>;
                    
                    if (elements != null && elements.Count > 0)
                    {
                        foreach (var element in elements)
                        {
                            if (element.Displayed)
                            {
                                return element;
                            }
                        }
                    }
                }
                
                // Try by generic attribute - FIXED JavaScript format
                var elementByAttr = js.ExecuteScript(
                    "return document.querySelector('[' + arguments[0] + '=\"' + arguments[1] + '\"]')", 
                    selectorType, selectorValue) as IWebElement;
                
                if (elementByAttr != null && elementByAttr.Displayed)
                {
                    Console.WriteLine("Found element using JavaScript");
                    return elementByAttr;
                }
            }
            catch (Exception jsEx)
            {
                Console.WriteLine($"JavaScript approach failed: {jsEx.Message}");
            }
            
            // Element not found after trying fallbacks
            throw new NoSuchElementException($"Element with {selectorType}='{selectorValue}' not found after trying fallbacks");
        }
    }
    }
}
