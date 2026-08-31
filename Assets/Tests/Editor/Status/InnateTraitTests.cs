using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SlimesRevenge.Tests
{
    public class InnateTraitTests
    {
        private readonly List<GameObject> spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in spawned)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }

            spawned.Clear();
        }

        [Test]
        public void StartingTraits_MatchAnimals()
        {
            var rat = Spawn<Rat>();
            Assert.IsNull(rat.FindStatus<Vampirism>());
            Assert.IsNull(rat.FindStatus<HatesRats>());
            Assert.IsNotNull(rat.FindStatus<FearsCats>());
            Assert.AreEqual(CreaturePersonality.Cowardly, rat.Personality);

            var cat = Spawn<Cat>();
            Assert.IsNotNull(cat.FindStatus<HatesRats>());
            Assert.IsNotNull(cat.FindStatus<FearsDogs>());
            Assert.IsNotNull(cat.FindStatus<FearsWater>());
            Assert.IsNull(cat.FindStatus<HatesCats>());

            var dog = Spawn<Dog>();
            Assert.IsNotNull(dog.FindStatus<HatesCats>());
            Assert.IsNull(dog.FindStatus<HatesRats>());

            var bat = Spawn<Bat>();
            Assert.IsNotNull(bat.FindStatus<Vampirism>());
            Assert.AreEqual(2, bat.Volume.CountOf<Blood>());
            Assert.AreEqual(3, bat.HitPoints);

            var mushroom = Spawn<Mushroom>();
            Assert.AreEqual(5, mushroom.HitPoints);
            Assert.AreEqual(1, mushroom.Volume.CountOf<Water>());

            var waterElement = Spawn<WaterElement>();
            Assert.AreEqual(5, waterElement.HitPoints);
            Assert.AreEqual(1, waterElement.Volume.CountOf<Water>());

            var cactus = Spawn<Cactus>();
            Assert.AreEqual(5, cactus.HitPoints);
            Assert.AreEqual(1, cactus.Volume.CountOf<Water>());

            var mandragora = Spawn<Mandragora>();
            Assert.AreEqual(5, mandragora.HitPoints);
            Assert.AreEqual(1, mandragora.Volume.CountOf<Water>());

            var eye = Spawn<Eye>();
            Assert.AreEqual(5, eye.HitPoints);
            Assert.AreEqual(1, eye.Volume.CountOf<Water>());

            var scorpion = Spawn<Scorpion>();
            Assert.IsNotNull(scorpion.FindStatus<Poisonous>());
            Assert.AreEqual(1, scorpion.Volume.UnitCount);
            Assert.IsInstanceOf<Acid>(scorpion.Volume.Units[0].Substance);
        }

        [Test]
        public void Vampirism_AtFullHp_TipIsWater_PopsWater_NoHeal()
        {
            var bat = Spawn<Bat>();
            Assert.AreEqual(3, bat.HitPoints);
            // Fill order: Blood then Water → tip is Water.
            var slime = BloodSlime(new Blood(), new Water());

            Assert.IsTrue(Combat.Attack(bat, slime));
            Assert.AreEqual(1, slime.Volume.CountOf<Blood>());
            Assert.AreEqual(0, slime.Volume.CountOf<Water>());
            Assert.AreEqual(3, bat.HitPoints);
            Assert.AreEqual(2, bat.Volume.CountOf<Blood>());
        }

        [Test]
        public void Vampirism_AtTwoHp_HealsToMax()
        {
            var bat = Spawn<Bat>();
            bat.Damage(1);
            Assert.AreEqual(2, bat.HitPoints);
            var slime = BloodSlime(new Blood());

            Assert.IsTrue(Combat.Attack(bat, slime));
            Assert.AreEqual(0, slime.Volume.CountOf<Blood>());
            Assert.AreEqual(3, bat.HitPoints);
        }

        [Test]
        public void Vampirism_AtOneHp_HealsToMax()
        {
            var bat = Spawn<Bat>();
            bat.Damage(1);
            bat.Damage(1);
            Assert.AreEqual(1, bat.HitPoints);
            var slime = BloodSlime(new Blood());

            Assert.IsTrue(Combat.Attack(bat, slime));
            Assert.AreEqual(3, bat.HitPoints);
        }

        [Test]
        public void BloodApply_OnBat_HealsButDoesNotGrantVampirismToTarget()
        {
            var bat = Spawn<Bat>();
            bat.Damage(1);
            Assert.AreEqual(2, bat.HitPoints);
            Assert.IsNotNull(bat.FindStatus<Vampirism>());

            new Blood().Apply(bat);

            Assert.AreEqual(3, bat.HitPoints);
            Assert.AreEqual(1, bat.CountStatus<Vampirism>());
        }

        [Test]
        public void BloodApply_OnRatWithoutVampirism_DoesNothing()
        {
            var rat = Spawn<Rat>();
            var before = rat.HitPoints;
            Assert.IsNull(rat.FindStatus<Vampirism>());

            new Blood().Apply(rat);

            Assert.AreEqual(before, rat.HitPoints);
            Assert.IsNull(rat.FindStatus<Vampirism>());
        }

        [Test]
        public void Vampirism_TipIsWater_DoesNotHeal_BloodRemains()
        {
            var bat = Spawn<Bat>();
            bat.Damage(1);
            Assert.AreEqual(2, bat.HitPoints);
            var slime = BloodSlime(new Blood(), new Water());

            Assert.IsTrue(Combat.Attack(bat, slime));
            Assert.AreEqual(1, slime.Volume.UnitCount);
            Assert.IsInstanceOf<Blood>(slime.Volume.Units[0].Substance);
            Assert.AreEqual(2, bat.HitPoints);
        }

        [Test]
        public void Vampirism_TipIsBlood_UnderOlderWater_Heals()
        {
            var bat = Spawn<Bat>();
            bat.Damage(1);
            Assert.AreEqual(2, bat.HitPoints);
            // Water then Blood → tip is Blood.
            var slime = BloodSlime(new Water(), new Blood());

            Assert.IsTrue(Combat.Attack(bat, slime));
            Assert.AreEqual(1, slime.Volume.UnitCount);
            Assert.IsInstanceOf<Water>(slime.Volume.Units[0].Substance);
            Assert.AreEqual(3, bat.HitPoints);
        }

        [Test]
        public void Slime_EmptyVolume_IsDead_CannotBeAttacked()
        {
            var bat = Spawn<Bat>();
            var slime = BloodSlime(new Blood());
            Assert.IsTrue(Combat.Attack(bat, slime));
            Assert.AreEqual(0, slime.Volume.UnitCount);
            Assert.IsFalse(slime.IsAlive);
            Assert.IsFalse(Combat.Attack(bat, slime));
        }

        [Test]
        public void Vampirism_NoBlood_DoesNotHeal_AtTwoHp()
        {
            var bat = Spawn<Bat>();
            bat.Damage(1);
            var slime = BloodSlime(new Water(), new Water());

            Assert.IsTrue(Combat.Attack(bat, slime));
            Assert.AreEqual(2, bat.HitPoints);
            Assert.AreEqual(2, bat.Volume.CountOf<Blood>());
        }

        [Test]
        public void Vampirism_NoBlood_AtFullHp_StillNoDrink()
        {
            var bat = Spawn<Bat>();
            var slime = BloodSlime(new Water(), new Oil());
            var units = slime.Volume.UnitCount;

            Assert.IsTrue(Combat.Attack(bat, slime));
            Assert.AreEqual(units - 1, slime.Volume.UnitCount);
            Assert.AreEqual(3, bat.HitPoints);
        }

        [Test]
        public void Poisonous_BlocksPoisonedFromPoisonStrike()
        {
            var scorpion = Spawn<Scorpion>();
            var slime = Spawn<Slime>();
            Assert.IsTrue(Combat.Attack(slime, scorpion, new Poison()));
            Assert.AreEqual(3, scorpion.HitPoints);
            Assert.IsNull(scorpion.FindStatus<Poisoned>());
        }

        [Test]
        public void Poisonous_MeleeAlwaysPoisonsTarget()
        {
            var scorpion = Spawn<Scorpion>();
            var slime = Spawn<Slime>();
            var units = slime.Volume.UnitCount;

            Assert.IsTrue(Combat.Attack(scorpion, slime));
            Assert.AreEqual(units - 1, slime.Volume.UnitCount);
            Assert.IsNotNull(slime.FindStatus<Poisoned>());
        }

        [Test]
        public void Poisonous_KillingBlow_DoesNotApplyPoisoned()
        {
            // Last volume pop kills before OnHit statuses; empty slime never receives Poisoned.
            var scorpion = Spawn<Scorpion>();
            var slime = BloodSlime(new Blood());
            Assert.AreEqual(1, slime.Volume.UnitCount);

            Assert.IsTrue(Combat.Attack(scorpion, slime));
            Assert.IsFalse(slime.IsAlive);
            Assert.AreEqual(0, slime.Volume.UnitCount);
            Assert.IsNull(slime.FindStatus<Poisoned>());
        }

        private Slime BloodSlime(params Substance[] substances)
        {
            var slime = Spawn<Slime>();
            slime.Volume.Clear();
            slime.Volume.Fill(substances);
            slime.RefreshVolumeStatuses();
            return slime;
        }

        private T Spawn<T>()
            where T : Creature
        {
            var creature = new GameObject(typeof(T).Name).AddComponent<T>();
            spawned.Add(creature.gameObject);
            creature.PlaceOn(Vector2Int.zero);
            if (creature.Volume.UnitCount == 0)
            {
                switch (creature)
                {
                    case Slime:
                        Slime.FillStarting(creature.Volume);
                        break;
                    case Rat:
                        Rat.FillStarting(creature.Volume);
                        break;
                    case Cat:
                        Cat.FillStarting(creature.Volume);
                        break;
                    case Dog:
                        Dog.FillStarting(creature.Volume);
                        break;
                    case Bat:
                        Bat.FillStarting(creature.Volume);
                        break;
                    case Scorpion:
                        Scorpion.FillStarting(creature.Volume);
                        break;
                    case Mushroom:
                        Mushroom.FillStarting(creature.Volume);
                        break;
                    case WaterElement:
                        WaterElement.FillStarting(creature.Volume);
                        break;
                    case Cactus:
                        Cactus.FillStarting(creature.Volume);
                        break;
                    case Mandragora:
                        Mandragora.FillStarting(creature.Volume);
                        break;
                    case Eye:
                        Eye.FillStarting(creature.Volume);
                        break;
                }
            }

            switch (creature)
            {
                case Slime:
                    creature.SetMaxHitPoints(0);
                    break;
                case Rat:
                    creature.SetMaxHitPoints(3);
                    break;
                case Cat:
                    creature.SetMaxHitPoints(5);
                    break;
                case Dog:
                    creature.SetSpeed(2);
                    creature.SetMaxHitPoints(10);
                    break;
                case Bat:
                    creature.SetMaxHitPoints(3);
                    break;
                case Scorpion:
                    creature.SetMaxHitPoints(3);
                    creature.SetArmor(1);
                    break;
                case Mushroom:
                case WaterElement:
                case Cactus:
                case Mandragora:
                case Eye:
                    creature.SetMaxHitPoints(5);
                    break;
            }

            if (creature is Slime)
            {
                creature.RefreshVolumeStatuses();
            }

            creature.EnsureInnateTraits();
            return creature;
        }
    }
}
