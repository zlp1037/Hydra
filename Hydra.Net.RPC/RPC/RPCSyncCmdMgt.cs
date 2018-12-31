using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;

namespace Hydra.Net.RPC
{
    /// <summary>
    /// RPC同步项
    /// </summary>
    public abstract class RPCSyncItem
    {
        /// <summary>
        /// 同步项ID ： id @method
        /// </summary>
        public string Id { get; set; }
         
        /// <summary>
        /// 参数
        /// </summary>
        public string Request { get; set; }

        /// <summary>
        /// 超时时间
        /// </summary>
        public DateTime ExpireTime { get; set; }
         
        /// <summary>
        /// 构造方法
        /// </summary>
        public RPCSyncItem(string id, string request, int timeout = 10)
        {
            Id = id;
            Request = request;
            ExpireTime = DateTime.Now.AddSeconds(timeout);
            
        }

        /// <summary>
        /// 超时
        /// </summary>
        public abstract void OnTimeout(string response);
    }

    /// <summary>
    /// 同步操作项
    /// </summary>
    public class SyncCallItem : RPCSyncItem
    {
        /// <summary>
        /// 同步操作结果响应数据
        /// </summary>
        public string Response { get; set; }

        /// <summary>
        /// 同步信号
        /// </summary>
        public AutoResetEvent Singal { get; private set; }

        /// <summary>
        /// 构造方法
        /// </summary>
        public SyncCallItem(string id, string request, int timeout = 10) : base(id, request, timeout)
        {
            Singal = new AutoResetEvent(false);
        }

        /// <summary>
        /// 同步超时
        /// </summary>
        public override void OnTimeout(string response)
        {
            if(null != Singal)
            {
                Response = response;
                Singal.Set();
            }
        }
    }

    /// <summary>
    /// 异步操作项
    /// </summary>
    public class PostCallItem : RPCSyncItem
    {
        /// <summary>
        /// 客户端对象，用于异步
        /// </summary>
        public RPCClient Client { get; set; }

        /// <summary>
        /// 构造方法
        /// </summary>
        public PostCallItem(RPCClient client, string id, string request, int timeout = 10) : base(id, request, timeout)
        {
        }

        /// <summary>
        /// 异步超时
        /// </summary>
        public override void OnTimeout(string response)
        {
            if(null != Client && null != Client.CmdBus)
            {
                Client.CmdBus.PostCall(Client, response);
            }
        }
    }

    /// <summary>
    /// RPC定时器管理
    /// </summary>
    public class RPCSyncCmdMgt
    {
        #region 静态实例
        private static  RPCSyncCmdMgt _instance = null;
         
        public static RPCSyncCmdMgt Instance
        {
            get
            {
                if(null == _instance)
                {
                    _instance = new RPCSyncCmdMgt();
                }
                return _instance;
            }
        }
        #endregion


        /// <summary>
        /// 锁定对象
        /// </summary>
        static private object _lockObj = new object();

        /// <summary>
        /// 同步项集合
        /// </summary>
        private List<RPCSyncItem> _syncItems = new List<RPCSyncItem>();

        /// <summary>
        /// 定时器
        /// </summary>
        private System.Timers.Timer _timer = new System.Timers.Timer(1000) { Enabled = false };
        
        /// <summary>
        /// 构造方法
        /// </summary>
        public RPCSyncCmdMgt()
        {
            _timer.Elapsed += _timer_Elapsed;
            _timer.Enabled = true; //1秒定时器
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock(_lockObj)
            {
                DateTime time = DateTime.Now;
                for(int i = 0; i < _syncItems.Count; i++)
                {
                    RPCSyncItem item = _syncItems[i];
                    if (item.ExpireTime <= time)
                    {
                        item.OnTimeout(RPCUitlity.GetBadResponse(item.Request, -1, "等待超时"));
                        _syncItems.RemoveAt(i);
                        i--;
                    }
                }
            }
        }

        public void Add(RPCSyncItem item)
        { 
            lock(_lockObj)
            {
                if(null != _syncItems.FirstOrDefault(sync => sync.Id == item.Id))
                {
                    _syncItems.Add(item);
                }
            }
        }

        public void Remove(string item)
        {
            lock (_lockObj)
            {
                RPCSyncItem found = _syncItems.FirstOrDefault(sync => sync.Id == item);
                if (null != found)
                {
                    _syncItems.Remove(found);
                }
            }
        }

        /// <summary>
        /// 操作返回
        /// </summary>
        public void Singal(string item, string response)
        {
            RPCSyncItem found = null;
            lock (_lockObj)
            {
                found = _syncItems.FirstOrDefault(sync => sync.Id == item);
                if (null != found)
                {
                    _syncItems.Remove(found);
                }
            }
            SyncCallItem syncItem = found as SyncCallItem;
            if(null != syncItem)
            {
                syncItem.OnTimeout(response);
            }
        }
    }
}
