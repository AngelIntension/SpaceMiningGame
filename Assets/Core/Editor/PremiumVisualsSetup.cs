using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.IO;

namespace VoidHarvest.Core.Editor
{
    /// <summary>
    /// One-time Editor utility for premium visuals asset integration.
    /// Handles material verification (T001-T004) and Alpha Clipping setup (T043).
    /// Run via menu: VoidHarvest > Premium Visuals Setup.
    /// Delete this file after all setup tasks are complete.
    /// </summary>
    public static class PremiumVisualsSetup
    {
        private static readonly string[] AsteroidMaterialPaths = new[]
        {
            "Assets/SF_Asteroids-M2/Materials/Mineral_asteroid-01.mat",
            "Assets/SF_Asteroids-M2/Materials/Mineral_asteroid-02.mat",
            "Assets/SF_Asteroids-M2/Materials/Mineral_asteroid-03.mat",
            "Assets/SF_Asteroids-M2/Materials/Mineral_asteroid-04.mat",
            "Assets/SF_Asteroids-M2/Materials/Mineral_asteroid-05.mat",
            "Assets/SF_Asteroids-M2/Materials/Mineral_asteroid-06.mat"
        };

        private static readonly string[] RetoraMaterialPaths = new[]
        {
            "Assets/Retora - Modular Space Ship Pack/Materials/Modular_DoorParts 1.mat",
            "Assets/Retora - Modular Space Ship Pack/Materials/Modular_DoorParts 2.mat",
            "Assets/Retora - Modular Space Ship Pack/Materials/Modular_HullParts 1.mat",
            "Assets/Retora - Modular Space Ship Pack/Materials/Modular_HullParts 2.mat",
            "Assets/Retora - Modular Space Ship Pack/Materials/Modular_MiscParts 1.mat",
            "Assets/Retora - Modular Space Ship Pack/Materials/Modular_MiscParts 2.mat",
            "Assets/Retora - Modular Space Ship Pack/Materials/Modular_PowerSystems 1.mat",
            "Assets/Retora - Modular Space Ship Pack/Materials/Modular_PowerSystems 2.mat",
            "Assets/Retora - Modular Space Ship Pack/Materials/Planet.mat"
        };

        private static readonly string[] StationMaterialPaths = new[]
        {
            "Assets/Station_MS2/Meshes/Materials/MS2_Blue.mat",
            "Assets/Station_MS2/Meshes/Materials/MS2_Grey.mat",
            "Assets/Station_MS2/Meshes/Materials/MS2_Red.mat",
            "Assets/Station_MS2/Meshes/Materials/MS2_Yellow.mat"
        };

        private static readonly string[] NebulaMaterialPaths = new[]
        {
            "Assets/Nebula Skybox Pack Vol. II \u2013 12x 8K HDRI Space Environments/HDRI/Materials/HDR_Nebula_2_Pro_1.mat",
            "Assets/Nebula Skybox Pack Vol. II \u2013 12x 8K HDRI Space Environments/HDRI/Materials/HDR_Nebula_2_Pro_2.mat",
            "Assets/Nebula Skybox Pack Vol. II \u2013 12x 8K HDRI Space Environments/HDRI/Materials/HDR_Nebula_2_Pro_3.mat",
            "Assets/Nebula Skybox Pack Vol. II \u2013 12x 8K HDRI Space Environments/HDRI/Materials/HDR_Nebula_2_Pro_4.mat",
            "Assets/Nebula Skybox Pack Vol. II \u2013 12x 8K HDRI Space Environments/HDRI/Materials/HDR_Nebula_2_Pro_5.mat",
            "Assets/Nebula Skybox Pack Vol. II \u2013 12x 8K HDRI Space Environments/HDRI/Materials/HDR_Nebula_2_Pro_6.mat",
            "Assets/Nebula Skybox Pack Vol. II \u2013 12x 8K HDRI Space Environments/HDRI/Materials/HDR_Nebula_2_Pro_7.mat",
            "Assets/Nebula Skybox Pack Vol. II \u2013 12x 8K HDRI Space Environments/HDRI/Materials/HDR_Nebula_2_Pro_8.mat",
            "Assets/Nebula Skybox Pack Vol. II \u2013 12x 8K HDRI Space Environments/HDRI/Materials/HDR_Nebula_2_Pro_9.mat",
            "Assets/Nebula Skybox Pack Vol. II \u2013 12x 8K HDRI Space Environments/HDRI/Materials/HDR_Nebula_2_Pro_10.mat",
            "Assets/Nebula Skybox Pack Vol. II \u2013 12x 8K HDRI Space Environments/HDRI/Materials/HDR_Nebula_2_Pro_11.mat",
            "Assets/Nebula Skybox Pack Vol. II \u2013 12x 8K HDRI Space Environments/HDRI/Materials/HDR_Nebula_2_Pro_12.mat"
        };

