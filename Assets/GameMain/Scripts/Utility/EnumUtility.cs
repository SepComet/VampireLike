using System.Collections.Generic;
using UnityGameFramework.Runtime;

namespace CustomUtility
{
    public static class EnumUtility<T> where T : struct, System.Enum
    {
        private static readonly Dictionary<string, T> _enumCache = new();

        public static T Get(string value)
        {
            if (!_enumCache.TryGetValue(value, out T result))
            {
                if (System.Enum.TryParse(value, true, out result))
                {
                    _enumCache[value] = result;
                }
                else
                {
                    Log.Error($"Enum 解析失败：类型：{typeof(T).Name} 不包含值 {value}");
                }
            }

            return result;
        }
    }    
}
