using Unity.Profiling;

namespace CustomDebugger
{
    public static class CustomProfilerMarker
    {
        public static readonly ProfilerMarker Movement_Update = new ProfilerMarker("Movement_Update"); 
        public static readonly ProfilerMarker ShopUI_Update = new("UGF.ShopUI.Update");
        public static readonly ProfilerMarker Inventory_Refresh = new("UGF.Inventory.Refresh");
    }    
}
