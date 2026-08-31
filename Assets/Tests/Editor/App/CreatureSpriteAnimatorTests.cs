using NUnit.Framework;
using UnityEngine;

namespace SlimesRevenge.Tests
{
    public class CreatureSpriteAnimatorTests
    {
        [SetUp]
        public void SetUp()
        {
            CreatureSheet.ResetCacheForTests();
        }

        [TearDown]
        public void TearDown()
        {
            CreatureSheet.ResetCacheForTests();
        }

        [Test]
        public void PlayIdle_LoopsWalk_NotSingleIdleFrame()
        {
            var go = new GameObject("BatIdle");
            try
            {
                var renderer = go.AddComponent<SpriteRenderer>();
                var anim = go.AddComponent<CreatureSpriteAnimator>();
                anim.Kind = CreatureKind.Bat;
                anim.Facing = WalkFacing.West;
                anim.PlayIdle();
                Assert.AreEqual("Bat_16", renderer.sprite.name);
                anim.Tick(CreatureSpriteAnimator.DefaultFrameSeconds + 0.01f);
                Assert.AreEqual("Bat_17", renderer.sprite.name);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void SlimeAndBat_TicksDoNotOverwriteEachOther()
        {
            var slimeGo = new GameObject("SlimeAnim");
            var batGo = new GameObject("BatAnim");
            try
            {
                var slimeRenderer = slimeGo.AddComponent<SpriteRenderer>();
                var batRenderer = batGo.AddComponent<SpriteRenderer>();
                var slimeAnim = slimeGo.AddComponent<CreatureSpriteAnimator>();
                var batAnim = batGo.AddComponent<CreatureSpriteAnimator>();
                var s0 = NamedSprite("S0");
                var s1 = NamedSprite("S1");
                var b0 = NamedSprite("B0");
                var b1 = NamedSprite("B1");
                slimeAnim.Play(new[] { s0, s1 }, 0.1f);
                batAnim.Play(new[] { b0, b1 }, 0.1f);
                Assert.AreEqual("S0", slimeRenderer.sprite.name);
                Assert.AreEqual("B0", batRenderer.sprite.name);
                slimeAnim.Tick(0.11f);
                Assert.AreEqual("S1", slimeRenderer.sprite.name);
                Assert.AreEqual("B0", batRenderer.sprite.name);
                batAnim.Tick(0.11f);
                Assert.AreEqual("S1", slimeRenderer.sprite.name);
                Assert.AreEqual("B1", batRenderer.sprite.name);
            }
            finally
            {
                Object.DestroyImmediate(slimeGo);
                Object.DestroyImmediate(batGo);
            }
        }

        private static Sprite NamedSprite(string name)
        {
            var sprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f
            );
            sprite.name = name;
            return sprite;
        }
    }
}
