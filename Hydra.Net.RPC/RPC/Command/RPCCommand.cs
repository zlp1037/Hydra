using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hydra.Net.RPC
{
    /// <summary>
    /// RPC协议类型
    /// </summary>
    public enum RPCCommandType
    {
        Json,
        Binary,
    }

    /// <summary>
    /// RPC信令，定义了不同协议类型的RPC信令
    /// </summary>
    public abstract class RPCCommand
    {
        /// <summary>
        /// 指令码，2个字节，表明使用的协议
        /// </summary>
        public RPCCommandType CmdType
        {
            get;
            private set;
        }
       
        /// <summary>
        /// 构造方法
        /// </summary>
        protected RPCCommand(RPCCommandType type)
        {
            CmdType = type;
        }

        /// <summary>
        /// 从字节数组中读取指令的具体内容
        /// </summary>
        public virtual void Read(RPCDataSerializer reader)
        {
            try
            {
                if (null == reader)
                {
                    throw new NullReferenceException("RPC数据序列化读对象为空");
                }
                CmdType = (RPCCommandType)reader.ReadUShort(); 
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("RPC数据序列化读失败，" + e.Message);
            }
        }

        /// <summary>
        /// 得到指令具体内容的字节流
        /// </summary>
        public virtual void Write(RPCDataSerializer writer)
        {
            try
            {
                if (null == writer)
                {
                    throw new NullReferenceException("RPC数据序列化写对象为空");
                }
                //写指令ID
                writer.WriteUShort((ushort)CmdType); 
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("RPC数据序列化写失败，" + e.Message);
            }
        }
    }
}
