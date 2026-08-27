using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace SlimesRevenge.Tests
{
    /// <summary>
    /// Devouring stacked corpses: floor decay follows max HP; matter transfers only on the
    /// final <see cref="Digesting"/> pulse (bulk absorb).
    /// </summary>
    public class DigestionDevourTests
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
        public void DevourMixedCorpse_BulkAtEnd_DiscardsRemainder()
        {
            // Stack end first: Water, Oil, Blood — room for two → keep Blood then Oil; Water discarded.
            var cell = Vector2Int.zero;
            var world = new World(3, 3, TerrainType.Grass);
            var slime = SpawnObject("Slime").AddComponent<Slime>();
            slime.PlaceOn(cell);
            slime.Volume.Clear();
            for (var i = 0; i < 8; i++)
            {
                slime.Volume.Add(new Acid());
            }

            slime.SetMaxHitPoints(0);
            slime.RefreshVolumeStatuses();

            var turns = SpawnObject("Turns").AddComponent<TurnManager>();
            turns.Rng = new FixedRng();
            turns.Bind(world, slime);

            var dog = SpawnObject("Dog").AddComponent<Dog>();
            dog.PlaceOn(cell);
            dog.Volume.Clear();
            dog.Volume.Add(new Water());
            dog.Volume.Add(new Oil());
            dog.Volume.Add(new Blood());
            dog.SetMaxHitPoints(10);
            dog.BecomeCorpse();
            world.Floor.AddCorpse(dog);

            Assert.IsTrue(turns.TryDevourCorpse(0));
            Assert.AreEqual(8, slime.Volume.UnitCount);
            Assert.IsTrue(slime.IsDigesting);
            Assert.IsInstanceOf<Oil>(slime.FindStatus<Digesting>().Meal.Volume.Units[1].Substance);

            Assert.IsTrue(turns.TryWait());
            Assert.AreEqual(8, slime.Volume.UnitCount);
            Assert.IsTrue(slime.IsDigesting);
            Assert.AreEqual(1, slime.FindStatus<Digesting>().Remaining);

            Assert.IsTrue(turns.TryWait());
            Assert.AreEqual(Volume.Capacity, slime.Volume.UnitCount);
            Assert.IsFalse(slime.IsDigesting);
            Assert.AreEqual(8, slime.Volume.CountOf<Acid>());
            Assert.AreEqual(0, slime.Volume.CountOf<Water>());
            Assert.AreEqual(1, slime.Volume.CountOf<Oil>());
            Assert.AreEqual(1, slime.Volume.CountOf<Blood>());
        }

        [Test]
        public void DevourRatThenCat_AllBloodReachesSlime()
        {
            var cell = Vector2Int.zero;
            var (slime, turns, floor) = SetupEmptySlimeOnCorpses(cell);

            Assert.AreEqual(2, floor.GetCorpses(cell).Count);
            Assert.AreEqual(CreatureKind.Rat, floor.GetCorpses(cell)[0].Kind);
            Assert.AreEqual(CreatureKind.Cat, floor.GetCorpses(cell)[1].Kind);
            Assert.AreEqual(2, slime.Volume.UnitCount);

            Assert.IsTrue(turns.TryDevourCorpse(0));
            Assert.AreEqual(3, slime.Volume.UnitCount);
            Assert.IsInstanceOf<Blood>(slime.Volume.Units[2].Substance);
            Assert.IsFalse(slime.IsDigesting);
            Assert.AreEqual(1, floor.GetCorpses(cell).Count);
            Assert.AreEqual(CreatureKind.Cat, floor.GetCorpses(cell)[0].Kind);

            Assert.IsTrue(turns.TryDevourCorpse(0));
            Assert.AreEqual(3, slime.Volume.UnitCount);
            Assert.IsTrue(slime.IsDigesting);
            Assert.AreEqual(0, floor.GetCorpses(cell).Count);

            Assert.IsTrue(turns.TryWait());
            Assert.AreEqual(5, slime.Volume.UnitCount);
            Assert.IsFalse(slime.IsDigesting);
            Assert.AreEqual(2, slime.Volume.CountOf<Water>());
            Assert.AreEqual(3, slime.Volume.CountOf<Blood>());
        }

        [Test]
        public void DevourCatFirst_RatStillPresent_CanDevourBoth()
        {
            var cell = Vector2Int.zero;
            var (slime, turns, floor) = SetupEmptySlimeOnCorpses(cell);

            Assert.IsTrue(turns.TryDevourCorpse(1));
            Assert.AreEqual(2, slime.Volume.UnitCount);
            Assert.IsTrue(slime.IsDigesting);
            Assert.AreEqual(1, floor.GetCorpses(cell).Count);
            Assert.AreEqual(CreatureKind.Rat, floor.GetCorpses(cell)[0].Kind);

            Assert.IsTrue(turns.TryWait());
            Assert.AreEqual(4, slime.Volume.UnitCount);
            Assert.IsFalse(slime.IsDigesting);
            Assert.AreEqual(1, floor.GetCorpses(cell).Count);
            Assert.AreEqual(CreatureKind.Rat, floor.GetCorpses(cell)[0].Kind);

            Assert.IsTrue(turns.TryDevourCorpse(0));
            Assert.AreEqual(5, slime.Volume.UnitCount);
            Assert.IsFalse(slime.IsDigesting);
            Assert.AreEqual(0, floor.GetCorpses(cell).Count);
            Assert.AreEqual(2, slime.Volume.CountOf<Water>());
            Assert.AreEqual(3, slime.Volume.CountOf<Blood>());
        }

        [Test]
        public void TryDevourCorpse_SmallerSlime_LeavesCorpseOnFloor()
        {
            var cell = Vector2Int.zero;
            var world = new World(3, 3, TerrainType.Grass);
            var slime = SpawnObject("Slime").AddComponent<Slime>();
            slime.PlaceOn(cell);
            slime.Volume.Clear();
            slime.Volume.Add(new Water());
            slime.Volume.Add(new Water());
            slime.SetMaxHitPoints(0);
            slime.RefreshVolumeStatuses();

            var turns = SpawnObject("Turns").AddComponent<TurnManager>();
            turns.Rng = new FixedRng();
            turns.Bind(world, slime);

            AddBeastCorpse(world.Floor, cell, CreatureKind.Dog);
            Assert.AreEqual(3, world.Floor.GetCorpses(cell)[0].Volume.UnitCount);
            Assert.AreEqual(2, slime.Volume.UnitCount);

            Assert.IsFalse(turns.TryDevourCorpse(0));
            Assert.AreEqual(1, world.Floor.GetCorpses(cell).Count);
            Assert.AreEqual(2, slime.Volume.UnitCount);
            Assert.IsFalse(slime.IsDigesting);
        }

        [Test]
        public void TryDevourCorpse_EqualVolume_StartsDigestion()
        {
            var cell = Vector2Int.zero;
            var world = new World(3, 3, TerrainType.Grass);
            var slime = SpawnObject("Slime").AddComponent<Slime>();
            slime.PlaceOn(cell);
            slime.Volume.Clear();
            for (var i = 0; i < 3; i++)
            {
                slime.Volume.Add(new Water());
            }

            slime.SetMaxHitPoints(0);
            slime.RefreshVolumeStatuses();

            var turns = SpawnObject("Turns").AddComponent<TurnManager>();
            turns.Rng = new FixedRng();
            turns.Bind(world, slime);

            AddBeastCorpse(world.Floor, cell, CreatureKind.Dog);
            Assert.IsTrue(turns.TryDevourCorpse(0));
            Assert.AreEqual(3, slime.Volume.UnitCount);
            Assert.IsTrue(slime.IsDigesting);
            Assert.AreEqual(0, world.Floor.GetCorpses(cell).Count);
        }

        [Test]
        public void TryDevourCorpse_SlimeLargerByOne_StartsDigestion()
        {
            var cell = Vector2Int.zero;
            var world = new World(3, 3, TerrainType.Grass);
            var slime = SpawnObject("Slime").AddComponent<Slime>();
            slime.PlaceOn(cell);
            slime.Volume.Clear();
            for (var i = 0; i < 4; i++)
            {
                slime.Volume.Add(new Water());
            }

            slime.SetMaxHitPoints(0);
            slime.RefreshVolumeStatuses();

            var turns = SpawnObject("Turns").AddComponent<TurnManager>();
            turns.Rng = new FixedRng();
            turns.Bind(world, slime);

            AddBeastCorpse(world.Floor, cell, CreatureKind.Dog);
            Assert.IsTrue(turns.TryDevourCorpse(0));
            Assert.AreEqual(4, slime.Volume.UnitCount);
            Assert.IsTrue(slime.IsDigesting);
            Assert.AreEqual(0, world.Floor.GetCorpses(cell).Count);
        }

        [Test]
        public void DevourRatCatDog_FromSixUnits_FillsToTenDiscardingOverflow()
        {
            // 6 + rat1 + cat2 = 9; dog has 3 units → only 1 unit fits, remaining 2 vanish.
            var cell = Vector2Int.zero;
            var world = new World(3, 3, TerrainType.Grass);
            var slime = SpawnObject("Slime").AddComponent<Slime>();
            slime.PlaceOn(cell);
            slime.Volume.Clear();
            for (var i = 0; i < 6; i++)
            {
                slime.Volume.Add(new Water());
            }

            slime.SetMaxHitPoints(0);
            slime.RefreshVolumeStatuses();

            var turns = SpawnObject("Turns").AddComponent<TurnManager>();
            turns.Rng = new FixedRng();
            turns.Bind(world, slime);

            AddBeastCorpse(world.Floor, cell, CreatureKind.Rat);
            AddBeastCorpse(world.Floor, cell, CreatureKind.Cat);
            AddBeastCorpse(world.Floor, cell, CreatureKind.Dog);
            Assert.AreEqual(3, world.Floor.GetCorpses(cell).Count);

            Assert.IsTrue(turns.TryDevourCorpse(0)); // rat finishes on first pulse
            Assert.AreEqual(7, slime.Volume.UnitCount);
            Assert.IsTrue(turns.TryDevourCorpse(0)); // cat: timer only
            Assert.AreEqual(7, slime.Volume.UnitCount);
            Assert.IsTrue(turns.TryWait()); // cat finish bulk +2
            Assert.AreEqual(9, slime.Volume.UnitCount);
            Assert.IsFalse(slime.IsDigesting);

            Assert.IsTrue(turns.TryDevourCorpse(0)); // dog pulse 1/3
            Assert.AreEqual(9, slime.Volume.UnitCount);
            Assert.IsTrue(slime.IsDigesting);

            Assert.IsTrue(turns.TryWait()); // pulse 2/3
            Assert.AreEqual(9, slime.Volume.UnitCount);
            Assert.IsTrue(slime.IsDigesting);

            Assert.IsTrue(turns.TryWait()); // pulse 3/3 bulk +1 discard 2
            Assert.AreEqual(Volume.Capacity, slime.Volume.UnitCount);
            Assert.IsFalse(slime.IsDigesting);
            Assert.AreEqual(6, slime.Volume.CountOf<Water>());
            Assert.AreEqual(4, slime.Volume.CountOf<Blood>());
        }

        [Test]
        public void SecondDevour_BlockedWhileDigesting()
        {
            var cell = Vector2Int.zero;
            var (slime, turns, floor) = SetupEmptySlimeOnCorpses(cell);

            Assert.IsTrue(turns.TryDevourCorpse(1)); // cat
            Assert.IsTrue(slime.IsDigesting);
            Assert.IsFalse(turns.TryDevourCorpse(0)); // rat still on floor
            Assert.AreEqual(1, floor.GetCorpses(cell).Count);
            Assert.AreEqual(CreatureKind.Rat, floor.GetCorpses(cell)[0].Kind);
        }

        [Test]
        public void Digesting_PulsesAfterEarlierStatus_Fifo()
        {
            var cell = Vector2Int.zero;
            var world = new World(3, 3, TerrainType.Grass);
            var slime = SpawnObject("Slime").AddComponent<Slime>();
            slime.PlaceOn(cell);
            slime.Volume.Clear();
            for (var i = 0; i < 4; i++)
            {
                slime.Volume.Add(new Water());
            }

            slime.SetMaxHitPoints(0);
            slime.RefreshVolumeStatuses();

            var turns = SpawnObject("Turns").AddComponent<TurnManager>();
            turns.Rng = new FixedRng();
            turns.Bind(world, slime);

            slime.AddStatus(new Burning(duration: 1));
            AddBeastCorpse(world.Floor, cell, CreatureKind.Rat);
            Assert.IsTrue(turns.TryDevourCorpse(0));
            // Same turn-start pass: Burning first (tip -1), then Digesting finishes (+1 blood).
            Assert.IsFalse(slime.IsDigesting);
            Assert.IsNull(slime.FindStatus<Burning>());
            Assert.AreEqual(4, slime.Volume.UnitCount);
            Assert.AreEqual(3, slime.Volume.CountOf<Water>());
            Assert.AreEqual(1, slime.Volume.CountOf<Blood>());
        }

        [Test]
        public void LastVolumeHit_KillsSlime_EvenWhileDigestingCorpse()
        {
            var cell = Vector2Int.zero;
            var slime = SpawnObject("Slime").AddComponent<Slime>();
            slime.PlaceOn(cell);
            slime.Volume.Clear();
            slime.Volume.Add(new Blood());
            slime.SetMaxHitPoints(0);
            slime.RefreshVolumeStatuses();

            var rat = SpawnObject("Rat").AddComponent<Rat>();
            rat.PlaceOn(cell);
            rat.Volume.Clear();
            Rat.FillStarting(rat.Volume);
            rat.SetMaxHitPoints(3);
            rat.BecomeCorpse();
            Assert.IsTrue(Digesting.CanBegin(slime.Volume, rat));
            Assert.IsTrue(slime.AddStatus(new Digesting(rat)));
            Assert.IsTrue(slime.IsDigesting);
            Assert.AreEqual(1, slime.Volume.UnitCount);
            Assert.AreEqual(1, slime.FindStatus<Digesting>().Meal.Volume.UnitCount);

            var dog = SpawnObject("Dog").AddComponent<Dog>();
            dog.PlaceOn(cell + Vector2Int.right);
            dog.SetMaxHitPoints(10);
            Assert.IsTrue(Combat.Attack(dog, slime));

            Assert.IsFalse(slime.IsAlive);
            Assert.AreEqual(0, slime.Volume.UnitCount);
            Assert.IsTrue(slime.IsDigesting);
            Assert.AreSame(rat, slime.FindStatus<Digesting>().Meal);
            Assert.AreEqual(1, slime.FindStatus<Digesting>().Meal.Volume.UnitCount);
        }

        [Test]
        public void Softcore_MidDigestDeath_AbortsMealAndRefillsWater()
        {
            GameSettings.Mode = GameMode.Softcore;
            var cell = Vector2Int.zero;
            var world = new World(3, 3, TerrainType.Grass);
            var slime = SpawnObject("Slime").AddComponent<Slime>();
            slime.PlaceOn(cell);
            slime.Volume.Clear();
            for (var i = 0; i < 4; i++)
            {
                slime.Volume.Add(new Oil());
            }

            slime.SetMaxHitPoints(0);
            slime.RefreshVolumeStatuses();

            var turns = SpawnObject("Turns").AddComponent<TurnManager>();
            turns.Rng = new FixedRng();
            turns.Bind(world, slime);

            var cat = SpawnObject("Cat").AddComponent<Cat>();
            cat.PlaceOn(cell);
            cat.Volume.Clear();
            Cat.FillStarting(cat.Volume);
            cat.SetMaxHitPoints(5);
            cat.BecomeCorpse();
            Assert.IsTrue(slime.AddStatus(new Digesting(cat)));
            Assert.IsTrue(slime.IsDigesting);

            slime.Die();
            Assert.IsTrue(SoftcoreRevive.TryContinue(turns, slime, world));

            Assert.IsTrue(slime.IsAlive);
            Assert.IsFalse(slime.IsDigesting);
            Assert.AreEqual(SoftcoreRevive.SpawnPoolWater, slime.Volume.UnitCount);
            Assert.AreEqual(SoftcoreRevive.SpawnPoolWater, slime.Volume.CountOf<Water>());
            Assert.IsTrue(cat == null || cat.Equals(null));
        }

        [Test]
        public void CellMenu_ShowsAllCorpses_DisablesTooLarge()
        {
            var cell = Vector2Int.zero;
            var slime = SpawnObject("Slime").AddComponent<Slime>();
            slime.PlaceOn(cell);
            slime.Volume.Clear();
            slime.Volume.Add(new Water());
            slime.Volume.Add(new Water());
            slime.SetMaxHitPoints(0);
            slime.RefreshVolumeStatuses();

            var rat = SpawnObject("Rat").AddComponent<Rat>();
            rat.PlaceOn(cell);
            rat.Volume.Clear();
            Rat.FillStarting(rat.Volume);
            rat.BecomeCorpse();

            var dog = SpawnObject("Dog").AddComponent<Dog>();
            dog.PlaceOn(cell);
            dog.Volume.Clear();
            Dog.FillStarting(dog.Volume);
            dog.BecomeCorpse();

            var menu = SpawnObject("Menu").AddComponent<CellMenu>();
            menu.Open(
                new CellMenuContext(cell, slime, null, null, new Creature[] { rat, dog }, null)
            );

            Assert.IsTrue(ClickMenuLabel(I18n.Get(TextKey.MenuDevour)));

            var ratButton = FindButtonWithLabel(CellMenu.CorpseChoiceLabel(rat));
            var dogButton = FindButtonWithLabel(CellMenu.CorpseChoiceLabel(dog));
            Assert.IsNotNull(ratButton);
            Assert.IsNotNull(dogButton);
            Assert.IsTrue(ratButton.interactable);
            Assert.IsFalse(dogButton.interactable);
            Assert.AreEqual(
                CardUi.SafeFormat(
                    TextKey.MenuCorpseChoice,
                    "{0} · {1} vol · {2}/{3} turns",
                    CardUi.CreatureName(CreatureKind.Rat),
                    1,
                    rat.DecayTurnsLeft,
                    rat.MaxHitPoints
                ),
                CellMenu.CorpseChoiceLabel(rat)
            );
            Assert.AreEqual(
                CardUi.SafeFormat(
                    TextKey.MenuCorpseChoice,
                    "{0} · {1} vol · {2}/{3} turns",
                    CardUi.CreatureName(CreatureKind.Dog),
                    3,
                    dog.DecayTurnsLeft,
                    dog.MaxHitPoints
                ),
                CellMenu.CorpseChoiceLabel(dog)
            );
        }

        private static bool ClickMenuLabel(string label)
        {
            foreach (var text in Object.FindObjectsByType<Text>())
            {
                if (text == null || text.text != label)
                {
                    continue;
                }

                var button = text.GetComponentInParent<Button>();
                if (button == null || !button.interactable)
                {
                    continue;
                }

                button.onClick.Invoke();
                return true;
            }

            return false;
        }

        private static Button FindButtonWithLabel(string label)
        {
            foreach (var text in Object.FindObjectsByType<Text>())
            {
                if (text != null && text.text == label)
                {
                    return text.GetComponentInParent<Button>();
                }
            }

            return null;
        }

        private (Slime slime, TurnManager turns, Floor floor) SetupEmptySlimeOnCorpses(
            Vector2Int cell
        )
        {
            var world = new World(3, 3, TerrainType.Grass);
            var slime = SpawnObject("Slime").AddComponent<Slime>();
            slime.PlaceOn(cell);
            slime.Volume.Clear();
            slime.Volume.Add(new Water());
            slime.Volume.Add(new Water());
            slime.SetMaxHitPoints(0);
            slime.RefreshVolumeStatuses();

            var turns = SpawnObject("Turns").AddComponent<TurnManager>();
            turns.Rng = new FixedRng();
            turns.Bind(world, slime);

            AddBeastCorpse(world.Floor, cell, CreatureKind.Rat);
            AddBeastCorpse(world.Floor, cell, CreatureKind.Cat);
            return (slime, turns, world.Floor);
        }

        private void AddBeastCorpse(Floor floor, Vector2Int cell, CreatureKind kind)
        {
            Creature creature;
            switch (kind)
            {
                case CreatureKind.Rat:
                    creature = SpawnObject("Rat").AddComponent<Rat>();
                    creature.PlaceOn(cell);
                    creature.Volume.Clear();
                    Rat.FillStarting(creature.Volume);
                    creature.SetMaxHitPoints(3);
                    break;
                case CreatureKind.Cat:
                    creature = SpawnObject("Cat").AddComponent<Cat>();
                    creature.PlaceOn(cell);
                    creature.Volume.Clear();
                    Cat.FillStarting(creature.Volume);
                    creature.SetMaxHitPoints(5);
                    break;
                case CreatureKind.Dog:
                    creature = SpawnObject("Dog").AddComponent<Dog>();
                    creature.PlaceOn(cell);
                    creature.Volume.Clear();
                    Dog.FillStarting(creature.Volume);
                    creature.SetMaxHitPoints(10);
                    break;
                default:
                    Assert.Fail($"Unexpected kind {kind}");
                    return;
            }

            creature.BecomeCorpse();
            floor.AddCorpse(creature);
        }

        private GameObject SpawnObject(string name)
        {
            var go = new GameObject(name);
            spawned.Add(go);
            return go;
        }
    }
}
