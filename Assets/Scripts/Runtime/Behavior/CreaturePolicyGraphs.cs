using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Behavior;
using UnityEngine;

namespace SlimesRevenge
{
    /// <summary>
    /// Builds turn-based Unity Behavior graphs (select → intent) for creature personalities.
    /// Uses reflection because BranchingConditionComposite / SequenceComposite are internal.
    /// </summary>
    public static class CreaturePolicyGraphs
    {
        private static readonly object Gate = new object();
        private static BehaviorGraph aggressive;
        private static BehaviorGraph cowardly;
        private static BehaviorGraph passive;

        private static Type BranchType => typeof(BehaviorGraph).Assembly.GetType("Unity.Behavior.BranchingConditionComposite");
        private static Type SequenceType => typeof(BehaviorGraph).Assembly.GetType("Unity.Behavior.SequenceComposite");

        public static BehaviorGraph For(CreaturePersonality personality)
        {
            EnsureBuilt();
            return personality switch
            {
                CreaturePersonality.Cowardly => cowardly,
                CreaturePersonality.Passive => passive,
                _ => aggressive,
            };
        }

        public static void BindOwner(BehaviorGraph graph, GameObject owner)
        {
            var module = RootModule(graph);
            module?.GetType()
                .GetProperty("GameObject", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.SetValue(module, owner);
        }

        private static void EnsureBuilt()
        {
            if (aggressive != null)
            {
                return;
            }

            lock (Gate)
            {
                if (aggressive != null)
                {
                    return;
                }

                aggressive = Build(BuildAggressiveRoot());
                cowardly = Build(BuildCowardlyRoot());
                passive = Build(BuildPassiveRoot());
            }
        }

        private static Unity.Behavior.Action Select<T>() where T : Unity.Behavior.Action
        {
            return (Unity.Behavior.Action)Activator.CreateInstance(typeof(T));
        }

        private static Condition Cond<T>() where T : Condition
        {
            return (Condition)Activator.CreateInstance(typeof(T));
        }

        private static Node BuildAggressiveRoot()
        {
            // adjacent? Attack : (aggroed | vision)? Chase : WanderOrIdle
            var chaseOrWander = Branch(
                requireAll: false,
                new Condition[] { Cond<IsAggroedCondition>(), Cond<PlayerInVisionCondition>() },
                Select<ChaseAction>(),
                Select<WanderOrIdleAction>());
            return Branch(
                requireAll: true,
                new Condition[] { Cond<PlayerAdjacentCondition>() },
                Select<AttackAction>(),
                chaseOrWander);
        }

        private static Node BuildCowardlyRoot()
        {
            // (aggroed & adjacent)? Attack : Sequence(ClearAggro, vision? Flee : WanderOrIdle)
            var afterDisengage = Branch(
                requireAll: true,
                new Condition[] { Cond<PlayerInVisionCondition>() },
                Select<FleeAction>(),
                Select<WanderOrIdleAction>());
            var disengage = Sequence(Select<ClearAggroAction>(), afterDisengage);
            return Branch(
                requireAll: true,
                new Condition[] { Cond<IsAggroedCondition>(), Cond<PlayerAdjacentCondition>() },
                Select<AttackAction>(),
                disengage);
        }

        private static Node BuildPassiveRoot()
        {
            // aggroed? AggressiveTree : WanderOrIdle
            return Branch(
                requireAll: true,
                new Condition[] { Cond<IsAggroedCondition>() },
                BuildAggressiveRoot(),
                Select<WanderOrIdleAction>());
        }

        private static Node Branch(bool requireAll, Condition[] conditions, Node ifTrue, Node ifFalse)
        {
            var branch = Activator.CreateInstance(BranchType, nonPublic: true);
            BranchType.GetProperty("RequiresAllConditions")?.SetValue(branch, requireAll);
            BranchType.GetProperty("Conditions")?.SetValue(branch, new List<Condition>(conditions));
            BranchType.GetField("True")?.SetValue(branch, ifTrue);
            BranchType.GetField("False")?.SetValue(branch, ifFalse);

            var composite = (Composite)branch;
            if (ifTrue != null)
            {
                composite.Add(ifTrue);
            }

            if (ifFalse != null)
            {
                composite.Add(ifFalse);
            }

            return (Node)branch;
        }

        private static Node Sequence(params Node[] children)
        {
            var sequence = Activator.CreateInstance(SequenceType, nonPublic: true);
            var composite = (Composite)sequence;
            foreach (var child in children)
            {
                composite.Add(child);
            }

            return (Node)sequence;
        }

        private static BehaviorGraph Build(Node root)
        {
            var graph = ScriptableObject.CreateInstance<BehaviorGraph>();
            graph.hideFlags = HideFlags.HideAndDontSave;
            var moduleType = typeof(BehaviorGraph).Assembly.GetType("Unity.Behavior.BehaviorGraphModule");
            var module = Activator.CreateInstance(moduleType, nonPublic: true);
            moduleType.GetField("Root", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.SetValue(module, root);

            var graphsField = typeof(BehaviorGraph).GetField(
                "Graphs",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var list = graphsField.GetValue(graph);
            list.GetType().GetMethod("Add")?.Invoke(list, new[] { module });

            moduleType
                .GetMethod("InitializeNodes", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.Invoke(module, null);

            WireConditionGraphs(root, module);
            return graph;
        }

        private static void WireConditionGraphs(Node node, object module)
        {
            if (node == null)
            {
                return;
            }

            if (BranchType.IsInstanceOfType(node))
            {
                var conditions = BranchType.GetProperty("Conditions")?.GetValue(node) as IList<Condition>;
                if (conditions != null)
                {
                    var graphField = typeof(Condition).GetField(
                        "Graph",
                        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    foreach (var condition in conditions)
                    {
                        graphField?.SetValue(condition, module);
                    }
                }

                WireConditionGraphs(BranchType.GetField("True")?.GetValue(node) as Node, module);
                WireConditionGraphs(BranchType.GetField("False")?.GetValue(node) as Node, module);
            }

            if (node is Composite composite)
            {
                foreach (var child in composite.Children)
                {
                    WireConditionGraphs(child, module);
                }
            }
        }

        private static object RootModule(BehaviorGraph graph)
        {
            var graphsField = typeof(BehaviorGraph).GetField(
                "Graphs",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var list = graphsField?.GetValue(graph) as System.Collections.IList;
            return list is { Count: > 0 } ? list[0] : null;
        }
    }
}
