using UnityEngine;

namespace SlimesRevenge
{
    public enum WalkFacing
    {
        South = 0,
        West = 1,
        East = 2,
        North = 3,
    }

    public enum FdrClip
    {
        Idle,
        Walk,
        Attack,
        Damage,
        Dead,
    }

    /// <summary>
    /// Single source of truth for Fantasy Dreamland Reborn enemy sheets.
    /// Native canvas is 512×208: 4×16 cells of 32×32 (world clips) then 8× 64×64 battle portraits.
    /// Change this class when the sheet format changes — clip loading and playback stay the same.
    /// </summary>
    public static class FdrSheetLayout
    {
        public const int CellsPerRow = 16;

        public const int FacingRows = 4;

        public const int ClipFrameCount = 4;

        public const int WalkFrameCount = ClipFrameCount;

        public const int WorldCellCount = CellsPerRow * FacingRows;

        public const int BattleStartIndex = WorldCellCount;

        public const int BattleFrameCount = 8;

        public const int WalkCellPixels = 32;

        public const int BattleCellPixels = 64;

        public const int WalkColumn = 0;

        public const int IdleColumn = 1;

        public const int AttackColumn = 4;

        public const int DamageColumn = 8;

        public const int DeadColumn = 12;

        public static int StartColumn(FdrClip clip)
        {
            switch (clip)
            {
                case FdrClip.Idle:
                    return IdleColumn;
                case FdrClip.Walk:
                    return WalkColumn;
                case FdrClip.Attack:
                    return AttackColumn;
                case FdrClip.Damage:
                    return DamageColumn;
                case FdrClip.Dead:
                    return DeadColumn;
                default:
                    return WalkColumn;
            }
        }

        public static int FrameCount(FdrClip clip)
        {
            return clip == FdrClip.Idle ? 1 : ClipFrameCount;
        }

        public static int Index(WalkFacing facing, FdrClip clip, int frame)
        {
            return (int)facing * CellsPerRow + StartColumn(clip) + frame;
        }

        public static int BattleIndex(int frame)
        {
            return BattleStartIndex + frame;
        }

        /// <summary>Unity texture rect (origin bottom-left) for a 32×32 world cell.</summary>
        public static Rect WorldRect(WalkFacing facing, int column, int textureHeight, int cell)
        {
            var size = cell > 0 ? cell : WalkCellPixels;
            var x = column * size;
            var y = textureHeight - ((int)facing + 1) * size;
            return new Rect(x, y, size, size);
        }

        /// <summary>Unity texture rect for one 64×64 south battle portrait (under the four walk rows).</summary>
        public static Rect BattleRect(int frame, int textureHeight)
        {
            var y = textureHeight - FacingRows * WalkCellPixels - BattleCellPixels;
            return new Rect(frame * BattleCellPixels, y, BattleCellPixels, BattleCellPixels);
        }

        public static string SpriteName(string sheet, int index)
        {
            return sheet + "_" + index;
        }

        public static WalkFacing FacingToward(Vector2Int from, Vector2Int to)
        {
            var delta = to - from;
            if (delta == Vector2Int.zero)
            {
                return WalkFacing.West;
            }

            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
            {
                return delta.x < 0 ? WalkFacing.West : WalkFacing.East;
            }

            return delta.y < 0 ? WalkFacing.South : WalkFacing.North;
        }
    }
}
