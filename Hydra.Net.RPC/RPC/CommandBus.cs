using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Hydra.Net.RPC
{
    /// <summary>
    /// RPC信令总线
    /// </summary>
    public class CommandBus
    {
        #region 字段
        /// <summary>
        /// 信令处理方法集合
        /// </summary>
        private Dictionary<string, Func<string, JsonValue, string>> _handlers = null; 
        #endregion

        /// <summary>
        /// 构造方法
        /// </summary>
        public CommandBus()
        {

        }
        
        #region 方法
        /// <summary>
        /// 注册信令处理方法
        /// </summary>
        public void Register(string cmd, Func<string, JsonValue, string> handler)
        {
            if (null == handler)
            {
                throw new ArgumentException(string.Format("注册信令失败，原因：信令{0}的处理方法为null！", cmd));
            }
            if (null == _handlers)
            {
                _handlers = new Dictionary<string, Func<string, JsonValue, string>>();
            }
            //目前不支持同一个信令被多个模块进行订订阅然后分别处理，目前只能同一个信令只能被一个模块处理
            //cmd = cmd.ToUpper();  //统一成大写,忽略大小写
            if (_handlers.ContainsKey(cmd))
            {
                throw new ArgumentException(string.Format("注册信令失败，原因：信令{0}已存在！", cmd));
            }
            else
            {
                _handlers[cmd] = handler;
            }
        }

        /// <summary>
        /// 执行信令处理方法
        /// </summary>
        /// <param name="bSync">同步标记，是否为同步操作</param>
        private void Call(RPCClient client, string jsonData, bool bSync)
        {
            HydraLog.Format("Call: {0}", jsonData);
            if (null == _handlers || 0 == _handlers.Count) return;
             
            JsonValue value = JsonValue.Parse(jsonData);
            if (null == value) return;
            
            //激活同步等待信号
            int id = value.AsInt("id");
            string method = value.AsString("method");
            RPCSyncCmdMgt.Instance.Singal(string.Format("{0}@{1}", id, method), jsonData);
            
            //调用
            if (_handlers.ContainsKey(method))
            {
                string sessionId = client.EndPoint.Client.Handle.ToString();
                Func<string, JsonValue, string> handler = _handlers[method];
                if (null == handler) return;
                if (bSync)
                {
                    client.Send(handler(sessionId, value)); //同步调用
                }
                else
                {
                    ThreadPool.QueueUserWorkItem(state => client.Send(handler(sessionId, value)));  //异步调用
                }
            }
        }

        /// <summary>
        /// 同步调用
        /// </summary> 
        public void SyncCall(RPCClient client, string jsonData)
        {
            Call(client, jsonData, true);
        }

        /// <summary>
        /// 异步调用
        /// </summary> 
        public void PostCall(RPCClient client, string jsonData)
        {
            Call(client, jsonData, false);
        } 
        #endregion
    }
}