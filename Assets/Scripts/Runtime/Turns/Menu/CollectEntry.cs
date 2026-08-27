namespace SlimesRevenge
{
    public sealed class CollectEntry : IMenuEntry
    {
        public string Label => I18n.Get(TextKey.MenuCollect);

        public bool IsVisible(CellMenuContext context)
        {
            if (context?.Actor == null || !context.IsActorCell || context.Puddle == null)
            {
                return false;
            }

            return context.Actor.Volume.UnitCount < Volume.Capacity;
        }

        public bool IsEnabled(CellMenuContext context) => true;

        public void Activate(CellMenu menu, CellMenuContext context)
        {
            if (context.Turns != null && context.Turns.TryCollectPuddle())
            {
                menu.CompleteAction();
            }
        }
    }
}
