namespace SlimesRevenge
{
    /// <summary>Selection side of Unity Behavior action nodes — writes intent, does not move the world.</summary>
    public static class CreatureSelect
    {
        public static void Choose(CreatureIntent intent)
        {
            CreatureTurnContext.ChosenIntent = intent;
        }
    }
}
