using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Hydra.Net.RPC
{
    /// <summary>
    /// json操作的封装
    /// </summary>
    public class JsonValue
    {
        /// <summary>
        /// json实例对象
        /// </summary>
        private JObject _json = null;

        /// <summary>
        /// 构造方法
        /// </summary>
        public JsonValue(JObject json)
        {
            _json = json;
        }

        #region 静态方法
        /// <summary>
        /// 解析json字符串
        /// </summary>
        public static JsonValue Parse(string jsonData)
        {
            JsonValue json = null;
            try
            {
                json = new JsonValue(JObject.Parse(jsonData));
            }
            catch (Exception e)
            {
                HydraLog.WriteLine("解析json字符串失败，原因：" + e.Message);
                json = null;
            }
            return json;
        }

        /// <summary>
        /// 将对象转为json字符串
        /// </summary>
        public static string Format(object obj)
        {
            return JObject.FromObject(obj).ToString();
        }

        #endregion
        
        #region 方法
        /// <summary>
        /// 根据路径获取JSON中的键值
        /// </summary>
        public T GetValue<T>(string path)
        {
            T value = default(T);
            if (null != _json)
            {
                try
                {
                    value = _json.SelectToken(path).ToObject<T>();
                }
                catch (Exception)
                {
                    value = default(T);
                }
            }
            return value;
        }

        /// <summary>
        /// 获取字符串值
        /// </summary>
        /// <param name="path">key路径</param>
        public string AsString(string path)
        {
            return GetValue<string>(path);
        }

        /// <summary>
        /// 获取整型值
        /// </summary>
        public int AsInt(string path)
        {
            return GetValue<int>(path);
        }
         
        /// <summary>
        /// 获取浮点整型值
        /// </summary>
        public float AsFloat(string path)
        {
            return GetValue<float>(path);
        }
         
        /// <summary>
        /// 获取双精度浮点型值
        /// </summary>
        public double AsDouble(string path)
        {
            return GetValue<double>(path);
        } 

        /// <summary>
        /// 获取bool值
        /// </summary>
        public bool AsBool(string path)
        {
            return GetValue<bool>(path);
        }

        /// <summary>
        /// 获取时间值
        /// </summary>
        public DateTime AsDateTime(string path)
        {
            string time = GetValue<string>(path);
            return DateTime.Parse(time);
        }

        /// <summary>
        /// 重置底层Json对象
        /// </summary>
        public void Reset()
        {
            _json = null;
        }

        /// <summary>
        /// 获取底层json对象，自定义操作
        /// </summary>
        public JObject GetJObject()
        {
            return _json;
        } 
        #endregion
        
    }
}
