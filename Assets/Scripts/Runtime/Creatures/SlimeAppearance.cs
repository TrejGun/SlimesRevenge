using System.Collections.Generic;

namespace SlimesRevenge
{
    public enum SlimeLook
    {
        Water,
        Oil,
        Poison,
        Acid,
        Blood,
        Lava,
    }

    public static class SlimeAppearance
    {
        /// <summary>
        /// Plurality among substance looks. Ties use <see cref="Substance.AppearanceTieBreak"/>
        /// (water → poison → acid → oil → blood → lava). Empty volume → water.
        /// </summary>
        public static SlimeLook FromVolume(Volume volume)
        {
            if (volume == null || volume.UnitCount == 0)
            {
                return SlimeLook.Water;
            }

            var counts = new Dictionary<SlimeLook, int>();
            var ties = new Dictionary<SlimeLook, int>();
            var units = volume.Units;
            for (var i = 0; i < units.Count; i++)
            {
                var substance = units[i].Substance;
                if (substance == null)
                {
                    continue;
                }

                var look = substance.Look;
                counts.TryGetValue(look, out var count);
                counts[look] = count + 1;
                ties[look] = substance.AppearanceTieBreak;
            }

            var bestLook = SlimeLook.Water;
            var bestCount = -1;
            var bestTie = int.MaxValue;
            foreach (var pair in counts)
            {
                var look = pair.Key;
                var count = pair.Value;
                var tie = ties[look];
                if (count > bestCount || (count == bestCount && tie < bestTie))
                {
                    bestLook = look;
                    bestCount = count;
                    bestTie = tie;
                }
            }

            return bestLook;
        }
    }
}
