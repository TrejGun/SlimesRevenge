using Unity.Behavior;
using Unity.Properties;

namespace SlimesRevenge
{
    [System.Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Attack",
        story: "Choose attack",
        category: "Action/Creatures",
        id: "a111b17e000000000000000000000001")]
    public partial class AttackAction : Action
    {
        protected override Node.Status OnStart()
        {
            CreatureSelect.Choose(CreatureIntent.Attack);
            return Node.Status.Success;
        }
    }
}
