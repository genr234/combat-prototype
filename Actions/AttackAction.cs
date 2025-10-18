using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Attack", story: "Attacks [Target]", category: "Action", id: "8ef62b2a85309e86bd67820401dd0f80")]
public partial class AttackAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<int> Damage;
    protected override Status OnStart()
    {
        Target.Value.GetComponent<PlayerManager>().TakeDamage(Damage, 200);
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

