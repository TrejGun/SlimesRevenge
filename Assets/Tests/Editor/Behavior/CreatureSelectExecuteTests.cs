using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SlimesRevenge.Tests
{
    public class CreatureSelectExecuteTests
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
            CreatureTurnContext.Pop();
        }

        [Test]
        public void BrainDecide_ThenPerform_RetaliateAndFlee()
        {
            var player = Spawn<Slime>(new Vector2Int(4, 0));
            var rat = Spawn<Rat>(new Vector2Int(5, 0));
            var session = Occupied(new World(10, 1, TerrainType.Grass), player, rat);
            var rng = new FixedRng();

            rat.MarkAggro();
            var retaliate = CreatureBrain.Decide(rat, player, session, rng);
            Assert.AreEqual(CreatureIntent.Attack, retaliate);
            CreatureMoves.Perform(retaliate, rat, player, session, rng);
            Assert.AreEqual(5, player.Volume.UnitCount);

            Assert.IsTrue(session.TryStep(Vector2Int.left));
            player.PlaceOn(session.PlayerCell);

            var flee = CreatureBrain.Decide(rat, player, session, rng);
            Assert.AreEqual(CreatureIntent.Flee, flee);
            Assert.IsFalse(rat.InCombat);
            CreatureMoves.Perform(flee, rat, player, session, rng);
            Assert.Greater(rat.Cell.x, 5);
        }

        [Test]
        public void BrainDecide_Personalities_PickExpectedIntents()
        {
            var player = Spawn<Slime>(new Vector2Int(4, 4));
            var rat = Spawn<Rat>(new Vector2Int(5, 4));
            var dog = Spawn<Dog>(new Vector2Int(6, 4));
            var cat = Spawn<Cat>(new Vector2Int(5, 5));
            var session = Occupied(World.CreateGrass(), player, rat, dog, cat);
            var rng = new FixedRng();

            Assert.AreEqual(CreaturePersonality.Cowardly, rat.Personality);
            Assert.AreEqual(CreaturePersonality.Aggressive, dog.Personality);
            Assert.AreEqual(CreaturePersonality.Passive, cat.Personality);

            // Adjacent, not aggroed: rat flees; dog (Chebyshev 2) chases; cat idles (FixedRng).
            Assert.AreEqual(CreatureIntent.Flee, CreatureBrain.Decide(rat, player, session, rng));
            Assert.AreEqual(CreatureIntent.Chase, CreatureBrain.Decide(dog, player, session, rng));
            Assert.AreEqual(CreatureIntent.Idle, CreatureBrain.Decide(cat, player, session, rng));

            rat.MarkAggro();
            Assert.AreEqual(CreatureIntent.Attack, CreatureBrain.Decide(rat, player, session, rng));

            cat.MarkAggro();
            Assert.AreEqual(CreatureIntent.Attack, CreatureBrain.Decide(cat, player, session, rng));

            Assert.IsTrue(session.TryStep(Vector2Int.left));
            player.PlaceOn(session.PlayerCell);
            Assert.AreEqual(CreatureIntent.Flee, CreatureBrain.Decide(rat, player, session, rng));
            Assert.IsFalse(rat.InCombat);
        }

        [Test]
        public void CreatureSelect_Choose_SetsChosenIntent()
        {
            CreatureTurnContext.Push(null, null, new FixedRng());
            CreatureSelect.Choose(CreatureIntent.Flee);
            Assert.AreEqual(CreatureIntent.Flee, CreatureTurnContext.ChosenIntent);
            CreatureTurnContext.Pop();
            Assert.AreEqual(CreatureIntent.None, CreatureTurnContext.ChosenIntent);
        }

        private T Spawn<T>(Vector2Int cell) where T : Creature
        {
            var creature = SpawnObject(typeof(T).Name).AddComponent<T>();
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
                    creature.RefreshVolumeStatuses();
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

            return creature;
        }

        private GameObject SpawnObject(string name)
        {
            var go = new GameObject(name);
            spawned.Add(go);
            return go;
        }

        private static GameSession Occupied(World world, Creature player, params Creature[] others)
        {
            var cells = new Vector2Int[others.Length];
            for (var i = 0; i < others.Length; i++)
            {
                cells[i] = others[i].Cell;
            }

            return new GameSession(world, player.Cell, cells);
        }
    }
}
