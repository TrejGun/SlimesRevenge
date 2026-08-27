namespace SlimesRevenge
{
    public sealed class InfoEntry : IMenuEntry
    {
        public string Label => I18n.Get(TextKey.MenuInfo);

        public bool IsVisible(CellMenuContext context) => context != null && context.HasAnything;

        public bool IsEnabled(CellMenuContext context) => true;

        public void Activate(CellMenu menu, CellMenuContext context)
        {
            var inspectables = context.BuildInspectables();
            if (inspectables.Count == 0)
            {
                menu.Close();
                return;
            }

            if (inspectables.Count == 1)
            {
                menu.ShowInfoPanel(inspectables[0]);
                return;
            }

            menu.ShowInspectablePicker(inspectables);
        }
    }
}
