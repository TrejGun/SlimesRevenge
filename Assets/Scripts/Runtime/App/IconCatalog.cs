using System;
using System.Collections.Generic;
using UnityEngine;

namespace SlimesRevenge
{
    /// <summary>
    /// Loads UI sprites from <c>Resources/Icons</c>.
    /// New substance/status types must <see cref="RegisterSubstance"/> / <see cref="RegisterStatus"/>
    /// (or be listed in the built-in maps) so cards and the action log can resolve icons.
    /// </summary>
    public static class IconCatalog
    {
        private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        private static readonly Dictionary<Type, string> substanceKeys = new Dictionary<
            Type,
            string
        >
        {
            { typeof(Water), "Icons/substance_water" },
            { typeof(Oil), "Icons/substance_oil" },
            { typeof(Poison), "Icons/substance_poison" },
            { typeof(Acid), "Icons/substance_acid" },
            { typeof(Blood), "Icons/substance_blood" },
            { typeof(Lava), "Icons/substance_lava" },
            { typeof(Mercury), "Icons/substance_mercury" },
        };

        private static readonly Dictionary<Type, string> statusKeys = new Dictionary<Type, string>
        {
            { typeof(Poisoned), "Icons/status_poisoned" },
            { typeof(Corroding), "Icons/status_corroding" },
            { typeof(Instability), "Icons/status_instability" },
            { typeof(Burning), "Icons/status_burning" },
            { typeof(Wet), "Icons/status_wet" },
            { typeof(Fireproof), "Icons/status_fireproof" },
            { typeof(Invisible), "Icons/status_invisible" },
            { typeof(Flammable), "Icons/status_flammable" },
            { typeof(Retaliation), "Icons/status_retaliation" },
            { typeof(Regeneration), "Icons/status_regeneration" },
            { typeof(Vampirism), "Icons/status_vampirism" },
            { typeof(Poisonous), "Icons/status_poisonous" },
            { typeof(HatesRats), "Icons/status_hates_rats" },
            { typeof(HatesCats), "Icons/status_hates_cats" },
            { typeof(FearsCats), "Icons/status_fears_cats" },
            { typeof(FearsDogs), "Icons/status_fears_dogs" },
            { typeof(FearsWater), "Icons/status_fears_water" },
            { typeof(Digesting), "Icons/status_digesting" },
        };

        /// <summary>Map a substance CLR type to a Resources path (e.g. <c>Icons/substance_slime_goo</c>).</summary>
        public static void RegisterSubstance(Type type, string resourcePath)
        {
            if (type == null || string.IsNullOrEmpty(resourcePath))
            {
                return;
            }

            substanceKeys[type] = resourcePath;
        }

        /// <summary>Map a status CLR type to a Resources path (e.g. <c>Icons/status_frozen</c>).</summary>
        public static void RegisterStatus(Type type, string resourcePath)
        {
            if (type == null || string.IsNullOrEmpty(resourcePath))
            {
                return;
            }

            statusKeys[type] = resourcePath;
        }

        public static string SubstanceKey(Type type)
        {
            return type != null && substanceKeys.TryGetValue(type, out var key) ? key : null;
        }

        public static string StatusKey(Type type)
        {
            return type != null && statusKeys.TryGetValue(type, out var key) ? key : null;
        }

        public static Sprite Substance(Substance substance)
        {
            return substance == null ? null : Load(SubstanceKey(substance.GetType()));
        }

        public static Sprite Status(StatusEffect effect)
        {
            return effect == null ? null : Load(StatusKey(effect.GetType()));
        }

        public static Sprite Creature(CreatureKind kind)
        {
            var path = $"Icons/creature_{kind.ToString().ToLowerInvariant()}";
            var sprite = Load(path);
            return sprite != null ? sprite : PlaceholderCreature(kind);
        }

        private static Sprite PlaceholderCreature(CreatureKind kind)
        {
            var key = $"__placeholder_creature_{kind}";
            if (cache.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            var color = kind switch
            {
                CreatureKind.Rat => new Color(0.55f, 0.45f, 0.35f),
                CreatureKind.Cat => new Color(0.85f, 0.65f, 0.25f),
                CreatureKind.Dog => new Color(0.55f, 0.4f, 0.25f),
                CreatureKind.Bat => new Color(0.35f, 0.3f, 0.45f),
                CreatureKind.Scorpion => new Color(0.7f, 0.45f, 0.15f),
                CreatureKind.Mushroom => new Color(0.75f, 0.2f, 0.2f),
                CreatureKind.Undine => new Color(0.35f, 0.65f, 0.9f),
                CreatureKind.Cactus => new Color(0.35f, 0.7f, 0.3f),
                CreatureKind.Mandragora => new Color(0.45f, 0.55f, 0.25f),
                CreatureKind.Eye => new Color(0.7f, 0.15f, 0.15f),
                _ => new Color(0.4f, 0.7f, 0.4f),
            };

            const int size = 48;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };
            var cx = (size - 1) * 0.5f;
            var r = size * 0.42f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = x - cx;
                    var dy = y - cx;
                    tex.SetPixel(x, y, dx * dx + dy * dy <= r * r ? color : Color.clear);
                }
            }

            tex.Apply();
            var generated = Sprite.Create(
                tex,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                size
            );
            generated.hideFlags = HideFlags.HideAndDontSave;
            cache[key] = generated;
            return generated;
        }

        public static Sprite Load(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath))
            {
                return null;
            }

            if (cache.TryGetValue(resourcePath, out var cached))
            {
                // UnityEngine.Object overload: destroyed sprites count as null → retry load.
                if (cached != null)
                {
                    return cached;
                }

                cache.Remove(resourcePath);
            }

            var sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite != null)
            {
                cache[resourcePath] = sprite;
            }

            return sprite;
        }
    }
}
