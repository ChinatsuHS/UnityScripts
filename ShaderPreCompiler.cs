using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public class VRCShaderPrecompiler : EditorWindow
{
    private BuildTarget targetPlatform = BuildTarget.StandaloneWindows64;
    
    // UI state for display
    private string estimationText = "Select an avatar to calculate estimate.";
    private int lastScannedCount = 0;

    [MenuItem("Reapa Studio/Advanced Shader Pre-compiler")]
    public static void ShowWindow() => GetWindow<VRCShaderPrecompiler>("Shader Pre-compiler");

    private void OnGUI()
    {
        GUILayout.Label("Advanced VRChat Shader Pre-compiler", EditorStyles.boldLabel);
        targetPlatform = (BuildTarget)EditorGUILayout.EnumPopup("Target Platform:", targetPlatform);

        EditorGUILayout.Space(5);
        
        // Dynamic Estimation Box
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("📊 Pre-compilation Estimate", EditorStyles.miniBoldLabel);
        GUILayout.Label(estimationText, EditorStyles.wordWrappedLabel);
        EditorGUILayout.EndVertical(); // <-- Fixed: Added missing parentheses here!

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Scan & Warm Cache", GUILayout.Height(40)))
        {
            ExecutePipeline();
        }

        if (GUILayout.Button("Recalculate Selection Estimate", GUILayout.Width(200)))
        {
            UpdateSelectionEstimate();
        }
    }

    private void OnSelectionChange()
    {
        UpdateSelectionEstimate();
    }

    private void UpdateSelectionEstimate()
    {
        HashSet<Material> materials = GatherMaterialsFromSelection();
        if (materials.Count == 0)
        {
            estimationText = "No materials found in current selection.";
            lastScannedCount = 0;
            Repaint();
            return;
        }

        double totalEstimatedSeconds = 0;
        int untrackedShaders = 0;

        foreach (var mat in materials)
        {
            if (mat == null || mat.shader == null) continue;
            
            string shaderKey = $"VRCPrecompiler_Time_{mat.shader.name}";
            
            if (EditorPrefs.HasKey(shaderKey))
            {
                totalEstimatedSeconds += EditorPrefs.GetFloat(shaderKey);
            }
            else
            {
                // Heuristic fallback metrics
                string sName = mat.shader.name;
                if (sName.Contains("Poiyomi") && !sName.Contains("Locked")) totalEstimatedSeconds += 2.5f;
                else if (sName.Contains("lilToon") && sName.Contains("Multi")) totalEstimatedSeconds += 2.0f;
                else if (sName.Contains("Ultimate") || sName.Contains("Pro")) totalEstimatedSeconds += 1.5f;
                else totalEstimatedSeconds += 0.4f;
                
                untrackedShaders++;
            }
        }

        lastScannedCount = materials.Count;
        
        System.TimeSpan t = System.TimeSpan.FromSeconds(totalEstimatedSeconds);
        string timeStr = t.TotalSeconds < 60 ? $"{t.Seconds}s" : $"{t.Minutes}m {t.Seconds}s";
        
        estimationText = $"Found {lastScannedCount} unique materials.\n" +
                         $"Estimated Time: ~{timeStr} " + 
                         $"{(untrackedShaders > 0 ? $" (Includes {untrackedShaders} unprofiled shaders using fallback metrics)" : "(Based on hardware history)")}.";
        
        Repaint();
    }

    private HashSet<Material> GatherMaterialsFromSelection()
    {
        HashSet<Material> mats = new HashSet<Material>();
        if (Selection.objects == null) return mats;

        foreach (var obj in Selection.objects)
        {
            if (obj is Material m) mats.Add(m);
            else if (obj is GameObject go)
            {
                foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                {
                    foreach (var sharedMat in r.sharedMaterials)
                    {
                        if (sharedMat != null) mats.Add(sharedMat);
                    }
                }
            }
        }
        return mats;
    }

    private void ExecutePipeline()
    {
        HashSet<Material> allMaterials = GatherMaterialsFromSelection();
        if (allMaterials.Count == 0) return;

        RunCompilerWithMetrics(allMaterials, targetPlatform);
        UpdateSelectionEstimate();
    }

    // --- External GUI Integration Hook ---
    // This restores the missing method definition for WuWaToonUltimateGUI.cs
    public static void PrecompileSingleMaterial(Material mat)
    {
        if (mat == null) return;
        HashSet<Material> singleMatSet = new HashSet<Material> { mat };
        
        // Automatically compile against whatever platform the project is currently targeting
        BuildTarget currentPlatform = EditorUserBuildSettings.activeBuildTarget;
        RunCompilerWithMetrics(singleMatSet, currentPlatform);
    }

    public static void RunCompilerWithMetrics(HashSet<Material> totalMaterials, BuildTarget targetPlatform)
    {
        string tempBuildDir = "Temp/_TempShaderBuild";
        if (!Directory.Exists(tempBuildDir)) Directory.CreateDirectory(tempBuildDir);

        Dictionary<Shader, List<string>> shaderToPaths = new Dictionary<Shader, List<string>>();
        
        foreach (var m in totalMaterials)
        {
            if (m == null || m.shader == null) continue;
            string path = AssetDatabase.GetAssetPath(m);
            if (string.IsNullOrEmpty(path)) continue;

            if (!shaderToPaths.ContainsKey(m.shader))
            {
                shaderToPaths[m.shader] = new List<string>();
            }
            shaderToPaths[m.shader].Add(path);
        }

        StringBuilder logSummary = new StringBuilder();
        logSummary.AppendLine("[Pre-compiler Profiler Results]");

        foreach (var kvp in shaderToPaths)
        {
            Shader shader = kvp.Key;
            List<string> assetPaths = kvp.Value;

            AssetBundleBuild[] buildMap = new AssetBundleBuild[1];
            buildMap[0].assetBundleName = "temp_metric_bundle";
            buildMap[0].assetNames = assetPaths.ToArray();

            Stopwatch sw = Stopwatch.StartNew();
            BuildPipeline.BuildAssetBundles(tempBuildDir, buildMap, BuildAssetBundleOptions.None, targetPlatform);
            sw.Stop();

            float elapsedSeconds = (float)sw.Elapsed.TotalSeconds;
            float timePerMaterial = elapsedSeconds / assetPaths.Count;

            string shaderKey = $"VRCPrecompiler_Time_{shader.name}";
            
            if (EditorPrefs.HasKey(shaderKey))
            {
                float oldTime = EditorPrefs.GetFloat(shaderKey);
                timePerMaterial = Mathf.Lerp(oldTime, timePerMaterial, 0.4f); 
            }
            
            EditorPrefs.SetFloat(shaderKey, timePerMaterial);
            logSummary.AppendLine($"- {shader.name}: Total {elapsedSeconds:F2}s for {assetPaths.Count} items (~{timePerMaterial:F2}s each)");
        }

        if (Directory.Exists(tempBuildDir)) Directory.Delete(tempBuildDir, true);
        AssetDatabase.Refresh();

        UnityEngine.Debug.Log(logSummary.ToString());
    }
}