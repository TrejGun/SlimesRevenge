using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SlimesRevenge.Tests
{
    /// <summary>Shared spawn bag for EditMode tests. Prefer <see cref="TestCreature"/> over Cat/Dog.</summary>
    public abstract class SpawnFixture
    {
        protected readonly List<GameObject> spawned = new List<GameObject>();

        [TearDown]
        public void TearDownSpawned()
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

        protected TestCreature Body(Vector2Int cell)
        {
            return TestCreatures.Body(spawned, cell);
        }

        protected TestCreature Aggressive(Vector2Int cell)
        {
            return TestCreatures.Aggressive(spawned, cell);
        }

        protected TestCreature Passive(Vector2Int cell)
        {
            return TestCreatures.Passive(spawned, cell);
        }

        protected TestCreature Cowardly(Vector2Int cell)
        {
            return TestCreatures.Cowardly(spawned, cell);
        }

        protected Slime SpawnSlime(Vector2Int cell)
        {
            var slime = SpawnObject("Slime").AddComponent<Slime>();
            slime.PlaceOn(cell);
            if (slime.Volume.UnitCount == 0)
            {
                Slime.FillStarting(slime.Volume);
            }

            slime.SetMaxHitPoints(0);
            slime.RefreshVolumeStatuses();
            return slime;
        }

        protected GameObject SpawnObject(string name)
        {
            var go = new GameObject(name);
            spawned.Add(go);
            return go;
        }
    }
}
