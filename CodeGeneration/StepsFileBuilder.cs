using System;
using System.Collections.Generic;
using System.Text;
using SpecFlowTestGenerator.Models;

namespace SpecFlowTestGenerator.CodeGeneration
{
    /// <summary>
    /// Builds SpecFlow steps files from recorded actions without website-specific code
    /// </summary>
    public class StepsFileBuilder
    {
        /// <summary>
        /// Build the content of a SpecFlow steps file
        /// </summary>
        public string BuildStepsFileContent(List<RecordedAction> actions, string stepsClassName)
        {
            StringBuilder stepsFile = new StringBuilder();
            
            // Add file header
            stepsFile.AppendLine("using OpenQA.Selenium;");
            stepsFile.AppendLine("using OpenQA.Selenium.Support.UI;");
            stepsFile.AppendLine("using TechTalk.SpecFlow;");
            stepsFile.AppendLine("using NUnit.Framework;");
            stepsFile.AppendLine("using System;");
            stepsFile.AppendLine("using System.Collections.Generic;");
            stepsFile.AppendLine("using System.Threading;");
            stepsFile.AppendLine();
            
            stepsFile.AppendLine("namespace SpecFlowTests.Steps");
            stepsFile.AppendLine("{");
            
            // Add class definition
            stepsFile.AppendLine("    [Binding]");
            stepsFile.AppendLine($"    public class {stepsClassName}");
            stepsFile.AppendLine("    {");
            stepsFile.AppendLine("        private readonly IWebDriver _driver;");
            stepsFile.AppendLine();
            
            // Add constructor
            stepsFile.AppendLine($"    public {stepsClassName}(IWebDriver driver)");
            stepsFile.AppendLine("    {");
            stepsFile.AppendLine("        _driver = driver ?? throw new ArgumentNullException(nameof(driver));");
            stepsFile.AppendLine("        ");
            stepsFile.AppendLine("        // Set implicit wait to improve stability");
            stepsFile.AppendLine("        _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);");
            stepsFile.AppendLine("    }");
            stepsFile.AppendLine();
            
            // Add step methods based on the types of actions recorded
            var generatedSignatures = new HashSet<string>();
            
            if (actions.Exists(a => a.ActionType == "Navigate"))
            {
                AddNavigateSteps(stepsFile, generatedSignatures);
            }
            
            if (actions.Exists(a => a.ActionType == "Click"))
            {
                AddClickSteps(stepsFile, generatedSignatures);
            }
            
            if (actions.Exists(a => a.ActionType == "SendKeys"))
            {
                AddSendKeysSteps(stepsFile, generatedSignatures);
            }
            
            if (actions.Exists(a => a.ActionType == "SendKeysEnter"))
            {
                AddEnterSteps(stepsFile, generatedSignatures);
            }
            
            if (actions.Exists(a => a.ActionType == "SelectOption"))
            {
                AddSelectSteps(stepsFile, generatedSignatures);
            }
            
            // Always add Then step and diagnostic steps
            AddThenSteps(stepsFile, generatedSignatures);
            
            // Add helper methods
            AddGetByHelper(stepsFile);
            AddFindElementsHelper(stepsFile);
            AddWaitForElementHelper(stepsFile);
            
            // Close class
            stepsFile.AppendLine("    }");
            stepsFile.AppendLine("}");
            
            return stepsFile.ToString();
        }

        private void AddNavigateSteps(StringBuilder s, HashSet<string> sig)
        {
            if (sig.Add("NavigateToUrl"))
            {
                s.AppendLine("    [Given(@\"I navigate to \"\"(.*)\"\"\")]");
                s.AppendLine("    [When(@\"I navigate to \"\"(.*)\"\"\")]");
                s.AppendLine("    public void NavigateToUrl(string url)");
                s.AppendLine("    {");
                s.AppendLine("        Console.WriteLine($\"Navigating to {url}\");");
                s.AppendLine("        _driver.Navigate().GoToUrl(url);");
                s.AppendLine("        ");
                s.AppendLine("        // Wait for page to load completely");
                s.AppendLine("        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(20));");
                s.AppendLine("        _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);");
                s.AppendLine("        ");
                s.AppendLine("        // Additional wait to ensure UI is ready");
                s.AppendLine("        Thread.Sleep(1000);");
                s.AppendLine("    }");
                s.AppendLine();
            }
        }

