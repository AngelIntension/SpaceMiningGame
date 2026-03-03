#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VoidHarvest.Features.World.Data;
using VoidHarvest.Features.Station.Data;

namespace VoidHarvest.Core.Editor
{
    /// <summary>
    /// Custom inspector for WorldDefinition showing inline station completeness badges.
    /// See Spec 009: Data-Driven World Config (US6).
    /// </summary>
    [CustomEditor(typeof(WorldDefinition))]
    public class WorldDefinitionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(10);
            GUILayout.Label("Station Validation", EditorStyles.boldLabel);

            var worldDef = (WorldDefinition)target;
            if (worldDef.Stations == null || worldDef.Stations.Length == 0)
            {
                EditorGUILayout.HelpBox("No stations configured.", MessageType.Warning);
                return;
            }

            if (GUILayout.Button("Validate All"))
            {
                ValidateStations(worldDef);
            }

            // Inline station list with badges
            var duplicateIds = FindDuplicateIds(worldDef);
            for (int i = 0; i < worldDef.Stations.Length; i++)
            {
                var station = worldDef.Stations[i];
                if (station == null)
                {
                    EditorGUILayout.HelpBox($"Station [{i}]: NULL reference", MessageType.Error);
                    continue;
                }

                var issues = GetStationIssues(station, duplicateIds);
                var color = issues.Count == 0 ? Color.green : Color.yellow;
                var icon = issues.Count == 0 ? "\u2713" : "\u26A0";

                var oldColor = GUI.color;
                GUI.color = color;
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"  {icon} [{i}] {station.DisplayName ?? "(unnamed)"} (ID: {station.StationId})");
                EditorGUILayout.EndHorizontal();
                GUI.color = oldColor;

                foreach (var issue in issues)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.HelpBox(issue, MessageType.Warning);
                    EditorGUI.indentLevel--;
                }
            }
        }

        private void ValidateStations(WorldDefinition worldDef)
        {
            int issues = 0;
            var duplicateIds = FindDuplicateIds(worldDef);

            for (int i = 0; i < worldDef.Stations.Length; i++)
            {
                var station = worldDef.Stations[i];
                if (station == null) { issues++; continue; }
                issues += GetStationIssues(station, duplicateIds).Count;
            }

            if (issues == 0)
                Debug.Log($"[WorldDefinition] All {worldDef.Stations.Length} stations valid.");
            else
                Debug.LogWarning($"[WorldDefinition] {issues} issue(s) found across {worldDef.Stations.Length} stations.");
        }

        private static HashSet<int> FindDuplicateIds(WorldDefinition worldDef)
        {
            var seen = new HashSet<int>();
            var duplicates = new HashSet<int>();
            foreach (var s in worldDef.Stations)
            {
                if (s == null) continue;
                if (!seen.Add(s.StationId))
                    duplicates.Add(s.StationId);
            }
            return duplicates;
        }

        private static List<string> GetStationIssues(StationDefinition station, HashSet<int> duplicateIds)
        {
            var issues = new List<string>();
            if (station.StationId <= 0)
                issues.Add("StationId must be > 0");
            if (duplicateIds.Contains(station.StationId))
                issues.Add($"Duplicate StationId: {station.StationId}");
            if (string.IsNullOrEmpty(station.DisplayName))
                issues.Add("DisplayName is empty");
            if (station.ServicesConfig == null)
                issues.Add("ServicesConfig is null");
            if (station.AvailableServices == null || station.AvailableServices.Length == 0)
                issues.Add("AvailableServices is empty");
            return issues;
        }
    }
}
#endif
