using Unity.Behavior;
using Unity.Properties;

namespace SlimesRevenge
{
    [System.Serializable, GeneratePropertyBag]
    [Condition(
        name: "Is Aggroed",
        story: "Self is aggroed",
        category: "Conditions/Creatures",
        id: "a111a660000000000000000000000001")]
    public partial class IsAggroedCondition : Condition
    {
        public override bool IsTrue()
        {
            var self = GameObject.GetComponent<Creature>();
            return self != null && self.Aggroed;
        }
    }
}