        private void AddClickSteps(StringBuilder s, HashSet<string> sig)
        {
            if (sig.Add("ClickElement"))
            {
                // Use the exact pattern that matches the feature file
                s.AppendLine("    [When(@\"I click the element with (.*?) \"\"(.*?)\"\"\")]");
                s.AppendLine("    public void ClickElementWith(string selectorType, string selectorValue)");
                s.AppendLine("    {");
                s.AppendLine("        Console.WriteLine($\"Clicking element with {selectorType}='{selectorValue}'\");");
                s.AppendLine("        var element = WaitForElement(selectorType, selectorValue);");
                s.AppendLine("        ");
                s.AppendLine("        try");
                s.AppendLine("        {");
                s.AppendLine("            // Scroll element into view for better reliability");
                s.AppendLine("            ((IJavaScriptExecutor)_driver).ExecuteScript(\"arguments[0].scrollIntoView({block: 'center'});\", element);");
                s.AppendLine("            Thread.Sleep(300);");
                s.AppendLine("            ");
                s.AppendLine("            element.Click();");
                s.AppendLine("        }");
                s.AppendLine("        catch (ElementClickInterceptedException)");
                s.AppendLine("        {");
                s.AppendLine("            // Fallback to JavaScript click if regular click fails");
                s.AppendLine("            Console.WriteLine(\"Regular click failed, trying JavaScript click...\");");
                s.AppendLine("            ((IJavaScriptExecutor)_driver).ExecuteScript(\"arguments[0].click();\", element);");
                s.AppendLine("        }");
                s.AppendLine("        ");
                s.AppendLine("        // Wait for UI to update after click");
                s.AppendLine("        Thread.Sleep(500);");
                s.AppendLine("    }");
                s.AppendLine();
            }

            // Add index-based click step
            if (sig.Add("ClickNthElementWith"))
            {
                s.AppendLine("    [When(@\"I click the (\\d+)[st|nd|rd|th]* element with (.*?) \"\"(.*?)\"\"\")]");
                s.AppendLine("    public void ClickNthElementWith(int index, string selectorType, string selectorValue)");
                s.AppendLine("    {");
                s.AppendLine("        Console.WriteLine($\"Clicking element #{index} with {selectorType}='{selectorValue}'\");");
                s.AppendLine("        var elements = FindElements(selectorType, selectorValue);");
                s.AppendLine("        ");
                s.AppendLine("        if (elements.Count <= index)");
                s.AppendLine("        {");
                s.AppendLine("            throw new NoSuchElementException($\"Found {elements.Count} elements with {selectorType}='{selectorValue}' but index {index} is out of range\");");
                s.AppendLine("        }");
                s.AppendLine("        ");
                s.AppendLine("        var element = elements.ElementAt(index);");
                s.AppendLine("        ");
                s.AppendLine("        try");
                s.AppendLine("        {");
                s.AppendLine("            // Scroll element into view for better reliability");
                s.AppendLine("            ((IJavaScriptExecutor)_driver).ExecuteScript(\"arguments[0].scrollIntoView({block: 'center'});\", element);");
                s.AppendLine("            Thread.Sleep(300);");
                s.AppendLine("            ");
                s.AppendLine("            element.Click();");
                s.AppendLine("        }");
                s.AppendLine("        catch (ElementClickInterceptedException)");
                s.AppendLine("        {");
                s.AppendLine("            // Fallback to JavaScript click if regular click fails");
                s.AppendLine("            Console.WriteLine(\"Regular click failed, trying JavaScript click...\");");
                s.AppendLine("            ((IJavaScriptExecutor)_driver).ExecuteScript(\"arguments[0].click();\", element);");
                s.AppendLine("        }");
                s.AppendLine("        ");
                s.AppendLine("        // Wait for UI to update after click");
                s.AppendLine("        Thread.Sleep(500);");
                s.AppendLine("    }");
                s.AppendLine();
            }
        }

