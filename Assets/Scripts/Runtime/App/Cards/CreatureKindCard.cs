using UnityEngine;

namespace SlimesRevenge
{
    /// <summary>Minimal card for a creature kind from the action log (no live instance).</summary>
    public sealed class CreatureKindCard : IPopupContent
    {
        private readonly CreatureKind kind;

        public CreatureKindCard(CreatureKind kind)
        {
            this.kind = kind;
        }

        public void Build(RectTransform parent, PopupHost host)
        {
            CardUi.AddTitle(parent, I18n.Creature(kind));
        }
    }
}
