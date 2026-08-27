using System.Collections.Generic;
using UnityEngine;

namespace SlimesRevenge
{
    /// <summary>
    /// Loads 8×4 walk sheets from <c>Resources/Animals/{Kind}</c> (same layout as Art/Animals).
    /// </summary>
    public static class AnimalSprites
    {
        private static readonly Dictionary<CreatureKind, Sprite[]> southWalk =
            new Dictionary<CreatureKind, Sprite[]>();

        /// <summary>Four walk frames facing south (row 0) for menu idle animation.</summary>
        public static Sprite[] SouthWalk(CreatureKind kind)
        {
            if (southWalk.TryGetValue(kind, out var cached) && cached != null && cached.Length > 0)
            {
                return cached;
            }

            var sheetName = kind.ToString();
            var loaded = Resources.LoadAll<Sprite>($"Animals/{sheetName}");
            if (loaded == null || loaded.Length == 0)
            {
                var fallback = IconCatalog.Creature(kind);
                cached = fallback != null ? new[] { fallback } : System.Array.Empty<Sprite>();
                southWalk[kind] = cached;
                return cached;
            }

            var frames = new List<Sprite>(4);
            var prefix = $"{sheetName}_0_";
            for (var i = 0; i < 4; i++)
            {
                var name = prefix + i;
                for (var j = 0; j < loaded.Length; j++)
                {
                    if (loaded[j] != null && loaded[j].name == name)
                    {
                        frames.Add(loaded[j]);
                        break;
                    }
                }
            }

            if (frames.Count == 0)
            {
                // Any sprites from the sheet if naming differs.
                for (var j = 0; j < loaded.Length && frames.Count < 4; j++)
                {
                    if (loaded[j] != null)
                    {
                        frames.Add(loaded[j]);
                    }
                }
            }

            cached = frames.ToArray();
            southWalk[kind] = cached;
            return cached;
        }

#if UNITY_EDITOR
        public static void ResetCacheForTests()
        {
            southWalk.Clear();
        }
#endif
    }
}
