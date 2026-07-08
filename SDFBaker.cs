using UnityEngine;
using UnityEditor;
using System.IO;

public class FaceShadowBaker : EditorWindow
{
    private SkinnedMeshRenderer targetSMR;
    private int selectedMaterialIndex = 0;
    private string[] materialNames = new string[0];

    private int resolution = 1024;
    private int stepCount = 32; 
    private string savePath = "Assets/BakedFaceSDF.png";
    
    private bool appendTimestamp = false;

    [MenuItem("Tools/Avatar Tools/SDF Shadow Baker")]
    public static void ShowWindow()
    {
        GetWindow<FaceShadowBaker>("SDF Shadow Baker");
    }

    private void OnGUI()
    {
        GUILayout.Label("Face SDF Shadow Baker", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        targetSMR = (SkinnedMeshRenderer)EditorGUILayout.ObjectField("Target SkinnedMesh", targetSMR, typeof(SkinnedMeshRenderer), true);
        
        if (EditorGUI.EndChangeCheck() && targetSMR != null)
        {
            UpdateMaterialList();
            UpdateSavePath();
        }

        if (targetSMR != null && materialNames.Length > 0)
        {
            EditorGUI.BeginChangeCheck();
            selectedMaterialIndex = EditorGUILayout.Popup("Target Material Slot", selectedMaterialIndex, materialNames);
            if (EditorGUI.EndChangeCheck())
            {
                UpdateSavePath();
            }
        }
        else if (targetSMR != null)
        {
            EditorGUILayout.HelpBox("No materials found on this SkinnedMeshRenderer.", MessageType.Warning);
        }

        GUILayout.Space(10);
        resolution = EditorGUILayout.IntSlider("Resolution", resolution, 256, 4096);
        stepCount = EditorGUILayout.IntSlider("Light Steps", stepCount, 8, 128);
        
        GUILayout.Space(5);
        // Display the dynamically generated path (user can still manually override if desired)
        savePath = EditorGUILayout.TextField("Save Path", savePath);
        appendTimestamp = EditorGUILayout.Toggle("Append Timestamp", appendTimestamp);

        GUILayout.Space(10);
        if (GUILayout.Button("Bake SDF Texture") && targetSMR != null)
        {
            BakeTexture();
        }
    }

    private void UpdateMaterialList()
    {
        Material[] mats = targetSMR.sharedMaterials;
        materialNames = new string[mats.Length];
        
        for (int i = 0; i < mats.Length; i++)
        {
            materialNames[i] = mats[i] != null ? $"[{i}] {mats[i].name}" : $"[{i}] Empty Slot";
        }
        
        selectedMaterialIndex = Mathf.Clamp(selectedMaterialIndex, 0, mats.Length - 1);
    }

    // Automatically finds the selected material's folder directory
    private void UpdateSavePath()
    {
        if (targetSMR == null) return;

        Material[] mats = targetSMR.sharedMaterials;
        if (selectedMaterialIndex >= 0 && selectedMaterialIndex < mats.Length)
        {
            Material targetMat = mats[selectedMaterialIndex];
            if (targetMat != null)
            {
                // Find where the material asset file lives in the project
                string matAssetPath = AssetDatabase.GetAssetPath(targetMat);
                
                if (!string.IsNullOrEmpty(matAssetPath))
                {
                    string targetFolder = Path.GetDirectoryName(matAssetPath);
                    // Format the path nicely for Unity using forward slashes
                    savePath = Path.Combine(targetFolder, $"{targetMat.name}_SDF.png").Replace("\\", "/");
                    return;
                }
            }
        }
        
        // Fallback default if material path can't be resolved (e.g., embedded or built-in materials)
        savePath = "Assets/BakedFaceSDF.png";
    }

    private void BakeTexture()
    {
        Mesh mesh = targetSMR.sharedMesh;
        if (mesh == null)
        {
            Debug.LogError("The SkinnedMeshRenderer does not have a valid mesh assigned!");
            return;
        }

        Shader bakeShader = Shader.Find("Hidden/UVSpaceBaker");
        if (bakeShader == null) return;

        string finalSavePath = savePath;

        if (appendTimestamp)
        {
            string directory = Path.GetDirectoryName(savePath);
            string filename = Path.GetFileNameWithoutExtension(savePath);
            string extension = Path.GetExtension(savePath);
            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            
            finalSavePath = Path.Combine(directory, $"{filename}_{timestamp}{extension}").Replace("\\", "/");
        }
        else
        {
            finalSavePath = AssetDatabase.GenerateUniqueAssetPath(savePath);
            if (string.IsNullOrEmpty(finalSavePath)) finalSavePath = savePath; 
        }

        Material bakeMat = new Material(bakeShader);
        RenderTexture rt = RenderTexture.GetTemporary(resolution, resolution, 0, RenderTextureFormat.ARGB32);
        Texture2D resultTex = new Texture2D(resolution, resolution, TextureFormat.RGB24, false);
        
        float[] accumulationR = new float[resolution * resolution];
        float[] accumulationG = new float[resolution * resolution];

        // PASS 1: Right Side Sweep (0 to 90 degrees) -> RED CHANNEL
        for (int i = 0; i < stepCount; i++)
        {
            float t = (float)i / (stepCount - 1);
            float angle = Mathf.Lerp(0f, 90f, t); 
            Vector3 lightDir = new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad), 0, Mathf.Cos(angle * Mathf.Deg2Rad)).normalized;
            bakeMat.SetVector("_LightDir", new Vector4(lightDir.x, lightDir.y, lightDir.z, 0));

            RenderTexture.active = rt;
            GL.Clear(true, true, Color.black);
            bakeMat.SetPass(0);
            Graphics.DrawMeshNow(mesh, Matrix4x4.identity, selectedMaterialIndex);

            Texture2D stepTex = new Texture2D(resolution, resolution, TextureFormat.RGB24, false);
            stepTex.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
            stepTex.Apply();

            Color[] pixels = stepTex.GetPixels();
            for (int p = 0; p < pixels.Length; p++)
            {
                if (pixels[p].r > 0.5f) accumulationR[p] += 1.0f / stepCount;
            }
            DestroyImmediate(stepTex);
        }

