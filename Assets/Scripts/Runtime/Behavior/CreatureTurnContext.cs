namespace SlimesRevenge
{
    /// <summary>
    /// Scratch for Unity Behavior condition/select nodes during a single graph tick.
    /// Flow: Push → Tick graph → read ChosenIntent → Pop → Perform.
    /// </summary>
    public static class CreatureTurnContext
    {
        public static GameSession Session { get; private set; }

        public static Creature Player { get; private set; }

        public static IRng Rng { get; private set; }

        public static CreatureIntent ChosenIntent { get; set; }

        public static void Push(GameSession session, Creature player, IRng rng)
        {
            Session = session;
            Player = player;
            Rng = rng ?? new SystemRng();
            ChosenIntent = CreatureIntent.None;
        }

        public static void Pop()
        {
            Session = null;
            Player = null;
            Rng = null;
            ChosenIntent = CreatureIntent.None;
        }
    }
}
