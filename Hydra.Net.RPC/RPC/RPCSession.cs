using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace Hydra.Net.RPC
{
    /// <summary>
    /// RPC客户端与服务的连接会话
    /// </summary>
    public class RPCSession : RPCClient
    {
        #region 字段
        /// <summary>
        /// RPC服务端
        /// </summary>
        private RPCServer _server = null; 
        #endregion

        /// <summary>
        /// 会话ID，唯一标识一个会话客户端
        /// </summary>
        public string SessionID { get; set; }

        /// <summary>
        /// 构造方法
        /// </summary>
        public RPCSession(RPCServer server, TcpClient client)
        {
            _server = server;
            EndPoint = client;
        }

        /// <summary>
        /// RPC客户端与服务端的会话使用服务端的命令总线进行信令处理
        /// </summary>
        protected override void Register(string cmd, Func<string, JsonValue, string> handler)
        {
            CmdBus = _server.CmdBus;
        }

        /// <summary>
        /// 关闭会话
        /// </summary>
        public void Close()
        {
            DisConnect();
        }

        public void Notify(string datas)
        {
            Send(datas);
        }
    }
}