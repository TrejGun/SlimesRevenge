using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SlimesRevenge.Tests
{
    public class ActionLogTests
    {
        private readonly List<GameObject> spawned = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            I18n.Use(I18n.English);
            ActionLog.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            ActionLog.Clear();
            if (PopupHost.Instance != null)
            {
                var hostGo = PopupHost.Instance.gameObject;
                PopupHost.Instance.Close();
                Object.DestroyImmediate(hostGo);
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
        public void AnnounceRunStarted_CampaignHardcore_WritesBanner()
        {
            GameSettings.Mode = GameMode.Hardcore;
            ActionLog.AnnounceRunStarted(RunKind.Campaign);

            var dump = ActionLog.Dump();
            StringAssert.Contains("Campaign started in Hardcore mode", dump);
            Assert.AreEqual(1, ActionLog.Entries.Count);
            Assert.AreEqual(ActionLogStyle.Banner, ActionLog.Entries[0].HeadlineLine.Style);
        }

        [Test]
        public void AnnounceRunStarted_CampaignSoftcore_UsesSoftcoreLabel()
        {
            GameSettings.Mode = GameMode.Softcore;
            try
            {
                ActionLog.AnnounceRunStarted(RunKind.Campaign);
                StringAssert.Contains("Campaign started in Softcore mode", ActionLog.Dump());
            }
            finally
            {
                GameSettings.Mode = GameMode.Hardcore;
            }
        }

        [Test]
        public void AnnounceRunStarted_Duel_IgnoresMode()
        {
            GameSettings.Mode = GameMode.Softcore;
            try
            {
                ActionLog.AnnounceRunStarted(RunKind.Duel);
                Assert.AreEqual("Duel started", ActionLog.Dump().Trim());
            }
            finally
            {
                GameSettings.Mode = GameMode.Hardcore;
            }
        }

        [Test]
        public void Format_BuildsClickablePartsFromTemplate()
        {
            var line = ActionLogLine.Format(
                "{0} hits {1} with {2}",
                ActionLogPart.Creature(CreatureKind.Slime),
                ActionLogPart.Creature(CreatureKind.Dog),
                ActionLogPart.Substance(new Oil())
            );

            Assert.AreEqual("Slime hits Dog with Oil", line.Text);
            Assert.AreEqual(5, line.Parts.Count);
            Assert.AreEqual(ActionLogLinkKind.Creature, line.Parts[0].Kind);
            Assert.AreEqual(ActionLogLinkKind.None, line.Parts[1].Kind);
            Assert.AreEqual(" hits ", line.Parts[1].Text);
            Assert.AreEqual(ActionLogLinkKind.Creature, line.Parts[2].Kind);
            Assert.AreEqual(ActionLogLinkKind.Substance, line.Parts[4].Kind);
            Assert.IsTrue(line.HasLinkKind(ActionLogLinkKind.Substance));
            Assert.IsTrue(line.HasLinkKind(ActionLogLinkKind.Creature));
        }

        [Test]
        public void FormatDamageDetail_SkipsZeros()
        {
            Assert.IsNull(ActionLog.FormatDamageDetail(0, 0, 0));
            Assert.AreEqual("Loses HP 2", ActionLog.FormatDamageDetail(0, 2, 0));
            Assert.AreEqual("Loses armor 1, HP 2", ActionLog.FormatDamageDetail(1, 2, 0));
            Assert.AreEqual("Loses volume 1", ActionLog.FormatDamageDetail(0, 0, 1));
        }

        [Test]
        public void LinkRouter_CreatesPopupHost_WhenMissing()
        {
            Assert.IsNull(PopupHost.Instance);

            var line = new ActionLogLine("Oil", new Oil());
            ActionLogLinkRouter.Open(line);

            Assert.IsNotNull(PopupHost.Instance);
            Assert.IsTrue(PopupHost.Instance.IsOpen);
            Assert.AreEqual(typeof(SubstanceCard), PopupHost.Instance.PeekType);
            spawned.Add(PopupHost.Instance.gameObject);
        }

        [Test]
        public void NestedBegin_KeepsOuterDetailsAfterInnerEnd()
        {
            ActionLog.Begin("outer");
            ActionLog.Begin("inner");
            ActionLog.Detail("inner detail");
            ActionLog.End();
            ActionLog.Detail("outer detail");
            ActionLog.End();

            Assert.AreEqual(2, ActionLog.Entries.Count);
            CollectionAssert.AreEqual(new[] { "outer detail" }, DetailTexts(ActionLog.Entries[0]));
            CollectionAssert.AreEqual(new[] { "inner detail" }, DetailTexts(ActionLog.Entries[1]));
            Assert.IsFalse(ActionLog.HasOpenGroup);
        }

        [Test]
        public void NestedBegin_OuterFinallyDoesNotDropInnerDetails()
        {
            ActionLog.Begin("outer");
            try
            {
                ActionLog.Begin("inner");
                try
                {
                    ActionLog.Detail("kept");
                }
                finally
                {
                    ActionLog.End();
                }

                ActionLog.Detail("also kept");
            }
            finally
            {
                ActionLog.End();
            }

            StringAssert.Contains("kept", ActionLog.Dump());
            StringAssert.Contains("also kept", ActionLog.Dump());
            Assert.AreEqual("also kept", ActionLog.Entries[0].Details[0].Text);
            Assert.AreEqual("kept", ActionLog.Entries[1].Details[0].Text);
        }

        [Test]
        public void End_DiscardIfEmpty_DropsHeadlineOnlyGroup()
        {
            ActionLog.Begin("empty turn");
            ActionLog.End(discardIfEmpty: true);
            Assert.AreEqual(0, ActionLog.Entries.Count);
            Assert.IsFalse(ActionLog.HasOpenGroup);
        }

        [Test]
        public void OilStrike_LogsHeadlineDamageAndStatuses_WithoutDuplicateSubstanceLine()
        {
            var slime = Spawn<Slime>(Vector2Int.zero);
            slime.Volume.Clear();
            slime.Volume.Add(new Oil());
            slime.Volume.Add(new Oil());
            slime.SetMaxHitPoints(0);
            slime.RefreshVolumeStatuses();

            var dog = Spawn<Dog>(Vector2Int.right);
            dog.Volume.Clear();
            Dog.FillStarting(dog.Volume);
            dog.SetMaxHitPoints(10);

            Assert.IsTrue(Combat.Attack(slime, dog, new Oil()));

            var dump = ActionLog.Dump();
            StringAssert.Contains("Slime hits Dog with Oil", dump);
            StringAssert.Contains("Loses HP 1", dump);
            StringAssert.Contains("Dog gains", dump);
            StringAssert.Contains("Instability", dump);
            StringAssert.Contains("Flammable", dump);
            Assert.IsFalse(ActionLog.HasOpenGroup);

            var entry = ActionLog.Entries[0];
            Assert.IsTrue(entry.HeadlineLine.HasLinkKind(ActionLogLinkKind.Substance));
            Assert.IsNotNull(entry.HeadlineLine.SubstanceSnapshot);
            Assert.IsFalse(
                DetailExists(
                    entry,
                    d => d.LinkKind == ActionLogLinkKind.Substance && d.Text == "Oil"
                )
            );
        }

        [Test]
        public void AcidOnScorpion_LogsArmorAndHp()
        {
            var slime = Spawn<Slime>(Vector2Int.zero);
            slime.Volume.Clear();
            slime.Volume.Add(new Acid());
            slime.Volume.Add(new Acid());
            slime.SetMaxHitPoints(0);
            slime.RefreshVolumeStatuses();

            var scorpion = SpawnScorpion(Vector2Int.right);

            Assert.IsTrue(Combat.Attack(slime, scorpion, new Acid()));

            var dump = ActionLog.Dump();
            StringAssert.Contains("Slime hits Scorpion with Acid", dump);
            StringAssert.Contains("Loses armor 1, HP 1", dump);
            StringAssert.Contains($"Scorpion gains {I18n.Get(TextKey.StatusCorroding)}", dump);
            Assert.AreEqual(0, scorpion.Armor);
            Assert.AreEqual(2, scorpion.HitPoints);
        }

        [Test]
        public void WaterOnScorpion_ArmorBlocksPower_NoHpLossLine()
        {
            var slime = Spawn<Slime>(Vector2Int.zero);
            slime.Volume.Clear();
            slime.Volume.Add(new Water());
            slime.Volume.Add(new Water());
            slime.SetMaxHitPoints(0);
            slime.RefreshVolumeStatuses();

            var scorpion = SpawnScorpion(Vector2Int.right);
            Assert.IsTrue(Combat.Attack(slime, scorpion, new Water()));

            var dump = ActionLog.Dump();
            StringAssert.Contains("Slime hits Scorpion with Water", dump);
            StringAssert.DoesNotContain("Loses", dump);
            StringAssert.Contains("Wet", dump);
            Assert.AreEqual(3, scorpion.HitPoints);
            Assert.AreEqual(1, scorpion.Armor);
        }

        [Test]
        public void ScorpionAcidPuddleThenTurn_LogsCorrodingArmorStrip()
        {
            var floor = new Floor();
            Assert.IsTrue(floor.TryPlacePuddle(Vector2Int.zero, new Acid()));

            var scorpion = SpawnScorpion(Vector2Int.zero);
            ActionLog.Clear();

            floor.ApplyContact(scorpion);
            StringAssert.Contains("Scorpion steps into Acid puddle", ActionLog.Dump());
            StringAssert.Contains(
                $"Scorpion gains {I18n.Get(TextKey.StatusCorroding)}",
                ActionLog.Dump()
            );
            Assert.AreEqual(1, scorpion.Armor);

            RunTurnStart(scorpion);

            var dump = ActionLog.Dump();
            StringAssert.Contains("Scorpion's turn", dump);
            StringAssert.Contains(I18n.Get(TextKey.StatusCorroding), dump);
            StringAssert.Contains("Loses armor 1", dump);
            Assert.AreEqual(0, scorpion.Armor);
            Assert.AreEqual(3, scorpion.HitPoints);
        }

        [Test]
        public void MeleeOnSlime_LogsVolume()
        {
            var slime = Spawn<Slime>(Vector2Int.zero);
            slime.Volume.Clear();
            slime.Volume.Add(new Water());
            slime.Volume.Add(new Water());
            slime.SetMaxHitPoints(0);
            slime.RefreshVolumeStatuses();

            var dog = Spawn<Dog>(Vector2Int.right);
            dog.SetMaxHitPoints(10);

            Assert.IsTrue(Combat.Attack(dog, slime));

            var dump = ActionLog.Dump();
            StringAssert.Contains("Dog hits Slime", dump);
            StringAssert.Contains("Loses volume 1", dump);
            StringAssert.DoesNotContain("HP ", dump);
        }

        [Test]
        public void ScorpionMelee_LogsPoisonousMarkerBeforePoisonedGain()
        {
            var scorpion = SpawnScorpion(Vector2Int.zero);
            var rat = Spawn<Rat>(Vector2Int.right);
            rat.SetMaxHitPoints(10);
            ActionLog.Clear();

            Assert.IsTrue(Combat.Attack(scorpion, rat));

            Assert.AreEqual(1, ActionLog.Entries.Count);
            var dump = ActionLog.Dump();
            StringAssert.Contains("Scorpion hits Rat", dump);
            StringAssert.Contains(I18n.Get(TextKey.StatusPoisonous), dump);
            StringAssert.Contains($"Rat gains {I18n.Get(TextKey.StatusPoisoned)}", dump);

            var texts = DetailTexts(ActionLog.Entries[0]);
            var poisonousIndex = texts.IndexOf(I18n.Get(TextKey.StatusPoisonous));
            Assert.GreaterOrEqual(poisonousIndex, 0);
            Assert.Greater(
                texts.FindIndex(poisonousIndex, t => t.Contains(I18n.Get(TextKey.StatusPoisoned))),
                poisonousIndex
            );

            var marker = ActionLog.Entries[0].Details[poisonousIndex];
            Assert.AreEqual(ActionLogLinkKind.Status, marker.LinkKind);
            Assert.IsTrue(marker.Status.HasValue);
        }

        [Test]
        public void RatHitsLavaSlime_RetaliationStaysInStrikeGroup()
        {
            var slime = Spawn<Slime>(Vector2Int.zero);
            slime.Volume.Clear();
            for (var i = 0; i < 8; i++)
            {
                slime.Volume.Add(new Lava());
            }

            slime.SetMaxHitPoints(0);
            slime.RefreshVolumeStatuses();
            Assert.IsNotNull(slime.FindStatus<Retaliation>());

            var rat = Spawn<Rat>(Vector2Int.right);
            rat.SetMaxHitPoints(10);
            ActionLog.Clear();

            Assert.IsTrue(Combat.Attack(rat, slime));

            Assert.AreEqual(1, ActionLog.Entries.Count);
            var dump = ActionLog.Dump();
            StringAssert.Contains("Rat hits Slime", dump);
            StringAssert.Contains("Loses volume 1", dump);
            StringAssert.Contains(I18n.Get(TextKey.StatusRetaliation), dump);
            StringAssert.Contains($"Rat gains {I18n.Get(TextKey.StatusBurning)}", dump);

            var texts = DetailTexts(ActionLog.Entries[0]);
            var retaliationIndex = texts.IndexOf(I18n.Get(TextKey.StatusRetaliation));
            Assert.GreaterOrEqual(retaliationIndex, 0);
            Assert.Greater(
                texts.FindIndex(retaliationIndex, t => t.Contains("Burning")),
                retaliationIndex
            );

            var marker = ActionLog.Entries[0].Details[retaliationIndex];
            Assert.AreEqual(ActionLogLinkKind.Status, marker.LinkKind);
            Assert.IsTrue(marker.Status.HasValue);
        }

        [Test]
        public void BatMeleeBloodTip_LogsVampirismHeal()
        {
            var bat = Spawn<Bat>(Vector2Int.zero);
            bat.Damage(1);
            Assert.AreEqual(2, bat.HitPoints);

            var slime = Spawn<Slime>(Vector2Int.right);
            slime.Volume.Clear();
            slime.Volume.Add(new Blood());
            slime.SetMaxHitPoints(0);
            slime.RefreshVolumeStatuses();
            ActionLog.Clear();

            Assert.IsTrue(Combat.Attack(bat, slime));

            var dump = ActionLog.Dump();
            StringAssert.Contains("Bat hits Slime", dump);
            StringAssert.Contains(I18n.Get(TextKey.StatusVampirism), dump);
            StringAssert.Contains("Heals 1 HP", dump);
            Assert.AreEqual(3, bat.HitPoints);
        }

        [Test]
        public void MessOil_LogsSpilledSubstanceAndDominanceChange()
        {
            var player = Spawn<Slime>(Vector2Int.zero);
            player.Volume.Clear();
            for (var i = 0; i < 8; i++)
            {
                player.Volume.Add(new Oil());
            }

            player.Volume.Add(new Water());
            player.Volume.Add(new Water());
            player.RefreshVolumeStatuses();
            Assert.IsNotNull(player.FindStatus<Flammable>());

            var turns = SpawnObject("Turns").AddComponent<TurnManager>();
            turns.Rng = new FixedRng();
            turns.Bind(World.CreateGrass(3), player);
            ActionLog.Clear();

            Assert.IsTrue(turns.TryMakeMess(new Oil()));
            Assert.IsNotNull(turns.Session.World.Floor.GetPuddle(player.Cell));

            var dump = ActionLog.Dump();
            StringAssert.Contains("Slime spills Oil", dump);
            // 7 oil / 9 ≈ 77% — dominance lost.
            StringAssert.Contains(I18n.Get(TextKey.LogDominanceLost), dump);

            var mess = ActionLog.Entries[0];
            Assert.IsTrue(mess.HeadlineLine.HasLinkKind(ActionLogLinkKind.Substance));
            Assert.IsNotNull(mess.HeadlineLine.SubstanceSnapshot);
        }

        [Test]
        public void DigestionFinish_LogsUniqueAbsorbedSubstances()
        {
            var player = Spawn<Slime>(Vector2Int.zero);
            player.Volume.Clear();
            player.Volume.Add(new Water());
            player.Volume.Add(new Water());
            player.Volume.Add(new Water());
            player.SetMaxHitPoints(0);
            player.RefreshVolumeStatuses();

            var turns = SpawnObject("Turns").AddComponent<TurnManager>();
            turns.Rng = new FixedRng();
            turns.Bind(World.CreateGrass(3), player);

            var dog = SpawnObject("DogCorpse").AddComponent<Dog>();
            dog.PlaceOn(player.Cell);
            dog.Volume.Clear();
            dog.Volume.Add(new Water());
            dog.Volume.Add(new Oil());
            dog.Volume.Add(new Blood());
            dog.SetMaxHitPoints(10);
            dog.BecomeCorpse();
            turns.Session.World.Floor.AddCorpse(dog);
            ActionLog.Clear();

            Assert.IsTrue(turns.TryDevourCorpse(0));
            Assert.IsTrue(player.IsDigesting);

            var beginDump = ActionLog.Dump();
            StringAssert.Contains("Slime begins digesting Dog", beginDump);
            StringAssert.Contains(I18n.Get(TextKey.StatusDigesting), beginDump);
            var beginEntry = FindEntryContaining("Slime begins digesting Dog");
            Assert.IsNotNull(beginEntry);
            Assert.IsTrue(
                DetailExists(
                    beginEntry,
                    d => d.HasLinkKind(ActionLogLinkKind.Status) && d.Text.Contains("Digesting")
                )
            );

            Assert.IsTrue(turns.TryWait());
            Assert.IsTrue(player.IsDigesting);
            Assert.IsTrue(turns.TryWait());
            Assert.IsFalse(player.IsDigesting);

            var dump = ActionLog.Dump();
            StringAssert.Contains("Finishes digesting Dog", dump);
            StringAssert.Contains("Blood", dump);
            StringAssert.Contains("Oil", dump);
            StringAssert.Contains("Water", dump);

            var finishEntry = FindEntryContaining("Finishes digesting Dog");
            Assert.IsNotNull(finishEntry);
            var substanceLinks = FindDetails(
                finishEntry,
                d => d.LinkKind == ActionLogLinkKind.Substance
            );
            Assert.AreEqual(3, substanceLinks.Count);
            Assert.AreEqual("Blood", substanceLinks[0].Text);
            Assert.AreEqual("Oil", substanceLinks[1].Text);
            Assert.AreEqual("Water", substanceLinks[2].Text);
        }

        [Test]
        public void DigestionFinish_CrossingOilDominance_LogsDominance()
        {
            var player = Spawn<Slime>(Vector2Int.zero);
            player.Volume.Clear();
            for (var i = 0; i < 7; i++)
            {
                player.Volume.Add(new Oil());
            }

            player.Volume.Add(new Water());
            player.Volume.Add(new Water());
            player.SetMaxHitPoints(0);
            player.RefreshVolumeStatuses();
            Assert.IsNull(player.FindStatus<Flammable>());

            var turns = SpawnObject("Turns").AddComponent<TurnManager>();
            turns.Rng = new FixedRng();
            turns.Bind(World.CreateGrass(3), player);

            var rat = SpawnObject("RatCorpse").AddComponent<Rat>();
            rat.PlaceOn(player.Cell);
            rat.Volume.Clear();
            rat.Volume.Add(new Oil());
            rat.SetMaxHitPoints(3);
            rat.BecomeCorpse();
            turns.Session.World.Floor.AddCorpse(rat);
            ActionLog.Clear();

            Assert.IsTrue(turns.TryDevourCorpse(0));
            Assert.IsFalse(player.IsDigesting);
            Assert.IsNotNull(player.FindStatus<Flammable>());

            var dump = ActionLog.Dump();
            StringAssert.Contains("Finishes digesting Rat", dump);
            StringAssert.Contains("Oil", dump);
            StringAssert.Contains("Dominance: Oil", dump);
            StringAssert.Contains("Flammable", dump);
            StringAssert.Contains("Retaliation", dump);

            var finishEntry = FindEntryContaining("Finishes digesting Rat");
            Assert.IsNotNull(finishEntry);
            Assert.IsTrue(
                DetailExists(
                    finishEntry,
                    d => d.LinkKind == ActionLogLinkKind.Substance && d.Text == "Oil"
                )
            );
            Assert.IsTrue(DetailExists(finishEntry, d => d.Text.Contains("Dominance: Oil")));
        }

        [Test]
        public void CollectOilToEmptySlime_LogsCollectAndDominance()
        {
            var player = Spawn<Slime>(Vector2Int.zero);
            player.Volume.Clear();
            player.RefreshVolumeStatuses();

            var turns = SpawnObject("Turns").AddComponent<TurnManager>();
            turns.Rng = new FixedRng();
            turns.Bind(World.CreateGrass(3), player);
            Assert.IsTrue(turns.Session.World.Floor.TryPlacePuddle(player.Cell, new Oil()));
            ActionLog.Clear();

            Assert.IsTrue(turns.TryCollectPuddle());

            var dump = ActionLog.Dump();
            StringAssert.Contains("Slime collects Oil", dump);
            // Single unit is 100% of volume → oil becomes dominant.
            StringAssert.Contains("Dominance: Oil", dump);
            StringAssert.Contains("Flammable", dump);
            StringAssert.Contains("Retaliation", dump);
        }

        [Test]
        public void CollectOilCrossingDominanceThreshold_LogsDominanceTraits()
        {
            var player = Spawn<Slime>(Vector2Int.zero);
            player.Volume.Clear();
            for (var i = 0; i < 7; i++)
            {
                player.Volume.Add(new Oil());
            }

            player.Volume.Add(new Water());
            player.Volume.Add(new Water());
            player.RefreshVolumeStatuses();
            Assert.IsNull(player.FindStatus<Flammable>());

            var turns = SpawnObject("Turns").AddComponent<TurnManager>();
            turns.Rng = new FixedRng();
            turns.Bind(World.CreateGrass(3), player);
            Assert.IsTrue(turns.Session.World.Floor.TryPlacePuddle(player.Cell, new Oil()));
            ActionLog.Clear();

            Assert.IsTrue(turns.TryCollectPuddle());
            Assert.IsNotNull(player.FindStatus<Flammable>());

            var dump = ActionLog.Dump();
            StringAssert.Contains("Slime collects Oil", dump);
            StringAssert.Contains("Dominance: Oil", dump);
            StringAssert.Contains("Flammable", dump);
            StringAssert.Contains("Retaliation", dump);
        }

        [Test]
        public void BurningTurn_LogsStatusAndDamage()
        {
            var dog = Spawn<Dog>(Vector2Int.zero);
            dog.SetMaxHitPoints(10);
            Assert.IsTrue(dog.AddStatus(new Burning()));
            ActionLog.Clear();

            RunTurnStart(dog);

            var dump = ActionLog.Dump();
            StringAssert.Contains("Dog's turn", dump);
            StringAssert.Contains(I18n.Get(TextKey.StatusBurning), dump);
            StringAssert.Contains("Loses HP 1", dump);
            Assert.AreEqual(9, dog.HitPoints);
        }

        [Test]
        public void PuddleContact_LogsStepsAndGains_WithoutDuplicateSubstanceLine()
        {
            var floor = new Floor();
            Assert.IsTrue(floor.TryPlacePuddle(Vector2Int.zero, new Oil()));

            var dog = Spawn<Dog>(Vector2Int.zero);
            dog.SetMaxHitPoints(10);
            floor.ApplyContact(dog);

            var dump = ActionLog.Dump();
            StringAssert.Contains("Dog steps into Oil puddle", dump);
            StringAssert.Contains("Dog gains", dump);
            Assert.IsNull(floor.GetPuddle(Vector2Int.zero));

            var entry = ActionLog.Entries[0];
            Assert.IsTrue(entry.HeadlineLine.HasLinkKind(ActionLogLinkKind.Substance));
            Assert.IsFalse(
                DetailExists(
                    entry,
                    d => d.LinkKind == ActionLogLinkKind.Substance && d.Text == "Oil"
                )
            );
        }

        [Test]
        public void BlockedWetVsBurning_DoesNotLogGains()
        {
            var rat = Spawn<Rat>(Vector2Int.zero);
            rat.SetMaxHitPoints(3);
            Assert.IsTrue(rat.AddStatus(new Burning()));
            ActionLog.Clear();

            ActionLog.Begin("test");
            Assert.IsFalse(rat.AddStatus(new Wet()));
            ActionLog.End();

            var dump = ActionLog.Dump();
            Assert.AreEqual("test", dump.Trim());
            StringAssert.DoesNotContain("gains", dump);
        }

        [Test]
        public void LethalStrike_LogsDies_WithSubstanceOnHeadline()
        {
            var slime = Spawn<Slime>(Vector2Int.zero);
            slime.Volume.Clear();
            slime.Volume.Add(new Acid());
            slime.Volume.Add(new Acid());
            slime.SetMaxHitPoints(0);
            slime.RefreshVolumeStatuses();

            var rat = Spawn<Rat>(Vector2Int.right);
            rat.SetMaxHitPoints(1);

            Assert.IsTrue(Combat.Attack(slime, rat, new Acid()));
            Assert.IsFalse(rat.IsAlive);

            var dump = ActionLog.Dump();
            StringAssert.Contains("Slime hits Rat with Acid", dump);
            StringAssert.Contains("Rat dies", dump);
            StringAssert.DoesNotContain("Rat gains", dump);

            var entry = ActionLog.Entries[0];
            Assert.IsTrue(entry.HeadlineLine.HasLinkKind(ActionLogLinkKind.Substance));
            Assert.IsFalse(
                DetailExists(
                    entry,
                    d => d.LinkKind == ActionLogLinkKind.Substance && d.Text == "Acid"
                )
            );
        }

        [Test]
        public void GainsLine_CarriesStatusLinkForPopup()
        {
            ActionLog.Begin("hit");
            Assert.IsTrue(Spawn<Rat>(Vector2Int.zero).AddStatus(new Instability()));
            ActionLog.End();

            Assert.AreEqual(1, ActionLog.Entries.Count);
            var line = ActionLog.Entries[0].Details[0];
            Assert.IsTrue(line.HasLinkKind(ActionLogLinkKind.Status));
            Assert.IsTrue(line.HasLinkKind(ActionLogLinkKind.Creature));
            Assert.IsTrue(line.Status.HasValue);
            Assert.AreEqual(I18n.Get(TextKey.StatusInstability), line.Status.Value.Label);
            Assert.AreEqual(I18n.Get(TextKey.StatusInstabilityDesc), line.Status.Value.Description);
        }

        private static List<string> DetailTexts(ActionLogEntry entry)
        {
            var texts = new List<string>(entry.Details.Count);
            for (var i = 0; i < entry.Details.Count; i++)
            {
                texts.Add(entry.Details[i].Text);
            }

            return texts;
        }

        private static bool DetailExists(
            ActionLogEntry entry,
            System.Predicate<ActionLogLine> match
        )
        {
            for (var i = 0; i < entry.Details.Count; i++)
            {
                if (match(entry.Details[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static List<ActionLogLine> FindDetails(
            ActionLogEntry entry,
            System.Predicate<ActionLogLine> match
        )
        {
            var found = new List<ActionLogLine>();
            for (var i = 0; i < entry.Details.Count; i++)
            {
                if (match(entry.Details[i]))
                {
                    found.Add(entry.Details[i]);
                }
            }

            return found;
        }

        private ActionLogEntry FindEntryContaining(string text)
        {
            var entries = ActionLog.Entries;
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry.Headline.Contains(text))
                {
                    return entry;
                }

                for (var d = 0; d < entry.Details.Count; d++)
                {
                    if (entry.Details[d].Text.Contains(text))
                    {
                        return entry;
                    }
                }
            }

            return null;
        }

        private static void RunTurnStart(Creature creature)
        {
            ActionLog.BeginKey(TextKey.LogTurn, ActionLogPart.Creature(creature.Kind));
            try
            {
                creature.TickStatuses();
            }
            finally
            {
                ActionLog.End(discardIfEmpty: true);
            }
        }

        private Scorpion SpawnScorpion(Vector2Int cell)
        {
            var scorpion = Spawn<Scorpion>(cell);
            scorpion.Volume.Clear();
            Scorpion.FillStarting(scorpion.Volume);
            scorpion.SetMaxHitPoints(3);
            scorpion.SetArmor(1);
            scorpion.EnsureInnateTraits();
            return scorpion;
        }

        private T Spawn<T>(Vector2Int cell)
            where T : Creature
        {
            var go = new GameObject(typeof(T).Name);
            spawned.Add(go);
            var creature = go.AddComponent<T>();
            creature.PlaceOn(cell);
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
