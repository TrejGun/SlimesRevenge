using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Unity.Behavior;
using UnityEngine;

namespace SlimesRevenge.Tests
{
    /// <summary>
    /// Evidence tests: what BindOwner / Graph.GameObject actually do (no speculation).
    /// </summary>
    public class BehaviorGraphBindOwnerProofTests
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
        public void Proof_BindOwner_SetsModuleGameObject()
        {
            var dog = SpawnDog(new Vector2Int(5, 0));
            var graph = CreaturePolicyGraphs.For(CreaturePersonality.Aggressive);
            var module = RootModule(graph);

            Assert.IsNull(
                ReadModuleGameObject(module),
                "Fresh module owner should be unset before BindOwner."
            );

            CreaturePolicyGraphs.BindOwner(graph, dog.gameObject);

            Assert.AreSame(
                dog.gameObject,
                ReadModuleGameObject(module),
                "BindOwner must assign BehaviorGraphModule.GameObject to the dog."
            );
        }

        [Test]
        public void Proof_BindOwner_ThenEnd_DoesNotClearModuleGameObject()
        {
            var dog = SpawnDog(new Vector2Int(5, 0));
            var graph = CreaturePolicyGraphs.For(CreaturePersonality.Aggressive);
            var module = RootModule(graph);

            CreaturePolicyGraphs.BindOwner(graph, dog.gameObject);
            graph.End();

            Assert.AreSame(
                dog.gameObject,
                ReadModuleGameObject(module),
                "graph.End()/Reset must not wipe Module.GameObject (Decide binds then End)."
            );
        }

        [Test]
        public void Proof_VisionCondition_GraphGameObject_MatchesBindOwner()
        {
            var dog = SpawnDog(new Vector2Int(5, 0));
            var graph = CreaturePolicyGraphs.For(CreaturePersonality.Aggressive);
            CreaturePolicyGraphs.BindOwner(graph, dog.gameObject);

            var vision = FindCondition<PlayerInVisionCondition>(RootModule(graph));
            Assert.IsNotNull(vision, "Aggressive graph must contain PlayerInVisionCondition.");

            var graphField = typeof(Condition).GetField(
                "Graph",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
            );
            Assert.IsNotNull(graphField);
            Assert.AreSame(
                RootModule(graph),
                graphField.GetValue(vision),
                "Condition.Graph must be the same module BindOwner writes to."
            );

            Assert.AreSame(
                dog.gameObject,
                vision.GameObject,
                "Condition.GameObject must equal dog after BindOwner (via Graph.GameObject)."
            );
            Assert.AreSame(dog, vision.GameObject.GetComponent<Creature>());
        }

        [Test]
        public void Proof_WithoutActor_VisionCondition_UsesBoundGameObject()
        {
            // Actor null + BindOwner: GameObject fallback must still see the player.
            var player = SpawnSlime(new Vector2Int(0, 0));
            var dog = SpawnDog(new Vector2Int(4, 0));
            var session = Occupied(World.CreateGrass(), player, dog);
            Assert.IsTrue(CreatureMoves.CanSee(dog, player));

            var graph = CreaturePolicyGraphs.For(CreaturePersonality.Aggressive);
            CreaturePolicyGraphs.BindOwner(graph, dog.gameObject);

            CreatureTurnContext.Push(session, player, new FixedRng());
            Assert.IsNull(CreatureTurnContext.Actor);

            var vision = FindCondition<PlayerInVisionCondition>(RootModule(graph));
            Assert.IsTrue(
                vision.IsTrue(),
                "With Actor==null, PlayerInVisionCondition must fall back to bound GameObject and see the slime."
            );
        }

        [Test]
        public void Proof_LegacyGameObjectVision_True_WhenBound_ActorNull()
        {
            // Reconstructs the OLD condition body; proves BindOwner was enough for GetComponent.
            var player = SpawnSlime(new Vector2Int(0, 0));
            var dog = SpawnDog(new Vector2Int(4, 0));
            var session = Occupied(World.CreateGrass(), player, dog);

            var graph = CreaturePolicyGraphs.For(CreaturePersonality.Aggressive);
            CreaturePolicyGraphs.BindOwner(graph, dog.gameObject);
            CreatureTurnContext.Push(session, player, new FixedRng());
            Assert.IsNull(CreatureTurnContext.Actor);

            var vision = FindCondition<PlayerInVisionCondition>(RootModule(graph));
            var selfViaGo = vision.GameObject.GetComponent<Creature>();
            Assert.AreSame(dog, selfViaGo);
            Assert.IsTrue(
                CreatureMoves.CanSee(selfViaGo, CreatureTurnContext.Player),
                "Legacy GameObject.GetComponent + CanSee is TRUE after BindOwner — bind itself works in EditMode."
            );
        }

