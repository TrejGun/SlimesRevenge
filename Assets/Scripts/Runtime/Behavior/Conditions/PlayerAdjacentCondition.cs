using Unity.Behavior;
using Unity.Properties;

namespace SlimesRevenge
{
    [System.Serializable, GeneratePropertyBag]
    [Condition(
        name: "Player Adjacent",
        story: "Player is adjacent",
        category: "Conditions/Creatures",
        id: "a111ad1a000000000000000000000001")]
    public partial class PlayerAdjacentCondition : Condition
    {
        public override bool IsTrue()
        {
            var self = GameObject.GetComponent<Creature>();
            return CreatureMoves.IsAdjacent(self, CreatureTurnContext.Player);
        }
    }
}