        private void AddSendKeysSteps(StringBuilder s, HashSet<string> sig)
        {
            if (sig.Add("TypeIntoElement"))
            {
                // Use the exact pattern that matches the feature file
                s.AppendLine("    [When(@\"I type \"\"(.*)\"\" into element with (.*?) \"\"(.*?)\"\"\")]");
                s.AppendLine("    [Given(@\"I type \"\"(.*)\"\" into element with (.*?) \"\"(.*?)\"\"\")]");
                s.AppendLine("    public void TypeIntoElement(string text, string selectorType, string selectorValue)");
                s.AppendLine("    {");
                s.AppendLine("        Console.WriteLine($\"Typing '{text}' into element with {selectorType}='{selectorValue}'\");");
                s.AppendLine("        var element = WaitForElement(selectorType, selectorValue);");
                s.AppendLine("        ");
                s.AppendLine("        try");
                s.AppendLine("        {");
                s.AppendLine("            // Scroll element into view");
                s.AppendLine("            ((IJavaScriptExecutor)_driver).ExecuteScript(\"arguments[0].scrollIntoView({block: 'center'});\", element);");
                s.AppendLine("            Thread.Sleep(300);");
                s.AppendLine("            ");
                s.AppendLine("            element.Clear();");
                s.AppendLine("            element.SendKeys(text);");
                s.AppendLine("        }");
                s.AppendLine("        catch (Exception ex)");
                s.AppendLine("        {");
                s.AppendLine("            Console.WriteLine($\"Standard typing failed: {ex.Message}\");");
                s.AppendLine("            Console.WriteLine(\"Trying JavaScript approach...\");");
                s.AppendLine("            ");
                s.AppendLine("            // Fallback to JavaScript for setting value");
                s.AppendLine("            IJavaScriptExecutor js = (IJavaScriptExecutor)_driver;");
                s.AppendLine("            js.ExecuteScript(\"arguments[0].value = arguments[1];\", element, text);");
                s.AppendLine("            js.ExecuteScript(\"arguments[0].dispatchEvent(new Event('change'));\", element);");
                s.AppendLine("        }");
                s.AppendLine("        ");
                s.AppendLine("        // Wait for UI to update after typing");
                s.AppendLine("        Thread.Sleep(300);");
                s.AppendLine("    }");
                s.AppendLine();
            }

            // Add index-based typing step
            if (sig.Add("TypeIntoNthElement"))
            {
                s.AppendLine("    [When(@\"I type \"\"(.*)\"\" into the (\\d+)[st|nd|rd|th]* element with (.*?) \"\"(.*?)\"\"\")]");
                s.AppendLine("    [Given(@\"I type \"\"(.*)\"\" into the (\\d+)[st|nd|rd|th]* element with (.*?) \"\"(.*?)\"\"\")]");
                s.AppendLine("    public void TypeIntoNthElement(string text, int index, string selectorType, string selectorValue)");
                s.AppendLine("    {");
                s.AppendLine("        Console.WriteLine($\"Typing '{text}' into element #{index} with {selectorType}='{selectorValue}'\");");
                s.AppendLine("        var elements = FindElements(selectorType, selectorValue);");
                s.AppendLine("        ");
                s.AppendLine("        if (elements.Count <= index)");
                s.AppendLine("        {");
                s.AppendLine("            throw new NoSuchElementException($\"Found {elements.Count} elements with {selectorType}='{selectorValue}' but index {index} is out of range\");");
                s.AppendLine("        }");
                s.AppendLine("        ");
                s.AppendLine("        var element = elements.ElementAt(index);");
                s.AppendLine("        ");
                s.AppendLine("        try");
                s.AppendLine("        {");
                s.AppendLine("            // Scroll element into view");
                s.AppendLine("            ((IJavaScriptExecutor)_driver).ExecuteScript(\"arguments[0].scrollIntoView({block: 'center'});\", element);");
                s.AppendLine("            Thread.Sleep(300);");
                s.AppendLine("            ");
                s.AppendLine("            element.Clear();");
                s.AppendLine("            element.SendKeys(text);");
                s.AppendLine("        }");
                s.AppendLine("        catch (Exception ex)");
                s.AppendLine("        {");
                s.AppendLine("            Console.WriteLine($\"Standard typing failed: {ex.Message}\");");
                s.AppendLine("            Console.WriteLine(\"Trying JavaScript approach...\");");
                s.AppendLine("            ");
                s.AppendLine("            // Fallback to JavaScript for setting value");
                s.AppendLine("            IJavaScriptExecutor js = (IJavaScriptExecutor)_driver;");
                s.AppendLine("            js.ExecuteScript(\"arguments[0].value = arguments[1];\", element, text);");
                s.AppendLine("            js.ExecuteScript(\"arguments[0].dispatchEvent(new Event('change'));\", element);");
                s.AppendLine("        }");
                s.AppendLine("        ");
                s.AppendLine("        // Wait for UI to update after typing");
                s.AppendLine("        Thread.Sleep(300);");
                s.AppendLine("    }");
                s.AppendLine();
            }
        }

