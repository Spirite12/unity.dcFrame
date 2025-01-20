using System.Collections.Generic;
using DCFrame.Utility;
using UnityEngine;
using UnityEngine.Networking;

namespace DCFrame {
    public class UILockData : Singleton<UILockData> {

        /// <summary>
        /// 获取上锁者的信息
        /// </summary>
        /// <param name="goName"></param>
        public string GetLockName(string goName) {
            if (!lockPrefabDic.ContainsKey(goName)) {
                return "";
            }

            return lockPrefabDic[goName].lockName;
        }

        /// <summary>
        /// 获取上锁者的时间
        /// </summary>
        /// <param name="goName"></param>
        /// <returns></returns>
        public long GetLockTime(string goName) {
            if (!lockPrefabDic.ContainsKey(goName)) {
                return 0;
            }

            return lockPrefabDic[goName].lockTime;
        }

        /// <summary>
        /// 设置上锁
        /// </summary>
        /// <param name="goName"></param>
        public void SetLock(string goName) {
            if (!lockPrefabDic.ContainsKey(goName)) {
                return;
            }
            lockPrefabDic[goName].lockName = SystemInfo.deviceName;
            lockPrefabDic[goName].lockTime = TimeUtil.GetCurTimestamp();
            SetNetLockInfo(goName, lockPrefabDic[goName]);
        }

        /// <summary>
        /// 设置解锁
        /// </summary>
        /// <param name="goName"></param>
        public void SetUnLock(string goName) {
            if (!lockPrefabDic.ContainsKey(goName)) {
                return;
            }

            lockPrefabDic[goName] = null;
            lockPrefabDic.Remove(goName);
            SetNetLockInfo(goName, null);
        }

        /// <summary>
        /// 获取请求地址
        /// </summary>
        public UnityWebRequest GetReqNetPath(string goName) {
            var web = UnityWebRequest.Get(reqGetPath + goName);
            //web.SendWebRequest();
            return web;
        }

        /// <summary>
        /// 向服务端设置上锁信息
        /// </summary>
        public void SetNetLockInfo(string goName, LockData lockData) {
            string strReq = reqPath + goName + "/";
            if (lockData != null) {
                strReq += lockData.lockName + "-" + lockData.lockTime;
            }

            var web = UnityWebRequest.Get(strReq);
            //web.SendWebRequest();
        }

        /// <summary>
        /// 解析请求到的地址，并更新数据
        /// </summary>
        /// <param name="jsonWeb"></param>
        public void AnalysisWebInfo(string jsonWeb) {
            var strList = jsonWeb.Split(',');
            string key = strList[0];
            var strValueList = strList[1].Split('-');
            lockPrefabDic[key].lockName = strValueList[0];
            lockPrefabDic[key].lockTime = long.Parse(strValueList[1]);
        }

        /// <summary>
        /// key:预制体名，value：上锁信息
        /// 原则：一个工程里面不能有重复的预制体名
        /// </summary>
        private static readonly Dictionary<string, LockData> lockPrefabDic = new Dictionary<string, LockData>();

        /// <summary>
        /// 请求的地址
        /// </summary>
        private const string reqPath = "xxxxx/";

        /// <summary>
        /// 获取的地址
        /// </summary>
        private const string reqGetPath = "xxxxx/";

        public class LockData {
            public string lockName;
            public long lockTime;

            public LockData(string lockName, int lockTime) {
                this.lockName = lockName; // 上锁机名
                this.lockTime = lockTime; // 上锁时间
            }
        }
    }
}
