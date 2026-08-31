using NUnit.Framework;
using UnityEngine;

namespace SlimesRevenge.Tests.Duels
{
    /// <summary>
    /// Shared spawn/bind helpers for duel scenarios.
    /// Layout convention for corridor 10×1: slime at x=4, front foe at x=5.
    /// </summary>
    public abstract class DuelFixture : SpawnFixture
    {
        public static readonly Vector2Int CorridorSlimeCell = new Vector2Int(4, 0);
        public static readonly Vector2Int CorridorFoeCell = new Vector2Int(5, 0);

        protected static World Corridor()
        {
            return new World(10, 1, TerrainType.Grass);
        }

        protected static World Plane(int size = World.DefaultSize)
        {
            return World.CreateGrass(size);
        }

        protected static Substance Clone(Substance substance)
        {
            return Volume.CloneSubstance(substance);
        }

        protected Slime SpawnFilled(Vector2Int cell, Substance kind, int units)
        {
            var slime = SpawnSlime(cell);
            slime.Volume.Clear();
            for (var i = 0; i < units; i++)
            {
                slime.Volume.Add(Clone(kind));
            }

            slime.RefreshVolumeStatuses();
            return slime;
        }

        protected Slime SpawnSlimeWith(Vector2Int cell, params Substance[] substances)
        {
            var slime = SpawnSlime(cell);
            slime.Volume.Clear();
            foreach (var substance in substances)
            {
                slime.Volume.Add(Clone(substance));
            }

            slime.RefreshVolumeStatuses();
            return slime;
        }

        protected TurnManager Bind(World world, Slime slime, params Creature[] foes)
        {
            var turns = SpawnObject("Turns").AddComponent<TurnManager>();
            turns.Rng = new FixedRng();
            turns.Bind(world, slime, foes);
            return turns;
        }

        protected Duel StartCorridorDuel(TestFoePreset foe, params Substance[] substances)
        {
            return StartCorridorDuel(TestCreatures.For(foe), substances);
        }

        protected Duel StartCorridorDuel(TestCreatureFactory foe, params Substance[] substances)
        {
            var slime = SpawnSlimeWith(CorridorSlimeCell, substances);
            var enemy = foe(spawned, CorridorFoeCell);
            var turns = Bind(Corridor(), slime, enemy);
            return new Duel(turns, slime, enemy);
        }

        protected readonly struct Duel
        {
            public Duel(TurnManager turns, Slime slime, Creature enemy)
            {
                Turns = turns;
                Slime = slime;
                Enemy = enemy;
            }

            public TurnManager Turns { get; }
            public Slime Slime { get; }
            public Creature Enemy { get; }
        }
    }
}
