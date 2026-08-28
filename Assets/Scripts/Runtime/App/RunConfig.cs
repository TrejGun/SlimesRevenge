using System;
using System.Text;

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
    /// Editor play-mode domain reload is handled by <c>RunConfigPlayModeBridge</c> (SessionState, no files).
    /// </summary>
    public sealed class RunConfig
    {
        public const int DuelWidth = 15;
        public const int DuelHeight = 5;

        /// <summary>Chebyshev/manhattan gap between slime and foe on the duel arena.</summary>
        public const int DuelSeparation = 7;
        public const int LoadoutSlots = 5;

        private static RunConfig pending;

        /// <summary>Fired when pending is cleared (Editor bridge erases SessionState).</summary>
        public static event Action Cleared;

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
            if (config != null)
            {
                Cleared?.Invoke();
            }

            return config != null;
        }

        public static void Clear()
        {
            var had = pending != null;
            pending = null;
            if (had)
            {
                Cleared?.Invoke();
            }
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

        /// <summary>Serialize pending for Editor SessionState (no disk).</summary>
        public static bool TryExportPending(out string payload)
        {
            if (pending == null)
            {
                payload = null;
                return false;
            }

            payload = Export(pending);
            return true;
        }

        /// <summary>Restore pending after Editor domain reload.</summary>
        public static void ImportPending(string payload)
        {
            pending = string.IsNullOrEmpty(payload) ? null : Parse(payload);
        }

#if UNITY_EDITOR
        public static void DropStaticForTests()
        {
            pending = null;
        }
#endif

        private static string Export(RunConfig config)
        {
            var sb = new StringBuilder();
            sb.Append(config.Kind);
            sb.Append('|');
            sb.Append(config.Opponent);
            sb.Append('|');
            if (config.Loadout != null)
            {
                for (var i = 0; i < config.Loadout.Length; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(',');
                    }

                    var unit = config.Loadout[i];
                    sb.Append(unit != null ? unit.GetType().Name : nameof(Water));
                }
            }

            return sb.ToString();
        }

        private static RunConfig Parse(string text)
        {
            var parts = text.Trim().Split('|');
            if (parts.Length < 2)
            {
                return null;
            }

            if (!Enum.TryParse(parts[0], out RunKind kind))
            {
                return null;
            }

            if (!Enum.TryParse(parts[1], out CreatureKind opponent))
            {
                opponent = CreatureKind.Rat;
            }

            Substance[] loadout = null;
            if (kind == RunKind.Duel)
            {
                loadout = new Substance[LoadoutSlots];
                var names =
                    parts.Length >= 3 && !string.IsNullOrEmpty(parts[2])
                        ? parts[2].Split(',')
                        : Array.Empty<string>();
                for (var i = 0; i < LoadoutSlots; i++)
                {
                    var name = i < names.Length ? names[i] : nameof(Water);
                    loadout[i] = SubstanceCatalog.Create(FindSubstanceType(name));
                }
            }

            return new RunConfig
            {
                Kind = kind,
                Opponent = opponent,
                Loadout = loadout,
            };
        }

        private static Type FindSubstanceType(string name)
        {
            var all = SubstanceCatalog.AllTypes;
            for (var i = 0; i < all.Count; i++)
            {
                if (string.Equals(all[i].Name, name, StringComparison.Ordinal))
                {
                    return all[i];
                }
            }

            return typeof(Water);
        }
    }
}
