using System.Collections.Generic;

namespace VoidHarvest.Core.Editor
{
    /// <summary>
    /// Pure validation logic for scene configuration completeness.
    /// Testable in EditMode without scene dependencies.
    /// See Spec 009: Data-Driven World Config (US6).
    /// </summary>
    public static class SceneConfigValidatorLogic
    {
        /// <summary>
        /// Validates a dictionary of field names to presence flags.
        /// Returns a list of error messages for fields that are null/missing.
        /// </summary>
        public static List<string> ValidateFields(Dictionary<string, bool> fieldPresence)
        {
            var errors = new List<string>();
            foreach (var kvp in fieldPresence)
            {
                if (!kvp.Value)
                    errors.Add($"Missing reference: {kvp.Key} is null");
            }
            return errors;
        }
    }
}
