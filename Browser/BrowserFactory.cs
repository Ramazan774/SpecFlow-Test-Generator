using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using SpecFlowTestGenerator.Utils;

namespace SpecFlowTestGenerator.Browser
{
    public class BrowserFactory
    {
        /// <summary>
        /// Creates a Chrome WebDriver with its service
        /// </summary>
        /// <returns>A tuple containing the driver and service</returns>
        public static (IWebDriver driver, ChromeDriverService service) CreateChromeDriver()
        {
            Logger.Log("Creating Chrome browser with service...");
            
            try
            {
                // Use WebDriverManager to setup the matching ChromeDriver
                new WebDriverManager.DriverManager().SetUpDriver(new WebDriverManager.DriverConfigs.Impl.ChromeConfig());

                // Create Chrome driver service
                ChromeDriverService service = ChromeDriverService.CreateDefaultService();
                service.EnableVerboseLogging = true;
                service.EnableAppendLog = true;
                service.SuppressInitialDiagnosticInformation = false;
                
                // Create Chrome options
                ChromeOptions options = new ChromeOptions();
                options.AddArgument("--no-sandbox");
                options.AddArgument("--disable-dev-shm-usage");
                options.AddArgument("--remote-allow-origins=*");
                
                // Create and return the driver with its service
                IWebDriver driver = new ChromeDriver(service, options);
                Logger.Log("SUCCESS: Chrome browser with service initialized!");
                return (driver, service);
            }
            catch (Exception ex)
            {
                Logger.Log($"FAIL: Failed to initialize Chrome browser with service: {ex.Message}");
                throw new WebDriverException("Failed to create Chrome driver with service", ex);
            }
        }

        /// <summary>
        /// Safely quits a WebDriver, handling null and exceptions
        /// </summary>
        /// <param name="driver">The WebDriver to quit</param>
        public static void SafeQuit(IWebDriver? driver)
        {
            if (driver == null)
                return;

            Logger.Log("Attempting to quit driver...");
            try
            {
                driver.Quit();
                Logger.Log("Driver quit successfully.");
            }
            catch (Exception ex)
            {
                Logger.Log($"Error quitting driver: {ex.Message}");
            }
        }
    }
}