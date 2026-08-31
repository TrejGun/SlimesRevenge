using System.Collections.Generic;
using UnityEngine;

namespace SlimesRevenge
{
    /// <summary>
    /// Configurable body for tests. In Runtime so it can be <c>AddComponent</c>'d
    /// (Editor-folder scripts cannot). <see cref="CreatureCatalog"/> skips this type.
    /// </summary>
    public sealed class TestCreature : Creature
    {
        private CreatureKind kind = CreatureKind.Bat;

        private CreaturePersonality personality = CreaturePersonality.Aggressive;

        public override CreatureKind Kind => kind;

        public override CreaturePersonality Personality => personality;

        public static TestCreature Spawn(IList<GameObject> spawned, Vector2Int cell)
        {
            var go = new GameObject("TestCreature");
            spawned?.Add(go);
            var creature = go.AddComponent<TestCreature>();
            creature.PlaceOn(cell);
            return creature;
        }

        public TestCreature As(CreatureKind value)
        {
            kind = value;
            return this;
        }

        public TestCreature Role(CreaturePersonality value)
        {
            personality = value;
            return this;
        }

        public TestCreature WithHp(int value)
        {
            SetMaxHitPoints(value);
            return this;
        }

        public TestCreature WithSpeed(int value)
        {
            SetSpeed(value);
            return this;
        }

        public TestCreature WithArmor(int value)
        {
            SetArmor(value);
            return this;
        }

        public TestCreature Fill(params Substance[] substances)
        {
            Volume.Clear();
            if (substances != null && substances.Length > 0)
            {
                Volume.Fill(substances);
            }

            return this;
        }

        public TestCreature Trait<T>()
            where T : InnateTrait, new()
        {
            EnsureInnate<T>();
            return this;
        }
    }
}
