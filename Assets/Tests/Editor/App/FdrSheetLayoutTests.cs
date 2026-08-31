using NUnit.Framework;
using UnityEngine;

namespace SlimesRevenge.Tests
{
    public class FdrSheetLayoutTests
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
        public void Index_WestWalk_MatchesFdrRow()
        {
            Assert.AreEqual(16, FdrSheetLayout.Index(WalkFacing.West, FdrClip.Walk, 0));
            Assert.AreEqual(19, FdrSheetLayout.Index(WalkFacing.West, FdrClip.Walk, 3));
        }

        [Test]
        public void Index_SouthAttackAndIdle_UseClipColumns()
        {
            Assert.AreEqual(1, FdrSheetLayout.Index(WalkFacing.South, FdrClip.Idle, 0));
            Assert.AreEqual(4, FdrSheetLayout.Index(WalkFacing.South, FdrClip.Attack, 0));
            Assert.AreEqual(8, FdrSheetLayout.Index(WalkFacing.South, FdrClip.Damage, 0));
            Assert.AreEqual(12, FdrSheetLayout.Index(WalkFacing.South, FdrClip.Dead, 0));
        }

        [Test]
        public void BattleIndex_CoversEightPortraitFrames()
        {
            Assert.AreEqual(64, FdrSheetLayout.BattleIndex(0));
            Assert.AreEqual(71, FdrSheetLayout.BattleIndex(7));
            Assert.AreEqual("Bat_64", FdrSheetLayout.SpriteName("Bat", 64));
            Assert.AreEqual(new Rect(0f, 16f, 64f, 64f), FdrSheetLayout.BattleRect(0, 208));
            Assert.AreEqual(new Rect(448f, 16f, 64f, 64f), FdrSheetLayout.BattleRect(7, 208));
        }

        [Test]
        public void Bat_WalkWest_UsesFdrLeftFlapRow()
        {
            var frames = CreatureSheet.Walk(CreatureKind.Bat, WalkFacing.West);
            Assert.AreEqual(FdrSheetLayout.WalkFrameCount, frames.Length);
            Assert.AreEqual("Bat_16", frames[0].name);
            Assert.AreEqual("Bat_17", frames[1].name);
            Assert.AreEqual("Bat_18", frames[2].name);
            Assert.AreEqual("Bat_19", frames[3].name);
        }

        [Test]
        public void Mushroom_WalkWest_SharesBatSheetLayout()
        {
            var frames = CreatureSheet.Walk(CreatureKind.Mushroom, WalkFacing.West);
            Assert.AreEqual(FdrSheetLayout.WalkFrameCount, frames.Length);
            Assert.AreEqual("Mushroom_16", frames[0].name);
            Assert.AreEqual("Mushroom_19", frames[3].name);
        }

        [Test]
        public void Bat_Portrait_UsesBattleSprites()
        {
            var frames = CreatureSheet.Portrait(CreatureKind.Bat);
            Assert.AreEqual(FdrSheetLayout.BattleFrameCount, frames.Length);
            Assert.AreEqual("Bat_64", frames[0].name);
            Assert.AreEqual("Bat_71", frames[7].name);
            Assert.AreEqual(FdrSheetLayout.BattleCellPixels, frames[0].rect.width);
            Assert.AreEqual(FdrSheetLayout.BattleCellPixels, frames[0].rect.height);
        }

        [Test]
        public void FacingToward_LeftOfFoe_IsWest()
        {
            Assert.AreEqual(
                WalkFacing.West,
                FdrSheetLayout.FacingToward(new Vector2Int(8, 2), new Vector2Int(3, 2))
            );
        }

        [Test]
        public void CreatureSpriteAnimator_Tick_AdvancesSprite()
        {
            var go = new GameObject("BatAnim");
            try
            {
                var renderer = go.AddComponent<SpriteRenderer>();
                var anim = go.AddComponent<CreatureSpriteAnimator>();
                var a = Sprite.Create(
                    Texture2D.whiteTexture,
                    new Rect(0f, 0f, 1f, 1f),
                    new Vector2(0.5f, 0.5f),
                    1f
                );
                var b = Sprite.Create(
                    Texture2D.whiteTexture,
                    new Rect(0f, 0f, 1f, 1f),
                    new Vector2(0.5f, 0.5f),
                    1f
                );
                a.name = "A";
                b.name = "B";
                anim.Play(new[] { a, b }, 0.1f);
                Assert.AreEqual("A", renderer.sprite.name);
                anim.Tick(0.11f);
                Assert.AreEqual("B", renderer.sprite.name);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
