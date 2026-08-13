using UnityEngine;
using UnityEditor;

/// <summary>
/// 确保 Assets/Resources/ 下的 PNG 导入为 Sprite(2D and UI) 类型。
/// 解决 Library 删除后重建时默认导入为 Texture 的问题。
/// </summary>
public class SpriteImportFixer : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        string path = assetPath.ToLower();
        // 只处理 Resources 目录下的 PNG
        if (!path.Contains("/resources/") || !path.EndsWith(".png"))
            return;

        var importer = assetImporter as TextureImporter;
        if (importer == null) return;

        // 已设置为 Sprite 的跳过
        if (importer.textureType == TextureImporterType.Sprite)
            return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
    }
}