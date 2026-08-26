using System.Collections.Generic;
using Unity.Behavior;

namespace SlimesRevenge
{
    /// <summary>
    /// Turn decide. Prefer the no-arg form when <see cref="CreatureTurnContext"/> is already pushed
    /// (see <see cref="Creature.TakeTurn"/>). The overload pushes context for tests / callers.
    /// </summary>
    public static class CreatureBrain
    {
        public static CreatureIntent Decide(Creature self)
        {
            if (self == null)
            {
                return CreatureIntent.Idle;
            }

            var graph = CreaturePolicyGraphs.For(self.Personality);
            CreaturePolicyGraphs.BindOwner(graph, self.gameObject);
            graph.End();
            graph.Start();
            graph.Tick();
            var intent = CreatureTurnContext.ChosenIntent;
            return intent == CreatureIntent.None ? CreatureIntent.Idle : intent;
        }

        public static CreatureIntent Decide(
            Creature self,
            Creature player,
            GameSession session,
            IRng rng,
            IReadOnlyList<Creature> others = null)
        {
            CreatureTurnContext.Push(session, player, rng, others);
            try
            {
                return CreatureHunt.Redirect(self, Decide(self), others);
            }
            finally
            {
                CreatureTurnContext.Pop();
            }
        }
    }
}
