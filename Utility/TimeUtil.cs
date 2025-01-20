using System;

namespace DCFrame.Utility {
    public abstract class TimeUtil {

        /// <summary>
        /// 获取当前系统的时间戳
        /// </summary>
        public static long GetCurTimestamp() {
            return (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000;
        }
    }
}
