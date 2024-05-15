#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class ImageAlphaDetection : AssetPostprocessor
{
    // This method is called by Unity whenever an asset is imported
private void OnPostprocessTexture(Texture2D texture)
{
        // Check if the imported texture has alpha transparency
bool hasAlpha = TextureHasAlpha(texture);

        // Set the "Alpha Is Transparent" property based on the alpha transparency
TextureImporter textureImporter = assetImporter as TextureImporter;
        if (textureImporter != null)
        {
            textureImporter.textureType = TextureImporterType.Default;
            textureImporter.alphaIsTransparency = hasAlpha;
            textureImporter.isReadable = true;
            textureImporter.streamingMipmaps = true;

            Debug.Log($"Alpha Is Transparent set to {hasAlpha}, Read/Write Enabled set to true, and Streaming MipMaps set to true for texture: {textureImporter.assetPath}");

            AssetDatabase.ImportAsset(textureImporter.assetPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
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
#endif
