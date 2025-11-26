using System;
using System.Collections.Generic;
using System.IO;
using SpecFlowTestGenerator.Models;
using SpecFlowTestGenerator.Utils;

namespace SpecFlowTestGenerator.CodeGeneration
{
    /// <summary>
    /// Generates SpecFlow feature and steps files from recorded actions
    /// </summary>
    public class SpecFlowGenerator
    {
        private readonly FeatureFileBuilder _featureBuilder;
        private readonly StepsFileBuilder _stepsBuilder;

        /// <summary>
        /// Constructor
        /// </summary>
        public SpecFlowGenerator()
        {
            _featureBuilder = new FeatureFileBuilder();
            _stepsBuilder = new StepsFileBuilder();
        }

        /// <summary>
        /// Generate SpecFlow files from recorded actions
        /// </summary>
        /// <summary>
        /// Generate SpecFlow files content from recorded actions
        /// </summary>
        public (string FeatureContent, string StepsContent) GenerateFiles(List<RecordedAction> actions, string featureName)
        {
            if (actions == null || actions.Count == 0)
            {
                Logger.Log($"No actions to generate files for feature '{featureName}'.");
                return (string.Empty, string.Empty);
            }

            string safeFeatureName = FileHelper.SanitizeForFileName(featureName);
            string stepsClassName = $"{safeFeatureName}Steps";

            try
            {
                // Create feature file content
                string featureContent = _featureBuilder.BuildFeatureFileContent(actions, safeFeatureName);
                
                // Create steps file content
                string stepsContent = _stepsBuilder.BuildStepsFileContent(actions, stepsClassName);

                return (featureContent, stepsContent);
            }
            catch (Exception ex)
            {
                Logger.Log($"ERROR generating files: {ex.Message}");
                return (string.Empty, string.Empty);
            }
        }
    }
}
