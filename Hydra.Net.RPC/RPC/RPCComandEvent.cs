using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Hydra.Net.RPC
{
    /// <summary>
    /// RPC信令
    /// </summary>
    public class CommandEvent
    {
        /// <summary>
        /// 信令类型,用于唯一标识一个信令
        /// </summary>
        public string ComandType { get; set; }

        /// <summary>
        /// 超时处理同步对象
        /// </summary>
        internal AutoResetEvent WaitEvent { get; set; }

        /// <summary>
        /// 信令操作的输入参数
        /// </summary>
        public string jParam
        {
            get
            {
                throw new System.NotImplementedException();
            }

            set
            {
            }
        }

        /// <summary>
        /// 信令操作的返回值
        /// </summary>
        public string jRet
        {
            get
            {
                throw new System.NotImplementedException();
            }

            set
            {
            }
        }


    }
}