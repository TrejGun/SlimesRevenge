namespace SlimesRevenge
{
    /// <summary>
    /// Softcore can plug in revive later; Hardcore is the default (defeat → menu).
    /// </summary>
    public enum GameMode
    {
        Softcore,
        Hardcore,
    }

    public static class GameSettings
    {
        public static GameMode Mode { get; set; } = GameMode.Hardcore;
    }
}
