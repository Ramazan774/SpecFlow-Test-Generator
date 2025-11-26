using System;
using System.Threading;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.DevTools;
using SpecFlowTestGenerator.Core;
using SpecFlowTestGenerator.Utils;

namespace SpecFlowTestGenerator.Browser
{
    /// <summary>
    /// Manages DevTools sessions for interacting with browser CDP with multi-version support
    /// </summary>
    public class DevToolsSessionManager : IDisposable
    {
        private EventHandlers? _eventHandlers;
        private JavaScriptInjector? _jsInjector;
        
        private IDevToolsSession? _session;
        private object? _domains;
        private IDevToolsEventAdapter? _eventAdapter;
        private bool _disposed;
        private bool _isInitialized;
        
        private const string JsBindingName = "sendActionToCSharp";

        /// <summary>
        /// Gets the JS binding name used for communication between browser and C#
        /// </summary>
        public string BindingName => JsBindingName;

        /// <summary>
        /// Gets whether the DevTools session is currently active
        /// </summary>
        public bool IsSessionActive => _session != null && _domains != null;

        /// <summary>
        /// Creates a new instance of DevToolsSessionManager (dependencies set via SetDependencies)
        /// </summary>
        public DevToolsSessionManager()
        {
            // Empty constructor for circular dependency resolution
            // Dependencies will be set via SetDependencies() method
        }

        /// <summary>
        /// Sets the dependencies after construction (for breaking circular dependencies)
        /// </summary>
        /// <param name="eventHandlers">The event handlers component</param>
        /// <param name="jsInjector">The JavaScript injector component</param>
        public void SetDependencies(EventHandlers eventHandlers, JavaScriptInjector jsInjector)
        {
            _eventHandlers = eventHandlers ?? throw new ArgumentNullException(nameof(eventHandlers));
            _jsInjector = jsInjector ?? throw new ArgumentNullException(nameof(jsInjector));
            _isInitialized = true;
            
            Logger.Log("DevToolsSessionManager dependencies set successfully");
        }

        /// <summary>
        /// Initializes the DevTools session from the WebDriver
        /// </summary>
        /// <param name="driver">The WebDriver instance to connect to</param>
        /// <returns>True if initialization succeeded, false otherwise</returns>
        public async Task<bool> InitializeSession(IWebDriver driver)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(DevToolsSessionManager));

            if (!_isInitialized)
            {
                Logger.Log("ERROR: DevToolsSessionManager not properly initialized. Call SetDependencies first.");
                return false;
            }

            if (driver == null)
            {
                Logger.Log("ERROR: Cannot initialize DevTools session - driver is null");
                return false;
            }

