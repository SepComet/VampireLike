using Definition.DataStruct;
using Entity;
using UnityEngine;

public abstract class EnemyBase : TargetableObject
{
    protected Transform _target;

    public abstract override ImpactData GetImpactData();
    public virtual float AttackRange => 1f;

    public virtual void SetTarget(Transform target) => _target = target;

    protected bool IsSimulationMovementEnabled()
    {
        return true;
    }
}
