using Unity.Behavior;
using Unity.Properties;

namespace SlimesRevenge
{
    [System.Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Wander",
        story: "Choose wander",
        category: "Action/Creatures",
        id: "a1111a7d000000000000000000000001"
    )]
    public partial class WanderAction : Action
    {
        protected override Node.Status OnStart()
        {
            CreatureSelect.Choose(CreatureIntent.Wander);
            return Node.Status.Success;
        }
    }
}
