using Unity.Behavior;
using Unity.Properties;

namespace SlimesRevenge
{
    [System.Serializable, GeneratePropertyBag]
    [Condition(
        name: "Player In Vision",
        story: "Player is in vision",
        category: "Conditions/Creatures",
        id: "a1115ee5000000000000000000000001")]
    public partial class PlayerInVisionCondition : Condition
    {
        public override bool IsTrue()
        {
            var self = GameObject.GetComponent<Creature>();
            return CreatureMoves.CanSee(self, CreatureTurnContext.Player);
        }
    }
}
