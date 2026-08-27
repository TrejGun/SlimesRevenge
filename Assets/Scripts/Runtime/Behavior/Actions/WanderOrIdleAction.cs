using Unity.Behavior;
using Unity.Properties;

namespace SlimesRevenge
{
    [System.Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Wander Or Idle",
        story: "Choose wander or idle",
        category: "Action/Creatures",
        id: "a11101d1e0000000000000000000001"
    )]
    public partial class WanderOrIdleAction : Action
    {
        protected override Node.Status OnStart()
        {
            var rng = CreatureTurnContext.Rng;
            CreatureSelect.Choose(
                rng != null && rng.WanderRatherThanIdle()
                    ? CreatureIntent.Wander
                    : CreatureIntent.Idle
            );
            return Node.Status.Success;
        }
    }
}