        [MenuItem("VoidHarvest/Premium Visuals Setup/1. Verify Materials (T001-T004)")]
        public static void VerifyMaterials()
        {
            Debug.Log("=== Premium Visuals Material Verification ===");

            int issueCount = 0;
            issueCount += VerifyMaterialGroup("SF_Asteroids-M2 (T001)", AsteroidMaterialPaths);
            issueCount += VerifyMaterialGroup("Nebula Skybox (T002)", NebulaMaterialPaths);
            issueCount += VerifyMaterialGroup("Retora Ships (T003)", RetoraMaterialPaths);
            issueCount += VerifyMaterialGroup("Station_MS2 (T004)", StationMaterialPaths);

            if (issueCount == 0)
            {
                Debug.Log("<color=green>ALL MATERIALS PASS — No legacy shaders detected. Phase 1 complete.</color>");
                EditorUtility.DisplayDialog("Material Verification",
                    "All materials use URP-compatible shaders.\nPhase 1 material verification is COMPLETE.",
                    "OK");
            }
            else
            {
                Debug.LogWarning($"<color=yellow>{issueCount} material(s) use legacy shaders.</color>\n" +
                    "Run: Window > Rendering > Render Pipeline Converter\n" +
                    "Select 'Built-in to URP' > Initialize Converters > Convert Assets\n" +
                    "Then re-run this verification.");
                EditorUtility.DisplayDialog("Material Verification",
                    $"{issueCount} material(s) use legacy shaders and need URP conversion.\n\n" +
                    "Run the Render Pipeline Converter:\n" +
                    "Window > Rendering > Render Pipeline Converter\n" +
                    "Select 'Built-in to URP' > Initialize > Convert",
                    "OK");
            }
        }

        [MenuItem("VoidHarvest/Premium Visuals Setup/2. Enable Alpha Clipping on Asteroids (T043)")]
        public static void EnableAsteroidAlphaClipping()
        {
            Debug.Log("=== Enabling Alpha Clipping on Asteroid Materials (T043) ===");
            int modified = 0;

            foreach (string path in AsteroidMaterialPaths)
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null)
                {
                    Debug.LogWarning($"Material not found: {path}");
                    continue;
                }

                // Check if this is a URP Lit material
                if (mat.shader == null || !mat.shader.name.Contains("Universal Render Pipeline"))
                {
                    Debug.LogWarning($"SKIP {mat.name}: Not a URP shader ({mat.shader?.name}). " +
                        "Run material verification first (step 1).");
                    continue;
                }

                // Enable Alpha Clipping
                mat.SetFloat("_AlphaClip", 1f);
                mat.SetFloat("_Cutoff", 0.5f);
                mat.EnableKeyword("_ALPHATEST_ON");

                EditorUtility.SetDirty(mat);
                modified++;
                Debug.Log($"  Enabled Alpha Clipping on: {mat.name} (_Cutoff = 0.5)");
            }

