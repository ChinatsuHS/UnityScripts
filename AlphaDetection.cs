using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.AssetImporters;

public class ImageAlphaDetection : MonoBehaviour
{
    void Awake()
{
        // Check if the Texture2D has transparency and enable the alpha transparency setting
Texture2D texture = (Texture2D)GetComponent<MeshRenderer>().material.mainTexture;
        if (TextureHasAlpha(texture))
        {
            TextureImporter textureImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;
            textureImporter.textureType = TextureImporterType.Default;
            textureImporter.alphaIsTransparency = true;
            textureImporter.alphaSource = TextureImporterAlphaSource.FromInput;
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(texture));
        }
    }

    bool TextureHasAlpha(Texture2D texture)
{
        Color32[] pixels = texture.GetPixels32();
        for (int i = 0; i < pixels.Length; i++)
        {
            if (pixels[i].a != 255)
                return true;
        }
        return false;
    }
}
