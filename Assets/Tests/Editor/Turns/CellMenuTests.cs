using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace SlimesRevenge.Tests
{
    public class CellMenuTests
    {
        private readonly System.Collections.Generic.List<GameObject> spawned =
            new System.Collections.Generic.List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            if (PopupHost.Instance != null)
            {
                PopupHost.Instance.Close();
                Object.DestroyImmediate(PopupHost.Instance.gameObject);
            }

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
        public void ActorCell_ShowsMessAndInfo_NotAttack()
        {
            var cell = Vector2Int.zero;
            var slime = SpawnSlime(cell, 2);
            var menu = SpawnObject("Menu").AddComponent<CellMenu>();
            menu.Open(
                new CellMenuContext(cell, slime, null, null, System.Array.Empty<Creature>(), null)
            );

            Assert.IsTrue(MenuHasLabel(I18n.Get(TextKey.MenuMess)));
            Assert.IsTrue(MenuHasLabel(I18n.Get(TextKey.MenuInfo)));
            Assert.IsFalse(MenuHasLabel(I18n.Get(TextKey.MenuAttack)));
            Assert.IsFalse(MenuHasLabel(I18n.Get(TextKey.MenuCollect)));
            Assert.IsFalse(MenuHasLabel(I18n.Get(TextKey.MenuDevour)));
            Assert.IsTrue(FindButtonWithLabel(I18n.Get(TextKey.MenuMess)).interactable);
        }

        [Test]
        public void LastVolumeUnit_DisablesMessAndAttack()
        {
            var slimeCell = Vector2Int.zero;
            var foeCell = Vector2Int.right;
            var slime = SpawnSlime(slimeCell, 1);
            var rat = TestCreatures.Cowardly(spawned, foeCell);

            Assert.IsFalse(slime.Volume.CanSpend);
            Assert.IsTrue(slime.IsAlive);

            var selfMenu = SpawnObject("SelfMenu").AddComponent<CellMenu>();
            selfMenu.Open(
                new CellMenuContext(
                    slimeCell,
                    slime,
                    null,
                    null,
                    System.Array.Empty<Creature>(),
                    null
                )
            );
            var mess = FindButtonWithLabel(I18n.Get(TextKey.MenuMess));
            Assert.IsNotNull(mess);
            Assert.IsFalse(mess.interactable);

            var attackMenu = SpawnObject("AttackMenu").AddComponent<CellMenu>();
            attackMenu.Open(
                new CellMenuContext(foeCell, slime, rat, null, System.Array.Empty<Creature>(), null)
            );
            var attack = FindButtonWithLabel(I18n.Get(TextKey.MenuAttack));
            Assert.IsNotNull(attack);
            Assert.IsFalse(attack.interactable);
            Assert.IsFalse(ClickMenuLabel(I18n.Get(TextKey.MenuAttack)));
        }

        [Test]
        public void AdjacentFoe_ShowsAttackAndInfo()
        {
            var slimeCell = Vector2Int.zero;
            var foeCell = Vector2Int.right;
            var slime = SpawnSlime(slimeCell, 2);
            var rat = TestCreatures.Cowardly(spawned, foeCell);

            var menu = SpawnObject("Menu").AddComponent<CellMenu>();
            menu.Open(
                new CellMenuContext(foeCell, slime, rat, null, System.Array.Empty<Creature>(), null)
            );

            Assert.IsTrue(MenuHasLabel(I18n.Get(TextKey.MenuAttack)));
            Assert.IsTrue(MenuHasLabel(I18n.Get(TextKey.MenuInfo)));
            Assert.IsFalse(MenuHasLabel(I18n.Get(TextKey.MenuMess)));
            Assert.IsTrue(FindButtonWithLabel(I18n.Get(TextKey.MenuAttack)).interactable);
        }

        [Test]
        public void DiagonalFoe_ShowsAttack()
        {
            var slimeCell = Vector2Int.zero;
            var foeCell = new Vector2Int(1, 1);
            var slime = SpawnSlime(slimeCell, 2);
            var dog = TestCreatures.Body(spawned, foeCell).WithHp(10);

            var menu = SpawnObject("Menu").AddComponent<CellMenu>();
            menu.Open(
                new CellMenuContext(foeCell, slime, dog, null, System.Array.Empty<Creature>(), null)
            );

            Assert.IsTrue(MenuHasLabel(I18n.Get(TextKey.MenuAttack)));
            Assert.IsTrue(MenuHasLabel(I18n.Get(TextKey.MenuInfo)));
            Assert.IsTrue(FindButtonWithLabel(I18n.Get(TextKey.MenuAttack)).interactable);
        }

        [Test]
        public void Info_OneInspectable_OpensCreatureCard()
        {
            var cell = Vector2Int.zero;
            var slime = SpawnSlime(cell, 2);
            var menu = SpawnObject("Menu").AddComponent<CellMenu>();
            menu.Open(
                new CellMenuContext(cell, slime, null, null, System.Array.Empty<Creature>(), null)
            );

            Assert.IsTrue(ClickMenuLabel(I18n.Get(TextKey.MenuInfo)));
            Assert.IsNotNull(PopupHost.Instance);
            Assert.IsTrue(PopupHost.Instance.IsOpen);
            Assert.AreEqual(typeof(CreatureCard), PopupHost.Instance.PeekType);
            PopupHost.Instance.Close();
        }

        [Test]
        public void Info_MultipleInspectables_ShowsFlatPicker()
        {
            var cell = Vector2Int.zero;
            var slime = SpawnSlime(cell, 2);
            var world = new World(3, 3, TerrainType.Grass);
            Assert.IsTrue(world.Floor.TryPlacePuddle(cell, new Water()));

            var menu = SpawnObject("Menu").AddComponent<CellMenu>();
            menu.Open(
                new CellMenuContext(
                    cell,
                    slime,
                    null,
                    world.Floor.GetPuddle(cell),
                    System.Array.Empty<Creature>(),
                    null
                )
            );

            Assert.IsTrue(ClickMenuLabel(I18n.Get(TextKey.MenuInfo)));
            Assert.IsTrue(
                MenuHasLabel(
                    I18n.Format(TextKey.MenuPuddleChoice, I18n.Get(TextKey.SubstanceWater))
                )
            );
            Assert.IsTrue(MenuHasLabel(I18n.Creature(CreatureKind.Slime)));
        }

        [Test]
        public void Devour_OneCorpse_ActsImmediately()
        {
            var cell = Vector2Int.zero;
            var world = new World(3, 3, TerrainType.Grass);
            var slime = SpawnSlime(cell, 2);
            var turns = SpawnObject("Turns").AddComponent<TurnManager>();
            turns.Rng = new FixedRng();
            turns.Bind(world, slime);

            var rat = TestCreatures.Cowardly(spawned, cell);
            rat.BecomeCorpse();
            world.Floor.AddCorpse(rat);

            var menu = SpawnObject("Menu").AddComponent<CellMenu>();
            menu.Open(
                new CellMenuContext(cell, slime, null, null, world.Floor.GetCorpses(cell), turns)
            );

            Assert.IsTrue(ClickMenuLabel(I18n.Get(TextKey.MenuDevour)));
            // 1-unit rat finishes on the devour Act turn-start pulse.
            Assert.IsFalse(slime.IsDigesting);
            Assert.AreEqual(3, slime.Volume.UnitCount);
        }

        [Test]
        public void Info_LivingOpensCreatureCard()
        {
            var slimeCell = Vector2Int.zero;
            var foeCell = Vector2Int.right;
            var slime = SpawnSlime(slimeCell, 1);
            var rat = TestCreatures.Cowardly(spawned, foeCell);
            rat.AddStatus(new Wet());

            var menu = SpawnObject("Menu").AddComponent<CellMenu>();
            menu.Open(
                new CellMenuContext(foeCell, slime, rat, null, System.Array.Empty<Creature>(), null)
            );

            Assert.IsTrue(ClickMenuLabel(I18n.Get(TextKey.MenuInfo)));
            Assert.IsTrue(PopupHost.Instance.IsOpen);
            Assert.AreEqual(typeof(CreatureCard), PopupHost.Instance.PeekType);
            PopupHost.Instance.Close();
        }

        [Test]
        public void Info_PuddleOpensSubstanceCard()
        {
            var cell = Vector2Int.zero;
            var slime = SpawnSlime(cell, 2);
            var world = new World(3, 3, TerrainType.Grass);
            Assert.IsTrue(world.Floor.TryPlacePuddle(cell, new Oil()));
            var puddle = world.Floor.GetPuddle(cell);

            // Direct target (skip picker)
            var menu = SpawnObject("Menu").AddComponent<CellMenu>();
            menu.ShowInfoPanel(CellInspectable.FromPuddle(puddle));
            Assert.IsTrue(PopupHost.Instance.IsOpen);
            Assert.AreEqual(typeof(SubstanceCard), PopupHost.Instance.PeekType);
            PopupHost.Instance.Close();
        }

        private Slime SpawnSlime(Vector2Int cell, int waterUnits)
        {
            var slime = SpawnObject("Slime").AddComponent<Slime>();
            slime.PlaceOn(cell);
            slime.Volume.Clear();
            for (var i = 0; i < waterUnits; i++)
            {
                slime.Volume.Add(new Water());
            }

            slime.SetMaxHitPoints(0);
            slime.RefreshVolumeStatuses();
            return slime;
        }

        private static bool MenuHasLabel(string label)
        {
            foreach (var text in Object.FindObjectsByType<Text>())
            {
                if (text != null && text.text == label)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool MenuHasLabelContaining(string fragment)
        {
            foreach (var text in Object.FindObjectsByType<Text>())
            {
                if (text != null && text.text != null && text.text.Contains(fragment))
                {
                    return true;
                }
            }

            return false;
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

        private GameObject SpawnObject(string name)
        {
            var go = new GameObject(name);
            spawned.Add(go);
            return go;
        }
    }
}