        private void AddEnterSteps(StringBuilder s, HashSet<string> sig)
        {
            if (sig.Add("TypeAndEnter"))
            {
                // Use the exact pattern that matches the feature file
                s.AppendLine("    [When(@\"I type \"\"(.*)\"\" and press Enter in element with (.*?) \"\"(.*?)\"\"\")]");
                s.AppendLine("    [Given(@\"I type \"\"(.*)\"\" and press Enter in element with (.*?) \"\"(.*?)\"\"\")]");
                s.AppendLine("    public void TypeAndEnter(string text, string selectorType, string selectorValue)");
                s.AppendLine("    {");
                s.AppendLine("        Console.WriteLine($\"Typing '{text}' and pressing Enter in element with {selectorType}='{selectorValue}'\");");
                s.AppendLine("        var element = WaitForElement(selectorType, selectorValue);");
                s.AppendLine("        ");
                s.AppendLine("        try");
                s.AppendLine("        {");
                s.AppendLine("            // Scroll element into view");
                s.AppendLine("            ((IJavaScriptExecutor)_driver).ExecuteScript(\"arguments[0].scrollIntoView({block: 'center'});\", element);");
                s.AppendLine("            Thread.Sleep(300);");
                s.AppendLine("            ");
                s.AppendLine("            element.Clear();");
                s.AppendLine("            element.SendKeys(text);");
                s.AppendLine("            Thread.Sleep(300); // Short pause before pressing Enter");
                s.AppendLine("            element.SendKeys(Keys.Enter);");
                s.AppendLine("        }");
                s.AppendLine("        catch (Exception ex)");
                s.AppendLine("        {");
                s.AppendLine("            Console.WriteLine($\"Standard typing failed: {ex.Message}\");");
                s.AppendLine("            Console.WriteLine(\"Trying JavaScript approach...\");");
                s.AppendLine("            ");
                s.AppendLine("            // Fallback to JavaScript for setting value and pressing Enter");
                s.AppendLine("            IJavaScriptExecutor js = (IJavaScriptExecutor)_driver;");
                s.AppendLine("            js.ExecuteScript(\"arguments[0].value = arguments[1];\", element, text);");
                s.AppendLine("            js.ExecuteScript(\"arguments[0].dispatchEvent(new Event('change'));\", element);");
                s.AppendLine("            js.ExecuteScript(\"arguments[0].dispatchEvent(new KeyboardEvent('keydown', {key: 'Enter', code: 'Enter', keyCode: 13}));\", element);");
                s.AppendLine("        }");
                s.AppendLine("        ");
                s.AppendLine("        // Wait for UI to update after pressing Enter");
                s.AppendLine("        Thread.Sleep(1000);");
                s.AppendLine("    }");
                s.AppendLine();
            }

            // Add index-based TypeAndEnter step
            if (sig.Add("TypeAndEnterNthElement"))
            {
                s.AppendLine("    [When(@\"I type \"\"(.*)\"\" and press Enter in the (\\d+)[st|nd|rd|th]* element with (.*?) \"\"(.*?)\"\"\")]");
                s.AppendLine("    [Given(@\"I type \"\"(.*)\"\" and press Enter in the (\\d+)[st|nd|rd|th]* element with (.*?) \"\"(.*?)\"\"\")]");
                s.AppendLine("    public void TypeAndEnterNthElement(string text, int index, string selectorType, string selectorValue)");
                s.AppendLine("    {");
                s.AppendLine("        Console.WriteLine($\"Typing '{text}' and pressing Enter in element #{index} with {selectorType}='{selectorValue}'\");");
                s.AppendLine("        var elements = FindElements(selectorType, selectorValue);");
                s.AppendLine("        ");
                s.AppendLine("        if (elements.Count <= index)");
                s.AppendLine("        {");
                s.AppendLine("            throw new NoSuchElementException($\"Found {elements.Count} elements with {selectorType}='{selectorValue}' but index {index} is out of range\");");
                s.AppendLine("        }");
                s.AppendLine("        ");
                s.AppendLine("        var element = elements.ElementAt(index);");
                s.AppendLine("        ");
                s.AppendLine("        try");
                s.AppendLine("        {");
                s.AppendLine("            // Scroll element into view");
                s.AppendLine("            ((IJavaScriptExecutor)_driver).ExecuteScript(\"arguments[0].scrollIntoView({block: 'center'});\", element);");
                s.AppendLine("            Thread.Sleep(300);");
                s.AppendLine("            ");
                s.AppendLine("            element.Clear();");
                s.AppendLine("            element.SendKeys(text);");
                s.AppendLine("            Thread.Sleep(300); // Short pause before pressing Enter");
                s.AppendLine("            element.SendKeys(Keys.Enter);");
                s.AppendLine("        }");
                s.AppendLine("        catch (Exception ex)");
                s.AppendLine("        {");
                s.AppendLine("            Console.WriteLine($\"Standard typing failed: {ex.Message}\");");
                s.AppendLine("            Console.WriteLine(\"Trying JavaScript approach...\");");
                s.AppendLine("            ");
                s.AppendLine("            // Fallback to JavaScript for setting value and pressing Enter");
                s.AppendLine("            IJavaScriptExecutor js = (IJavaScriptExecutor)_driver;");
                s.AppendLine("            js.ExecuteScript(\"arguments[0].value = arguments[1];\", element, text);");
                s.AppendLine("            js.ExecuteScript(\"arguments[0].dispatchEvent(new Event('change'));\", element);");
                s.AppendLine("            js.ExecuteScript(\"arguments[0].dispatchEvent(new KeyboardEvent('keydown', {key: 'Enter', code: 'Enter', keyCode: 13}));\", element);");
                s.AppendLine("        }");
                s.AppendLine("        ");
                s.AppendLine("        // Wait for UI to update after pressing Enter");
                s.AppendLine("        Thread.Sleep(1000);");
                s.AppendLine("    }");
                s.AppendLine();
            }

            if (sig.Add("PressEnterInElement"))
            {
                // Use the exact pattern that matches the feature file
                s.AppendLine("    [When(@\"I press Enter in element with (.*?) \"\"(.*?)\"\"\")]");
                s.AppendLine("    [Given(@\"I press Enter in element with (.*?) \"\"(.*?)\"\"\")]");
                s.AppendLine("    public void PressEnterInElement(string selectorType, string selectorValue)");
                s.AppendLine("    {");
                s.AppendLine("        Console.WriteLine($\"Pressing Enter in element with {selectorType}='{selectorValue}'\");");
                s.AppendLine("        var element = WaitForElement(selectorType, selectorValue);");
                s.AppendLine("        ");
                s.AppendLine("        try");
                s.AppendLine("        {");
                s.AppendLine("            element.SendKeys(Keys.Enter);");
                s.AppendLine("        }");
                s.AppendLine("        catch (Exception ex)");
                s.AppendLine("        {");
                s.AppendLine("            Console.WriteLine($\"Standard Enter key failed: {ex.Message}\");");
                s.AppendLine("            Console.WriteLine(\"Trying JavaScript approach...\");");
                s.AppendLine("            ");
                s.AppendLine("            // Fallback to JavaScript for pressing Enter");
                s.AppendLine("            IJavaScriptExecutor js = (IJavaScriptExecutor)_driver;");
                s.AppendLine("            js.ExecuteScript(\"arguments[0].dispatchEvent(new KeyboardEvent('keydown', {key: 'Enter', code: 'Enter', keyCode: 13}));\", element);");
                s.AppendLine("        }");
                s.AppendLine("        ");
                s.AppendLine("        // Wait for UI to update after pressing Enter");
                s.AppendLine("        Thread.Sleep(1000);");
                s.AppendLine("    }");
                s.AppendLine();
            }
        }