        [Test]
        public void Proof_GraphTick_WithoutActor_StillChases_WhenGameObjectBound()
        {
            // If this is Chase, BindOwner+GameObject path is sufficient (Actor not required).
            // If this is Wander/Idle, BindOwner failed to feed conditions.
            var player = SpawnSlime(new Vector2Int(0, 0));
            var dog = SpawnDog(new Vector2Int(4, 0));
            var session = Occupied(World.CreateGrass(), player, dog);

            CreatureTurnContext.Push(session, player, new FixedRng(wander: true));
            var graph = CreaturePolicyGraphs.For(CreaturePersonality.Aggressive);
            CreaturePolicyGraphs.BindOwner(graph, dog.gameObject);
            Assert.IsNull(CreatureTurnContext.Actor);

            graph.End();
            graph.Start();
            graph.Tick();

            Assert.AreEqual(
                CreatureIntent.Chase,
                CreatureTurnContext.ChosenIntent,
                "BindOwner alone must make Aggressive+vision choose Chase (no Actor)."
            );
        }

        [Test]
        public void Proof_Decide_WithSetActor_ChoosesChase_InVision()
        {
            var player = SpawnSlime(new Vector2Int(0, 0));
            var dog = SpawnDog(new Vector2Int(4, 0));
            var session = Occupied(World.CreateGrass(), player, dog);

            Assert.AreEqual(
                CreatureIntent.Chase,
                CreatureBrain.Decide(dog, player, session, new FixedRng(wander: true))
            );
        }

        private TestCreature SpawnDog(Vector2Int cell)
        {
            return TestCreatures.Aggressive(spawned, cell);
        }

        private Slime SpawnSlime(Vector2Int cell)
        {
            var slime = SpawnObject("Slime").AddComponent<Slime>();
            slime.PlaceOn(cell);
            Slime.FillStarting(slime.Volume);
            slime.SetMaxHitPoints(0);
            slime.RefreshVolumeStatuses();
            return slime;
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

        private static object RootModule(BehaviorGraph graph)
        {
            var graphsField = typeof(BehaviorGraph).GetField(
                "Graphs",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
            );
            var list = graphsField?.GetValue(graph) as System.Collections.IList;
            return list is { Count: > 0 } ? list[0] : null;
        }

        private static GameObject ReadModuleGameObject(object module)
        {
            return module
                    ?.GetType()
                    .GetProperty(
                        "GameObject",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                    )
                    ?.GetValue(module) as GameObject;
        }

        private static T FindCondition<T>(object module)
            where T : Condition
        {
            var root =
                module
                    .GetType()
                    .GetField(
                        "Root",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                    )
                    ?.GetValue(module) as Node;
            return FindCondition<T>(root);
        }

        private static T FindCondition<T>(Node node)
            where T : Condition
        {
            if (node == null)
            {
                return null;
            }

            var branchType = typeof(BehaviorGraph).Assembly.GetType(
                "Unity.Behavior.BranchingConditionComposite"
            );
            if (branchType.IsInstanceOfType(node))
            {
                var conditions =
                    branchType.GetProperty("Conditions")?.GetValue(node) as IList<Condition>;
                if (conditions != null)
                {
                    foreach (var condition in conditions)
                    {
                        if (condition is T match)
                        {
                            return match;
                        }
                    }
                }

                var found = FindCondition<T>(branchType.GetField("True")?.GetValue(node) as Node);
                if (found != null)
                {
                    return found;
                }

                found = FindCondition<T>(branchType.GetField("False")?.GetValue(node) as Node);
                if (found != null)
                {
                    return found;
                }
            }

            if (node is Composite composite)
            {
                foreach (var child in composite.Children)
                {
                    var found = FindCondition<T>(child);
                    if (found != null)
                    {
                        return found;
                    }
                }
            }

            return null;
        }

        [Test]
        public void Proof_ChaseBranch_RequiresAllConditions_IsFalse_OrOfAggroAndVision()
        {
            var graph = CreaturePolicyGraphs.For(CreaturePersonality.Aggressive);
            var module = RootModule(graph);
            var root =
                module
                    .GetType()
                    .GetField(
                        "Root",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                    )
                    ?.GetValue(module) as Node;

            var branchType = typeof(BehaviorGraph).Assembly.GetType(
                "Unity.Behavior.BranchingConditionComposite"
            );
            // Root = adjacent? Attack : chaseOrWander
            var chaseOrWander = branchType.GetField("False")?.GetValue(root);
            Assert.IsNotNull(chaseOrWander);
            Assert.IsTrue(branchType.IsInstanceOfType(chaseOrWander));

            var requiresAll = (bool)
                branchType.GetProperty("RequiresAllConditions")!.GetValue(chaseOrWander)!;
            Assert.IsFalse(
                requiresAll,
                "Vision|Aggro branch must be OR (RequiresAllConditions=false). If true, Aggro alone would not Chase."
            );

            var conditions =
                branchType.GetProperty("Conditions")?.GetValue(chaseOrWander) as IList<Condition>;
            Assert.IsNotNull(conditions);
            Assert.AreEqual(2, conditions.Count);
            Assert.IsInstanceOf<IsAggroedCondition>(conditions[0]);
            Assert.IsInstanceOf<PlayerInVisionCondition>(conditions[1]);
        }
    }
}
