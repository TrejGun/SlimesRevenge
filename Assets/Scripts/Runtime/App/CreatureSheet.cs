using System.Collections.Generic;
using UnityEngine;

namespace SlimesRevenge
{
    /// <summary>
    /// Loads FDR clips and battle portraits from <c>Resources/Animals/{Kind}</c> using
    /// <see cref="FdrSheetLayout"/>. Same sheet format for every pack enemy.
    /// </summary>
    public static class CreatureSheet
    {
        private static readonly Dictionary<CreatureKind, Sprite[]> sheets =
            new Dictionary<CreatureKind, Sprite[]>();

        private static readonly Dictionary<(CreatureKind, WalkFacing, FdrClip), Sprite[]> clips =
            new Dictionary<(CreatureKind, WalkFacing, FdrClip), Sprite[]>();

        private static readonly Dictionary<CreatureKind, Sprite[]> portraits =
            new Dictionary<CreatureKind, Sprite[]>();

        public static Sprite[] Walk(CreatureKind kind, WalkFacing facing)
        {
            return Clip(kind, facing, FdrClip.Walk);
        }

        public static Sprite[] Clip(CreatureKind kind, WalkFacing facing, FdrClip clip)
        {
            var key = (kind, facing, clip);
            if (clips.TryGetValue(key, out var cached) && cached != null && cached.Length > 0)
            {
                return cached;
            }

            var loaded = Load(kind);
            if (loaded.Length == 0)
            {
                cached = FallbackIcon(kind);
                clips[key] = cached;
                return cached;
            }

            var sheetName = kind.ToString();
            var count = FdrSheetLayout.FrameCount(clip);
            cached = CollectNamed(loaded, sheetName, facing, clip, count);
            if (cached.Length == 0)
            {
                cached = CollectFirst(loaded, count);
            }

            if (cached.Length == 0)
            {
                cached = FallbackIcon(kind);
            }

            clips[key] = cached;
            return cached;
        }

        /// <summary>Eight 64×64 south battle frames for duel chips and other UI avatars.</summary>
        public static Sprite[] Portrait(CreatureKind kind)
        {
            if (portraits.TryGetValue(kind, out var cached) && cached != null && cached.Length > 0)
            {
                return cached;
            }

            var loaded = Load(kind);
            var texture = TextureOf(loaded);
            if (texture == null)
            {
                cached = FallbackIcon(kind);
                portraits[kind] = cached;
                return cached;
            }

            var sheetName = kind.ToString();
            var ppu = (float)FdrSheetLayout.WalkCellPixels;
            for (var i = 0; i < loaded.Length; i++)
            {
                if (loaded[i] != null)
                {
                    ppu = loaded[i].pixelsPerUnit;
                    break;
                }
            }
            var frames = new Sprite[FdrSheetLayout.BattleFrameCount];
            for (var i = 0; i < frames.Length; i++)
            {
                var sprite = Sprite.Create(
                    texture,
                    FdrSheetLayout.BattleRect(i, texture.height),
                    new Vector2(0.5f, 0.5f),
                    ppu
                );
                sprite.name = FdrSheetLayout.SpriteName(sheetName, FdrSheetLayout.BattleIndex(i));
                frames[i] = sprite;
            }

            portraits[kind] = frames;
            return frames;
        }

        private static Texture2D TextureOf(Sprite[] loaded)
        {
            if (loaded == null)
            {
                return null;
            }

            for (var i = 0; i < loaded.Length; i++)
            {
                if (loaded[i] != null && loaded[i].texture != null)
                {
                    return loaded[i].texture;
                }
            }

            return null;
        }

        private static Sprite[] Load(CreatureKind kind)
        {
            if (sheets.TryGetValue(kind, out var loaded) && loaded != null)
            {
                return loaded;
            }

            loaded = Resources.LoadAll<Sprite>($"Animals/{kind}");
            if (loaded == null)
            {
                loaded = System.Array.Empty<Sprite>();
            }

            sheets[kind] = loaded;
            return loaded;
        }

        private static Sprite[] CollectNamed(
            Sprite[] loaded,
            string sheetName,
            WalkFacing facing,
            FdrClip clip,
            int count
        )
        {
            var frames = new List<Sprite>(count);
            for (var i = 0; i < count; i++)
            {
                var found = FindByName(
                    loaded,
                    FdrSheetLayout.SpriteName(sheetName, FdrSheetLayout.Index(facing, clip, i))
                );
                if (found != null)
                {
                    frames.Add(found);
                }
            }

            return frames.ToArray();
        }

        private static Sprite[] CollectFirst(Sprite[] loaded, int count)
        {
            var frames = new List<Sprite>(count);
            for (var j = 0; j < loaded.Length && frames.Count < count; j++)
            {
                if (loaded[j] != null)
                {
                    frames.Add(loaded[j]);
                }
            }

            return frames.ToArray();
        }

        private static Sprite[] FallbackIcon(CreatureKind kind)
        {
            var fallback = IconCatalog.Creature(kind);
            return fallback != null ? new[] { fallback } : System.Array.Empty<Sprite>();
        }

        private static Sprite FindByName(Sprite[] loaded, string name)
        {
            for (var j = 0; j < loaded.Length; j++)
            {
                if (loaded[j] != null && loaded[j].name == name)
                {
                    return loaded[j];
                }
            }

            return null;
        }

#if UNITY_EDITOR
        public static void ResetCacheForTests()
        {
            sheets.Clear();
            clips.Clear();
            portraits.Clear();
        }
#endif
    }
}
