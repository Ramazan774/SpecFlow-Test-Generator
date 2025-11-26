using SpecFlowTests.Drivers;
using TechTalk.SpecFlow;
using BoDi;
using OpenQA.Selenium;

namespace SpecFlowTests.Hooks
{
    [Binding]
    public class Hooks
    {
        private readonly BrowserDriver _browserDriver;
        private readonly IObjectContainer _objectContainer;

        public Hooks(BrowserDriver browserDriver, IObjectContainer objectContainer)
        {
            _browserDriver = browserDriver;
            _objectContainer = objectContainer;
        }

        [BeforeScenario]
        public void RegisterWebDriver()
        {
            _objectContainer.RegisterInstanceAs<IWebDriver>(_browserDriver.Current);
        }

        [AfterScenario]
        public void AfterScenario()
        {
            // Small delay to allow user to see the final state
            System.Threading.Thread.Sleep(3000);
            _browserDriver.Dispose();
        }
    }
}
