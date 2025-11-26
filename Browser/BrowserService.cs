using System;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using SpecFlowTestGenerator.Utils;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;

namespace SpecFlowTestGenerator.Browser
{
    /// <summary>
    /// Service responsible for managing the browser lifecycle and DevTools connection.
    /// </summary>
    public class BrowserService : IDisposable
    {
        private IWebDriver? _driver;
        private ChromeDriverService? _service;
        private readonly DevToolsSessionManager _devToolsSessionManager;
        private bool _disposed;

        public BrowserService(DevToolsSessionManager devToolsSessionManager)
        {
            _devToolsSessionManager = devToolsSessionManager ?? throw new ArgumentNullException(nameof(devToolsSessionManager));
        }

        /// <summary>
        /// Launches the Chrome browser and initializes the DevTools session.
        /// </summary>
        /// <returns>True if successful, false otherwise.</returns>
        public async Task<bool> LaunchAndConnectAsync()
        {
            try
            {
                Logger.Log("Launching Chrome browser...");
                
                // 1. Setup ChromeDriver using WebDriverManager
                new DriverManager().SetUpDriver(new ChromeConfig());

                // 2. Configure Chrome Options
                var options = new ChromeOptions();
                options.AddArgument("--no-sandbox");
                options.AddArgument("--disable-dev-shm-usage");
                options.AddArgument("--remote-allow-origins=*");
                // Ensure we can interact with the browser window
                options.AddArgument("--start-maximized"); 

                // 3. Create Service with Logging
                _service = ChromeDriverService.CreateDefaultService();
                _service.EnableVerboseLogging = true;
                _service.EnableAppendLog = true;
                _service.SuppressInitialDiagnosticInformation = false;

                // 4. Initialize WebDriver
                _driver = new ChromeDriver(_service, options);
                Logger.Log("Chrome browser launched successfully.");

                // 5. Connect DevTools
                Logger.Log("Connecting to DevTools...");
                bool connected = await _devToolsSessionManager.InitializeSession(_driver);
                
                if (connected)
                {
                    Logger.Log("DevTools connected successfully.");
                    return true;
                }
                else
                {
                    Logger.Log("Failed to connect to DevTools.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to launch browser: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Navigates the browser to the specified URL.
        /// </summary>
        public void NavigateTo(string url)
        {
            if (_driver == null)
            {
                Logger.Log("Cannot navigate: Browser is not initialized.");
                return;
            }

            try
            {
                _driver.Navigate().GoToUrl(url);
                Logger.Log($"Navigated to: {url}");
            }
            catch (Exception ex)
            {
                Logger.Log($"Navigation failed: {ex.Message}");
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _devToolsSessionManager.Dispose();
                
                if (_driver != null)
                {
                    Logger.Log("Closing browser...");
                    try
                    {
                        _driver.Quit();
                        _driver.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"Error closing browser: {ex.Message}");
                    }
                }

                _service?.Dispose();
            }

            _disposed = true;
        }
    }
}
