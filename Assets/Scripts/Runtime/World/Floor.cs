using System.Collections.Generic;
using UnityEngine;

namespace SlimesRevenge
{
    public sealed class Floor
    {
        private readonly Dictionary<Vector2Int, Puddle> puddles = new Dictionary<Vector2Int, Puddle>();
        private readonly Dictionary<Vector2Int, List<Creature>> corpses = new Dictionary<Vector2Int, List<Creature>>();

        public Puddle GetPuddle(Vector2Int cell)
        {
            return puddles.TryGetValue(cell, out var puddle) ? puddle : null;
        }

        public bool TryPlacePuddle(Vector2Int cell, Substance substance)
        {
            if (substance == null || puddles.ContainsKey(cell))
            {
                return false;
            }

            puddles[cell] = new Puddle(substance);
            return true;
        }

        public bool TryRemovePuddle(Vector2Int cell)
        {
            return puddles.Remove(cell);
        }

        /// <summary>
        /// Floor contact: non-slimes get <see cref="Substance.Apply"/> from the puddle and
        /// consume it; slimes leave the puddle alone.
        /// </summary>
        public void ApplyContact(Creature creature)
        {
            if (creature == null)
            {
                return;
            }

            var cell = creature.Cell;
            if (!puddles.TryGetValue(cell, out var puddle))
            {
                return;
            }

            if (creature is Slime)
            {
                return;
            }

            puddle.Substance.Apply(creature);
            puddles.Remove(cell);
        }

        public bool TryCollectPuddle(Vector2Int cell, out Substance substance)
        {
            substance = null;
            if (!puddles.TryGetValue(cell, out var puddle))
            {
                return false;
            }

            substance = puddle.Substance;
            puddles.Remove(cell);
            return true;
        }

        public IReadOnlyList<Creature> GetCorpses(Vector2Int cell)
        {
            return corpses.TryGetValue(cell, out var list) ? list : System.Array.Empty<Creature>();
        }

        public void AddCorpse(Creature corpse)
        {
            if (corpse == null || !corpse.IsCorpse)
            {
                return;
            }

            if (!corpses.TryGetValue(corpse.Cell, out var list))
            {
                list = new List<Creature>();
                corpses[corpse.Cell] = list;
            }

            list.Add(corpse);
        }

        public bool TryTakeCorpse(Vector2Int cell, int index, out Creature corpse)
        {
            corpse = null;
            if (!corpses.TryGetValue(cell, out var list) || index < 0 || index >= list.Count)
            {
                return false;
            }

            corpse = list[index];
            list.RemoveAt(index);
            if (list.Count == 0)
            {
                corpses.Remove(cell);
            }

            return true;
        }

        public void Tick()
        {
            var empty = new List<Vector2Int>();
            foreach (var pair in corpses)
            {
                for (var i = pair.Value.Count - 1; i >= 0; i--)
                {
                    var body = pair.Value[i];
                    if (body == null)
                    {
                        pair.Value.RemoveAt(i);
                        continue;
                    }

                    body.TickCorpseDecay();
                    if (body.CorpseExpired)
                    {
                        pair.Value.RemoveAt(i);
                        DestroyCorpse(body);
                    }
                }

                if (pair.Value.Count == 0)
                {
                    empty.Add(pair.Key);
                }
            }

            foreach (var cell in empty)
            {
                corpses.Remove(cell);
            }
        }

        private static void DestroyCorpse(Creature body)
        {
            if (body == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(body.gameObject);
            }
            else
            {
                Object.DestroyImmediate(body.gameObject);
            }
        }
    }
}
