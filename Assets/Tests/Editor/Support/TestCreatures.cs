using System.Collections.Generic;
using UnityEngine;

namespace SlimesRevenge.Tests
{
    public delegate TestCreature TestCreatureFactory(IList<GameObject> spawned, Vector2Int cell);

    /// <summary>
    /// Named presets for <see cref="TestCaseSource"/> (Unity cannot serialize delegates).
    /// Kind stamps exist so fear/hate statuses match; these are not Cat.cs / Dog.cs.
    /// </summary>
    public enum TestFoePreset
    {
        Cowardly,
        Passive,
        Aggressive,
    }

    /// <summary>
    /// Optional presets for <see cref="TestCreature"/>. Tests may also configure a blank
    /// body. Kind stamps exist so fear/hate statuses match; these are not Cat.cs / Dog.cs.
    /// </summary>
    public static class TestCreatures
    {
        public static TestCreatureFactory For(TestFoePreset preset)
        {
            return preset switch
            {
                TestFoePreset.Passive => Passive,
                TestFoePreset.Aggressive => Aggressive,
                _ => Cowardly,
            };
        }

        public static TestCreature Body(IList<GameObject> spawned, Vector2Int cell)
        {
            return TestCreature.Spawn(spawned, cell).WithHp(5).Fill(new Water());
        }

        public static TestCreature Aggressive(IList<GameObject> spawned, Vector2Int cell)
        {
            return TestCreature
                .Spawn(spawned, cell)
                .As(CreatureKind.Dog)
                .Role(CreaturePersonality.Aggressive)
                .WithSpeed(2)
                .WithHp(10)
                .Fill(new Blood(), new Blood(), new Blood())
                .Trait<HatesCats>();
        }

        public static TestCreature Passive(IList<GameObject> spawned, Vector2Int cell)
        {
            return TestCreature
                .Spawn(spawned, cell)
                .As(CreatureKind.Cat)
                .Role(CreaturePersonality.Passive)
                .WithHp(5)
                .Fill(new Blood(), new Blood())
                .Trait<HatesRats>()
                .Trait<FearsDogs>()
                .Trait<FearsWater>();
        }

        public static TestCreature Cowardly(IList<GameObject> spawned, Vector2Int cell)
        {
            return TestCreature
                .Spawn(spawned, cell)
                .As(CreatureKind.Rat)
                .Role(CreaturePersonality.Cowardly)
                .WithHp(3)
                .Fill(new Blood())
                .Trait<FearsCats>();
        }
    }
}
