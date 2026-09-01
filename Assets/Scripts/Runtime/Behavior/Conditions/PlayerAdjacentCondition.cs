using Unity.Behavior;
using Unity.Properties;

namespace SlimesRevenge
{
    [System.Serializable, GeneratePropertyBag]
    [Condition(
        name: "Player Adjacent",
        story: "Player is adjacent",
        category: "Conditions/Creatures",
        id: "a111ad1a000000000000000000000001"
    )]
    public partial class PlayerAdjacentCondition : Condition
    {
        public override bool IsTrue()
        {
            var self = ResolveSelf();
            var player = CreatureTurnContext.Player;
            return CreatureMoves.IsAdjacent(self, player) && CreatureMoves.IsDetected(self, player);
        }

        private Creature ResolveSelf()
        {
            if (CreatureTurnContext.Actor != null)
            {
                return CreatureTurnContext.Actor;
            }

            try
            {
                return GameObject != null ? GameObject.GetComponent<Creature>() : null;
            }
            catch (System.NullReferenceException)
            {
                return null;
            }
        }
    }
}
