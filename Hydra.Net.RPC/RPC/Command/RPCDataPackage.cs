using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hydra.Net.RPC
{
    /// <summary>
    /// RPC数据包: head(4) + body + crc(4) +  tail(4)
    /// </summary>
    public class RPCDataPackage
    {
        #region 字段
        /// <summary>
        /// 包头
        /// </summary>
        private byte[] _head = new byte[] { 0x9A, 0x9A, 0xF0, 0xF0 };

        /// <summary>
        /// 包尾
        /// </summary>
        private byte[] _tail = new byte[] { 0xA9, 0x0F, 0xA9, 0x0F }; 
        #endregion
         
        #region 属性
        /// <summary>
        /// 包头，4个字节
        /// </summary>
        public byte[] Head
        {
            get
            {
                return _head;
            }
        }

        /// <summary>
        /// 数据包具体内容: 有效数据长度 + 有效数据
        /// </summary>
        public byte[] Body
        {
            get;
            set;
        }

        /// <summary>
        /// CRC校验码
        /// </summary>
        public int CRC32
        {
            get;
            private set;
        }
         
        /// <summary>
        /// 包尾，4个字节
        /// </summary>
        public byte[] Tail
        {
            get
            {
                return _tail;
            }
        }
        #endregion

        /// <summary>
        /// 数据包，从字节流中生成一个数据包
        /// </summary>
        public RPCDataPackage()
        {
        }

        #region 方法
        /// <summary>
        /// 获取数据包的字节数组， 用于SOCKET发送数据包
        /// </summary>
        internal byte[] GetBytes()
        {
            try
            {
                List<byte> package = new List<byte>();
                //包头
                package.AddRange(Head);
                //有效数据长度
                package.AddRange(BitConverter.GetBytes(Body.Length));
                //包的具体内容
                package.AddRange(Body);
                //CRC校验码
                byte[] crc = BitConverter.GetBytes(CRC32);
                package.AddRange(crc);
                //包尾
                package.AddRange(Tail);
                return package.ToArray();
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("获取数据包字节数组失败！" + e.Message);
            }
        }

        /// <summary>
        /// 构造一个RPC协议
        /// </summary>
        /// <param name="package">RPC数据包</param>
        internal RPCCommand BuildCommand()
        {
            try
            {
                if (Body.Length < 2)
                {
                    return null;  //内容最少需要2个字节";
                }
                RPCCommandType type = (RPCCommandType)BitConverter.ToUInt16(Body, 0);
                RPCDataSerializer reader = new RPCDataSerializer(Body);
                RPCCommand cmd = null;
                if (RPCCommandType.Json == type)
                {
                    cmd = new RPCJsonCommand();
                }
                else if (RPCCommandType.Binary == type)
                {
                    cmd = new RPCBinaryCommand();
                }
                if (null != cmd)
                {
                    cmd.Read(reader);
                }
                return cmd;
            }
            catch (Exception e)
            {
                HydraLog.WriteLine("构造一个RPC协议包出错！" + e.Message);
            }
            return null;
        }

        #endregion
        
        #region 打包、解包方法
        /// <summary>
        /// 打包指令
        /// </summary>
        public static byte[] Encode(string data)
        {
            try
            {
                //打包
                RPCDataPackage package = new RPCDataPackage();
                RPCJsonCommand cmd = new RPCJsonCommand(data);
                //有效数据
                RPCDataSerializer writer = new RPCDataSerializer();
                cmd.Write(writer);
                package.Body = writer.Buffer;
                if (null == package.Body)
                {
                    throw new InvalidOperationException("有效数据为空");
                }
                //计算CRC校验码
                package.CRC32 = RPC.CRC32.GetCRC(package.Body);

                //返回打包字节数组
                return package.GetBytes();
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Encode失败，" + e.Message);
            }
        }

        /// <summary>
        /// 从字节流从生成一个数据包
        /// </summary>
        public static RPCDataPackage Decode(byte[] datas)
        {
            RPCDataPackage package = new RPCDataPackage();
            try
            {
                if (null == datas || datas.Length < 16)
                {
                    return null; //无足够数据用于构造一个RPC数据包
                }

                //校验包头、包尾
                bool bMatch = (package.Head[0] == datas[0] && package.Head[1] == datas[1]
                              && package.Head[2] == datas[2] && package.Head[3] == datas[3]);
                if (!bMatch)
                {
                    return null; //包头不匹配
                }
                int length = datas.Length;
                bMatch = (package.Tail[0] == datas[length - 4] && package.Tail[1] == datas[length - 3]
                              && package.Tail[2] == datas[length - 2] && package.Tail[3] == datas[length - 1]);
                if (!bMatch)
                {
                    return null; //包尾不匹配
                }

                //得到有效数据长度
                length = BitConverter.ToInt32(datas, 4);
                if (length < 0)
                {
                    return null; //有效数据长度小于0
                }
                //得到有效数据
                List<byte> body = new List<byte>(datas);
                package.Body = body.GetRange(8, length).ToArray();

                //得到CRC校验码
                package.CRC32 = BitConverter.ToInt32(datas, datas.Length - 8);
                int code = RPC.CRC32.GetCRC(package.Body);
                if (package.CRC32 != code)
                {
                    return null; //数据被损坏，无效数据包
                }
            }
            catch (Exception e)
            {
                HydraLog.WriteLine("Decode失败", e);
                package = null;
            }
            return package;
        } 
        #endregion
        
    }
}
