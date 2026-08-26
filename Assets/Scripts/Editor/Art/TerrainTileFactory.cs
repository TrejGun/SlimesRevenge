using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SlimesRevenge.Editor
{
    public static class TerrainTileFactory
    {
        private const string TerrainFolder = "Assets/Art/Tiles";

        [MenuItem("SlimesRevenge/Create Terrain Tiles")]
        public static void CreateTerrainTiles()
        {
            CreateTile("Grass");
            CreateTile("Dirt");
            CreateTile("Sand");
            AssetDatabase.SaveAssets();
        }

        private static void CreateTile(string name)
        {
            var path = $"{TerrainFolder}/{name}.png";
            var sprites = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .OrderBy(sprite => sprite.name, StringComparer.Ordinal)
                .ToArray();
            if (sprites.Length != 47)
            {
                throw new InvalidOperationException($"{path} must contain 47 blob sprites.");
            }

            var assetPath = $"{TerrainFolder}/{name}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<BlobTile>(assetPath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<BlobTile>();
                AssetDatabase.CreateAsset(tile, assetPath);
            }

            var so = new SerializedObject(tile);
            var spritesProperty = so.FindProperty("sprites");
            spritesProperty.arraySize = sprites.Length;
            for (var i = 0; i < sprites.Length; i++)
            {
                spritesProperty.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tile);
        }
    }
}
