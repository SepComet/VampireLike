using Unity.Profiling;

namespace CustomDebugger
{
    public static class CustomProfilerMarker
    {
        public static readonly ProfilerMarker TickEnemies = new("TickEnemies");
        public static readonly ProfilerMarker TickEnemies_BuildInput = new("TickEnemies.BuildInput");
        public static readonly ProfilerMarker TickEnemies_StateUpdate = new("TickEnemies.StateUpdate");
        public static readonly ProfilerMarker TickEnemies_Schedule = new("TickEnemies.Schedule");
        public static readonly ProfilerMarker TickEnemies_Complete = new("TickEnemies.Complete");
        public static readonly ProfilerMarker TickEnemies_MainThreadCommit = new("TickEnemies.MainThreadCommit");
        public static readonly ProfilerMarker TickEnemies_WriteBack = new("TickEnemies.WriteBack");

        public static readonly ProfilerMarker Collision = new("Collision");
        public static readonly ProfilerMarker Collision_BuildQueries = new("Collision.BuildQueries");
        public static readonly ProfilerMarker Collision_BuildBuckets = new("Collision.BuildBuckets");
        public static readonly ProfilerMarker Collision_QueryCandidates = new("Collision.QueryCandidates");
        public static readonly ProfilerMarker Collision_ResolveProjectile = new("Collision.ResolveProjectile");
        public static readonly ProfilerMarker Collision_ResolveArea = new("Collision.ResolveArea");
        
        public static readonly ProfilerMarker TargetSelection_BuildBuckets = new("TargetSelection.BuildBuckets");
        public static readonly ProfilerMarker TargetSelection_QueryNeighbors = new("TargetSelection.QueryNeighbors");
        
        public static readonly ProfilerMarker Movement_Update = new("Movement_Update");
        public static readonly ProfilerMarker ShopUI_Update = new("UGF.ShopUI.Update");
        public static readonly ProfilerMarker Inventory_Refresh = new("UGF.Inventory.Refresh");
    }
}