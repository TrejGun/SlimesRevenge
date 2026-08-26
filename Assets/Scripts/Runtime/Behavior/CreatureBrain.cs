using Unity.Behavior;
using UnityEngine;

namespace SlimesRevenge
{
    /// <summary>
    /// Turn decide: Push → one BehaviorGraph tick → ChosenIntent → Pop.
    /// Execute stays in <see cref="CreatureMoves.Perform"/>.
    /// </summary>
    public static class CreatureBrain
    {
        public static CreatureIntent Decide(Creature self, Creature player, GameSession session, IRng rng)
        {
            if (self == null)
            {
                return CreatureIntent.Idle;
            }

            CreatureTurnContext.Push(session, player, rng);
            try
            {
                var graph = CreaturePolicyGraphs.For(self.Personality);
                CreaturePolicyGraphs.BindOwner(graph, self.gameObject);
                graph.End();
                graph.Start();
                graph.Tick();
                var intent = CreatureTurnContext.ChosenIntent;
                return intent == CreatureIntent.None ? CreatureIntent.Idle : intent;
            }
            finally
            {
                CreatureTurnContext.Pop();
            }
        }
    }
}
