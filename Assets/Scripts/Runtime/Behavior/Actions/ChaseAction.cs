using Unity.Behavior;
using Unity.Properties;

namespace SlimesRevenge
{
    [System.Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Chase",
        story: "Choose chase",
        category: "Action/Creatures",
        id: "a111c4a5000000000000000000000001")]
    public partial class ChaseAction : Action
    {
        protected override Node.Status OnStart()
        {
            CreatureSelect.Choose(CreatureIntent.Chase);
            return Node.Status.Success;
        }
    }
}
