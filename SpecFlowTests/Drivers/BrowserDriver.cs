using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace SpecFlowTests.Drivers
{
    public class BrowserDriver : IDisposable
    {
        private readonly Lazy<IWebDriver> _currentWebDriverLazy;
        private bool _isDisposed;

        public BrowserDriver()
        {
            _currentWebDriverLazy = new Lazy<IWebDriver>(CreateWebDriver);
        }

        public IWebDriver Current => _currentWebDriverLazy.Value;

        private IWebDriver CreateWebDriver()
        {
            // We use the ChromeDriver directly as we have the package installed
            // But for robustness in CI/CD, using WebDriverManager is often better.
            // Since we added Selenium.WebDriver.ChromeDriver package, it should be in the path.
            
            var options = new ChromeOptions();
            options.AddArgument("--start-maximized");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--remote-allow-origins=*");

            // Optional: Headless mode
            // options.AddArgument("--headless");

            return new ChromeDriver(options);
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            if (_currentWebDriverLazy.IsValueCreated)
            {
                Current.Quit();
            }

            _isDisposed = true;
        }
    }
}
