using Unity.Behavior;
using Unity.Properties;

namespace SlimesRevenge
{
    [System.Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Clear Aggro",
        story: "Clear aggro",
        category: "Action/Creatures",
        id: "a111c1ea000000000000000000000001")]
    public partial class ClearAggroAction : Action
    {
        protected override Node.Status OnStart()
        {
            GameObject.GetComponent<Creature>()?.ClearAggro();
            return Node.Status.Success;
        }
    }
}
