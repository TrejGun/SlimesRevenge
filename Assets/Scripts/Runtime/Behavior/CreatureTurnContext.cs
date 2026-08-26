using System.Collections.Generic;

namespace SlimesRevenge
{
    /// <summary>
    /// Scratch for Unity Behavior condition/select nodes during a single creature turn.
    /// Flow: Push → Decide (+ hunt redirect) → Perform → Pop.
    /// </summary>
    public static class CreatureTurnContext
    {
        public static GameSession Session { get; private set; }

        public static Creature Player { get; private set; }

        public static IRng Rng { get; private set; }

        public static IReadOnlyList<Creature> Others { get; private set; }

        public static CreatureIntent ChosenIntent { get; set; }

        /// <summary>Optional chase/attack focus (prey). Null → use <see cref="Player"/>.</summary>
        public static Creature FocusTarget { get; set; }

        public static void Push(
            GameSession session,
            Creature player,
            IRng rng,
            IReadOnlyList<Creature> others = null)
        {
            Session = session;
            Player = player;
            Rng = rng ?? new SystemRng();
            Others = others;
            ChosenIntent = CreatureIntent.None;
            FocusTarget = null;
        }

        public static void Pop()
        {
            Session = null;
            Player = null;
            Rng = null;
            Others = null;
            ChosenIntent = CreatureIntent.None;
            FocusTarget = null;
        }
    }
}
