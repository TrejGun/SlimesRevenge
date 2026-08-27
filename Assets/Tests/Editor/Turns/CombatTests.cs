using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace SlimesRevenge.Tests
{
    public class CombatTests
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
        public void SlimeStartingKinds_IncludeLava()
        {
            var volume = new Volume();
            Slime.FillStarting(volume);
            var kinds = volume.UniqueKinds();

            Assert.AreEqual(6, volume.UnitCount);
            Assert.AreEqual(5, kinds.Count);
            Assert.IsTrue(kinds.Any(kind => kind is Lava));
        }

        [Test]
        public void ThreeWaters_KillRat()
        {
            var slime = Spawn<Slime>(new Vector2Int(1, 1));
            slime.Volume.Add(new Water());
            slime.RefreshVolumeStatuses();
            var rat = Spawn<Rat>(new Vector2Int(2, 1));
            Assert.AreEqual(3, rat.HitPoints);

            Assert.IsTrue(Combat.Attack(slime, rat, new Water()));
            Assert.AreEqual(2, rat.HitPoints);
            Assert.IsTrue(Combat.Attack(slime, rat, new Water()));
            Assert.AreEqual(1, rat.HitPoints);
            Assert.IsTrue(Combat.Attack(slime, rat, new Water()));
            Assert.AreEqual(0, rat.HitPoints);
            Assert.IsFalse(rat.IsAlive);
            Assert.AreEqual(1, rat.Volume.UnitCount);
        }

        [Test]
        public void OneAcid_KillsRatThroughDoT()
        {
            var slime = Spawn<Slime>(new Vector2Int(1, 1));
            var rat = Spawn<Rat>(new Vector2Int(2, 1));
            Assert.IsTrue(Combat.Attack(slime, rat, new Acid()));
            Assert.AreEqual(2, rat.HitPoints);
            Assert.AreEqual(1, rat.CountStatus<Corroding>());

            rat.TickStatuses();
            Assert.AreEqual(1, rat.HitPoints);
            rat.TickStatuses();
            Assert.AreEqual(0, rat.HitPoints);
            Assert.IsFalse(rat.IsAlive);
        }

        [Test]
        public void AttackWithoutSubstance_Fails()
        {
            var slime = Spawn<Slime>(new Vector2Int(1, 1));
            slime.Volume.Clear();
            slime.Volume.Add(new Oil());
            slime.RefreshVolumeStatuses();
            var rat = Spawn<Rat>(new Vector2Int(2, 1));

            Assert.IsFalse(Combat.Attack(slime, rat, new Water()));
            Assert.AreEqual(1, slime.Volume.UnitCount);
            Assert.AreEqual(3, rat.HitPoints);
        }

        [Test]
        public void Substances_DealOneDamage()
        {
            Substance[] substances =
            {
                new Water(),
                new Oil(),
                new Poison(),
                new Acid(),
                new Blood(),
                new Lava(),
            };
            foreach (var substance in substances)
            {
                Assert.AreEqual(1, substance.Power);
            }
        }

        [Test]
        public void AdjacentOccupiedCell_CanBeStruck()
        {
            var session = SessionFactory.WithControlled(
                World.CreateGrass(3),
                new Vector2Int(1, 1),
                new Vector2Int(2, 1)
            );

            Assert.IsTrue(session.CanAttack(new Vector2Int(2, 1)));
            Assert.IsFalse(session.CanAttack(new Vector2Int(1, 2)));
            Assert.IsTrue(session.TryAttack(new Vector2Int(2, 1)));
            Assert.AreEqual(new Vector2Int(1, 1), session.ControlledCell);
            Assert.AreEqual(1, session.Turn);
        }

        [Test]
        public void Vacate_AllowsMovingOntoFormerRatCell()
        {
            var rat = new Vector2Int(2, 1);
            var session = SessionFactory.WithControlled(
                World.CreateGrass(3),
                new Vector2Int(1, 1),
                rat
            );

            session.Vacate(rat);
            Assert.IsTrue(session.TryStep(Vector2Int.right));
            Assert.AreEqual(rat, session.ControlledCell);
        }

        [Test]
        public void SlimeDamage_SpendsVolume_DiesWhenEmpty()
        {
            var slime = Spawn<Slime>(Vector2Int.zero);
            Assert.AreEqual(6, slime.Volume.UnitCount);
            Assert.AreEqual(0, slime.HitPoints);
            slime.Damage(1);
            Assert.AreEqual(5, slime.Volume.UnitCount);
            Assert.IsTrue(slime.IsAlive);
            slime.Volume.Clear();
            slime.RefreshVolumeStatuses();
            Assert.AreEqual(0, slime.HitPoints);
            Assert.IsFalse(slime.IsAlive);
        }

        private T Spawn<T>(Vector2Int cell)
            where T : Creature
        {
            var creature = new GameObject(typeof(T).Name).AddComponent<T>();
            spawned.Add(creature.gameObject);
            creature.PlaceOn(cell);
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
            }

            if (creature is Slime)
            {
                creature.RefreshVolumeStatuses();
            }

            return creature;
        }
    }
}
