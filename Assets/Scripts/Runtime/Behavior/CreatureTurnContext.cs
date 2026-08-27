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

        /// <summary>Creature whose Behavior graph is ticking (set by <see cref="CreatureBrain.Decide"/>).</summary>
        public static Creature Actor { get; private set; }

        /// <summary>
        /// Personality focus (usually the slime). Null when the board has no controlled player —
        /// Behavior conditions vs player are then false and hunt redirect owns mob↔mob intents.
        /// </summary>
        public static Creature Player { get; private set; }

        public static IRng Rng { get; private set; }

        public static IReadOnlyList<Creature> Others { get; private set; }

        public static CreatureIntent ChosenIntent { get; set; }

        /// <summary>Optional chase/attack/flee focus. Null → use <see cref="Player"/> (also nullable).</summary>
        public static Creature FocusTarget { get; set; }

        public static void Push(
            GameSession session,
            Creature player,
            IRng rng,
            IReadOnlyList<Creature> others = null
        )
        {
            Session = session;
            Actor = null;
            // Personality focus toward the controlled slime; null in board-only sims.
            Player = player;
            Rng = rng ?? new SystemRng();
            Others = others;
            ChosenIntent = CreatureIntent.None;
            FocusTarget = null;
        }

        public static void SetActor(Creature actor)
        {
            Actor = actor;
        }

        public static void Pop()
        {
            Session = null;
            Actor = null;
            Player = null;
            Rng = null;
            Others = null;
            ChosenIntent = CreatureIntent.None;
            FocusTarget = null;
        }
    }
}
