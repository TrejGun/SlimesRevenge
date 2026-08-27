using System.Collections.Generic;
using UnityEngine;

namespace SlimesRevenge
{
    /// <summary>
    /// Snapshot of what sits on a cell when a cell menu opens.
    /// </summary>
    public sealed class CellMenuContext
    {
        public CellMenuContext(
            Vector2Int cell,
            Creature actor,
            Creature occupant,
            Puddle puddle,
            IReadOnlyList<Creature> corpses,
            TurnManager turns
        )
        {
            Cell = cell;
            Actor = actor;
            Occupant = occupant;
            Puddle = puddle;
            Corpses = corpses ?? System.Array.Empty<Creature>();
            Turns = turns;
        }

        public Vector2Int Cell { get; }

        public Creature Actor { get; }

        public Creature Occupant { get; }

        public Puddle Puddle { get; }

        public IReadOnlyList<Creature> Corpses { get; }

        public TurnManager Turns { get; }

        public bool IsActorCell => Actor != null && Actor.Cell == Cell;

        public bool HasAnything =>
            (Occupant != null && Occupant.IsAlive)
            || Corpses.Count > 0
            || Puddle != null
            || IsActorCell;

        public List<CellInspectable> BuildInspectables()
        {
            var list = new List<CellInspectable>();
            if (Occupant != null && Occupant.IsAlive)
            {
                list.Add(CellInspectable.FromCreature(Occupant));
            }

            for (var i = 0; i < Corpses.Count; i++)
            {
                list.Add(CellInspectable.FromCreature(Corpses[i]));
            }

            if (Puddle != null)
            {
                list.Add(CellInspectable.FromPuddle(Puddle));
            }

            if (IsActorCell && Actor != null)
            {
                list.Add(CellInspectable.FromCreature(Actor));
            }

            return list;
        }
    }

    /// <summary>One target the Info flow can show.</summary>
    public sealed class CellInspectable
    {
        private CellInspectable(Creature creature, Puddle puddle)
        {
            Creature = creature;
            Puddle = puddle;
        }

        public Creature Creature { get; }

        public Puddle Puddle { get; }

        public static CellInspectable FromCreature(Creature creature) =>
            new CellInspectable(creature, null);

        public static CellInspectable FromPuddle(Puddle puddle) =>
            new CellInspectable(null, puddle);

        public string PickerLabel
        {
            get
            {
                if (Puddle?.Substance != null)
                {
                    return I18n.FormatOr(
                        TextKey.MenuPuddleChoice,
                        "{0} puddle",
                        Puddle.Substance.Label
                    );
                }

                if (Creature == null)
                {
                    return string.Empty;
                }

                if (Creature.IsCorpse)
                {
                    return CellMenu.CorpseChoiceLabel(Creature);
                }

                return I18n.Creature(Creature.Kind);
            }
        }
    }
}
