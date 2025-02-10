using System.Collections.Generic;
using UnityEngine;

namespace DCFrame {
    public class RedTipMgr {
        public static void Init(RedTipBase redTipBase) {
            redTipMain = redTipBase;
            redTipMain.CreateAllChildRedTip();
            redTipMain.InitAllRedTip();
        }

        public static void Destroy() {
            redTipMain?.DestroyAllRedTip();
            redTipDic.Clear();
        }

        /// <summary>
        /// 判断红点是否激活
        /// </summary>
        public static bool IsActive(string redTipName, int id = 0) {
            string key = GetKey(redTipName, id);
            if (!redTipDic.ContainsKey(key)) {
                return false;
            }
            if (!redTipDic[key].GetIsOpen()) {
                return false;
            }
            return redTipDic[key].IsActive();
        }

        /// <summary>
        /// 获取红点的类型
        /// </summary>
        public static RedTipBase.RedTipType GetRedTipType(string redTipName, int id = 0) {
            string key = GetKey(redTipName, id);
            if (!redTipDic.ContainsKey(key)) {
                return RedTipBase.RedTipType.Default;
            }
            return redTipDic[key].type;
        }

        /// <summary>
        /// 获取某个红点
        /// </summary>
        /// <param name="redTipName"></param>
        public static RedTipBase GetRedTip(string redTipName, int id = 0) {
            string key = GetKey(redTipName, id);
            if (!redTipDic.ContainsKey(key)) {
                return null;
            }
            return redTipDic[key];
        }

        /// <summary>
        /// 创建一个红点
        /// </summary>
        /// <param name="redTipName"></param>
        /// <param name="parent"></param>
        public static void CreateRedTip(string redTipName, RedTipBase parent, int id = 0) {
            string key = GetKey(redTipName, id);
            if (redTipDic.ContainsKey(key)) {
                Debug.LogError("Exits redTip Name is ：" + redTipName + " Id is :" + id);
                return;
            }
            RedTipBase redTipBase = RedTipTree.GetRedTipClass(redTipName);
            redTipBase.SetNameAndParent(redTipName, parent, id);
            AddRedTipToDic(redTipBase);
        }

        /// <summary>
        /// 移除一个红点
        /// </summary>
        public static void DestroyRedTip(string redTipName, int id = 0) {
            string key = GetKey(redTipName, id);
            if (!redTipDic.ContainsKey(key)) {
                Debug.LogError("Not exits redTip Name is ：" + redTipName + " Id is :" + id);
                return;
            }
            RemoveRedTipFromDic(redTipName, id);
        }

        /// <summary>
        /// 添加红点到存储列表内
        /// </summary>
        public static void AddRedTipToDic(RedTipBase redTip) {
            redTipDic.Add(GetKey(redTip.name, redTip.id), redTip);
        }

        /// <summary>
        /// 拼接主Key
        /// </summary>
        public static string GetKey(string name, int id = 0) {
            return name + "_" + id;
        }

        /// <summary>
        /// 从key里面获取名称
        /// </summary>
        public static string GetNameByKey(string key) {
            return key.Split("_")[0];
        }

        /// <summary>
        /// 从key里面获取ID
        /// </summary>
        public static int GetIdByKey(string key) {
            return int.Parse(key.Split("_")[1]);
        }

        /// <summary>
        /// 从存储列表内移除红点
        /// </summary>
        public static void RemoveRedTipFromDic(string redTipName, int id = 0) {
            string key = GetKey(redTipName, id);
            redTipDic.Remove(key);
        }
        
        public static RedTipBase redTipMain;
        /// <summary>
        /// 红点列表（key：遵守GetKey()返回值规则）
        /// </summary>
        private static readonly Dictionary<string, RedTipBase> redTipDic = new Dictionary<string, RedTipBase>();
    }
}