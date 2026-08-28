using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

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
            AssertSprite("Assets/Art/Animals/Rat.png");
            AssertSprite("Assets/Art/Animals/Cat.png");
            AssertSprite("Assets/Art/Animals/Dog.png");
            AssertSprite("Assets/Art/Animals/Bat.png");
            AssertSprite("Assets/Art/Animals/Scorpion.png");
        }

        [Test]
        public void SlimeSubstanceSprites_Exist()
        {
            foreach (var name in new[] { "Water", "Oil", "Poison", "Acid", "Blood", "Lava" })
            {
                AssertSprite($"Assets/Art/Slimes/{name}.png");
            }
        }

        [Test]
        public void FantasyDreamlandGrassFill_Exists()
        {
            var grass = AssetDatabase.LoadAssetAtPath<TileBase>(
                "Assets/ElvGames/Fantasy Dreamland Reborn/Tilesets/Ground Tiles/Tiles/FDR_Ground_Tiles_25.asset"
            );
            Assert.IsNotNull(grass, "Fantasy Dreamland Reborn green ground fill tile must exist.");
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
