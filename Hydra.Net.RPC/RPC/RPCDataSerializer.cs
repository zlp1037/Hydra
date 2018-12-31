using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hydra.Net.RPC
{
    /// <summary>
    /// 数据序列化器,用于解析和打包各种数据类型的值
    /// </summary>
    public class RPCDataSerializer
    {
        #region 字段
        /// <summary>
        /// 缓存区
        /// </summary>
        private List<byte> _buffer;

        /// <summary>
        /// 缓存区当前下标值, 读取数据时有效
        /// </summary>
        private int _index = 0;
        #endregion

        #region 属性
        /// <summary>
        /// 获得缓存区字节数组, 写数据时有效
        /// </summary>
        public byte[] Buffer
        {
            get
            {
                return (null == _buffer ? null : _buffer.ToArray());
            }
        } 
        #endregion

        /// <summary>
        /// 构造函数，用于将具体的数据转换成字节数组
        /// </summary>
        public RPCDataSerializer()
        {
            _buffer = new List<byte>();
        }

        /// <summary>
        /// 构造函数，用于从提供的字节数组中解析出具体数据
        /// </summary>
        public RPCDataSerializer(byte[] buffer)
        {
            _buffer = new List<byte>(buffer);
        }
        
        #region 读数据
        /// <summary>
        /// 缓存中读取无符号短整型数据
        /// </summary>
        public ushort ReadUShort()
        {
            try
            {
                if (_index < 0 || _index >= _buffer.Count)
                {
                    throw new IndexOutOfRangeException("ReadUShort失败");
                }
                ushort value = BitConverter.ToUInt16(_buffer.GetRange(_index, sizeof(ushort)).ToArray(), 0);
                _index += sizeof(ushort);
                return value;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// 缓存中读取整型数据
        /// </summary>
        public int ReadInt()
        {
            try
            {
                if (_index < 0 || _index >= _buffer.Count)
                {
                    throw new IndexOutOfRangeException("ReadInt失败");
                }
                int value = BitConverter.ToInt32(_buffer.GetRange(_index, sizeof(int)).ToArray(), 0);
                _index += sizeof(int);
                return value;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// 读取无符号整型数据
        /// </summary>
        public uint ReadUInt()
        {
            try
            {
                if (_index < 0 || _index >= _buffer.Count)
                {
                    throw new IndexOutOfRangeException("ReadUInt失败");
                }
                uint value = BitConverter.ToUInt32(_buffer.GetRange(_index, sizeof(uint)).ToArray(), 0);
                _index += sizeof(uint);
                return value;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// 缓存中读取双精度浮点型数据
        /// </summary>
        public double ReadDouble()
        {
            try
            {
                if (_index < 0 || _index >= _buffer.Count)
                {
                    throw new IndexOutOfRangeException("ReadDouble失败");
                }
                double value = BitConverter.ToDouble(_buffer.GetRange(_index, sizeof(double)).ToArray(), 0);
                _index += sizeof(double);
                return value;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// 缓存中读取无符号长整型数据
        /// </summary>
        public ulong ReadULong()
        {
            try
            {
                if (_index < 0 || _index >= _buffer.Count)
                {
                    throw new IndexOutOfRangeException("ReadULong失败");
                }
                ulong value = BitConverter.ToUInt64(_buffer.GetRange(_index, sizeof(ulong)).ToArray(), 0);
                _index += sizeof(ulong);
                return value;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// 读取字节类型数据
        /// </summary>
        public byte ReadByte()
        {
            try
            {
                if (_index < 0 || _index >= _buffer.Count)
                {
                    throw new IndexOutOfRangeException("ReadByte失败");
                }
                return _buffer[_index++];
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// 缓存中读取字符串类型数据
        /// </summary>
        public string ReadString()
        {
            try
            {
                if (_index < 0 || _index >= _buffer.Count)
                {
                    throw new IndexOutOfRangeException("ReadString失败");
                }
                //读取字符串长度
                int length = ReadInt();
                if (length >= 0)
                {
                    //读取字符串具体内容
                    string value = System.Text.Encoding.Unicode.GetString(_buffer.GetRange(_index, length).ToArray());
                    _index += length;
                    return value;
                }
                else
                {
                    throw new IndexOutOfRangeException("ReadString失败,字符串长度小于0！");
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// 缓存中读取字节数组类型数据
        /// </summary>
        public byte[] ReadBytes()
        {
            try
            {
                if (_index < 0 || _index >= _buffer.Count)
                {
                    throw new IndexOutOfRangeException("ReadBytes失败");
                }
                //读取长度
                int length = ReadInt();
                if (length >= 0)
                {
                    //读取字符串具体内容
                    byte[] value = _buffer.GetRange(_index, length).ToArray();
                    _index += length;
                    return value;
                }
                else
                {
                    throw new IndexOutOfRangeException("ReadBytes失败,长度小于0！");
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        #endregion

        #region 写数据
        /// <summary>
        /// 写数据到缓存
        /// </summary>
        private void Write(string method, Action handler)
        {
            try
            {
                if (null == _buffer)
                {
                    throw new InvalidOperationException("数据缓存区未初始化！");
                }
                handler();
            }
            catch (InvalidOperationException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(string.Format("{0}失败，{1}" , method, e.Message));
            }
        }

        /// <summary>
        /// 无符号短整型数据写到缓存
        ///</summary>
        public void WriteUShort(ushort value)
        {
            Write("WriteUShort", () => _buffer.AddRange(BitConverter.GetBytes(value)));
        }

        /// <summary>
        /// 整型数据写到缓存
        ///</summary>
        public void WriteInt(int value)
        {
            Write("WriteInt", () => _buffer.AddRange(BitConverter.GetBytes(value)));
        }

        /// <summary>
        /// 无符号整型数据写到缓存
        /// </summary>
        public void WriteUInt(uint value)
        {
            Write("WriteUInt", () => _buffer.AddRange(BitConverter.GetBytes(value)));
        }

        /// <summary>
        /// 双精度浮点型数据写到缓存
        /// </summary>
        public void WriteDouble(double value)
        {
            Write("WriteDouble", () => _buffer.AddRange(BitConverter.GetBytes(value)));
        }

        /// <summary>
        /// 无符号长整型数据写到缓存
        /// </summary>
        public void WriteULong(ulong value)
        {
            Write("WriteULong", () => _buffer.AddRange(BitConverter.GetBytes(value)));
        }

        /// <summary>
        /// 字节类型数据写到缓存
        /// </summary>
        public void WriteByte(byte value)
        {
            Write("WriteByte", () => _buffer.Add(value));
        }

        /// <summary>
        /// 字节数组类型数据写到缓存
        /// </summary>
        public void WriteBytes(byte[] value)
        {
            Write("WriteBytes", () =>
            {
                WriteInt(value.Length);
                _buffer.AddRange(value);
            });
        }

        /// <summary>
        /// 字符串类型数据写到缓存
        /// </summary>
        public void WriteString(string value)
        {
            Write("WriteString", () =>
            {
                //添加字符串长度
                byte[] content = System.Text.ASCIIEncoding.Unicode.GetBytes(value);
                WriteInt(content.Length);
                //添加字符串具体内容
                _buffer.AddRange(content);
            });
        }
        #endregion
    }
}
