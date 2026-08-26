using System.Collections.Generic;
using UnityEngine;

namespace SlimesRevenge
{
    public sealed class Floor
    {
        private readonly Dictionary<Vector2Int, Puddle> puddles = new Dictionary<Vector2Int, Puddle>();
        private readonly Dictionary<Vector2Int, List<Corpse>> corpses = new Dictionary<Vector2Int, List<Corpse>>();

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

        public IReadOnlyList<Corpse> GetCorpses(Vector2Int cell)
        {
            return corpses.TryGetValue(cell, out var list) ? list : System.Array.Empty<Corpse>();
        }

        public void AddCorpse(Corpse corpse)
        {
            if (corpse == null)
            {
                return;
            }

            if (!corpses.TryGetValue(corpse.Cell, out var list))
            {
                list = new List<Corpse>();
                corpses[corpse.Cell] = list;
            }

            list.Add(corpse);
        }

        public bool TryTakeCorpse(Vector2Int cell, int index, out Corpse corpse)
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
                    pair.Value[i].Tick();
                    if (pair.Value[i].Expired)
                    {
                        pair.Value.RemoveAt(i);
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
    }
}
