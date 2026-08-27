using Unity.Behavior;
using Unity.Properties;

namespace SlimesRevenge
{
    [System.Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Clear Aggro",
        story: "Clear aggro",
        category: "Action/Creatures",
        id: "a111c1ea000000000000000000000001"
    )]
    public partial class ClearAggroAction : Action
    {
        protected override Node.Status OnStart()
        {
            var self = CreatureTurnContext.Actor;
            if (self == null)
            {
                try
                {
                    self = GameObject != null ? GameObject.GetComponent<Creature>() : null;
                }
                catch (System.NullReferenceException)
                {
                    self = null;
                }
            }

            self?.ClearAggro();
            return Node.Status.Success;
        }
    }
}
