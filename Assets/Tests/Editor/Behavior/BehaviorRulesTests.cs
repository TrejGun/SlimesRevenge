using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SlimesRevenge.Tests
{
    /// <summary>
    /// Contract tests for docs/behavior.md: personality vs slime, fear/hate mob↔mob, duel vision.
    /// </summary>
    public class BehaviorRulesTests
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

        /// <summary>
        /// Duel approach contract (TurnManager + Aggressive Behavior graph):
        /// Chebyshev 7 — no sight, Idle|Wander (not Chase);
        /// step to 6 — still blind, Idle|Wander (may step randomly, must not Chase);
        /// step to 5 — vision, Decide=Chase, dog closes toward slime (Speed 2).
        /// </summary>
        [Test]
        public void Aggressive_ApproachFrom7_At6Idle_At5SeesAndCloses()
        {
            Assert.AreEqual(7, RunConfig.DuelSeparation, "Duel gap must be beyond VisionRange.");

            var world = new World(RunConfig.DuelWidth, RunConfig.DuelHeight, TerrainType.Grass);
            var slimeStart = new Vector2Int(
                (RunConfig.DuelWidth - RunConfig.DuelSeparation) / 2,
                RunConfig.DuelHeight / 2
            );
            var dogStart = new Vector2Int(slimeStart.x + RunConfig.DuelSeparation, slimeStart.y);

            var player = Spawn<Slime>(slimeStart);
            var dog = TestCreatures.Aggressive(spawned, dogStart);
            Assert.AreEqual(5, dog.VisionRange);
            Assert.AreEqual(2, dog.Speed);
            Assert.AreEqual(CreaturePersonality.Aggressive, dog.Personality);

            var turns = SpawnObject("Turns").AddComponent<TurnManager>();
            turns.Rng = new FixedRng();
            turns.Bind(world, player, dog);

            // --- distance 7: outside vision ---
            Assert.AreEqual(7, GridStep.Chebyshev(player.Cell, dog.Cell));
            Assert.IsFalse(CreatureMoves.CanSee(dog, player));
            AssertNotChasing(dog, player, turns.Session);
            Assert.AreEqual(dogStart, dog.Cell);

            // --- step to 6: still outside vision; may Idle or Wander, must not Chase ---
            Assert.IsTrue(turns.TryStep(Vector2Int.right));
            Assert.AreEqual(
                6,
                GridStep.Chebyshev(player.Cell, dogStart),
                "Slime advanced one cell toward dog spawn."
            );
            Assert.IsFalse(
                CreatureMoves.CanSee(dog, player),
                "VisionRange is 5; distance 6 is blind."
            );
            AssertNotChasing(dog, player, turns.Session);
            Assert.AreEqual(6, player.Volume.UnitCount);
            // FixedRng → Idle, so dog stays put; Wander would be allowed with SystemRng.
            Assert.AreEqual(dogStart, dog.Cell);

            // --- step to 5: enters vision; Aggressive graph → Chase; Speed 2 closes by 2 ---
            Assert.IsTrue(turns.TryStep(Vector2Int.right));
            Assert.AreEqual(
                5,
                GridStep.Chebyshev(player.Cell, dogStart),
                "Slime on vision edge vs dog spawn."
            );
            Assert.AreNotEqual(dogStart, dog.Cell, "Dog must leave spawn on Chase.");
            Assert.Less(
                GridStep.Chebyshev(dog.Cell, player.Cell),
                GridStep.Chebyshev(dogStart, player.Cell),
                "Dog must close distance toward the slime."
            );
            Assert.AreEqual(
                3,
                GridStep.Chebyshev(dog.Cell, player.Cell),
                "Speed 2 Chase from distance 5 must land at Chebyshev 3."
            );
            Assert.IsTrue(CreatureMoves.CanSee(dog, player));
            Assert.AreEqual(
                CreatureIntent.Chase,
                CreatureBrain.Decide(dog, player, turns.Session, new FixedRng())
            );
            Assert.AreEqual(6, player.Volume.UnitCount, "Chase does not bite at range.");
        }

        [Test]
        public void Aggressive_TakeTurnAtDist5_MustCloseTowardSlime()
        {
            var world = new World(RunConfig.DuelWidth, RunConfig.DuelHeight, TerrainType.Grass);
            var player = Spawn<Slime>(new Vector2Int(6, 2));
            var dog = TestCreatures.Aggressive(spawned, new Vector2Int(11, 2));
            var session = Occupied(world, player, dog);
            var spawn = dog.Cell;

            Assert.AreEqual(5, GridStep.Chebyshev(player.Cell, dog.Cell));
            Assert.AreEqual(2, dog.Speed);
            Assert.AreEqual(
                CreatureIntent.Chase,
                CreatureBrain.Decide(dog, player, session, new FixedRng())
            );

            Assert.IsTrue(
                CreatureMoves.TryChase(dog, player, session, new FixedRng()),
                "TryChase itself must succeed on open duel grass."
            );
            Assert.AreNotEqual(spawn, dog.Cell);
            Assert.AreEqual(3, GridStep.Chebyshev(dog.Cell, player.Cell));
        }

        [Test]
        public void Aggressive_TakeTurnAtDist5_ViaTakeTurn_MustCloseTowardSlime()
        {
            var world = new World(RunConfig.DuelWidth, RunConfig.DuelHeight, TerrainType.Grass);
            var player = Spawn<Slime>(new Vector2Int(6, 2));
            var dog = TestCreatures.Aggressive(spawned, new Vector2Int(11, 2));
            var session = Occupied(world, player, dog);
            var spawn = dog.Cell;

            dog.TakeTurn(session, player, new FixedRng());
            Assert.AreNotEqual(spawn, dog.Cell, "TakeTurn(Chase) must move the dog.");
            Assert.Less(GridStep.Chebyshev(dog.Cell, player.Cell), 5);
        }

        [Test]
        public void Aggressive_InVision_BehaviorGraphChoosesChase_NotWander()
        {
            // Proves Aggressive graph takes Chase when slime is in range — not WanderOrIdle.
            var player = Spawn<Slime>(new Vector2Int(0, 0));
            var dog = TestCreatures.Aggressive(spawned, new Vector2Int(4, 0));
            var session = Occupied(World.CreateGrass(), player, dog);

            Assert.AreEqual(4, GridStep.Chebyshev(dog.Cell, player.Cell));
            Assert.IsTrue(CreatureMoves.CanSee(dog, player));
            Assert.AreEqual(CreaturePersonality.Aggressive, dog.Personality);

            // Even if RNG would prefer Wander, vision branch must win.
            Assert.AreEqual(
                CreatureIntent.Chase,
                CreatureBrain.Decide(dog, player, session, new FixedRng(wander: true))
            );
        }

        private static void AssertNotChasing(Creature dog, Creature player, GameSession session)
        {
            var intent = CreatureBrain.Decide(dog, player, session, new FixedRng());
            Assert.IsTrue(
                intent == CreatureIntent.Idle || intent == CreatureIntent.Wander,
                $"Expected Idle|Wander outside vision, got {intent}."
            );
            Assert.AreNotEqual(CreatureIntent.Chase, intent);
            Assert.AreNotEqual(CreatureIntent.Attack, intent);
        }

        /// <summary>
        /// 2) Slime is adjacent → steps away → aggressive enemy closes and bites.
        /// </summary>
        [Test]
        public void Aggressive_Adjacent_InvisibleMercurySlime_IdlesUntilAggro()
        {
            var world = new World(RunConfig.DuelWidth, RunConfig.DuelHeight, TerrainType.Grass);
            var player = Spawn<Slime>(new Vector2Int(5, 2));
            FillDominantMercury(player);
            var dog = TestCreatures.Aggressive(spawned, new Vector2Int(6, 2));
            var turns = SpawnObject("Turns").AddComponent<TurnManager>();
            turns.Rng = new FixedRng();
            turns.Bind(world, player, dog);

            Assert.IsTrue(GridStep.IsAdjacent(dog.Cell, player.Cell));
            Assert.IsNotNull(player.FindStatus<Invisible>());
            Assert.IsFalse(CreatureMoves.CanSee(dog, player));
            Assert.IsFalse(CreatureMoves.IsDetected(dog, player));
            Assert.AreEqual(10, player.Volume.UnitCount);

            Assert.IsTrue(turns.TryWait());
            Assert.AreEqual(10, player.Volume.UnitCount, "Invisible slime adjacent must not be bitten.");
            Assert.IsFalse(dog.Aggroed);

            dog.MarkAggro();
            Assert.IsTrue(CreatureMoves.IsDetected(dog, player));
            Assert.IsTrue(turns.TryWait());
            Assert.AreEqual(9, player.Volume.UnitCount, "After aggro, adjacent dog bites.");
        }

        [Test]
        public void Aggressive_Adjacent_SlimeStepsAway_CatchesAndBites()
        {
            var world = new World(RunConfig.DuelWidth, RunConfig.DuelHeight, TerrainType.Grass);
            var player = Spawn<Slime>(new Vector2Int(5, 2));
            var dog = TestCreatures.Aggressive(spawned, new Vector2Int(6, 2));
            var turns = SpawnObject("Turns").AddComponent<TurnManager>();
            turns.Rng = new FixedRng();
            turns.Bind(world, player, dog);

            Assert.IsTrue(GridStep.IsAdjacent(dog.Cell, player.Cell));
            Assert.AreEqual(6, player.Volume.UnitCount);

            // Slime steps away (distance 2). Dog's turn: Speed 2 → back to adjacent (chase, no bite yet).
            Assert.IsTrue(turns.TryStep(Vector2Int.left));
            Assert.IsTrue(
                GridStep.IsAdjacent(dog.Cell, player.Cell),
                "Dog must catch up on the same enemy turn."
            );
            Assert.AreEqual(6, player.Volume.UnitCount, "Catch-up is Chase, not Attack.");

            // Next enemy turn: adjacent → bite.
            Assert.IsTrue(turns.TryWait());
            Assert.AreEqual(5, player.Volume.UnitCount, "Dog bites once adjacent again.");
            Assert.IsTrue(dog.InCombat);
            Assert.IsTrue(GridStep.IsAdjacent(dog.Cell, player.Cell));
        }

        [Test]
        public void TurnManager_RepeatedChasePathfinds_DoNotFreezeAggressive()
        {
            // Regression: deferred A* Destroy in Play Mode poisoned the next pathfind.
            var world = World.CreateGrass();
            var player = Spawn<Slime>(new Vector2Int(2, 4));
            var dog = TestCreatures.Aggressive(spawned, new Vector2Int(7, 4));
            var turns = SpawnObject("Turns").AddComponent<TurnManager>();
            turns.Rng = new FixedRng();
            turns.Bind(world, player, dog);

            // Wait once at Chebyshev 5 so the dog sees the slime, Chases, and locks pursuit.
            // Stepping away first would put distance at 6 before the dog acts (blind Idle).
            Assert.AreEqual(5, GridStep.Chebyshev(dog.Cell, player.Cell));
            Assert.IsTrue(turns.TryWait());
            Assert.Less(GridStep.Chebyshev(dog.Cell, player.Cell), 5);

            for (var i = 0; i < 5; i++)
            {
                Assert.IsTrue(turns.TryStep(Vector2Int.left) || turns.TryWait());
            }

            Assert.LessOrEqual(GridStep.Chebyshev(dog.Cell, player.Cell), 1);
            Assert.IsTrue(dog.InCombat);
        }

        [Test]
        public void Cowardly_FleesSlimeOnSight_WithoutAggro()
        {
            var origin = new Vector2Int(4, 4);
            var player = Spawn<Slime>(new Vector2Int(4, 0));
            var rat = Spawn<Rat>(origin);
            var session = Occupied(World.CreateGrass(), player, rat);

            Assert.AreEqual(CreaturePersonality.Cowardly, rat.Personality);
            Assert.IsTrue(CreatureMoves.CanSee(rat, player));
            Assert.IsFalse(rat.InCombat);

            rat.TakeTurn(session, player, new FixedRng());
            Assert.Greater(
                GridStep.Chebyshev(rat.Cell, player.Cell),
                GridStep.Chebyshev(origin, player.Cell)
            );
            Assert.AreEqual(6, player.Volume.UnitCount);
        }

        [Test]
        public void Cowardly_AdjacentFight_ThenSlimeStepsAway_DropsAggroAndFlees()
        {
            var player = Spawn<Slime>(new Vector2Int(4, 4));
            var rat = Spawn<Rat>(new Vector2Int(5, 4));
            var session = Occupied(World.CreateGrass(), player, rat);

            rat.MarkAggro();
            Assert.AreEqual(
                CreatureIntent.Attack,
                CreatureBrain.Decide(rat, player, session, new FixedRng())
            );
            var volume = player.Volume.UnitCount;
            rat.TakeTurn(session, player, new FixedRng());
            Assert.AreEqual(volume - 1, player.Volume.UnitCount);

            Assert.IsTrue(session.TryStep(Vector2Int.left));
            player.PlaceOn(session.ControlledCell.Value);
            Assert.IsFalse(GridStep.IsAdjacent(rat.Cell, player.Cell));

            Assert.AreEqual(
                CreatureIntent.Flee,
                CreatureBrain.Decide(rat, player, session, new FixedRng())
            );
            Assert.IsFalse(rat.InCombat);

            var distBefore = GridStep.Chebyshev(rat.Cell, player.Cell);
            rat.TakeTurn(session, player, new FixedRng());
            Assert.Greater(GridStep.Chebyshev(rat.Cell, player.Cell), distBefore);
        }

        [Test]
        public void Passive_IgnoresSlimeUntilHit_ThenChases()
        {
            var player = Spawn<Slime>(new Vector2Int(0, 0));
            var cat = TestCreatures.Passive(spawned, new Vector2Int(3, 0));
            var session = Occupied(World.CreateGrass(), player, cat);

            Assert.AreEqual(CreaturePersonality.Passive, cat.Personality);
            cat.TakeTurn(session, player, new FixedRng());
            Assert.AreEqual(new Vector2Int(3, 0), cat.Cell);
            Assert.AreEqual(6, player.Volume.UnitCount);

            cat.MarkAggro();
            Assert.AreEqual(
                CreatureIntent.Chase,
                CreatureBrain.Decide(cat, player, session, new FixedRng())
            );
            cat.TakeTurn(session, player, new FixedRng());
            Assert.Less(GridStep.Chebyshev(cat.Cell, player.Cell), 3);
        }

        [Test]
        public void Passive_AfterHit_KeepsAggroWhenSlimeStepsAway_AndChases()
        {
            var player = Spawn<Slime>(new Vector2Int(4, 4));
            var cat = TestCreatures.Passive(spawned, new Vector2Int(5, 4));
            var session = Occupied(World.CreateGrass(), player, cat);

            // Slime "hits" → MarkAggro (TurnManager.TryAttack).
            cat.MarkAggro();
            Assert.AreEqual(
                CreatureIntent.Attack,
                CreatureBrain.Decide(cat, player, session, new FixedRng())
            );
            var volume = player.Volume.UnitCount;
            cat.TakeTurn(session, player, new FixedRng());
            Assert.AreEqual(volume - 1, player.Volume.UnitCount);
            Assert.IsTrue(cat.InCombat);

            Assert.IsTrue(session.TryStep(Vector2Int.left));
            player.PlaceOn(session.ControlledCell.Value);
            Assert.IsFalse(GridStep.IsAdjacent(cat.Cell, player.Cell));

            // Unlike rat: passive does not ClearAggro on break contact.
            Assert.IsTrue(cat.InCombat);
            Assert.AreEqual(
                CreatureIntent.Chase,
                CreatureBrain.Decide(cat, player, session, new FixedRng())
            );
            Assert.IsTrue(cat.InCombat);

            var distBefore = GridStep.Chebyshev(cat.Cell, player.Cell);
            cat.TakeTurn(session, player, new FixedRng());
            Assert.Less(GridStep.Chebyshev(cat.Cell, player.Cell), distBefore);
            Assert.IsTrue(cat.InCombat);
        }

        [Test]
        public void PassiveScorpionAndBat_IgnoreSlimeUntilAggro()
        {
            var player = Spawn<Slime>(new Vector2Int(0, 0));
            // Distance 2: after MarkAggro, Aggressive subtree chooses Chase (Attack only when adjacent).
            var scorpion = Spawn<Scorpion>(new Vector2Int(2, 0));
            var bat = Spawn<Bat>(new Vector2Int(0, 2));
            var session = Occupied(World.CreateGrass(), player, scorpion, bat);

            scorpion.TakeTurn(session, player, new FixedRng());
            bat.TakeTurn(session, player, new FixedRng());
            Assert.AreEqual(new Vector2Int(2, 0), scorpion.Cell);
            Assert.AreEqual(new Vector2Int(0, 2), bat.Cell);
            Assert.AreEqual(6, player.Volume.UnitCount);

            scorpion.MarkAggro();
            bat.MarkAggro();
            Assert.AreEqual(
                CreatureIntent.Chase,
                CreatureBrain.Decide(scorpion, player, session, new FixedRng())
            );
            Assert.AreEqual(
                CreatureIntent.Chase,
                CreatureBrain.Decide(bat, player, session, new FixedRng())
            );
        }

        [Test]
        public void Passive_HuntsCowardly_CowardlyFlees()
        {
            // Player beyond VisionRange so personality stays Idle/Wander and hunt redirect owns intents.
            var player = Spawn<Slime>(new Vector2Int(0, 0));
            var cat = TestCreatures.Passive(spawned, new Vector2Int(6, 6));
            var rat = Spawn<Rat>(new Vector2Int(6, 7));
            var session = Occupied(World.CreateGrass(), player, cat, rat);
            var others = new List<Creature> { cat, rat };

            Assert.IsFalse(CreatureMoves.CanSee(cat, player));
            Assert.IsNotNull(cat.FindStatus<HatesRats>());
            Assert.IsNotNull(rat.FindStatus<FearsCats>());

            Assert.AreEqual(
                CreatureIntent.Attack,
                CreatureBrain.Decide(cat, player, session, new FixedRng(), others)
            );
            Assert.AreEqual(
                CreatureIntent.Flee,
                CreatureBrain.Decide(rat, player, session, new FixedRng(), others)
            );

            var ratHp = rat.HitPoints;
            var ratStart = rat.Cell;
            rat.TakeTurn(session, player, new FixedRng(), others);
            Assert.AreNotEqual(ratStart, rat.Cell);
            Assert.AreEqual(ratHp, rat.HitPoints);

            cat.TakeTurn(session, player, new FixedRng(), others);
            Assert.AreEqual(6, player.Volume.UnitCount);
        }

        [Test]
        public void Aggressive_HuntsPassive_PassiveFlees()
        {
            var player = Spawn<Slime>(new Vector2Int(0, 0));
            var dog = TestCreatures.Aggressive(spawned, new Vector2Int(8, 8));
            var cat = TestCreatures.Passive(spawned, new Vector2Int(8, 7));
            var session = Occupied(World.CreateGrass(), player, dog, cat);
            var others = new List<Creature> { dog, cat };

            Assert.IsFalse(CreatureMoves.CanSee(dog, player));
            Assert.IsNotNull(dog.FindStatus<HatesCats>());
            Assert.IsNotNull(cat.FindStatus<FearsDogs>());

            Assert.AreEqual(
                CreatureIntent.Attack,
                CreatureBrain.Decide(dog, player, session, new FixedRng(), others)
            );
            Assert.AreEqual(
                CreatureIntent.Flee,
                CreatureBrain.Decide(cat, player, session, new FixedRng(), others)
            );

            var catHp = cat.HitPoints;
            dog.TakeTurn(session, player, new FixedRng(), others);
            Assert.AreEqual(catHp - 1, cat.HitPoints);
            Assert.AreEqual(6, player.Volume.UnitCount);
        }

        [Test]
        public void AggressivePrefersSlimeOverHuntingPassive_WhenSlimeInVision()
        {
            var player = Spawn<Slime>(new Vector2Int(5, 5));
            var dog = TestCreatures.Aggressive(spawned, new Vector2Int(5, 8));
            var cat = TestCreatures.Passive(spawned, new Vector2Int(5, 9));
            var session = Occupied(World.CreateGrass(), player, dog, cat);
            var others = new List<Creature> { dog, cat };

            Assert.IsTrue(CreatureMoves.CanSee(dog, player));
            Assert.IsTrue(CreatureMoves.CanSee(dog, cat));
            Assert.AreEqual(
                CreatureIntent.Chase,
                CreatureBrain.Decide(dog, player, session, new FixedRng(), others)
            );

            var catHp = cat.HitPoints;
            var volume = player.Volume.UnitCount;
            dog.TakeTurn(session, player, new FixedRng(), others);
            Assert.AreEqual(catHp, cat.HitPoints);
            Assert.AreEqual(volume, player.Volume.UnitCount);
            Assert.Less(GridStep.Chebyshev(dog.Cell, player.Cell), 3);
        }

        [Test]
        public void BatAndRat_DoNotFightEachOther()
        {
            var player = Spawn<Slime>(new Vector2Int(0, 0));
            var bat = Spawn<Bat>(new Vector2Int(6, 6));
            var rat = Spawn<Rat>(new Vector2Int(6, 7));
            var session = Occupied(World.CreateGrass(), player, bat, rat);
            var others = new List<Creature> { bat, rat };

            Assert.IsFalse(CreatureMoves.CanSee(bat, player));
            Assert.IsFalse(CreatureHunt.IsPreyOf(bat, rat));
            Assert.IsFalse(CreatureHunt.IsPreyOf(rat, bat));

            var batHp = bat.HitPoints;
            var ratHp = rat.HitPoints;
            bat.TakeTurn(session, player, new FixedRng(), others);
            rat.TakeTurn(session, player, new FixedRng(), others);
            Assert.AreEqual(batHp, bat.HitPoints);
            Assert.AreEqual(ratHp, rat.HitPoints);
        }

        [Test]
        public void MeleeBiteOnSlime_MarksAggroAndPursuit()
        {
            var player = Spawn<Slime>(new Vector2Int(1, 1));
            var dog = TestCreatures.Aggressive(spawned, new Vector2Int(1, 2));
            Assert.IsFalse(dog.InCombat);

            Assert.IsTrue(Combat.Attack(dog, player));
            Assert.IsTrue(dog.InCombat);
            Assert.AreEqual(player.Cell, dog.PursuitCell);
        }

        private static void FillDominantMercury(Slime slime)
        {
            slime.Volume.Clear();
            for (var i = 0; i < 8; i++)
            {
                slime.Volume.Add(new Mercury());
            }

            slime.Volume.Add(new Water());
            slime.Volume.Add(new Water());
            slime.RefreshVolumeStatuses();
        }

        private T Spawn<T>(Vector2Int cell)
            where T : Creature
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
                    case Bat:
                        Bat.FillStarting(creature.Volume);
                        break;
                    case Scorpion:
                        Scorpion.FillStarting(creature.Volume);
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
                    break;
            }

            if (creature is Slime)
            {
                creature.RefreshVolumeStatuses();
            }

            creature.EnsureInnateTraits();
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
            return SessionFactory.WithControlled(world, player, others);
        }
    }
}
