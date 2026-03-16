using UnityEngine;

namespace Simulation
{
    public sealed partial class SimulationWorld
    {
        private struct AreaCollisionRequestData
        {
            public int SourceEntityId;
            public int SourceOwnerEntityId;
            public bool SourceWasActiveAtQueryTime;
            public Vector3 Center;
            public float Radius;
            public int MaxTargets;
            public int ShapeType;
            public Vector3 Direction;
            public float HalfAngleDeg;
        }
    }
}