        private void AddSelectSteps(StringBuilder s, HashSet<string> sig)
        {
            if (sig.Add("SelectOptionByValue"))
            {
                // Use the exact pattern that matches the feature file
                s.AppendLine("    [When(@\"I select option with value \"\"(.*)\"\" from element with (.*?) \"\"(.*?)\"\"\")]");
                s.AppendLine("    public void SelectOptionByValue(string valueToSelect, string selectorType, string selectorValue)");
                s.AppendLine("    {");
                s.AppendLine("        Console.WriteLine($\"Selecting option '{valueToSelect}' from element with {selectorType}='{selectorValue}'\");");
                s.AppendLine("        var element = WaitForElement(selectorType, selectorValue);");
                s.AppendLine("        ");
                s.AppendLine("        try");
                s.AppendLine("        {");
                s.AppendLine("            var selectElement = new SelectElement(element);");
                s.AppendLine("            selectElement.SelectByValue(valueToSelect);");
                s.AppendLine("        }");
                s.AppendLine("        catch (Exception ex)");
                s.AppendLine("        {");
                s.AppendLine("            Console.WriteLine($\"Standard select failed: {ex.Message}\");");
                s.AppendLine("            Console.WriteLine(\"Trying JavaScript approach...\");");
                s.AppendLine("            ");
                s.AppendLine("            // Fallback to JavaScript for selecting option");
                s.AppendLine("            IJavaScriptExecutor js = (IJavaScriptExecutor)_driver;");
                s.AppendLine("            js.ExecuteScript(\"arguments[0].value = arguments[1]; arguments[0].dispatchEvent(new Event('change'));\", element, valueToSelect);");
                s.AppendLine("        }");
                s.AppendLine("        ");
                s.AppendLine("        // Wait for UI to update after selection");
                s.AppendLine("        Thread.Sleep(500);");
                s.AppendLine("    }");
                s.AppendLine();
            }
        }

