using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

/// <summary>
/// Automatically detects Humanoid rigs and extracts materials for newly imported FBX files.
/// </summary>
public class AutoFBXProcessor : AssetPostprocessor
{
    // OnPostprocessModel is called after Unity has generated the GameObject hierarchy 
    // but right before the import is finalized.
    void OnPostprocessModel(GameObject g)
    {
        // Only run for FBX files
        if (!assetPath.ToLower().EndsWith(".fbx")) return;

        ModelImporter importer = assetImporter as ModelImporter;
        if (importer == null) return;

        bool needsReimport = false;

        // ----------------------------------------------------
        // 1. AUTOMATIC HUMANOID DETECTION
        // ----------------------------------------------------
        // If the rig isn't already set to Humanoid, let's analyze the bones.
        if (importer.animationType != ModelImporterAnimationType.Human)
        {
            if (IsLikelyHumanoid(g))
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                needsReimport = true;
                
                Debug.Log($"[AutoFBXProcessor] Humanoid skeleton detected in '{Path.GetFileName(assetPath)}'. Set Rig to Humanoid.");
            }
        }

        // If we modified the import settings (changed to Humanoid), we must save and reimport.
        // We return early here to prevent infinite loops. The materials will be extracted
        // on the second pass when the animationType is already set to Human.
        if (needsReimport)
        {
            importer.SaveAndReimport();
            return;
        }

        // ----------------------------------------------------
        // 2. AUTOMATIC MATERIAL EXTRACTION
        // ----------------------------------------------------
        // We use EditorApplication.delayCall because extracting sub-assets while 
        // the asset is actively being processed by Unity can throw locking errors.
        ExtractMaterialsNextFrame(assetPath);
    }

    /// <summary>
    /// Scans the hierarchy for standard humanoid bone names.
    /// </summary>
    private static bool IsLikelyHumanoid(GameObject root)
    {
        Transform[] allTransforms = root.GetComponentsInChildren<Transform>(true);
        
        bool hasHips = false;
        bool hasHead = false;
        bool hasArm = false;
        bool hasLeg = false;

        foreach (Transform t in allTransforms)
        {
            string lowerName = t.name.ToLower();

            if (lowerName.Contains("hips") || lowerName.Contains("pelvis") || lowerName.Contains("b_root")) hasHips = true;
            if (lowerName.Contains("head") || lowerName.Contains("neck")) hasHead = true;
            if (lowerName.Contains("arm") || lowerName.Contains("shoulder") || lowerName.Contains("hand")) hasArm = true;
            if (lowerName.Contains("leg") || lowerName.Contains("thigh") || lowerName.Contains("calf") || lowerName.Contains("foot")) hasLeg = true;
        }

        // If it contains at least one of all major body parts, it's almost certainly a humanoid.
        return hasHips && hasHead && hasArm && hasLeg;
    }

    /// <summary>
    /// Extracts embedded materials into a 'Materials' folder next to the FBX.
    /// </summary>
    private static void ExtractMaterialsNextFrame(string path)
    {
        EditorApplication.delayCall += () =>
        {
            // Fetch any materials that are currently embedded INSIDE the FBX.
            // If they were already extracted, they won't show up here.
            var embeddedMaterials = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>().ToList();
            
            if (embeddedMaterials.Count == 0) return;

            string fbxDir = Path.GetDirectoryName(path);
            string materialDir = Path.Combine(fbxDir, "Materials").Replace("\\", "/");
            bool materialsExtracted = false;

            foreach (Material mat in embeddedMaterials)
            {
                // Skip Unity's built-in default materials
                if (mat.name == "Default-Material" || mat.name == "No Name") continue;

                // Create the Materials folder if it doesn't exist yet
                if (!AssetDatabase.IsValidFolder(materialDir))
                {
                    AssetDatabase.CreateFolder(fbxDir, "Materials");
                }

                string targetPath = Path.Combine(materialDir, mat.name + ".mat").Replace("\\", "/");
                
                // If a material with this name already exists, ensure we don't overwrite it
                // by appending a number (e.g., Material 1.mat)
                targetPath = AssetDatabase.GenerateUniqueAssetPath(targetPath);

                // This physically extracts the material and sets up the remap 
                // in the FBX's import settings automatically!
                string error = AssetDatabase.ExtractAsset(mat, targetPath);

                if (string.IsNullOrEmpty(error))
                {
                    materialsExtracted = true;
                }
                else
                {
                    Debug.LogWarning($"[AutoFBXProcessor] Failed to extract material '{mat.name}': {error}");
                }
            }

            // If we successfully extracted anything, we need to save the new remap settings
            if (materialsExtracted)
            {
                AssetDatabase.WriteImportSettingsIfDirty(path);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                Debug.Log($"[AutoFBXProcessor] Extracted materials for '{Path.GetFileName(path)}' into {materialDir}");
            }
        };
    }
}