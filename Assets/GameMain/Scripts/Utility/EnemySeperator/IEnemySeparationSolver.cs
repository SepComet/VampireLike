using Components;
using UnityEngine;

namespace CustomUtility
{
    public interface IEnemySeparationSolver
    {
        void Register(MovementComponent mover, Transform transform, float bodyRadius);
        void Unregister(MovementComponent mover);
        Vector3 Resolve(MovementComponent mover, Vector3 desiredPosition, Vector3 fallbackDirection, int iterations);
    }
}