using System;

namespace SlimesRevenge
{
    public enum RunKind
    {
        /// <summary>10×10 grass with the full creature cast (former default Game scene).</summary>
        Campaign,
        Duel,
    }

    /// <summary>
    /// One-shot bootstrap payload from Main Menu → Game scene.
    /// </summary>
    public sealed class RunConfig
    {
        public const int DuelWidth = 15;
        public const int DuelHeight = 5;

        /// <summary>Chebyshev/manhattan gap between slime and foe on the duel arena.</summary>
        public const int DuelSeparation = 7;
        public const int LoadoutSlots = 5;

        private static RunConfig pending;

        public RunKind Kind { get; private set; }

        public CreatureKind Opponent { get; private set; }

        /// <summary>
        /// Cloned substance units for duel slime (length = <see cref="LoadoutSlots"/>).
        /// Null for campaign — slime keeps <see cref="Slime.FillStarting"/>.
        /// </summary>
        public Substance[] Loadout { get; private set; }

        public static bool HasPending => pending != null;

        public static void SetCampaign()
        {
            pending = new RunConfig
            {
                Kind = RunKind.Campaign,
                Opponent = CreatureKind.Slime,
                Loadout = null,
            };
        }

        public static void SetDuel(CreatureKind opponent, Substance[] loadout)
        {
            if (loadout == null || loadout.Length != LoadoutSlots)
            {
                throw new ArgumentException(
                    $"Duel loadout must have {LoadoutSlots} substances.",
                    nameof(loadout)
                );
            }

            var copy = new Substance[LoadoutSlots];
            for (var i = 0; i < LoadoutSlots; i++)
            {
                copy[i] = Volume.CloneSubstance(loadout[i] ?? new Water());
            }

            pending = new RunConfig
            {
                Kind = RunKind.Duel,
                Opponent = opponent,
                Loadout = copy,
            };
        }

        public static bool TryPeek(out RunConfig config)
        {
            config = pending;
            return config != null;
        }

        public static bool TryConsume(out RunConfig config)
        {
            config = pending;
            pending = null;
            return config != null;
        }

        public static void Clear()
        {
            pending = null;
        }

        public static Substance[] DefaultWaterLoadout()
        {
            var units = new Substance[LoadoutSlots];
            for (var i = 0; i < LoadoutSlots; i++)
            {
                units[i] = new Water();
            }

            return units;
        }
    }
}
