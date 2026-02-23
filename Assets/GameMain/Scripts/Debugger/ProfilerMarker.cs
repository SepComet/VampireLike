using Unity.Profiling;

namespace CustomDebugger
{
    public static class CustomProfilerMarker
    {
        public static readonly ProfilerMarker TickEnemies = new ProfilerMarker("TickEnemies"); 
        public static readonly ProfilerMarker TickEnemies_BuildInput = new ProfilerMarker("TickEnemies.BuildInput");
        public static readonly ProfilerMarker TickEnemies_MoveSeparation = new ProfilerMarker("TickEnemies.MoveSeparation");
        public static readonly ProfilerMarker TickEnemies_StateUpdate = new ProfilerMarker("TickEnemies.StateUpdate");
        public static readonly ProfilerMarker TickEnemies_Schedule = new ProfilerMarker("TickEnemies.Schedule");
        public static readonly ProfilerMarker TickEnemies_Complete = new ProfilerMarker("TickEnemies.Complete");
        public static readonly ProfilerMarker TickEnemies_MainThreadCommit = new ProfilerMarker("TickEnemies.MainThreadCommit");
        public static readonly ProfilerMarker TickEnemies_WriteBack = new ProfilerMarker("TickEnemies.WriteBack");
        public static readonly ProfilerMarker Collision_BuildQueries = new("Collision.BuildQueries");
        public static readonly ProfilerMarker Collision_BuildBuckets = new("Collision.BuildBuckets");
        public static readonly ProfilerMarker Collision_QueryCandidates = new("Collision.QueryCandidates");
        public static readonly ProfilerMarker Collision_ResolveProjectile = new("Collision.ResolveProjectile");
        public static readonly ProfilerMarker Collision_ResolveArea = new("Collision.ResolveArea");
        public static readonly ProfilerMarker TargetSelection_BuildBuckets = new("TargetSelection.BuildBuckets");
        public static readonly ProfilerMarker TargetSelection_QueryNeighbors = new("TargetSelection.QueryNeighbors");
        public static readonly ProfilerMarker Movement_Update = new ProfilerMarker("Movement_Update"); 
        public static readonly ProfilerMarker ShopUI_Update = new("UGF.ShopUI.Update");
        public static readonly ProfilerMarker Inventory_Refresh = new("UGF.Inventory.Refresh");
    }    
}
