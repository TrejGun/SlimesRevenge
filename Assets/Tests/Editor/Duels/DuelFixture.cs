using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SlimesRevenge.Tests.Duels
{
    /// <summary>
    /// Shared spawn/bind helpers for duel scenarios.
    /// Layout convention for corridor 10×1: slime at x=4, front foe at x=5.
    /// </summary>
    public abstract class DuelFixture
    {
        public static readonly Vector2Int CorridorSlimeCell = new Vector2Int(4, 0);
        public static readonly Vector2Int CorridorFoeCell = new Vector2Int(5, 0);

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
            var slime = Spawn<Slime>(cell);
            slime.Volume.Clear();
            for (var i = 0; i < units; i++)
            {
                slime.Volume.Add(Clone(kind));
            }

            slime.SetMaxHitPoints(1);
            slime.RefreshBodyTraits();
            return slime;
        }

        protected Slime SpawnSlimeWith(Vector2Int cell, params Substance[] substances)
        {
            var slime = Spawn<Slime>(cell);
            slime.Volume.Clear();
            foreach (var substance in substances)
            {
                slime.Volume.Add(Clone(substance));
            }

            slime.SetMaxHitPoints(1);
            slime.RefreshBodyTraits();
            return slime;
        }

        protected TurnManager Bind(World world, Slime slime, params Creature[] foes)
        {
            var turns = SpawnObject("Turns").AddComponent<TurnManager>();
            turns.Rng = new FixedRng();
            turns.Bind(world, slime, foes);
            return turns;
        }

        protected Duel StartCorridorDuel(System.Type beastType, params Substance[] substances)
        {
            var slime = SpawnSlimeWith(CorridorSlimeCell, substances);
            var beast = SpawnBeast(beastType, CorridorFoeCell);
            var turns = Bind(Corridor(), slime, beast);
            return new Duel(turns, slime, beast);
        }

        protected Creature SpawnBeast(System.Type beastType, Vector2Int cell)
        {
            if (beastType == typeof(Rat))
            {
                return Spawn<Rat>(cell);
            }

            if (beastType == typeof(Cat))
            {
                return Spawn<Cat>(cell);
            }

            return Spawn<Dog>(cell);
        }

        protected T Spawn<T>(Vector2Int cell) where T : Creature
        {
            var creature = SpawnObject(typeof(T).Name).AddComponent<T>();
            creature.PlaceOn(cell);
            if (creature.Volume.UnitCount == 0)
            {
                switch (creature)
                {
                    case Rat:
                        Rat.FillStarting(creature.Volume);
                        break;
                    case Cat:
                        Cat.FillStarting(creature.Volume);
                        break;
                    case Dog:
                        Dog.FillStarting(creature.Volume);
                        break;
                }
            }

            switch (creature)
            {
                case Slime:
                    creature.SetMaxHitPoints(1);
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
            }

            return creature;
        }

        protected GameObject SpawnObject(string name)
        {
            var go = new GameObject(name);
            spawned.Add(go);
            return go;
        }

        protected readonly struct Duel
        {
            public Duel(TurnManager turns, Slime slime, Creature beast)
            {
                Turns = turns;
                Slime = slime;
                Beast = beast;
            }

            public TurnManager Turns { get; }
            public Slime Slime { get; }
            public Creature Beast { get; }
        }
    }
}
