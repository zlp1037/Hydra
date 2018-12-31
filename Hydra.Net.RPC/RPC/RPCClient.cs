using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Timers;

namespace Hydra.Net.RPC
{
    /// <summary>
    /// RPC客户端
    /// </summary>
    public abstract class RPCClient
    {
        #region 缓存
        /// <summary>
        /// 网络数据区缓存
        /// </summary>
        private byte[] _buffer = new byte[256];

        /// <summary>
        /// 网络数据区缓存
        /// </summary>
        internal byte[] Buffer
        {
            get
            {
                return _buffer;
            }
        }
        #endregion
        
        #region 事件
        /// <summary>
        /// 会话关闭事件
        /// </summary>
        public event Action<object, string> SessionClosed;
        #endregion

        #region 内部属性
        /// <summary>
        /// RPC服务端通信终端
        /// </summary>
        internal TcpClient EndPoint { get; set; }

        /// <summary>
        /// RPC通信协议解析器
        /// </summary>
        internal RPCProtocolParser Parser { get; private set; }

        /// <summary>
        /// 信令总线
        /// </summary>
        internal CommandBus CmdBus { get; set; }
        #endregion

        #region 连接属性
        /// <summary>
        /// RPC服务端IP
        /// </summary>
        public string ServerIP { get; set; }

        /// <summary>
        /// RPC服务端端口
        /// </summary>
        public int Port { get; set; }
         
        /// <summary>
        /// 是否保持与服务端的连接
        /// </summary>
        public bool IsConnected
        {
            get
            {
                return (null != EndPoint && EndPoint.Connected);
            }
        }
        #endregion

        /// <summary>
        /// 构造方法
        /// </summary>
        protected RPCClient()
        {
            Parser = new RPCProtocolParser();
        }

        #region 方法
        /// <summary>
        /// 连接到目标RPC服务端
        /// </summary>
        /// <param name="ip">启动RPC服务端，开始监听端口</param>
        /// <param name="port">RPC服务端口号</param>
        public bool Connect(string ip, int port)
        {
            //检查参数是否有效
            if (port <= 0 || port >= UInt16.MaxValue)
            {
                HydraLog.Format("连接到RPC服务失败，无效端口号: {0}", port);
                return false;
            }
            IPAddress addr;
            if (!IPAddress.TryParse(ip, out addr))
            {
                HydraLog.Format("连接到RPC服务失败，无效的IP: {0}]", ip);
                return false;
            }

            //连接到RPC服务端
            if (null == EndPoint)
            {
                EndPoint = new TcpClient();
            }
            if (!IsConnected)
            {
                try
                {
                    EndPoint.Connect(addr, port);
                    //开始准备读数据
                    NetworkStream ns = EndPoint.GetStream();
                    if (null != ns)
                    {
                        ns.BeginRead(Buffer, 0, Buffer.Length, OnData, this);
                    }
                }
                catch(Exception e)
                {
                    HydraLog.Format("", e.Message);
                }
            }
            return IsConnected;
        }
        
        /// <summary>
        /// 断开连接
        /// </summary>
        public void DisConnect()
        {
            if(IsConnected)
            {
                EndPoint.Close();
            }
            if (null != SessionClosed)
            {
                SessionClosed(this, "");  //通知会话关闭
            }
            EndPoint = null;
        }
        
        /// <summary>
        /// RPC调用
        /// </summary>
        /// <param name="requestObj">请求对象，可序列化为json</param>
        /// <param name="timeout">调用超时时间，单位秒</param>
        public string SyncCall(object requestObj, int nTimeout = 5)
        {
            string result = JsonValue.Format(new
            {
                result = false,
                error = new
                {
                    Code = 0,
                    Message = "序列化请求失败",
                }
            });

            //序列化
            string request = JsonValue.Format(requestObj);
            if (string.IsNullOrWhiteSpace(request))
            {
                return result;
            }
            JsonValue value = JsonValue.Parse(request);
            if (null != value)
            {
                return result;
            }

            //生成同步等待项
            string id = string.Format("{0}@{1}", value.AsInt("id"), value.AsString("method"));
            SyncCallItem item = new SyncCallItem(id, request);
            RPCSyncCmdMgt.Instance.Add(item);

            //发送请求
            if (!Send(request))
            {
                RPCSyncCmdMgt.Instance.Remove(id);
                return RPCUitlity.GetBadResponse(request, -1, "发送到服务端失败");
            }

            //同步等待操作结果
            item.Singal.WaitOne(TimeSpan.FromSeconds(30));  //加个30秒的保护，必须小于操作超时
            return item.Response;
        }
        
