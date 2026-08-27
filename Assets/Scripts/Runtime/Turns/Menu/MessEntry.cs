namespace SlimesRevenge
{
    public sealed class MessEntry : IMenuEntry
    {
        public string Label => I18n.Get(TextKey.MenuMess);

        public bool IsVisible(CellMenuContext context)
        {
            if (context?.Actor == null || !context.IsActorCell)
            {
                return false;
            }

            if (context.Puddle != null)
            {
                return false;
            }

            return context.Actor.Volume.UnitCount > 0;
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
                if (context.Turns != null && context.Turns.TryMakeMess(substance))
                {
                    menu.CompleteAction();
                }
            });
        }
    }
}
