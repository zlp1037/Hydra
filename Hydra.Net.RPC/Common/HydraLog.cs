using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Hydra.Net.RPC
{
    /// <summary>
    /// 日志类
    /// </summary>
    public static class HydraLog
    {
        public static void WriteLine(string msg, string reason)
        {
#if TRACE
            Trace.WriteLine(string.Format("{0}, 原因： {1} ", msg, reason), DateTime.Now.ToString());
#else
            Debug.WriteLine(string.Format("{0}, 原因： {1} ", msg, reason), DateTime.Now.ToString());
#endif
        }

        public static void WriteLine(string msg)
        {
#if TRACE
            Trace.WriteLine(msg, DateTime.Now.ToString());
#else
            Debug.WriteLine(msg, DateTime.Now.ToString());
#endif
        }

        public static void WriteLine(string msg, Exception e)
        {
#if TRACE
            Trace.WriteLine(string.Format("{0}, 原因： {1} ", msg, e.Message), DateTime.Now.ToString());
#else
            Debug.WriteLine(string.Format("{0}, 原因： {1} ", msg, e.Message), DateTime.Now.ToString());
#endif
        }

        public static void Format(string format, params object[] args)
        {
            WriteLine(string.Format(format, args));
        }
        
        public static void Throw(string format, params object[] args)
        {
            string msg = string.Format(format, args);
            WriteLine(msg);
            throw new InvalidOperationException(msg);
        }
    }
}
