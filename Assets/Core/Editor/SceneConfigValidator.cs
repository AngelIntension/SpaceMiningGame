#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace VoidHarvest.Core.Editor
{
    /// <summary>
    /// Editor window that validates all serialized config fields on SceneLifetimeScope
    /// and RootLifetimeScope, reporting missing/null assignments.
    /// Uses reflection to avoid direct Assembly-CSharp references.
    /// See Spec 009: Data-Driven World Config (US6).
    /// </summary>
    public class SceneConfigValidator : EditorWindow
    {
        private Vector2 _scrollPos;
        private List<FieldResult> _results = new List<FieldResult>();
        private bool _hasRun;

        private struct FieldResult
        {
            public string ScopeName;
            public string FieldName;
            public bool IsAssigned;
        }

        [MenuItem("VoidHarvest/Validate Scene Config")]
        public static void ShowWindow()
        {
            var window = GetWindow<SceneConfigValidator>("Scene Config Validator");
            window.RunValidation();
        }

        private void OnGUI()
        {
            GUILayout.Label("Scene Config Validator", EditorStyles.boldLabel);
            GUILayout.Space(5);

            if (GUILayout.Button("Validate"))
                RunValidation();

            if (!_hasRun)
            {
                GUILayout.Label("Click 'Validate' to check scene configuration.");
                return;
            }

            GUILayout.Space(10);
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            int errors = 0;
            int valid = 0;

            foreach (var result in _results)
            {
                var color = result.IsAssigned ? Color.green : Color.red;
                var icon = result.IsAssigned ? "\u2713" : "\u2717";
                var oldColor = GUI.color;
                GUI.color = color;
                GUILayout.Label($"  {icon} [{result.ScopeName}] {result.FieldName}");
                GUI.color = oldColor;

                if (result.IsAssigned) valid++;
                else errors++;
            }

            EditorGUILayout.EndScrollView();

            GUILayout.Space(5);
            var summaryColor = errors == 0 ? Color.green : Color.yellow;
            var oldSummaryColor = GUI.color;
            GUI.color = summaryColor;
            GUILayout.Label($"  {valid} valid, {errors} missing");
            GUI.color = oldSummaryColor;
        }

        private void RunValidation()
        {
            _results.Clear();

            // Find scopes by type name via reflection (avoids Assembly-CSharp dependency)
            FindAndValidateScope("RootLifetimeScope");
            FindAndValidateScope("SceneLifetimeScope");

            _hasRun = true;
            Repaint();
        }

        private void FindAndValidateScope(string typeName)
        {
            Type scopeType = null;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                scopeType = assembly.GetType(typeName);
                if (scopeType != null) break;
            }

            if (scopeType == null)
            {
                _results.Add(new FieldResult { ScopeName = typeName, FieldName = $"(type '{typeName}' not found)", IsAssigned = false });
                return;
            }

            var scope = FindFirstObjectByType(scopeType) as MonoBehaviour;
            if (scope == null)
            {
                _results.Add(new FieldResult { ScopeName = typeName, FieldName = "(not found in scene)", IsAssigned = false });
                return;
            }

            ValidateScope(scope, typeName);
        }

        private static UnityEngine.Object FindFirstObjectByType(Type type)
        {
            var objects = UnityEngine.Object.FindObjectsByType(type, FindObjectsSortMode.None);
            return objects.Length > 0 ? objects[0] : null;
        }

        private void ValidateScope(MonoBehaviour scope, string scopeName)
        {
            var type = scope.GetType();
            var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var field in fields)
            {
                if (!field.IsDefined(typeof(SerializeField), true))
                    continue;

                if (!typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType))
                    continue;

                var value = field.GetValue(scope) as UnityEngine.Object;
                _results.Add(new FieldResult
                {
                    ScopeName = scopeName,
                    FieldName = field.Name,
                    IsAssigned = value != null
                });
            }
        }
    }
}
#endif
