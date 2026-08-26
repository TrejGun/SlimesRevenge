using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace SlimesRevenge.Tests
{
    /// <summary>
    /// Puddle dump / collect around capacity, and self-menu Collect gating when full.
    /// </summary>
    public class PuddleCapacityTests
    {
        private readonly System.Collections.Generic.List<GameObject> spawned =
            new System.Collections.Generic.List<GameObject>();

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
        public void DumpTwoPuddles_ThenDevourDog_FillsWithoutOverflow()
        {
            // Dog keeps MaxHitPoints 10 (decay), but is softened so one strike finishes it.
            // HP decay outlives: mess → step aside → mess → step onto corpse → digest.
            var center = new Vector2Int(1, 1);
            var dogCell = new Vector2Int(2, 1);
            var side = new Vector2Int(1, 0);

            var world = new World(3, 3, TerrainType.Grass);
            var slime = SpawnFilled(center, new Water(), 10);
            var dog = Spawn<Dog>(dogCell);
            Assert.AreEqual(10, dog.MaxHitPoints);
            while (dog.HitPoints > 1)
            {
                dog.Damage(1);
            }

            var turns = Bind(world, slime, dog);

            Assert.IsTrue(turns.TryAttack(dogCell, new Water()));
            Assert.IsFalse(dog.IsAlive);
            Assert.AreEqual(9, slime.Volume.UnitCount);
            var dogCorpse = turns.Session.World.Floor.GetCorpses(dogCell);
            Assert.AreEqual(1, dogCorpse.Count);
            Assert.AreEqual(3, dogCorpse[0].Volume.UnitCount);
            Assert.AreEqual(10, dogCorpse[0].DecayTurnsLeft);

            Assert.IsTrue(turns.TryMakeMess(new Water()));
            Assert.AreEqual(8, slime.Volume.UnitCount);
            Assert.IsInstanceOf<Water>(turns.Session.World.Floor.GetPuddle(center).Substance);

            Assert.IsTrue(turns.TryMoveTo(side));
            Assert.IsTrue(turns.TryMakeMess(new Water()));
            Assert.AreEqual(7, slime.Volume.UnitCount);
            Assert.IsInstanceOf<Water>(turns.Session.World.Floor.GetPuddle(side).Substance);

            Assert.IsTrue(turns.TryMoveTo(dogCell));
            Assert.AreEqual(1, turns.Session.World.Floor.GetCorpses(dogCell).Count);
            Assert.IsTrue(turns.TryDevourCorpse(0));
            Assert.AreEqual(8, slime.Volume.UnitCount);
            Assert.IsTrue(slime.Digestion.IsBusy);
            Assert.IsTrue(turns.TryWait());
            Assert.AreEqual(9, slime.Volume.UnitCount);
            Assert.IsTrue(turns.TryWait());
            Assert.AreEqual(Volume.Capacity, slime.Volume.UnitCount);
            Assert.IsFalse(slime.Digestion.IsBusy);
            Assert.AreEqual(7, slime.Volume.CountOf<Water>());
            Assert.AreEqual(3, slime.Volume.CountOf<Blood>());
            Assert.IsNotNull(turns.Session.World.Floor.GetPuddle(center));
            Assert.IsNotNull(turns.Session.World.Floor.GetPuddle(side));
        }

        [Test]
        public void FullOilSlime_CollectBlocked_DumpThenSwapForWater()
        {
            var waterCell = new Vector2Int(1, 1);
            var dumpCell = new Vector2Int(1, 0);
            var world = new World(3, 3, TerrainType.Grass);
            var slime = SpawnFilled(waterCell, new Oil(), 10);
            var turns = Bind(world, slime);
            Assert.IsTrue(turns.Session.World.Floor.TryPlacePuddle(waterCell, new Water()));

            Assert.IsFalse(turns.TryCollectPuddle());
            Assert.AreEqual(Volume.Capacity, slime.Volume.UnitCount);
            Assert.IsInstanceOf<Water>(turns.Session.World.Floor.GetPuddle(waterCell).Substance);

            var menu = SpawnObject("Menu").AddComponent<CombatMenu>();
            menu.OpenSelf(
                slime.Volume.UniqueKinds(),
                canMess: false,
                canCollect: slime.Volume.UnitCount < Volume.Capacity,
                floorCorpses: System.Array.Empty<Corpse>(),
                mess: _ => { },
                collect: () => { },
                devour: _ => { });
            Assert.IsFalse(MenuHasLabel(I18n.Get(TextKey.CombatCollect)));

            Assert.IsTrue(turns.TryMoveTo(dumpCell));
            Assert.IsTrue(turns.TryMakeMess(new Oil()));
            Assert.AreEqual(9, slime.Volume.UnitCount);
            Assert.IsInstanceOf<Oil>(turns.Session.World.Floor.GetPuddle(dumpCell).Substance);

            Assert.IsTrue(turns.TryMoveTo(waterCell));
            Assert.IsTrue(turns.TryCollectPuddle());
            Assert.AreEqual(Volume.Capacity, slime.Volume.UnitCount);
            Assert.AreEqual(9, slime.Volume.CountOf<Oil>());
            Assert.AreEqual(1, slime.Volume.CountOf<Water>());
            Assert.IsNull(turns.Session.World.Floor.GetPuddle(waterCell));

            menu.OpenSelf(
                slime.Volume.UniqueKinds(),
                canMess: turns.Session.World.Floor.GetPuddle(waterCell) == null && slime.Volume.UnitCount > 0,
                canCollect: turns.Session.World.Floor.GetPuddle(waterCell) != null
                    && slime.Volume.UnitCount < Volume.Capacity,
                floorCorpses: System.Array.Empty<Corpse>(),
                mess: _ => { },
                collect: () => { },
                devour: _ => { });
            Assert.IsFalse(MenuHasLabel(I18n.Get(TextKey.CombatCollect)));
            Assert.IsTrue(MenuHasLabel(I18n.Get(TextKey.CombatMess)));
        }

        private static bool MenuHasLabel(string label)
        {
            // CombatMenu builds its canvas on a separate root object.
            foreach (var text in Object.FindObjectsByType<Text>())
            {
                if (text != null && text.text == label)
                {
                    return true;
                }
            }

            return false;
        }

        private Slime SpawnFilled(Vector2Int cell, Substance kind, int units)
        {
            var slime = Spawn<Slime>(cell);
            slime.Volume.Clear();
            for (var i = 0; i < units; i++)
            {
                slime.Volume.Add(Volume.CloneSubstance(kind));
            }

            slime.SetMaxHitPoints(1);
            slime.RefreshBodyTraits();
            return slime;
        }

        private TurnManager Bind(World world, Slime slime, params Creature[] foes)
        {
            var turns = SpawnObject("Turns").AddComponent<TurnManager>();
            turns.Rng = new FixedRng();
            turns.Bind(world, slime, foes);
            return turns;
        }

        private T Spawn<T>(Vector2Int cell) where T : Creature
        {
            var creature = SpawnObject(typeof(T).Name).AddComponent<T>();
            creature.PlaceOn(cell);
            if (creature.Volume.UnitCount == 0)
            {
                if (creature is Dog)
                {
                    Dog.FillStarting(creature.Volume);
                }
            }

            if (creature is Dog)
            {
                creature.SetSpeed(2);
                creature.SetMaxHitPoints(10);
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