            try
            {
                Logger.Log("Initializing DevTools session...");
                // Get IDevTools interface from driver
                if (!TryGetDevToolsInterface(driver, out var devTools))
                {
                    return false;
                }

                // Get DevTools session
                if (!TryGetDevToolsSession(devTools!, out _session))
                {
                    return false;
                }

                Logger.Log("SUCCESS: DevTools session acquired");
                // Try to initialize with supported CDP versions
                return await InitializeBasedOnVersion();
            }
            catch (Exception ex)
            {
                Logger.Log($"FAIL: DevTools session initialization error: {ex.Message}\n{ex}");
                return false;
            }
        }

        /// <summary>
        /// Attempts to get the IDevTools interface from the driver
        /// </summary>
        private bool TryGetDevToolsInterface(IWebDriver driver, out IDevTools? devTools)
        {
            Logger.Log("Attempting to get DevTools interface...");
            
            devTools = driver as IDevTools;
            
            if (devTools == null)
            {
                Logger.Log("FAIL: Driver does not support IDevTools interface");
                Logger.Log("Ensure you're using ChromeDriver or another browser that supports DevTools Protocol");
                return false;
            }

            Logger.Log("SUCCESS: Got IDevTools interface");
            return true;
        }

        /// <summary>
        /// Attempts to get the DevTools session
        /// </summary>
        private bool TryGetDevToolsSession(IDevTools devTools, out IDevToolsSession? session)
        {
            Logger.Log("Attempting to get DevTools session...");
            
            try
            {
                session = devTools.GetDevToolsSession();
                
                if (session == null)
                {
                    Logger.Log("FAIL: GetDevToolsSession() returned null");
                    return false;
                }

                Logger.Log("SUCCESS: Got DevTools session");
                return true;
            }
            catch (WebDriverException ex)
            {
                bool isVersionMismatch = ex.Message.Contains("DevTools version is not in the supported range") || 
                                       (ex.InnerException != null && ex.InnerException.Message.Contains("DevTools version is not in the supported range"));

                if (isVersionMismatch)
                {
                    Logger.Log($"WARNING: Version mismatch detected: {ex.InnerException?.Message ?? ex.Message}");
                    Logger.Log("Attempting to force V131 protocol...");

                    try
                    {
                        // Create options to force V131
                        var options = new DevToolsOptions
                        {
                            ProtocolVersion = 131
                        };
                        
                        session = devTools.GetDevToolsSession(options);
                        
                        if (session == null)
                        {
                            Logger.Log("FAIL: GetDevToolsSession(options) returned null");
                            return false;
                        }
                        
                        Logger.Log("SUCCESS: Got DevTools session (Forced V131)");
                        return true;
                    }
                    catch (Exception forceEx)
                    {
                         Logger.Log($"FAIL: Could not force V131 session: {forceEx.Message}");
                         session = null;
                         return false;
                    }
                }
                
                Logger.Log($"FAIL: Error getting DevTools session: {ex.Message}\n{ex}");
                session = null;
                return false;
            }
        }

        /// <summary>
        /// Tries to initialize using available CDP versions
        /// </summary>
        private async Task<bool> InitializeBasedOnVersion()
        {
            Logger.Log("Detecting Chrome DevTools Protocol version...");
            
            // Try supported versions in order (newest first)
            if (await TryInitializeV131())
            {
                Logger.Log("Successfully initialized with CDP V131");
                return true;
            }
            
            // Add more versions here as needed:
            // if (await TryInitializeV135()) return true;
            // if (await TryInitializeV134()) return true;
            
            Logger.Log("FAIL: No supported DevTools Protocol version found");
            Logger.Log("Supported versions: V131");
            return false;
        }

        /// <summary>
        /// Tries to initialize with Chrome DevTools Protocol version 131
        /// </summary>
        private async Task<bool> TryInitializeV131()
        {
            try
            {
                Logger.Log("Attempting V131 initialization...");
                
                // Get version-specific domains
                var domains = _session!.GetVersionSpecificDomains<OpenQA.Selenium.DevTools.V131.DevToolsSessionDomains>();
                
                if (domains == null)
                {
                    Logger.Log("V131: Failed to get version-specific domains");
                    return false;
                }

                Logger.Log("V131: Got version-specific domains");
                
                // Enable required domains
                await EnableV131Domains(domains);
                
                // Add JavaScript binding for communication
                await AddV131Binding(domains);
                
                // Set up event adapters
                SetupV131Adapters(domains);
                
                // Store domains for cleanup
                _domains = domains;
                
                Logger.Log("SUCCESS: V131 initialization complete");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"V131: Initialization failed - {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Enables required V131 domains (Page and Runtime)
        /// </summary>
        private async Task EnableV131Domains(OpenQA.Selenium.DevTools.V131.DevToolsSessionDomains domains)
        {
            Logger.Log("V131: Enabling Page domain...");
            await domains.Page.Enable(new OpenQA.Selenium.DevTools.V131.Page.EnableCommandSettings());
            
            Logger.Log("V131: Enabling Runtime domain...");
            await domains.Runtime.Enable(new OpenQA.Selenium.DevTools.V131.Runtime.EnableCommandSettings());
            
            Logger.Log("V131: Domains enabled successfully");
        }

        /// <summary>
        /// Adds the JavaScript binding for C# communication
        /// </summary>
        private async Task AddV131Binding(OpenQA.Selenium.DevTools.V131.DevToolsSessionDomains domains)
        {
            Logger.Log($"V131: Adding JavaScript binding '{JsBindingName}'...");
            
            await domains.Runtime.AddBinding(
                new OpenQA.Selenium.DevTools.V131.Runtime.AddBindingCommandSettings 
                { 
                    Name = JsBindingName 
                });
            
            Logger.Log("V131: JavaScript binding added successfully");
        }

        /// <summary>
        /// Sets up event and injection adapters for V131
        /// </summary>
        private void SetupV131Adapters(OpenQA.Selenium.DevTools.V131.DevToolsSessionDomains domains)
        {
            Logger.Log("V131: Setting up event adapters...");
            
            // Create and register event adapter
            _eventAdapter = new V131EventAdapter(domains);
            _eventHandlers!.SetAdapter(_eventAdapter);
            
            // Set up injection adapter
            var injectionAdapter = new V131JavaScriptInjectionAdapter(domains);
            _jsInjector!.SetAdapter(injectionAdapter);
            
            Logger.Log("V131: Event adapters configured");
        }

        /// <summary>
        /// Cleans up the DevTools session and releases resources
        /// </summary>
        public async Task CleanUpSession()
        {
            if (_disposed)
                return;

            if (_session == null || _domains == null)
            {
                Logger.Log("DevTools session cleanup: No active session to clean up");
                return;
            }

            Logger.Log("Cleaning up DevTools session...");

            try
            {
                // Unregister event handlers first
                if (_eventAdapter != null)
                {
                    _eventAdapter.UnregisterEventHandlers();
                    _eventAdapter = null;
                }

                // Remove JavaScript binding
                await RemoveBindingAsync();
                
                Logger.Log("SUCCESS: DevTools session cleaned up");
            }
            catch (Exception ex)
            {
                Logger.Log($"Warning: Error during DevTools cleanup: {ex.Message}");
            }
            finally
            {
                _session = null;
                _domains = null;
            }
        }

        /// <summary>
        /// Removes the JavaScript binding with timeout protection
        /// </summary>
        private async Task RemoveBindingAsync()
        {
            Logger.Log($"Removing JavaScript binding '{JsBindingName}'...");
            
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                
                // Remove binding based on active version
                if (_domains is OpenQA.Selenium.DevTools.V131.DevToolsSessionDomains v131)
                {
                    await v131.Runtime.RemoveBinding(
                        new OpenQA.Selenium.DevTools.V131.Runtime.RemoveBindingCommandSettings 
                        { 
                            Name = JsBindingName 
                        }, 
                        cts.Token);
                    
                    Logger.Log("JavaScript binding removed successfully");
                }
            }
            catch (TaskCanceledException)
            {
                Logger.Log("Warning: Timeout while removing JavaScript binding");
            }
            catch (TimeoutException)
            {
                Logger.Log("Warning: Timeout while removing JavaScript binding");
            }
            catch (Exception ex)
            {
                Logger.Log($"Warning: Error removing JavaScript binding: {ex.Message}");
            }
        }

        /// <summary>
        /// Disposes of the DevToolsSessionManager
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes resources
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // Clean up managed resources
                CleanUpSession().GetAwaiter().GetResult();
            }

            _disposed = true;
        }
    }
}