namespace SlimesRevenge
{
    public interface IMenuEntry
    {
        string Label { get; }

        bool IsVisible(CellMenuContext context);

        bool IsEnabled(CellMenuContext context);

        void Activate(CellMenu menu, CellMenuContext context);
    }
}
