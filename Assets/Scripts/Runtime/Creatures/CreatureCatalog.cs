using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SlimesRevenge
{
    /// <summary>
    /// Discovers concrete <see cref="Creature"/> subclasses via reflection (no hand-maintained list).
    /// </summary>
    public static class CreatureCatalog
    {
        private static List<Entry> entries;
        private static Dictionary<CreatureKind, Type> byKind;
        private static Dictionary<Type, CreatureKind> byType;

        public readonly struct Entry
        {
            public Entry(CreatureKind kind, Type type)
            {
                Kind = kind;
                Type = type;
            }

            public CreatureKind Kind { get; }
            public Type Type { get; }
        }

        public static IReadOnlyList<Entry> All
        {
            get
            {
                Ensure();
                return entries;
            }
        }

        /// <summary>All creatures except the player slime and retired animals.</summary>
        public static IReadOnlyList<Entry> Opponents
        {
            get
            {
                Ensure();
                var list = new List<Entry>(entries.Count);
                for (var i = 0; i < entries.Count; i++)
                {
                    if (entries[i].Kind != CreatureKind.Slime && !IsRetired(entries[i].Kind))
                    {
                        list.Add(entries[i]);
                    }
                }

                return list;
            }
        }

        /// <summary>Rat / Cat / Dog / Scorpion stay in code and tests but are not in the live roster.</summary>
        public static bool IsRetired(CreatureKind kind)
        {
            return kind == CreatureKind.Rat
                || kind == CreatureKind.Cat
                || kind == CreatureKind.Dog
                || kind == CreatureKind.Scorpion;
        }

        public static Type TypeOf(CreatureKind kind)
        {
            Ensure();
            return byKind.TryGetValue(kind, out var type) ? type : null;
        }

        public static bool TryKindOf(Type type, out CreatureKind kind)
        {
            Ensure();
            if (type != null && byType.TryGetValue(type, out kind))
            {
                return true;
            }

            kind = default;
            return false;
        }

        private static void Ensure()
        {
            if (entries != null)
            {
                return;
            }

            entries = new List<Entry>();
            byKind = new Dictionary<CreatureKind, Type>();
            byType = new Dictionary<Type, CreatureKind>();

            var creatureType = typeof(Creature);
            var types = creatureType.Assembly.GetTypes();
            var scratch = new GameObject("CreatureCatalog_Scratch")
            {
                hideFlags = HideFlags.HideAndDontSave,
            };

            try
            {
                for (var i = 0; i < types.Length; i++)
                {
                    var type = types[i];
                    if (
                        type == null
                        || !type.IsClass
                        || type.IsAbstract
                        || type == typeof(TestCreature)
                        || !creatureType.IsAssignableFrom(type)
                    )
                    {
                        continue;
                    }

                    Creature sample = null;
                    try
                    {
                        sample = scratch.AddComponent(type) as Creature;
                    }
                    catch
                    {
                        continue;
                    }

                    if (sample == null)
                    {
                        continue;
                    }

                    var kind = sample.Kind;
                    if (byKind.ContainsKey(kind))
                    {
                        continue;
                    }

                    byKind[kind] = type;
                    byType[type] = kind;
                    entries.Add(new Entry(kind, type));

                    if (Application.isPlaying)
                    {
                        UnityEngine.Object.Destroy(sample);
                    }
                    else
                    {
                        UnityEngine.Object.DestroyImmediate(sample);
                    }
                }
            }
            finally
            {
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(scratch);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(scratch);
                }
            }

            entries.Sort((a, b) => ((int)a.Kind).CompareTo((int)b.Kind));
        }

#if UNITY_EDITOR
        /// <summary>EditMode tests may reset static caches between runs.</summary>
        public static void ResetCacheForTests()
        {
            entries = null;
            byKind = null;
            byType = null;
        }
#endif
    }
}
