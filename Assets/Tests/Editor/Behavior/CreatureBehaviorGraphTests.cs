using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SlimesRevenge.Tests
{
    /// <summary>
    /// Unity Behavior personality graphs via <see cref="CreatureBrain"/> plus
    /// <see cref="CreatureHunt"/> predator/prey redirects.
    /// </summary>
    public class CreatureBehaviorGraphTests
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

        // --- Rat (cowardly) ---

        [Test]
        public void Rat_SeesSlime_DecidesFlee()
        {
            var player = Spawn<Slime>(new Vector2Int(4, 0));
            var rat = Spawn<Rat>(new Vector2Int(4, 4));
            var session = Occupied(World.CreateGrass(), player, rat);

            Assert.AreEqual(CreaturePersonality.Cowardly, rat.Personality);
            Assert.AreEqual(CreatureIntent.Flee, Decide(rat, player, session));
        }

        [Test]
        public void Rat_NoSlimeInVision_IdlesOrWanders()
        {
            var player = Spawn<Slime>(new Vector2Int(0, 0));
            var rat = Spawn<Rat>(new Vector2Int(9, 9));
            var session = Occupied(World.CreateGrass(), player, rat);

            Assert.IsFalse(CreatureMoves.CanSee(rat, player));
            Assert.AreEqual(CreatureIntent.Idle, Decide(rat, player, session, wander: false));
            Assert.AreEqual(CreatureIntent.Wander, Decide(rat, player, session, wander: true));
        }

        [Test]
        public void Rat_SeesPassive_FleesFromPassive()
        {
            var player = Spawn<Slime>(new Vector2Int(0, 0));
            var cat = TestCreatures.Passive(spawned, new Vector2Int(8, 8));
            var rat = Spawn<Rat>(new Vector2Int(8, 6));
            var session = Occupied(World.CreateGrass(), player, cat, rat);
            var others = Others(cat, rat);

            Assert.IsFalse(CreatureMoves.CanSee(rat, player));
            Assert.IsTrue(CreatureMoves.CanSee(rat, cat));
            Assert.AreEqual(CreatureIntent.Flee, Decide(rat, player, session, others: others));

            var before = GridStep.Chebyshev(rat.Cell, cat.Cell);
            rat.TakeTurn(session, player, new FixedRng(), others);
            Assert.Greater(GridStep.Chebyshev(rat.Cell, cat.Cell), before);
        }

        [Test]
        public void Rat_SeesSlimeAndPassive_FleesSlimeNotHunt()
        {
            var player = Spawn<Slime>(new Vector2Int(5, 5));
            var cat = TestCreatures.Passive(spawned, new Vector2Int(7, 5));
            var rat = Spawn<Rat>(new Vector2Int(6, 5));
            var session = Occupied(World.CreateGrass(), player, cat, rat);
            var others = Others(cat, rat);

            // Cowardly graph: slime in vision → Flee (player), hunt redirect does not override Flee.
            Assert.AreEqual(CreatureIntent.Flee, Decide(rat, player, session, others: others));
            var toPlayer = GridStep.Chebyshev(rat.Cell, player.Cell);
            rat.TakeTurn(session, player, new FixedRng(), others);
            Assert.Greater(GridStep.Chebyshev(rat.Cell, player.Cell), toPlayer);
        }

        [Test]
        public void Rat_StartsWithFearsCats_NoFearsSlimesStatus()
        {
            var rat = Spawn<Rat>(Vector2Int.zero);
            Assert.IsNotNull(rat.FindStatus<FearsCats>());
            Assert.IsNull(rat.FindStatus<FearsDogs>());
            Assert.AreEqual(CreaturePersonality.Cowardly, rat.Personality);
        }

        [Test]
        public void Rat_FearsCats_FleesFromPassive_IncreasesDistance()
        {
            // Explicit: FearsCats → flee from cat (not bat/scorpion).
            var player = Spawn<Slime>(new Vector2Int(0, 0));
            var cat = TestCreatures.Passive(spawned, new Vector2Int(8, 8));
            var rat = Spawn<Rat>(new Vector2Int(8, 5));
            var session = Occupied(World.CreateGrass(), player, cat, rat);
            var others = Others(cat, rat);

            Assert.IsNotNull(rat.FindStatus<FearsCats>());
            Assert.AreEqual(cat, CreatureHunt.FindPredator(rat, others));
            Assert.AreEqual(CreatureIntent.Flee, Decide(rat, player, session, others: others));

            var before = GridStep.Chebyshev(rat.Cell, cat.Cell);
            Assert.AreEqual(3, before);
            rat.TakeTurn(session, player, new FixedRng(), others);
            Assert.Greater(GridStep.Chebyshev(rat.Cell, cat.Cell), before);
            Assert.AreEqual(5, cat.HitPoints);
        }

        [Test]
        public void Rat_DoesNotFleeFromBatOrScorpion()
        {
            var player = Spawn<Slime>(new Vector2Int(0, 0));
            var bat = Spawn<Bat>(new Vector2Int(8, 8));
            var scorpion = Spawn<Scorpion>(new Vector2Int(8, 7));
            var rat = Spawn<Rat>(new Vector2Int(8, 5));
            var session = Occupied(World.CreateGrass(), player, bat, scorpion, rat);
            var others = Others(bat, scorpion, rat);

            Assert.IsNull(CreatureHunt.FindPredator(rat, others));
            Assert.AreEqual(CreatureIntent.Idle, Decide(rat, player, session, others: others));
            var origin = rat.Cell;
            rat.TakeTurn(session, player, new FixedRng(), others);
            Assert.AreEqual(origin, rat.Cell);
        }

        [Test]
        public void Rat_Cowardly_FleesSlimeViaPersonality_NotStatus()
        {
            var player = Spawn<Slime>(new Vector2Int(4, 0));
            var rat = Spawn<Rat>(new Vector2Int(4, 4));
            var session = Occupied(World.CreateGrass(), player, rat);
            var others = Others(rat);

            CreatureTurnContext.Push(session, player, new FixedRng(), others);
            try
            {
                Assert.IsNull(
                    CreatureHunt.FindPredator(rat, others),
                    "Slime is not a hunt-predator; flee is Cowardly."
                );
            }
            finally
            {
                CreatureTurnContext.Pop();
            }

            Assert.AreEqual(CreatureIntent.Flee, Decide(rat, player, session, others: others));
            var before = GridStep.Chebyshev(rat.Cell, player.Cell);
            rat.TakeTurn(session, player, new FixedRng(), others);
            Assert.Greater(GridStep.Chebyshev(rat.Cell, player.Cell), before);
        }

        // --- Passive ---

        [Test]
        public void Passive_SeesSlime_Ignores()
        {
            var player = Spawn<Slime>(new Vector2Int(0, 0));
            var cat = TestCreatures.Passive(spawned, new Vector2Int(3, 0));
            var session = Occupied(World.CreateGrass(), player, cat);

            Assert.AreEqual(CreaturePersonality.Passive, cat.Personality);
            Assert.AreEqual(CreatureIntent.Idle, Decide(cat, player, session));
            cat.TakeTurn(session, player, new FixedRng());
            Assert.AreEqual(new Vector2Int(3, 0), cat.Cell);
            Assert.AreEqual(6, player.Volume.UnitCount);
        }

        [Test]
        public void Passive_SeesCowardlyAdjacent_Attacks()
        {
            var player = Spawn<Slime>(new Vector2Int(0, 0));
            var cat = TestCreatures.Passive(spawned, new Vector2Int(3, 0));
            var rat = Spawn<Rat>(new Vector2Int(4, 0));
            var session = Occupied(World.CreateGrass(), player, cat, rat);
            var others = Others(cat, rat);

            Assert.AreEqual(CreatureIntent.Attack, Decide(cat, player, session, others: others));
            var hp = rat.HitPoints;
            cat.TakeTurn(session, player, new FixedRng(), others);
            Assert.AreEqual(hp - 1, rat.HitPoints);
            Assert.AreEqual(6, player.Volume.UnitCount);
        }

        [Test]
        public void Passive_SeesCowardlyFar_ChasesCloser()
        {
            var player = Spawn<Slime>(new Vector2Int(0, 0));
            var cat = TestCreatures.Passive(spawned, new Vector2Int(3, 0));
            var rat = Spawn<Rat>(new Vector2Int(6, 0));
            var session = Occupied(World.CreateGrass(), player, cat, rat);
            var others = Others(cat, rat);

            Assert.AreEqual(CreatureIntent.Chase, Decide(cat, player, session, others: others));
            var before = GridStep.Chebyshev(cat.Cell, rat.Cell);
            cat.TakeTurn(session, player, new FixedRng(), others);
            Assert.Less(GridStep.Chebyshev(cat.Cell, rat.Cell), before);
        }

        [Test]
        public void Passive_InCombatWithSlime_IgnoresCowardly()
        {
            var player = Spawn<Slime>(new Vector2Int(4, 4));
            var cat = TestCreatures.Passive(spawned, new Vector2Int(5, 5));
            var rat = Spawn<Rat>(new Vector2Int(5, 4));
            var session = Occupied(World.CreateGrass(), player, cat, rat);
            var others = Others(cat, rat);

            cat.MarkAggro();
            Assert.AreEqual(CreatureIntent.Attack, Decide(cat, player, session, others: others));
            var ratHp = rat.HitPoints;
            var volume = player.Volume.UnitCount;
            cat.TakeTurn(session, player, new FixedRng(), others);
            Assert.AreEqual(ratHp, rat.HitPoints);
            Assert.AreEqual(volume - 1, player.Volume.UnitCount);
        }

        [Test]
        public void Passive_SeesAggressive_FleesFromAggressive()
        {
            var player = Spawn<Slime>(new Vector2Int(0, 0));
            var dog = TestCreatures.Aggressive(spawned, new Vector2Int(8, 8));
            var cat = TestCreatures.Passive(spawned, new Vector2Int(8, 6));
            var session = Occupied(World.CreateGrass(), player, dog, cat);
            var others = Others(dog, cat);

            Assert.IsFalse(CreatureMoves.CanSee(cat, player));
            Assert.AreEqual(CreatureIntent.Flee, Decide(cat, player, session, others: others));
            var before = GridStep.Chebyshev(cat.Cell, dog.Cell);
            cat.TakeTurn(session, player, new FixedRng(), others);
            Assert.Greater(GridStep.Chebyshev(cat.Cell, dog.Cell), before);
        }

        [Test]
        public void Passive_SeesAggressiveAndCowardly_FearsDogsBeatsHatesRats()
        {
            // Priority: FearsDogs (flee) > HatesRats (hunt). Rat is adjacent — without the dog
            // this would be Attack; with the dog it must be Flee and the rat is untouched.
            var player = Spawn<Slime>(new Vector2Int(0, 0));
            var dog = TestCreatures.Aggressive(spawned, new Vector2Int(8, 8));
            var cat = TestCreatures.Passive(spawned, new Vector2Int(8, 6));
            var rat = Spawn<Rat>(new Vector2Int(8, 5));
            var session = Occupied(World.CreateGrass(), player, dog, cat, rat);
            var others = Others(dog, cat, rat);

            Assert.IsTrue(CreatureMoves.IsAdjacent(cat, rat));
            Assert.IsTrue(CreatureMoves.CanSee(cat, dog));
            Assert.AreEqual(dog, CreatureHunt.FindPredator(cat, others));
            Assert.AreEqual(rat, CreatureHunt.FindPrey(cat, others));
            Assert.AreEqual(CreatureIntent.Flee, Decide(cat, player, session, others: others));

            var ratHp = rat.HitPoints;
            var beforeDog = GridStep.Chebyshev(cat.Cell, dog.Cell);
            cat.TakeTurn(session, player, new FixedRng(), others);
            Assert.AreEqual(ratHp, rat.HitPoints);
            Assert.Greater(GridStep.Chebyshev(cat.Cell, dog.Cell), beforeDog);
        }

        [Test]
        public void Passive_StartsWithFearsDogsAndHatesRats()
        {
            var cat = TestCreatures.Passive(spawned, Vector2Int.zero);
            Assert.IsNotNull(cat.FindStatus<FearsDogs>());
            Assert.IsNotNull(cat.FindStatus<FearsWater>());
            Assert.IsNotNull(cat.FindStatus<HatesRats>());
            Assert.IsNull(cat.FindStatus<FearsCats>());
        }

        [Test]
        public void Passive_FearsDogs_FleesFromAggressive_IncreasesDistance()
        {
            // Explicit: FearsDogs → flee from dog.
            var player = Spawn<Slime>(new Vector2Int(0, 0));
            var dog = TestCreatures.Aggressive(spawned, new Vector2Int(8, 8));
            var cat = TestCreatures.Passive(spawned, new Vector2Int(8, 5));
            var session = Occupied(World.CreateGrass(), player, dog, cat);
            var others = Others(dog, cat);

            Assert.IsNotNull(cat.FindStatus<FearsDogs>());
            Assert.AreEqual(dog, CreatureHunt.FindPredator(cat, others));
            Assert.AreEqual(CreatureIntent.Flee, Decide(cat, player, session, others: others));

            var before = GridStep.Chebyshev(cat.Cell, dog.Cell);
            Assert.AreEqual(3, before);
            cat.TakeTurn(session, player, new FixedRng(), others);
            Assert.Greater(GridStep.Chebyshev(cat.Cell, dog.Cell), before);
            Assert.AreEqual(10, dog.HitPoints);
        }

        [Test]
        public void Passive_DoesNotFleeFromBatOrScorpion()
        {
            var player = Spawn<Slime>(new Vector2Int(0, 0));
            var bat = Spawn<Bat>(new Vector2Int(8, 8));
            var scorpion = Spawn<Scorpion>(new Vector2Int(8, 7));
            var cat = TestCreatures.Passive(spawned, new Vector2Int(8, 5));
            var session = Occupied(World.CreateGrass(), player, bat, scorpion, cat);
            var others = Others(bat, scorpion, cat);

            Assert.IsNull(CreatureHunt.FindPredator(cat, others));
            Assert.AreEqual(CreatureIntent.Idle, Decide(cat, player, session, others: others));
        }

        // --- Aggressive ---

        [Test]
        public void Aggressive_NoTargets_IdlesOrWanders()
        {
            var player = Spawn<Slime>(new Vector2Int(0, 0));
            var dog = TestCreatures.Aggressive(spawned, new Vector2Int(9, 9));
            var session = Occupied(World.CreateGrass(), player, dog);

            Assert.IsFalse(CreatureMoves.CanSee(dog, player));
            Assert.AreEqual(CreatureIntent.Idle, Decide(dog, player, session, wander: false));
            Assert.AreEqual(CreatureIntent.Wander, Decide(dog, player, session, wander: true));
        }

        [Test]
        public void Aggressive_SeesPassiveOnly_HuntsPassive()
        {
            var player = Spawn<Slime>(new Vector2Int(0, 0));
            var dog = TestCreatures.Aggressive(spawned, new Vector2Int(8, 8));
            var cat = TestCreatures.Passive(spawned, new Vector2Int(8, 7));
            var session = Occupied(World.CreateGrass(), player, dog, cat);
            var others = Others(dog, cat);

            Assert.AreEqual(CreatureIntent.Attack, Decide(dog, player, session, others: others));
            var hp = cat.HitPoints;
            dog.TakeTurn(session, player, new FixedRng(), others);
            Assert.AreEqual(hp - 1, cat.HitPoints);
        }

        [Test]
        public void Aggressive_SeesSlimeAndPassive_PrioritizesSlime()
        {
            var player = Spawn<Slime>(new Vector2Int(5, 5));
            var dog = TestCreatures.Aggressive(spawned, new Vector2Int(7, 5));
            var cat = TestCreatures.Passive(spawned, new Vector2Int(7, 6));
            var session = Occupied(World.CreateGrass(), player, dog, cat);
            var others = Others(dog, cat);

            Assert.IsTrue(CreatureMoves.CanSee(dog, player));
            Assert.IsTrue(CreatureMoves.CanSee(dog, cat));
            // Aggressive graph: slime in vision → Chase (dist 2), not Wander → hunt redirect skipped.
            Assert.AreEqual(CreatureIntent.Chase, Decide(dog, player, session, others: others));
            var catHp = cat.HitPoints;
            var before = GridStep.Chebyshev(dog.Cell, player.Cell);
            dog.TakeTurn(session, player, new FixedRng(), others);
            Assert.AreEqual(catHp, cat.HitPoints);
            Assert.Less(GridStep.Chebyshev(dog.Cell, player.Cell), before);
        }

        [Test]
        public void Aggressive_AdjacentToSlimeWithPassiveVisible_AttacksSlime()
        {
            var player = Spawn<Slime>(new Vector2Int(4, 4));
            var dog = TestCreatures.Aggressive(spawned, new Vector2Int(5, 4));
            var cat = TestCreatures.Passive(spawned, new Vector2Int(5, 5));
            var session = Occupied(World.CreateGrass(), player, dog, cat);
            var others = Others(dog, cat);

            Assert.AreEqual(CreatureIntent.Attack, Decide(dog, player, session, others: others));
            var catHp = cat.HitPoints;
            var volume = player.Volume.UnitCount;
            dog.TakeTurn(session, player, new FixedRng(), others);
            Assert.AreEqual(catHp, cat.HitPoints);
            Assert.AreEqual(volume - 1, player.Volume.UnitCount);
        }

        // --- Bat / Scorpion (passive, not hunted) ---

        [Test]
        public void BatAndScorpion_ArePassive_IgnoreSlimeUntilAggro()
        {
            var player = Spawn<Slime>(new Vector2Int(4, 4));
            var bat = Spawn<Bat>(new Vector2Int(5, 5));
            var scorpion = Spawn<Scorpion>(new Vector2Int(5, 4));
            var session = Occupied(World.CreateGrass(), player, bat, scorpion);

            Assert.AreEqual(CreaturePersonality.Passive, bat.Personality);
            Assert.AreEqual(CreaturePersonality.Passive, scorpion.Personality);
            Assert.AreEqual(CreatureIntent.Idle, Decide(bat, player, session));
            Assert.AreEqual(CreatureIntent.Idle, Decide(scorpion, player, session));

            bat.MarkAggro();
            scorpion.MarkAggro();
            Assert.AreEqual(CreatureIntent.Attack, Decide(bat, player, session));
            Assert.AreEqual(CreatureIntent.Attack, Decide(scorpion, player, session));
        }

        [Test]
        public void PassiveAndAggressive_DoNotHuntBatOrScorpion()
        {
            var player = Spawn<Slime>(new Vector2Int(0, 0));
            var cat = TestCreatures.Passive(spawned, new Vector2Int(8, 5));
            // Keep dog out of the cat's vision so FearsDogs does not force Flee;
            // this test only asserts bat/scorpion are not prey.
            var dog = TestCreatures.Aggressive(spawned, new Vector2Int(0, 8));
            var bat = Spawn<Bat>(new Vector2Int(8, 6));
            var scorpion = Spawn<Scorpion>(new Vector2Int(8, 7));
            var session = Occupied(World.CreateGrass(), player, cat, dog, bat, scorpion);
            var others = Others(cat, dog, bat, scorpion);

            Assert.IsFalse(CreatureMoves.CanSee(cat, dog));
            Assert.AreEqual(CreatureIntent.Idle, Decide(cat, player, session, others: others));
            Assert.AreEqual(CreatureIntent.Idle, Decide(dog, player, session, others: others));
            Assert.IsNull(CreatureHunt.FindPrey(cat, others));
            Assert.IsNull(CreatureHunt.FindPrey(dog, others));
        }

        private CreatureIntent Decide(
            Creature self,
            Creature player,
            GameSession session,
            bool wander = false,
            IReadOnlyList<Creature> others = null
        )
        {
            return CreatureBrain.Decide(
                self,
                player,
                session,
                new FixedRng(wander: wander),
                others
            );
        }

        private static List<Creature> Others(params Creature[] creatures)
        {
            return new List<Creature>(creatures);
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
                case Bat:
                    creature.SetMaxHitPoints(3);
                    break;
                case Scorpion:
                    creature.SetMaxHitPoints(3);
                    break;
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
