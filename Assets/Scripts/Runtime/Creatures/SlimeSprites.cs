using System.Collections.Generic;
using UnityEngine;

namespace SlimesRevenge
{
    /// <summary>
    /// FDR slime sheets (same 16-cell rows as enemy walk): south / west / east / north,
    /// walk is columns 0–3. Frames are sliced from the substance texture already on the slime.
    /// </summary>
    public static class SlimeSprites
    {
        private static readonly Dictionary<(Texture, WalkFacing), Sprite[]> walk =
            new Dictionary<(Texture, WalkFacing), Sprite[]>();

        public static Sprite[] Walk(Sprite sheetMember, WalkFacing facing)
        {
            if (sheetMember == null || sheetMember.texture == null)
            {
                return System.Array.Empty<Sprite>();
            }

            var key = ((Texture)sheetMember.texture, facing);
            if (walk.TryGetValue(key, out var cached) && cached != null && cached.Length > 0)
            {
                return cached;
            }

            var texture = sheetMember.texture;
            var cell = Mathf.Max(1, Mathf.RoundToInt(sheetMember.rect.width));
            var ppu = sheetMember.pixelsPerUnit;
            var frames = new Sprite[FdrSheetLayout.WalkFrameCount];
            for (var i = 0; i < frames.Length; i++)
            {
                frames[i] = Sprite.Create(
                    texture,
                    FdrSheetLayout.WorldRect(
                        facing,
                        FdrSheetLayout.WalkColumn + i,
                        texture.height,
                        cell
                    ),
                    new Vector2(0.5f, 0.5f),
                    ppu
                );
            }

            walk[key] = frames;
            return frames;
        }

#if UNITY_EDITOR
        public static void ResetCacheForTests()
        {
            walk.Clear();
        }
#endif
    }
}
