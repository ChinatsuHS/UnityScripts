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
            textureImporter.alphaIsTransparency = hasAlpha;
            textureImporter.isReadable = true;
            textureImporter.streamingMipmaps = true;
            
            Debug.Log($"Alpha Is Transparent set to {hasAlpha}, Read/Write Enabled set to true, and Streaming MipMaps set to true for texture: {textureImporter.assetPath}");
            
            AssetDatabase.ImportAsset(textureImporter.assetPath);
        }
    }

    // Check if the texture has alpha transparency
    private bool TextureHasAlpha(Texture2D texture)
    {
        Color[] pixels = texture.GetPixels();

        for (int i = 0; i < pixels.Length; i++)
        {
            if (pixels[i].a < 1f)
            {
                return true;
            }
        }

        return false;
    }
}
#endif
