using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SlimesRevenge.Tests
{
    public class CorpseTests
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
        public void Death_LeavesWalkableCorpse_DecayEqualsMaxHitPoints()
        {
            var player = Spawn<Slime>(new Vector2Int(1, 1));
            // Rat attacks between strikes and Volume.Damage pops newest shield units — keep spare water.
            player.Volume.Add(new Water());
            player.Volume.Add(new Water());
            player.Volume.Add(new Water());
            player.RefreshBodyTraits();
            var rat = Spawn<Rat>(new Vector2Int(2, 1));
            var turns = SpawnObject("Turns").AddComponent<TurnManager>();
            turns.Rng = new FixedRng();
            turns.Bind(World.CreateGrass(3), player, rat);

            while (rat.IsAlive)
            {
                Assert.IsTrue(turns.TryAttack(rat.Cell, new Water()));
            }

            Assert.IsFalse(turns.Session.IsOccupied(new Vector2Int(2, 1)));
            var corpses = turns.Session.World.Floor.GetCorpses(new Vector2Int(2, 1));
            Assert.AreEqual(1, corpses.Count);
            Assert.AreEqual(1, corpses[0].Volume.UnitCount);
            // Decay tracks max HP (3), not blood units (1). Death resolve skipped aging.
            Assert.AreEqual(3, corpses[0].DecayTurnsLeft);
            Assert.IsTrue(turns.TryMoveTo(new Vector2Int(2, 1)));
        }

        [Test]
        public void Devour_TransfersOneUnitPerTurn()
        {
            var slime = Spawn<Slime>(Vector2Int.zero);
            slime.Volume.Clear();
            slime.RefreshBodyTraits();
            var corpse = new Corpse(CreatureKind.Cat, new Volume(), Vector2Int.zero);
            Cat.FillStarting(corpse.Volume);
            Assert.AreEqual(2, corpse.Volume.UnitCount);

            Assert.IsTrue(slime.Digestion.TryBegin(corpse));
            Assert.IsTrue(slime.Digestion.Tick(slime.Volume));
            Assert.AreEqual(1, slime.Volume.UnitCount);
            Assert.AreEqual(1, corpse.Volume.UnitCount);
            Assert.IsTrue(slime.Digestion.Tick(slime.Volume));
            Assert.AreEqual(2, slime.Volume.UnitCount);
            Assert.IsFalse(slime.Digestion.IsBusy);
        }

        [Test]
        public void RatCorpse_ExpiresAfterThreeAgingTicks_NotOne()
        {
            // skip + 3 decrements (HP) — cat (5) still remains if left on the floor.
            var floor = new Floor();
            var cell = Vector2Int.zero;
            var rat = new Corpse(CreatureKind.Rat, new Volume(), cell);
            Rat.FillStarting(rat.Volume);
            var cat = new Corpse(CreatureKind.Cat, new Volume(), cell);
            Cat.FillStarting(cat.Volume);
            floor.AddCorpse(rat);
            floor.AddCorpse(cat);

            Assert.IsTrue(floor.TryTakeCorpse(cell, 1, out var taken));
            Assert.AreSame(cat, taken);
            Assert.AreEqual(1, floor.GetCorpses(cell).Count);

            floor.Tick(); // skip
            Assert.AreEqual(1, floor.GetCorpses(cell).Count);
            floor.Tick(); // 3 → 2
            Assert.AreEqual(1, floor.GetCorpses(cell).Count);
            floor.Tick(); // 2 → 1
            Assert.AreEqual(1, floor.GetCorpses(cell).Count);
            floor.Tick(); // 1 → 0, remove
            Assert.AreEqual(0, floor.GetCorpses(cell).Count);
        }

        private T Spawn<T>(Vector2Int cell) where T : Creature
        {
            var creature = SpawnObject(typeof(T).Name).AddComponent<T>();
            creature.PlaceOn(cell);
            if (creature.Volume.UnitCount == 0)
            {
                if (creature is Slime)
                {
                    Slime.FillStarting(creature.Volume);
                }
                else if (creature is Rat)
                {
                    Rat.FillStarting(creature.Volume);
                }
            }

            if (creature is Slime)
            {
                creature.SetMaxHitPoints(1);
                creature.RefreshBodyTraits();
            }
            else if (creature is Rat)
            {
                creature.SetMaxHitPoints(3);
            }

            return creature;
        }

        private GameObject SpawnObject(string name)
        {
            var go = new GameObject(name);
            spawned.Add(go);
            return go;
        }
    }
}
