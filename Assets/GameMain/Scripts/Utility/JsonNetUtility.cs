using System;
using Newtonsoft.Json;

namespace CustomUtility
{
    /// <summary>
    /// Newtonsoft.Json 函数集辅助器。
    /// </summary>
    public class JsonNetUtility : GameFramework.Utility.Json.IJsonHelper
    {
        public string ToJson(object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        public T ToObject<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }

        public object ToObject(Type objectType, string json)
        {
            return JsonConvert.DeserializeObject(json, objectType);
        }
    }
}