using System;
using System.Threading.Tasks;
using SpecFlowTestGenerator.Browser;
using SpecFlowTestGenerator.Utils;

namespace SpecFlowTestGenerator.Core
{
    /// <summary>
    /// Interface for version-specific JavaScript injection implementations
    /// </summary>
    public interface IJavaScriptInjectionAdapter
    {
        Task AddScriptToEvaluateOnNewDocument(string script);
        Task EvaluateScript(string script);
    }

    /// <summary>
    /// Handles injection of JavaScript code for event monitoring
    /// </summary>
    public class JavaScriptInjector
    {
        private DevToolsSessionManager? _sessionManager;
        private IJavaScriptInjectionAdapter? _injectionAdapter;

        /// <summary>
        /// Constructor
        /// </summary>
        public JavaScriptInjector()
        {
            // Empty constructor for circular dependency resolution
        }

        /// <summary>
        /// Set the session manager (for breaking circular dependencies)
        /// </summary>
        public void SetSessionManager(DevToolsSessionManager sessionManager)
        {
            _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        }

        /// <summary>
        /// Gets the JavaScript code to inject
        /// </summary>
        private string GetInjectionScript()
        {
            if (_sessionManager == null)
                throw new InvalidOperationException("SessionManager not set");

            string bindingName = _sessionManager.BindingName;
            
            // Read the script from the resource file
            string scriptPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Core", "Resources", "recorder.js");
            
            // If running from source, try to find the source file
            if (!System.IO.File.Exists(scriptPath))
            {
                // Fallback for development environment
                scriptPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Core", "Resources", "recorder.js");
            }

            if (!System.IO.File.Exists(scriptPath))
            {
                throw new System.IO.FileNotFoundException($"Could not find recorder.js at {scriptPath}");
            }

            string scriptContent = System.IO.File.ReadAllText(scriptPath);
            
            // Replace the binding name placeholder
            return scriptContent.Replace("sendActionToCSharp", bindingName);
        }
        /// <summary>
        /// Sets the version-specific adapter
        /// </summary>
        public void SetAdapter(IJavaScriptInjectionAdapter adapter)
        {
            _injectionAdapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        }

        /// <summary>
        /// Injects listener script into the page
        /// </summary>
        public async Task InjectListeners()
        {
            if (_sessionManager == null)
            {
                Logger.Log("ERROR: Cannot inject JavaScript - SessionManager not set");
                return;
            }

            if (_injectionAdapter == null)
            {
                Logger.Log("ERROR: Cannot inject JavaScript - Injection adapter not available");
                return;
            }

            Logger.Log("Injecting JavaScript listeners...");
            string script = GetInjectionScript();

            try
            {
                // Add script to evaluate on new document loads
                await _injectionAdapter.AddScriptToEvaluateOnNewDocument(script);
                
                // Evaluate script on current document
                await _injectionAdapter.EvaluateScript(script);
                
                Logger.Log("SUCCESS: JavaScript injection completed.");
            }
            catch (Exception ex)
            {
                Logger.Log($"Error injecting script: {ex.Message}");
            }
        }

    }

    /// <summary>
    /// V131 specific JavaScript injection adapter
    /// </summary>
    public class V131JavaScriptInjectionAdapter : IJavaScriptInjectionAdapter
    {
        private readonly OpenQA.Selenium.DevTools.V131.DevToolsSessionDomains _domains;

        public V131JavaScriptInjectionAdapter(OpenQA.Selenium.DevTools.V131.DevToolsSessionDomains domains)
        {
            _domains = domains ?? throw new ArgumentNullException(nameof(domains));
        }

        public async Task AddScriptToEvaluateOnNewDocument(string script)
        {
            await _domains.Page.AddScriptToEvaluateOnNewDocument(
                new OpenQA.Selenium.DevTools.V131.Page.AddScriptToEvaluateOnNewDocumentCommandSettings 
                { 
                    Source = script 
                });
        }

        public async Task EvaluateScript(string script)
        {
            await _domains.Runtime.Evaluate(
                new OpenQA.Selenium.DevTools.V131.Runtime.EvaluateCommandSettings 
                { 
                    Expression = script, 
                    Silent = false 
                });
        }
    }
}