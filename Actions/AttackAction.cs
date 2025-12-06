using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Serialization;
using Action = Unity.Behavior.Action;

namespace Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "Attack", story: "Attacks [Target]", category: "Action", id: "8ef62b2a85309e86bd67820401dd0f80")]
    public partial class AttackAction : Action
    {
        [FormerlySerializedAs("Target")] [SerializeReference] public BlackboardVariable<GameObject> target;
        [FormerlySerializedAs("Damage")] [SerializeReference] public BlackboardVariable<int> damage;
        protected override Status OnStart()
        {
            target.Value.GetComponent<PlayerManager>().TakeDamage(damage, 200);
            return Status.Running;
        }

        protected override Status OnUpdate()
        {
        
            return Status.Success;
        }

        protected override void OnEnd()
        {
        }
    }
}

