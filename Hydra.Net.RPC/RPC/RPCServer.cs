using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Hydra.Net.RPC
{
    /// <summary>
    /// Json RPC Server， 负责底层TCP通讯，具体业务逻辑有子类负责实现
    /// </summary>
    public abstract class RPCServer
    {
        #region 静态实例
        /// <summary>
        /// 同步对象
        /// </summary>
        private static object _lockObj = new object();

        ///// <summary>
        ///// 静态信令总线字段
        ///// </summary>
        //private static CommandBus _bus = null;

        ///// <summary>
        ///// 信令总线实例
        ///// </summary>
        //internal static CommandBus CmdBus
        //{
        //    get
        //    {
        //        if (null == _bus)
        //        {
        //            _bus = new CommandBus();
        //        }
        //        return _bus;
        //    }
        //}
        #endregion

        #region 属性
        /// <summary>
        /// RPC客户端会话集合
        /// </summary>
        public List<RPCSession> Sessions { get; set; }

        /// <summary>
        /// 监听IP
        /// </summary>
        public string IP { get; set; }

        /// <summary>
        /// 监听端口号
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// socket监听对象
        /// </summary>
        internal TcpListener Listener { get; set; }

        /// <summary>
        /// 信令总线实例
        /// </summary>
        internal CommandBus CmdBus { get; private set; }
        #endregion

        /// <summary>
        /// 构造方法
        /// </summary>
        protected RPCServer()
        {
            Sessions = new List<RPCSession>();
            CmdBus = new CommandBus();
        }

        #region 方法
        /// <summary>启动RPC服务</summary>
        /// <param name="ip">启动RPC服务端，开始监听端口</param>
        /// <param name="port">RPC服务端口号</param>
        public bool Start(string ip, int port)
        {
            //检查参数是否有效
            if (port <= 0 || port >= UInt16.MaxValue)
            {
                HydraLog.Format("启动RPC服务失败，无效端口号: {0}", port);
                return false;
            }
            IPAddress addr;
            if (!IPAddress.TryParse(ip, out addr))
            {
                HydraLog.Format("启动RPC服务失败，无效的IP: {0}]", ip);
                return false;
            }

            //开始监听
            if (null != Listener) return true; //已经启动监听了
            try
            {
                Listener = new TcpListener(addr, port);
                Listener.Start();
                Listener.BeginAcceptTcpClient(OnConnect, this); //异步接收客户端的连接请求

                IP = ip;
                Port = port;
                HydraLog.Format("启动RPC服务[{0}:{1}]成功!", IP, Port);
            }
            catch (Exception e)
            {
                HydraLog.WriteLine("启动RPC服务失败！" + e.Message);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 停止RPC服务
        /// </summary>
        public bool Stop()
        {
            try
            {
                //停止监听
                if (null != Listener)
                {
                    Listener.Stop();
                }
                //释放已经连接的客户端
                if (null != Sessions)
                {
                    Sessions.ForEach(session =>
                    {
                        session.Close();
                    });
                    Sessions.Clear();
                }
                HydraLog.WriteLine("停止RPC服务成功!");
            }
            catch (Exception e)
            {
                HydraLog.WriteLine("停止RPC服务失败！" + e.Message);
            }
            return true;
        }

        /// <summary>
        /// 答复请求
        /// </summary>
        public void Reply(string sessionId, string jsonData)
        {
            if (null == Sessions) return;
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                //发送给所有客户端
                Sessions.ForEach(session => session.Notify(jsonData));
            }
            else
            {
                //发送给指定客户端
                RPCSession session = Sessions.FirstOrDefault(s => s.SessionID == sessionId);
                if (null != session)
                {
                    session.Notify(jsonData);
                }
            }
        } 
        #endregion

        #region 辅助方法
        /// <summary>
        /// 处理RPC客户端的连接请求
        /// </summary>
        private void OnConnect(IAsyncResult ar)
        {
            try
            {
                RPCServer server = ar.AsyncState as RPCServer;
                if (null != server && null != server.Listener)
                {
                    TcpClient client = server.Listener.EndAcceptTcpClient(ar);
                    if (null != client)
                    {
                        RPCSession session = new RPCSession(server, client);
                        lock (_lockObj)
                        {
                            Sessions.Add(session);
                        }
                        HydraLog.Format("New RPC Client From {0} Connectd!", client.Client.RemoteEndPoint.ToString());
                    }

                    //准备接受下一个客户端连接
                    server.Listener.BeginAcceptTcpClient(OnConnect, server);
                }
            }
            catch (Exception e)
            {
                HydraLog.WriteLine("接收RPC客户端连接请求异常！" + e.Message);
            }
        }

        /// <summary>
        /// 处理PRC客户端发送过来的请求数据
        /// </summary>
        private void OnData()
        {
        }

        /// <summary>
        /// RPC客户端会话关闭处理
        /// </summary>
        public void OnClose(string sessionId)
        {
            if (null != Sessions)
            {
                lock (_lockObj)
                {
                    RPCSession session = Sessions.FirstOrDefault(s => s.SessionID == sessionId);
                    if (null != session)
                    {
                        Sessions.Remove(session);
                    }
                }
            }
        }
        #endregion

        #region 请求处理 
        /// <summary>
        /// 关联请求以及请求处理方法
        /// </summary>
        protected void Attach(string cmd, Func<string, JsonValue, string> handler)
        {
            if(string.IsNullOrWhiteSpace(cmd))
            {
                HydraLog.Throw("请求方法名称为空", cmd);
            }
            if(null == handler)
            {
                HydraLog.Throw("请求的处理方法委托为空对象");
            } 
            CmdBus.Register(cmd, handler);
        }

        /// <summary>
        /// 注册RPC请求的处理方法, 子类必须实现
        /// </summary> 
        /// <param name="cmd">命令名称</param>
        /// <param name="handler">命令的处理方法</param>
        /// <remarks> 
        /// 命令处理方法委托： Func<JsonValue, string>
        ///                      JsonValue [in]:  输入参数类型,其内容为请求发送的内容，json格式
        ///                      string   [out]:  输出参数类型,其内容为请求对应的答复呢绒，json格式
        /// 请求以及答复的json内容详见具体业务的协议说明
        /// </remarks>
        abstract protected void Register(string cmd, Func<JsonValue, string> handler);
        #endregion
    }
}
