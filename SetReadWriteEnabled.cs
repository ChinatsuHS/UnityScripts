using UnityEngine;
using UnityEditor;

public class SetReadWriteEnabled : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        TextureImporter textureImporter = (TextureImporter)assetImporter;
        textureImporter.isReadable = true;
    }
}