using Unity.Behavior;
using Unity.Properties;

namespace SlimesRevenge
{
    [System.Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Flee",
        story: "Choose flee",
        category: "Action/Creatures",
        id: "a111f1ee000000000000000000000001")]
    public partial class FleeAction : Action
    {
        protected override Node.Status OnStart()
        {
            CreatureSelect.Choose(CreatureIntent.Flee);
            return Node.Status.Success;
        }
    }
}