        private void AddThenSteps(StringBuilder s, HashSet<string> sig)
        {
            if (sig.Add("ThenExpectedState"))
            {
                s.AppendLine("    [Then(@\"the page should be in the expected state\")]");
                s.AppendLine("    public void ThenExpectedState()");
                s.AppendLine("    {");
                s.AppendLine("        Console.WriteLine(\"Verifying the expected page state\");");
                s.AppendLine("        ");
                s.AppendLine("        // Basic verification that page loaded successfully");
                s.AppendLine("        Assert.That(_driver.Title != null, \"Page title should not be null\");");
                s.AppendLine("        Assert.That(_driver.PageSource.Length > 0, \"Page source should not be empty\");");
                s.AppendLine("        ");
                s.AppendLine("        // Wait to see the final state");
                s.AppendLine("        Thread.Sleep(2000);");
                s.AppendLine("    }");
                s.AppendLine();
            }

            // Add a diagnostic step for troubleshooting
            if (sig.Add("ThenIWaitAndPrintElementInfo"))
            {
                s.AppendLine("    [Then(@\"I wait and print element info for (.*?) \"\"(.*?)\"\"\")]");
                s.AppendLine("    public void ThenIWaitAndPrintElementInfo(string selectorType, string selectorValue)");
                s.AppendLine("    {");
                s.AppendLine("        Console.WriteLine($\"Diagnostic info for {selectorType}='{selectorValue}'\");");
                s.AppendLine("        ");
                s.AppendLine("        try");
                s.AppendLine("        {");
                s.AppendLine("            var elements = FindElements(selectorType, selectorValue);");
                s.AppendLine("            Console.WriteLine($\"Found {elements.Count} matching elements\");");
                s.AppendLine("            ");
                s.AppendLine("            int index = 0;");
                s.AppendLine("            foreach (var element in elements)");
                s.AppendLine("            {");
                s.AppendLine("                Console.WriteLine($\"Element #{index}: Tag={element.TagName}, Displayed={element.Displayed}, Enabled={element.Enabled}\");");
                s.AppendLine("                index++;");
                s.AppendLine("            }");
                s.AppendLine("            ");
                s.AppendLine("            // If no elements found, try to report what's on the page");
                s.AppendLine("            if (elements.Count == 0)");
                s.AppendLine("            {");
                s.AppendLine("                Console.WriteLine(\"Looking for similar elements on the page...\");");
                s.AppendLine("                var allElements = _driver.FindElements(By.XPath(\"//*\"));");
                s.AppendLine("                var relevantElements = new List<IWebElement>();");
                s.AppendLine("                ");
                s.AppendLine("                foreach (var el in allElements)");
                s.AppendLine("                {");
                s.AppendLine("                    try {");
                s.AppendLine("                        string tagName = el.TagName?.ToLower() ?? \"\";");
                s.AppendLine("                        if (tagName == \"input\" || tagName == \"button\" || tagName == \"a\" || tagName == \"select\")");
                s.AppendLine("                        {");
                s.AppendLine("                            relevantElements.Add(el);");
                s.AppendLine("                        }");
                s.AppendLine("                    } catch { /* Ignore stale elements */ }");
                s.AppendLine("                }");
                s.AppendLine("                ");
                s.AppendLine("                Console.WriteLine($\"Found {relevantElements.Count} interactive elements on the page\");");
                s.AppendLine("            }");
                s.AppendLine("        }");
                s.AppendLine("        catch (Exception ex)");
                s.AppendLine("        {");
                s.AppendLine("            Console.WriteLine($\"Error during diagnostics: {ex.Message}\");");
                s.AppendLine("        }");
                s.AppendLine("    }");
                s.AppendLine();
            }

            // Add a step to count and print elements
            if (sig.Add("ThenIShouldSeeElements"))
            {
                s.AppendLine("    [Then(@\"I should see (\\d+) elements? with (.*?) \"\"(.*?)\"\"\")]");
                s.AppendLine("    public void ThenIShouldSeeElements(int expectedCount, string selectorType, string selectorValue)");
                s.AppendLine("    {");
                s.AppendLine("        Console.WriteLine($\"Checking for {expectedCount} elements with {selectorType}='{selectorValue}'\");");
                s.AppendLine("        var elements = FindElements(selectorType, selectorValue);");
                s.AppendLine("        Assert.That(elements.Count, Is.EqualTo(expectedCount), $\"Expected {expectedCount} elements but found {elements.Count}\");");
                s.AppendLine("    }");
                s.AppendLine();
            }
        }

