using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hydra.Net.RPC
{
    /// <summary>
    /// json协议
    /// </summary>
    public class RPCJsonCommand : RPCCommand
    {
        /// <summary>
        /// 通信数据
        /// </summary>
        public string Data { get; set; }

        /// <summary>
        /// 构造方法
        /// </summary>
        public RPCJsonCommand() : base(RPCCommandType.Json)
        {
        }

        /// <summary>
        /// 构造方法
        /// </summary>
        public RPCJsonCommand(string cmd) : base(RPCCommandType.Json)
        {
            Data = cmd;
        }

        /// <summary>
        /// 读数据
        /// </summary>
        public override void Read(RPCDataSerializer reader)
        {
            base.Read(reader);
            Data = reader.ReadString();
        }

        /// <summary>
        /// 写数据
        /// </summary>
        public override void Write(RPCDataSerializer writer)
        {
            base.Write(writer);
            writer.WriteString(Data);
        }
    }
}
