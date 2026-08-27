using System;
using System.Collections.Generic;

namespace SlimesRevenge
{
    /// <summary>
    /// Discovers concrete <see cref="Substance"/> subclasses via reflection.
    /// </summary>
    public static class SubstanceCatalog
    {
        private static List<Type> types;
        private static List<Substance> samples;

        public static IReadOnlyList<Type> AllTypes
        {
            get
            {
                Ensure();
                return types;
            }
        }

        /// <summary>Live preview instances (one per type) for labels/icons — do not mutate.</summary>
        public static IReadOnlyList<Substance> Samples
        {
            get
            {
                Ensure();
                return samples;
            }
        }

        public static Substance Create(Type type)
        {
            if (type == null || !typeof(Substance).IsAssignableFrom(type) || type.IsAbstract)
            {
                return new Water();
            }

            try
            {
                return (Substance)Activator.CreateInstance(type);
            }
            catch
            {
                return new Water();
            }
        }

        public static Substance Create(Substance sample) =>
            sample == null ? new Water() : Volume.CloneSubstance(sample);

        private static void Ensure()
        {
            if (types != null)
            {
                return;
            }

            types = new List<Type>();
            samples = new List<Substance>();
            var baseType = typeof(Substance);
            var found = baseType.Assembly.GetTypes();
            for (var i = 0; i < found.Length; i++)
            {
                var type = found[i];
                if (
                    type == null
                    || !type.IsClass
                    || type.IsAbstract
                    || !baseType.IsAssignableFrom(type)
                )
                {
                    continue;
                }

                Substance sample;
                try
                {
                    sample = (Substance)Activator.CreateInstance(type);
                }
                catch
                {
                    continue;
                }

                if (sample == null)
                {
                    continue;
                }

                types.Add(type);
                samples.Add(sample);
            }

            types.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            samples.Sort((a, b) => string.CompareOrdinal(a.GetType().Name, b.GetType().Name));
        }

#if UNITY_EDITOR
        public static void ResetCacheForTests()
        {
            types = null;
            samples = null;
        }
#endif
    }
}
