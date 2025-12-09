using System;
using Unity.Behavior;
using UnityEngine;

namespace Actions
{
    [Serializable, Unity.Properties.GeneratePropertyBag]
    [Condition(name: "Target Exists", story: "Check if [Target] [Exists]", category: "Conditions", id: "12d2ca5da9bbb454a61b89bdaa65e626")]
    public class TargetExistsCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<GameObject> target;
        [Comparison(comparisonType: ComparisonType.Boolean)]
        [SerializeReference] public BlackboardVariable<ConditionOperator> exists;

        public override bool IsTrue()
        {
            return target.Value != null;
        }

        public override void OnStart()
        {
            
        }

        public override void OnEnd()
        {
        }
    }
}
