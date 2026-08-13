using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// 强制重新导入 Resources 下的 PNG 为 Sprite 类型。
/// 菜单: Tools > Fix Sprite Imports
/// </summary>
public static class FixSpriteImports
{
    [MenuItem("Tools/Fix Sprite Imports")]
    public static void ReimportAllResourceSprites()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture", new[] { "Assets/Resources" });
        List<string> paths = guids
            .Select(g => AssetDatabase.GUIDToAssetPath(g))
            .Where(p => p.EndsWith(".png"))
            .ToList();

        int changed = 0;
        foreach (string path in paths)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null || importer.textureType == TextureImporterType.Sprite)
                continue;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            changed++;
        }

        Debug.Log($"[FixSpriteImports] 已将 {changed} 个 PNG 重新导入为 Sprite 类型（共 {paths.Count} 个）");
        EditorUtility.DisplayDialog("Fix Sprite Imports",
            $"已将 {changed} 个 PNG 重新导入为 Sprite 类型\n（共 {paths.Count} 个 PNG 在 Resources 下）", "OK");
    }
}