        private void AddGetByHelper(StringBuilder s)
        {
            s.AppendLine("    // Helper method to get By locator from string values");
            s.AppendLine("    private By GetBy(string selectorType, string selectorValue)");
            s.AppendLine("    {");
            s.AppendLine("        selectorValue = selectorValue ?? string.Empty;");
            s.AppendLine("        selectorType = selectorType ?? string.Empty;");
            s.AppendLine("        switch (selectorType.ToLowerInvariant().Trim())");
            s.AppendLine("        {");
            s.AppendLine("            case \"id\": return By.Id(selectorValue);");
            s.AppendLine("            case \"name\": return By.Name(selectorValue);");
            s.AppendLine("            case \"classname\": return By.ClassName(selectorValue);");
            s.AppendLine("            case \"cssselector\": return By.CssSelector(selectorValue);");
            s.AppendLine("            case \"xpath\": return By.XPath(selectorValue);");
            s.AppendLine("            case \"linktext\": return By.LinkText(selectorValue);");
            s.AppendLine("            case \"partiallinktext\": return By.PartialLinkText(selectorValue);");
            s.AppendLine("            case \"tagname\": return By.TagName(selectorValue);");
            s.AppendLine("            // Handle attribute selectors");
            s.AppendLine("            case \"aria-label\": return By.CssSelector($\"[aria-label='{selectorValue}']\");");
            s.AppendLine("            case \"placeholder\": return By.CssSelector($\"[placeholder='{selectorValue}']\");");
            s.AppendLine("            case \"data-test-id\": return By.CssSelector($\"[data-test-id='{selectorValue}']\");");
            s.AppendLine("            case \"data-testid\": return By.CssSelector($\"[data-testid='{selectorValue}']\");");
            s.AppendLine("            case \"data-test\": return By.CssSelector($\"[data-test='{selectorValue}']\");");
            s.AppendLine("            // Handle other attribute-based selectors");
            s.AppendLine("            default:");
            s.AppendLine("                // If selector type looks like an attribute, use it as an attribute selector");
            s.AppendLine("                if (!selectorType.Contains(\" \") && !selectorType.Contains(\">\") && !selectorType.Contains(\"[\"))");
            s.AppendLine("                {");
            s.AppendLine("                    return By.CssSelector($\"[{selectorType}='{selectorValue}']\");");
            s.AppendLine("                }");
            s.AppendLine("                throw new ArgumentException($\"Unsupported selector type provided: '{selectorType}'. Value was '{selectorValue}'.\");");
            s.AppendLine("        }");
            s.AppendLine("    }");
            s.AppendLine();
        }

        private void AddFindElementsHelper(StringBuilder s)
        {
            s.AppendLine("    // Helper method to find all elements matching a selector");
            s.AppendLine("    private IReadOnlyCollection<IWebElement> FindElements(string selectorType, string selectorValue, int timeoutSeconds = 10)");
            s.AppendLine("    {");
            s.AppendLine("        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));");
            s.AppendLine("        var by = GetBy(selectorType, selectorValue);");
            s.AppendLine("        ");
            s.AppendLine("        try");
            s.AppendLine("        {");
            s.AppendLine("            Console.WriteLine($\"Finding elements with {selectorType}='{selectorValue}'\");");
            s.AppendLine("            var elements = wait.Until(driver => {");
            s.AppendLine("                var foundElements = driver.FindElements(by);");
            s.AppendLine("                return foundElements.Count > 0 ? foundElements : null;");
            s.AppendLine("            });");
            s.AppendLine("            ");
            s.AppendLine("            Console.WriteLine($\"Found {elements.Count} elements with {selectorType}='{selectorValue}'\");");
            s.AppendLine("            return elements;");
            s.AppendLine("        }");
            s.AppendLine("        catch (WebDriverTimeoutException)");
            s.AppendLine("        {");
            s.AppendLine("            Console.WriteLine($\"No elements found with {selectorType}='{selectorValue}'\");");
            s.AppendLine("            return new List<IWebElement>();");
            s.AppendLine("        }");
            s.AppendLine("    }");
            s.AppendLine();
        }

