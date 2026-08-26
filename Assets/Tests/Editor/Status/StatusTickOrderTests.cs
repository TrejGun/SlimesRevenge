using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SlimesRevenge.Tests
{
    public class StatusTickOrderTests
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
            GameSettings.Mode = GameMode.Hardcore;
        }

        [Test]
        public void Statuses_PulseInApplicationOrder_StopWhenDead()
        {
            var slime = SpawnObject("Slime").AddComponent<Slime>();
            slime.PlaceOn(Vector2Int.zero);
            slime.Volume.Clear();
            slime.SetMaxHitPoints(1);
            slime.RefreshBodyTraits();

            var first = new PulseProbe();
            var second = new PulseProbe();
            slime.AddStatus(first);
            slime.AddStatus(second);

            slime.TickStatuses();
            Assert.AreEqual(1, first.Pulses);
            Assert.AreEqual(0, second.Pulses);
            Assert.IsFalse(slime.IsAlive);
        }

        [Test]
        public void PlayerStatusDeath_TriggersGameOverHardcore()
        {
            var slime = SpawnObject("Slime").AddComponent<Slime>();
            slime.PlaceOn(new Vector2Int(4, 0));
            slime.Volume.Clear();
            slime.SetMaxHitPoints(1);
            slime.RefreshBodyTraits();
            slime.AddStatus(new Burning());

            var turns = SpawnObject("Turns").AddComponent<TurnManager>();
            turns.Rng = new FixedRng();
            var defeated = false;
            turns.PlayerDefeated += () => defeated = true;
            turns.Bind(new World(10, 1, TerrainType.Grass), slime);

            Assert.IsTrue(defeated);
            Assert.IsTrue(turns.IsGameOver);
            Assert.IsFalse(slime.IsAlive);
            Assert.IsFalse(turns.IsWaitingForInput);
        }

        [Test]
        public void Softcore_CanContinueViaConnector()
        {
            GameSettings.Mode = GameMode.Softcore;
            var slime = SpawnObject("Slime").AddComponent<Slime>();
            slime.PlaceOn(new Vector2Int(4, 0));
            slime.Volume.Clear();
            slime.SetMaxHitPoints(1);
            slime.RefreshBodyTraits();
            slime.AddStatus(new Burning());

            var turns = SpawnObject("Turns").AddComponent<TurnManager>();
            turns.Rng = new FixedRng();
            turns.TrySoftcoreContinue = () =>
            {
                slime.gameObject.SetActive(true);
                slime.SetMaxHitPoints(1);
                slime.Volume.Add(new Water());
                slime.RefreshBodyTraits();
                return true;
            };
            turns.Bind(new World(10, 1, TerrainType.Grass), slime);

            Assert.IsFalse(turns.IsGameOver);
            Assert.IsTrue(slime.IsAlive);
            Assert.AreEqual(1, slime.Volume.UnitCount);
        }

        [Test]
        public void CreatureAttack_DoesNotDropPlayerCorpse_ButEndsGame()
        {
            var slime = SpawnObject("Slime").AddComponent<Slime>();
            slime.PlaceOn(new Vector2Int(4, 0));
            slime.Volume.Clear();
            slime.SetMaxHitPoints(1);
            slime.RefreshBodyTraits();

            var dog = SpawnObject("Dog").AddComponent<Dog>();
            dog.PlaceOn(new Vector2Int(5, 0));
            Dog.FillStarting(dog.Volume);
            dog.SetSpeed(2);
            dog.SetMaxHitPoints(10);
            dog.MarkAggro();

            var turns = SpawnObject("Turns").AddComponent<TurnManager>();
            turns.Rng = new FixedRng();
            turns.Bind(new World(10, 1, TerrainType.Grass), slime, dog);

            Assert.IsTrue(turns.TryWait());
            Assert.IsFalse(slime.IsAlive);
            Assert.IsTrue(turns.IsGameOver);
            Assert.AreEqual(0, turns.Session.World.Floor.GetCorpses(new Vector2Int(4, 0)).Count);
        }

        private sealed class PulseProbe : StatusEffect
        {
            public int Pulses;

            public PulseProbe() : base(3)
            {
            }

            public override string Label => "probe";

            protected override void OnPulse(Creature creature)
            {
                Pulses++;
                creature?.Damage(99);
            }
        }

        private GameObject SpawnObject(string name)
        {
            var go = new GameObject(name);
            spawned.Add(go);
            return go;
        }
    }
}
