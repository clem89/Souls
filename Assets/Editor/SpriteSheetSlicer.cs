using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class SpriteSheetSlicer : EditorWindow
{
    private const int CELL_SIZE = 100;

    [MenuItem("Tools/Sprite Slicer/Slice Character Sprites (100x100)")]
    public static void SliceAll()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D",
            new[] { "Assets/Resources/Characters(100x100)" });

        int processed = 0;
        int skipped = 0;

        EditorUtility.DisplayProgressBar("슬라이싱 중...", "준비 중", 0f);

        try
        {
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);

                EditorUtility.DisplayProgressBar("슬라이싱 중...",
                    Path.GetFileName(path), (float)i / guids.Length);

                if (ShouldSkip(path))
                {
                    skipped++;
                    continue;
                }

                SliceSheet(path);
                processed++;
            }

            AssetDatabase.Refresh();
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        EditorUtility.DisplayDialog("슬라이싱 완료",
            $"처리: {processed}개\n건너뜀: {skipped}개", "확인");
        Debug.Log($"[SpriteSheetSlicer] 완료 — 처리: {processed}, 건너뜀: {skipped}");
    }

    static bool ShouldSkip(string path)
    {
        // "with shadows"와 "Shadow sprites" 폴더는 제외
        if (path.Contains("with shadows")) return true;
        if (path.Contains("Shadow sprites")) return true;
        return false;
    }

    static void SliceSheet(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return;

        importer.GetSourceTextureWidthAndHeight(out int texWidth, out int texHeight);
        if (texWidth == 0 || texHeight == 0) return;

        int cols = texWidth / CELL_SIZE;
        int rows = texHeight / CELL_SIZE;

        // 단일 프레임짜리 (전체 시트 overview 이미지 등) 는 Single로
        if (cols <= 1 && rows <= 1)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = CELL_SIZE;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return;
        }

        string baseName = Path.GetFileNameWithoutExtension(assetPath);
        var sprites = new List<SpriteMetaData>();

        // Unity 좌표계: Y=0이 아래쪽 → 마지막 행부터 역순으로 읽어야 frame 0이 좌상단
        for (int row = rows - 1; row >= 0; row--)
        {
            for (int col = 0; col < cols; col++)
            {
                int frameIndex = (rows - 1 - row) * cols + col;
                sprites.Add(new SpriteMetaData
                {
                    name      = $"{baseName}_{frameIndex}",
                    rect      = new Rect(col * CELL_SIZE, row * CELL_SIZE, CELL_SIZE, CELL_SIZE),
                    pivot     = new Vector2(0.5f, 0.5f),
                    alignment = (int)SpriteAlignment.Center
                });
            }
        }

        importer.textureType          = TextureImporterType.Sprite;
        importer.spriteImportMode     = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit  = CELL_SIZE;
        importer.filterMode           = FilterMode.Point;
        importer.textureCompression   = TextureImporterCompression.Uncompressed;
        importer.spritesheet          = sprites.ToArray();

        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
    }
}
