using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace Hydra.Net.RPC
{
    /// <summary>
    /// RPC数据协议解析器，用于接收网络上的数据，并从中解析出RPC数据包
    /// </summary>
    public class RPCProtocolParser
    {
        #region 辅助字段
        /// <summary>
        /// 网络上接收的数据缓存区
        /// </summary>
        private List<byte> _buffer = new List<byte>();

        /// <summary>
        /// 根据数据包头计算出来的KMP next数组值
        /// </summary>
        private int[] _headNext = new int[4];

        /// <summary>
        /// Adcon数据包的包头
        /// </summary>
        private byte[] _head = new byte[4] { 0x9A, 0x9A, 0xF0, 0xF0 };

        #endregion

        /// <summary>
        /// 协议帮助类构造函数
        /// </summary>
        public RPCProtocolParser()
        {
            //计算数据包包头的NEXT数组值
            Next(_head, _headNext, 4);
        }

        /// <summary>
        /// 添加接收到数据
        /// </summary>
        public  void Push(byte[] datas)
        {
            try
            {
                //将数据放到缓存区
                if (null != _buffer && null != datas)
                {
                    _buffer.AddRange(datas);
                }
            }
            catch (Exception e)
            {
                HydraLog.WriteLine("添加已接收数据失败！" + e.Message);
            }
        }

        /// <summary>
        /// 从接收的数据中解析出Adcon数据包，若解析成功则向AdconKernelClient投递数据包处理请求
        /// </summary>
        /// <param name="client">数据包处理对象</param>
        public  void Parse(RPCClient client)
        {
            try
            {
                if (null == _buffer || 0 == _buffer.Count)
                {
                    return;
                }
                //遍历缓存区数据找出存在的所有数据包并处理之
                while (true)
                {
                    //首先找到数据包的起始位置
                    int begin = KMPSearch(_buffer, _head, _headNext, 4);
                    if (begin >= 0)
                    {
                        //删除数据包起始位置以前的数据,那么缓存区起始位置就是数据包的包头
                        _buffer.RemoveRange(0, begin);
                        //计算数据包长度
                        int length = 0; //数据包长度
                        if (_buffer.Count >= 8) //包头 + 有效数据长度 = 8个字节
                        {
                            length = BitConverter.ToInt32(_buffer.GetRange(4, 4).ToArray(), 0); //有效数据长度
                            length += 16;//加上包头、包尾、数据长度自身、CRC校验字段长度得到数据包的长度
                        }
                        //解析数据包，若果数据包的长度小于缓存长度则等待下次接收到数据后进行解包
                        if (length >= 16 && _buffer.Count >= 16 && _buffer.Count >= length)
                        {
                            try
                            {
                                RPCDataPackage package = RPCDataPackage.Decode(_buffer.GetRange(0, length).ToArray());
                                if (null != package)
                                {
                                    //解析成功则从缓存区中删除数据包字段,并投递数据包处理请求
                                    _buffer.RemoveRange(0, length);
                                    //解析出RPC数据协议包
                                    RPCCommand cmd = package.BuildCommand();
                                    if(null != cmd && null != client)
                                    {
                                        RPCJsonCommand jsonCmd = cmd as RPCJsonCommand;
                                        if(null != jsonCmd)
                                        {
                                            client.CmdBus.PostCall(client, jsonCmd.Data);
                                        }
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                HydraLog.WriteLine(e.Message);
                                //解析失败，不是一个有效的数据包,则移除当前数据包的包头，寻找下个数据包包头
                                _buffer.RemoveRange(0, 4);
                            }
                        }
                        else
                        {
                            //数据包长度不够，直接退出
                            break;
                        }
                    }
                    else
                    {
                        //找不到包头
                        _buffer.Clear();
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                HydraLog.WriteLine("解析数据包出错！" + e.Message);
            }
        }

        #region KMP模式匹配算法
        /// <summary>
        /// KMP Next函数计算
        /// </summary>
        /// <param name="pattern">模式</param>
        /// <returns>Next数组</returns>
        private void Next(byte[] pattern, int[] next, int length)
        {
            next[0] = -1;
            if (length < 2) //如果只有1个元素不用kmp效率会好一些
            {
                return;
            }

            next[1] = 0;    //第二个元素的回溯函数值必然是0，可以证明：
            //1的前置序列集为{空集,L[0]}，L[0]的长度不小于1，所以淘汰，空集的长度为0，故回溯函数值为0
            int i = 2;  //正被计算next值的字符的索引
            int j = 0;  //计算next值所需要的中间变量，每一轮迭代初始时j总为next[i-1]
            while (i < length)    //很明显当i==length时所有字符的next值都已计算完毕，任务已经完成
            { //状态点
                if (pattern[i - 1] == pattern[j])   //首先必须记住在本函数实现中，迭代计算next值是从第三个元素开始的
                {   //如果L[i-1]等于L[j]，那么next[i] = j + 1
                    next[i++] = ++j;
                }
                else
                {   //如果不相等则检查next[i]的下一个可能值----next[j]
                    j = next[j];
                    if (j == -1)    //如果j == -1则表示next[i]的值是1
                    {   //可以把这一部分提取出来与外层判断合并
                        //书上的kmp代码很难理解的一个原因就是已经被优化，从而遮蔽了其实际逻辑
                        next[i++] = ++j;
                    }
                }
            }
        }

        /// <summary>
        /// KMP查找函数
        /// </summary>
        /// <param name="source">主串</param>
        /// <param name="pattern">用于查找主串中一个位置的模式串</param>
        /// <returns>-1表示没有匹配，否则返回匹配的标号</returns>
        private int KMPSearch(List<byte> source, byte[] pattern, int[] next, int length)
        {
            int i = 0;  //主串指针
            int j = 0;  //模式串指针
            //如果子串没有匹配完毕并且主串没有搜索完成
            while (j < length && i < source.Count)
            {
                if (source[i] == pattern[j])    //i和j的逻辑意义体现于此，用于指示本轮迭代中要判断是否相等的主串字符和模式串字符
                {
                    i++;
                    j++;
                }
                else
                {
                    j = next[j];    //依照指示迭代回溯
                    if (j == -1)    //回溯有情况，这是第二种
                    {
                        i++;
                        j++;
                    }
                }
            }
            //如果j==pattern.Length则表示循环的退出是由于子串已经匹配完毕而不是主串用尽
            return j < length ? -1 : i - j;
        }
        #endregion
    }
}