        /// <summary>
        /// 异步调用
        /// </summary>
        public bool PostCall(object requestObj, int nTimeout = 5)
        {
            bool result = false;
            //序列化
            string request = JsonValue.Format(requestObj);
            if (string.IsNullOrWhiteSpace(request))
            {
                return result;
            }
            JsonValue value = JsonValue.Parse(request);
            if (null != value)
            {
                return result;
            }

            //生成异步等待项
            string id = string.Format("{0}@{1}", value.AsInt("id"), value.AsString("method"));
            PostCallItem item = new PostCallItem(this, id, request);
            RPCSyncCmdMgt.Instance.Add(item);
           
            //发送请求
            if (!Send(request))
            {
                RPCSyncCmdMgt.Instance.Remove(id);
                return false;
            }
             
            return true;
        }
        #endregion

        #region 辅助方法
        /// <summary>
        /// 开始接收数据
        /// </summary>
        protected bool BeginRead(TcpClient client)
        {
            try
            {
                if (null == client) return false;
                EndPoint = client;
                //开始准备读数据
                NetworkStream ns = EndPoint.GetStream();
                if (null != ns)
                {
                    ns.BeginRead(Buffer, 0, Buffer.Length, OnData, this);
                    return true;
                }
            }
            catch (Exception e)
            {
                HydraLog.Format("", e.Message);
            }
            return false;
        }

        /// <summary>
        /// 接收来自RPC服务端返回的数据
        /// </summary>
        private void OnData(IAsyncResult ar)
        {
            RPCClient client = ar.AsyncState as RPCClient;
            if (null == client || null == client.EndPoint)
                return;

            try
            {
                //读取请求答复数据
                NetworkStream ns = client.EndPoint.GetStream();
                if (null == ns) return;
                int numberOfBytesRead = ns.EndRead(ar);
                if (numberOfBytesRead > 0)
                {
                    client.Parser.Push(client.Buffer.Take(numberOfBytesRead).ToArray());
                }
                else
                {
                    client.DisConnect();
                    return;
                }
                //数据若已接收完成则解析响应包
                if (!ns.DataAvailable)
                {
                    client.Parser.Parse(client);
                }
                //准备再次读取服务端的请求响应数据
                ns.BeginRead(client.Buffer, 0, client.Buffer.Length, OnData, client);
            }
            catch (IOException e)
            {
                HydraLog.Format("接收服务端响应数据发生异常，原因：{0}", e.Message);
                client.DisConnect();
            }
            catch (ObjectDisposedException e)
            {
                HydraLog.Format("当前客户端主动断开与服务器的连接！", e.Message);
            }
            catch (Exception e)
            {
                HydraLog.Format("接收服务端响应数据发生异常，原因：{0}", e.Message);
                client.DisConnect();
            }
        }

        /// <summary>
        /// 发送请求到RPC服务端
        /// </summary>
        internal bool Send(string cmd)
        {
            if(null == EndPoint || string.IsNullOrWhiteSpace(cmd))
            {
                return false;
            }
            try
            {
                NetworkStream ns = EndPoint.GetStream();
                if (null == ns)
                {
                    return false;
                } 

                //发包 
                byte[] datas = RPCDataPackage.Encode(cmd);
                if(datas.Length > 0)
                {
                    ns.Write(datas, 0, datas.Length);
                    return true;
                }
            }
            catch (Exception e)
            {
                HydraLog.WriteLine("发送数据到RPC服务端失败，原因：" + e.Message);
            }
            return false;
        }
        #endregion

        #region 虚方法，子类必须实现
        /// <summary>
        /// 关联请求以及请求处理方法
        /// </summary>
        protected void Attach(string cmd, Func<string, JsonValue, string> handler)
        {
            if (string.IsNullOrWhiteSpace(cmd))
            {
                HydraLog.Throw("请求方法名称为空", cmd);
            }
            if (null == handler)
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
        abstract protected void Register(string cmd, Func<string, JsonValue, string> handler);
        #endregion

    }
}