using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SlimesRevenge.Tests
{
    public class ArtTests
    {
        [Test]
        public void TitleImage_Exists()
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/UI/Title.png");
            Assert.IsNotNull(texture, "Assets/Art/UI/Title.png must exist.");
        }

        [Test]
        public void CharacterSprites_Exist()
        {
            AssertSprite("Assets/Art/Characters/Slime.png");
            AssertSprite("Assets/Art/Characters/Rat.png");
            AssertSprite("Assets/Art/Characters/Cat.png");
            AssertSprite("Assets/Art/Characters/Dog.png");
        }

        [Test]
        public void TerrainTiles_Exist()
        {
            foreach (var name in new[] { "Grass", "Dirt", "Sand" })
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Art/Tiles/{name}.png");
                Assert.IsNotNull(texture, $"Assets/Art/Tiles/{name}.png must exist.");

                var sprites = AssetDatabase.LoadAllAssetsAtPath($"Assets/Art/Tiles/{name}.png")
                    .OfType<Sprite>()
                    .ToArray();
                Assert.AreEqual(47, sprites.Length, $"{name} sheet must contain 47 blob variants.");

                var tile = AssetDatabase.LoadAssetAtPath<BlobTile>($"Assets/Art/Tiles/{name}.asset");
                Assert.IsNotNull(tile, $"Assets/Art/Tiles/{name}.asset must exist.");
                Assert.AreEqual(47, tile.Sprites.Length, $"{name} blob tile must reference 47 sprites.");
                Assert.IsTrue(tile.Sprites.All(sprite => sprite != null), $"{name} blob tile sprites must be assigned.");
            }
        }

        [Test]
        public void PuddleSprites_Exist()
        {
            foreach (var name in new[] { "Water", "Oil", "Poison", "Acid", "Blood", "Lava" })
            {
                AssertSprite($"Assets/Art/Puddles/{name}.png");
            }
        }

        private static void AssertSprite(string path)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Assert.IsNotNull(texture, $"{path} must exist.");
        }
    }
}
