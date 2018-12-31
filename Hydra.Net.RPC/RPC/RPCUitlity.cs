using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/********************************************************************************************
RPC之间的协议建议是JSON格式的，方便扩展，一般的定义的格式：
请求：
{
    "id": 101,  //请求端管理，保证唯一性即可
    "method": "Onlie.SetParameterValue",
    "token": {
        "user": "admin",          
        "pwd": "md5"  //访问令牌信息，可以简单的登录密码，也可以一次性的token
    },
    "params": {  //节点内容根据不同操作自定义
        "prj": "1by1",
        "path": {
            "var": "mv1",
            "parameter": "MV开关"
        },
        "value": "ON"
    }
}
注意：必须包含节点id, method, token, params
-----------------------------------------------------------------------------------------------
答复：
{
    "id": 101,
    "method": "CU.GetPrjs",
    "result": true,
    "error": {
        "errCode": 404,
        "errMsg": "not found prj"
    }
    "Info" : [
            {
                "Name" : "1by1",
                "Version" : "V1.0"
            },
            {
                 ...
            }
            ...
        ]
    }
}
注意：必须包含节点id, method, result, 只有result为false时 error才是必须的, result 为true时，根据具体信令Info可选
*************************************************************************************************/

namespace Hydra.Net.RPC
{
    /// <summary>
    /// RPC请求，答复工具包
    /// </summary>
    /// <remarks>
    /// </remarks>
    public class RPCUitlity
    {
        /// <summary>
        /// 生成失败响应
        /// </summary>
        public static string  GetBadResponse(string request, int errCode, string errMsg)
        {
            return GetRepsone(request, (errCode == 0 ? -1 : errCode), ( string.IsNullOrWhiteSpace(errMsg) ? "请求操作失败" : errMsg), null);
        }

        /// <summary>
        ///生成成功响应
        /// </summary>
        public static string GetOKResponse(string request, object info)
        {
            return GetRepsone(request, 0, "", info);
        }

        /// <summary>
        /// 生成一个响应
        /// </summary> 
        private static string GetRepsone(string request, int errCode, string errMsg, object info)
        {
            JsonValue value = JsonValue.Parse(request);
            if (null == value)
            {
                HydraLog.Throw("生成失败响应失败，原因： 不是一个有效的json数据{0}", request);
            }

            int id = value.AsInt("id");
            string method = value.AsString("method");
            string response = "";
            if (0 == errCode)
            {
                response = JsonValue.Format(new
                {
                    id = id,
                    method = method,
                    result = true,
                    info = (null == info ? "" : JsonValue.Format(info))
                });
            }
            else
            {
                response = JsonValue.Format(new
                {
                    id = id,
                    method = method,
                    result = false,
                    error = new
                    {
                        errCode = errCode,
                        errMsg = errMsg
                    }
                });
            }
            return response;
        }

        /// <summary>
        /// 判断是否成功
        /// </summary>
        public static string IsOK(string response)
        {
            JsonValue value = JsonValue.Parse(response);
            if (null == value) return "解析json字符串失败";
            if(! value.AsBool("result"))
            {
                string msg = value.AsString("error.errMsg");
                if(null == msg)
                {
                    msg = "未定义错误";
                    return msg;
                }
            }
            return null;
        }
    }
}