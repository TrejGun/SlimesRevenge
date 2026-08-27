namespace SlimesRevenge
{
    public sealed class DevourEntry : IMenuEntry
    {
        public string Label => I18n.Get(TextKey.MenuDevour);

        public bool IsVisible(CellMenuContext context)
        {
            if (context?.Actor == null || !context.IsActorCell)
            {
                return false;
            }

            if (context.Corpses.Count == 0)
            {
                return false;
            }

            return !context.Actor.IsDigesting;
        }

        public bool IsEnabled(CellMenuContext context) => true;

        public void Activate(CellMenu menu, CellMenuContext context)
        {
            if (context.Corpses.Count == 1)
            {
                TryDevourAt(menu, context, 0);
                return;
            }

            menu.ShowCorpsePicker(context);
        }

        public static void TryDevourAt(CellMenu menu, CellMenuContext context, int index)
        {
            if (context.Turns == null)
            {
                return;
            }

            if (context.Turns.TryDevourCorpse(index))
            {
                menu.CompleteAction();
            }
        }
    }
}
