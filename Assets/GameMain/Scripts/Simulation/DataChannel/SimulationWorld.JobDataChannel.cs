using System.Collections.Generic;
using Unity.Collections;

namespace Simulation
{
    public sealed partial class SimulationWorld
    {
        // Shared channel constants plus compatibility fields still reflected by tests.
        private const int CollisionSourceTypeProjectile = 1;
        private const int CollisionSourceTypeArea = 2;
        private const int CollisionShapeCircle = 0;
        private const int CollisionShapeSector = 1;
        private const int CollisionShapeRectangle = 2;

        // Kept as top-level fields because current regression tests reflect them directly.
        private NativeList<CollisionQueryData> _collisionQueryInputs;
        private readonly List<AreaCollisionRequestData> _areaCollisionRequests = new(16);
    }
}
