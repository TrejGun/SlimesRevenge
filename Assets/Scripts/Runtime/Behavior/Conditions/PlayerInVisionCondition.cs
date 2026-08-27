using Unity.Behavior;
using Unity.Properties;

namespace SlimesRevenge
{
    [System.Serializable, GeneratePropertyBag]
    [Condition(
        name: "Player In Vision",
        story: "Player is in vision",
        category: "Conditions/Creatures",
        id: "a1115ee5000000000000000000000001"
    )]
    public partial class PlayerInVisionCondition : Condition
    {
        public override bool IsTrue()
        {
            return CreatureMoves.CanSee(ResolveSelf(), CreatureTurnContext.Player);
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
