using Unity.Behavior;
using Unity.Properties;

namespace SlimesRevenge
{
    [System.Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Idle",
        story: "Choose idle",
        category: "Action/Creatures",
        id: "a1111d1e000000000000000000000001"
    )]
    public partial class IdleAction : Action
    {
        protected override Node.Status OnStart()
        {
            CreatureSelect.Choose(CreatureIntent.Idle);
            return Node.Status.Success;
        }
    }
}
