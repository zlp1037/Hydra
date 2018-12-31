using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hydra.Net.RPC
{
    /// <summary>
    /// 二进制协议的RPC通信
    /// </summary>
    public class RPCBinaryCommand : RPCCommand
    {
        /// <summary>
        /// 二进制传输数据
        /// </summary>
        public Byte[] Datas { get; set; }

        /// <summary>
        /// 构造方法
        /// </summary>
        public RPCBinaryCommand() : base(RPCCommandType.Binary)
        {

        }

        /// <summary>
        /// 构造方法
        /// </summary>
        public RPCBinaryCommand(byte[] datas) : base(RPCCommandType.Binary)
        {
            Datas = datas;
        }

        /// <summary>
        /// 读数据
        /// </summary>
        public override void Read(RPCDataSerializer reader)
        {
            base.Read(reader);
            Datas = reader.ReadBytes();
            
        }

        /// <summary>
        /// 写数据
        /// </summary>
        public override void Write(RPCDataSerializer writer)
        {
            base.Write(writer);
            writer.WriteBytes(Datas);
        }
    }
}
