namespace SlimesRevenge
{
    public sealed class AttackEntry : IMenuEntry
    {
        public string Label => I18n.Get(TextKey.MenuAttack);

        public bool IsVisible(CellMenuContext context)
        {
            if (context?.Actor == null || context.Occupant == null || !context.Occupant.IsAlive)
            {
                return false;
            }

            if (context.IsActorCell)
            {
                return false;
            }

            return GridStep.IsAdjacent(context.Actor, context.Occupant);
        }

        public bool IsEnabled(CellMenuContext context) =>
            context?.Actor != null && context.Actor.Volume.CanSpend;

        public void Activate(CellMenu menu, CellMenuContext context)
        {
            if (context?.Actor == null || !context.Actor.Volume.CanSpend)
            {
                return;
            }

            menu.ShowSubstancePicker(substance =>
            {
                if (context.Turns != null && context.Turns.TryAttack(context.Cell, substance))
                {
                    menu.CompleteAction();
                }
            });
        }
    }
}