        private void AddWaitForElementHelper(StringBuilder s)
        {
            // Add a simple, generic waitForElement method without any website-specific code
            s.AppendLine("    // Helper method to wait for an element to be present and visible");
            s.AppendLine("    private IWebElement WaitForElement(string selectorType, string selectorValue, int timeoutSeconds = 10)");
            s.AppendLine("    {");
            s.AppendLine("        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));");
            s.AppendLine("        var by = GetBy(selectorType, selectorValue);");
            s.AppendLine("        ");
            s.AppendLine("        try");
            s.AppendLine("        {");
            s.AppendLine("            // First attempt with the provided selector");
            s.AppendLine("            Console.WriteLine($\"Looking for element with {selectorType}='{selectorValue}'\");");
            s.AppendLine("            var element = wait.Until(driver => {");
            s.AppendLine("                var foundElement = driver.FindElement(by);");
            s.AppendLine("                ");
            s.AppendLine("                // Special handling for checkboxes/radios that might be hidden by custom UI");
            s.AppendLine("                if (!foundElement.Displayed && foundElement.TagName.ToLower() == \"input\" && ");
            s.AppendLine("                   (foundElement.GetAttribute(\"type\") == \"checkbox\" || foundElement.GetAttribute(\"type\") == \"radio\"))");
            s.AppendLine("                {");
            s.AppendLine("                    return foundElement;");
            s.AppendLine("                }");
            s.AppendLine("                ");
            s.AppendLine("                return foundElement.Displayed ? foundElement : null;");
            s.AppendLine("            });");
            s.AppendLine("            ");
            s.AppendLine("            // Check for multiple matches and warn if found");
            s.AppendLine("            var allMatches = _driver.FindElements(by);");
            s.AppendLine("            if (allMatches.Count > 1)");
            s.AppendLine("            {");
            s.AppendLine("                Console.WriteLine($\"WARNING: Found {allMatches.Count} elements with {selectorType}='{selectorValue}'. Using the first match. For a specific element, use 'I click the Nth element with...' step\");");
            s.AppendLine("            }");
            s.AppendLine("            ");
            s.AppendLine("            return element;");
            s.AppendLine("        }");
            s.AppendLine("        catch (WebDriverTimeoutException)");
            s.AppendLine("        {");
            s.AppendLine("            Console.WriteLine($\"Element not found with primary selector, trying fallbacks\");");
            s.AppendLine("            ");
            s.AppendLine("            // Try with CSS selector alternative if className was used");
            s.AppendLine("            if (selectorType.Equals(\"ClassName\", StringComparison.OrdinalIgnoreCase))");
            s.AppendLine("            {");
            s.AppendLine("                try ");
            s.AppendLine("                {");
            s.AppendLine("                    Console.WriteLine($\"Trying CSS selector alternative: .{selectorValue}\");");
            s.AppendLine("                    var cssSelector = By.CssSelector(\".\" + selectorValue);");
            s.AppendLine("                    var element = _driver.FindElement(cssSelector);");
            s.AppendLine("                    if (element.Displayed)");
            s.AppendLine("                    {");
            s.AppendLine("                        Console.WriteLine(\"Found element with CSS selector\");");
            s.AppendLine("                        return element;");
            s.AppendLine("                    }");
            s.AppendLine("                }");
            s.AppendLine("                catch { /* Continue to next approach */ }");
            s.AppendLine("            }");
            s.AppendLine("            ");
            s.AppendLine("            // Try simple JavaScript approach as a last resort");
            s.AppendLine("            try");
            s.AppendLine("            {");
            s.AppendLine("                IJavaScriptExecutor js = (IJavaScriptExecutor)_driver;");
            s.AppendLine("                Console.WriteLine(\"Trying JavaScript to find element\");");
            s.AppendLine("                ");
            s.AppendLine("                // Simple script to try class name or attribute");
            s.AppendLine("                if (selectorType.Equals(\"ClassName\", StringComparison.OrdinalIgnoreCase))");
            s.AppendLine("                {");
            s.AppendLine("                    var elements = js.ExecuteScript(");
            s.AppendLine("                        \"return document.getElementsByClassName(arguments[0])\", selectorValue) as IReadOnlyCollection<IWebElement>;");
            s.AppendLine("                    ");
            s.AppendLine("                    if (elements != null && elements.Count > 0)");
            s.AppendLine("                    {");
            s.AppendLine("                        foreach (var element in elements)");
            s.AppendLine("                        {");
            s.AppendLine("                            if (element.Displayed)");
            s.AppendLine("                            {");
            s.AppendLine("                                return element;");
            s.AppendLine("                            }");
            s.AppendLine("                        }");
            s.AppendLine("                    }");
            s.AppendLine("                }");
            s.AppendLine("                ");
            s.AppendLine("                // Try by generic attribute - FIXED JavaScript format");
            s.AppendLine("                var elementByAttr = js.ExecuteScript(");
            s.AppendLine("                    \"return document.querySelector('[' + arguments[0] + '=\\\"' + arguments[1] + '\\\"]')\", ");
            s.AppendLine("                    selectorType, selectorValue) as IWebElement;");
            s.AppendLine("                ");
            s.AppendLine("                if (elementByAttr != null && elementByAttr.Displayed)");
            s.AppendLine("                {");
            s.AppendLine("                    Console.WriteLine(\"Found element using JavaScript\");");
            s.AppendLine("                    return elementByAttr;");
            s.AppendLine("                }");
            s.AppendLine("            }");
            s.AppendLine("            catch (Exception jsEx)");
            s.AppendLine("            {");
            s.AppendLine("                Console.WriteLine($\"JavaScript approach failed: {jsEx.Message}\");");
            s.AppendLine("            }");
            s.AppendLine("            ");
            s.AppendLine("            // Element not found after trying fallbacks");
            s.AppendLine("            throw new NoSuchElementException($\"Element with {selectorType}='{selectorValue}' not found after trying fallbacks\");");
            s.AppendLine("        }");
            s.AppendLine("    }");
        }
    }
}