        // PASS 2: Left Side Sweep (0 to -90 degrees) -> GREEN CHANNEL
        for (int i = 0; i < stepCount; i++)
        {
            float t = (float)i / (stepCount - 1);
            float angle = Mathf.Lerp(0f, -90f, t); 
            Vector3 lightDir = new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad), 0, Mathf.Cos(angle * Mathf.Deg2Rad)).normalized;
            bakeMat.SetVector("_LightDir", new Vector4(lightDir.x, lightDir.y, lightDir.z, 0));

            RenderTexture.active = rt;
            GL.Clear(true, true, Color.black);
            bakeMat.SetPass(0);
            Graphics.DrawMeshNow(mesh, Matrix4x4.identity, selectedMaterialIndex);

            Texture2D stepTex = new Texture2D(resolution, resolution, TextureFormat.RGB24, false);
            stepTex.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
            stepTex.Apply();

            Color[] pixels = stepTex.GetPixels();
            for (int p = 0; p < pixels.Length; p++)
            {
                if (pixels[p].r > 0.5f) accumulationG[p] += 1.0f / stepCount;
            }
            DestroyImmediate(stepTex);
        }

        Color[] finalPixels = new Color[accumulationR.Length];
        for (int i = 0; i < finalPixels.Length; i++)
        {
            finalPixels[i] = new Color(accumulationR[i], accumulationG[i], 0.0f, 1.0f);
        }

        resultTex.SetPixels(finalPixels);
        resultTex.Apply();

        byte[] bytes = resultTex.EncodeToPNG();
        File.WriteAllBytes(finalSavePath, bytes);
        AssetDatabase.Refresh();

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        DestroyImmediate(bakeMat);
        DestroyImmediate(resultTex);

        Debug.Log($"<color=green><b>SDF Shadow Map saved directly to material folder:</b></color> {finalSavePath}");
    }
}