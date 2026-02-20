using UnityEngine;

namespace CustomUtility
{
    public interface IEnemySeparationSolver
    {
        void Register(Transform transform, float bodyRadius);
        void Unregister(Transform transform);
        Vector3 Resolve(Transform transform, Vector3 desiredPosition, Vector3 fallbackDirection, int iterations);
    }
}