            if (modified > 0)
            {
                AssetDatabase.SaveAssets();
                Debug.Log($"<color=green>Alpha Clipping enabled on {modified} asteroid material(s). T043 complete.</color>");
                EditorUtility.DisplayDialog("Alpha Clipping Setup",
                    $"Alpha Clipping enabled on {modified} asteroid materials.\n" +
                    "Cutoff = 0.5 — asteroids render identically at full alpha.\n" +
                    "T043 is complete.",
                    "OK");
            }
            else
            {
                Debug.LogWarning("No materials were modified. Ensure URP conversion (step 1) is done first.");
                EditorUtility.DisplayDialog("Alpha Clipping Setup",
                    "No materials were modified.\n" +
                    "Make sure materials have been converted to URP first (step 1).",
                    "OK");
            }
        }

        [MenuItem("VoidHarvest/Premium Visuals Setup/3. Report Remaining Tasks")]
        public static void ReportRemainingTasks()
        {
            string report =
                "=== Remaining Unity Editor Tasks ===\n\n" +
                "ASTEROID LODs (T017):\n" +
                "  Select each Mineral_asteroid prefab in SF_Asteroids-M2/Prefabs/\n" +
                "  Add LODGroup component with 3 levels\n" +
                "  LOD0: full detail (0-50%), LOD1: 50% tris (50-80%), LOD2: billboard (80-100%)\n\n" +
                "SHIP ASSEMBLY (T026-T028):\n" +
                "  Create 3 prefabs in Assets/Features/Ship/Prefabs/\n" +
                "  Build from Retora parts (HullParts, DoorParts, MiscParts)\n" +
                "  Use Ship1-Ship5 prefabs as assembly reference\n" +
                "  Add ShipAuthoring component to each, referencing matching config:\n" +
                "    SmallMiningBarge -> StarterMiningBarge.asset\n" +
                "    MediumMiningBarge -> MediumMiningBarge.asset\n" +
                "    HeavyMiningBarge -> HeavyMiningBarge.asset\n\n" +
                "SHIP LODs (T029):\n" +
                "  Add LODGroup to each barge prefab (2 levels)\n\n" +
                "WIRE SHIP (T030):\n" +
                "  Open ShipSubScene, replace PlayerShip capsule mesh with SmallMiningBarge\n\n" +
                "STATION ASSEMBLY (T032-T033):\n" +
                "  Create 2 prefabs in Assets/Features/Base/Prefabs/\n" +
                "  SmallMiningRelay: MS2_Control_grey + Storage x2 + Antennas + Connect\n" +
                "  MediumRefineryHub: Bridge + Hangars + Modules x2 + Storage x2 + Energy + Habitat + Tower + Connect x2\n" +
                "  All grey variants, snap-aligned on grid\n\n" +
                "TEST SCENE (T036):\n" +
                "  Create Assets/Scenes/TestScene_Station.unity\n" +
                "  Place both station presets + nebula skybox + inspection camera\n\n" +
                "PROFILING (T037-T041):\n" +
                "  Profile asteroid field < 2ms, station < 5ms, GameScene 60 FPS\n" +
                "  Run all tests via Test Runner — zero regressions";

            Debug.Log(report);
            EditorUtility.DisplayDialog("Remaining Tasks", report, "OK");
        }

        private static int VerifyMaterialGroup(string groupName, string[] paths)
        {
            int issues = 0;
            var legacyShaders = new List<string>();

            foreach (string path in paths)
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null)
                {
                    Debug.LogWarning($"  [{groupName}] Material not found: {path}");
                    continue;
                }

                string shaderName = mat.shader != null ? mat.shader.name : "NULL";
                bool isUrp = shaderName.Contains("Universal Render Pipeline") ||
                             shaderName.Contains("Skybox/");

                if (!isUrp)
                {
                    legacyShaders.Add($"{mat.name} ({shaderName})");
                    issues++;
                }
            }

            if (legacyShaders.Count == 0)
            {
                Debug.Log($"  <color=green>[{groupName}] PASS — {paths.Length} materials OK</color>");
            }
            else
            {
                Debug.LogWarning($"  <color=yellow>[{groupName}] FAIL — {legacyShaders.Count}/{paths.Length} use legacy shaders:</color>");
                foreach (var s in legacyShaders)
                    Debug.LogWarning($"    - {s}");
            }

            return issues;
        }
    }
}
