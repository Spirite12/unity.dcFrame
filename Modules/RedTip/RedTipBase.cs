using System.Collections.Generic;
using UnityEngine;

namespace DCFrame {
    public class RedTipBase {
        public enum RedTipType {
            // 默认
            Default = 0,
            // 带数字的红点
            Number = 1,
        }

        /// <summary>
        /// 预加载函数，处理红点未开放到开放的事件监听
        /// </summary>
        public virtual void PreInit() { }
        /// <summary>
        /// 初始化函数，处理红点开放数据
        /// </summary>
        public virtual void Init() { }
        /// <summary>
        /// 销毁函数，处理红点数据和注销相关事件监听
        /// </summary>
        public virtual void Destroy() { }

        public RedTipBase(string name, RedTipBase parent, int id = 0) {
            active = false;
            this.name = name;
            this.parent = parent;
            this.id = id;
            if (RedTipTree.redTipTreeDic.TryGetValue(name, out var value)) {
                foreach (var childName in value) {
                    var key = RedTipMgr.GetKey(childName);
                    childDic.Add(key, null);
                }
            }
            RedTipMgr.AddRedTipToDic(this);
        }

        public RedTipBase() {
            active = false;
        }

        /// <summary>
        /// 当传入无参的时候需要调用此函数传参
        /// </summary>
        public void SetNameAndParent(string name, RedTipBase parent, int id = 0) {
            this.name = name;
            this.parent = parent;
            this.id = id;
            if (RedTipTree.redTipTreeDic.TryGetValue(name, out var value)) {
                foreach (var childName in value) {
                    var key = RedTipMgr.GetKey(childName);
                    childDic.Add(key, null);
                }
            }
            RedTipMgr.AddRedTipToDic(this);
        }

        public virtual bool GetIsOpen() {
            return isOpen;
        }

        /// <summary>
        /// 设置红点是否开启
        /// </summary>
        protected void SetIsOpen(bool isOpen) {
            if (this.isOpen == isOpen) {
                return;
            }
            this.isOpen = isOpen;
            if (isOpen) {
                InitAllRedTip();
            }
        }

        /// <summary>
        /// 迭代调用初始化红点
        /// </summary>
        public void InitAllRedTip() {
            if (!GetIsOpen()) {
                SetIsOpen(false);
                PreInit();
                return;
            }
            Init();
            foreach (var keyValue in childDic) {
                keyValue.Value.InitAllRedTip();
            }
        }

        /// <summary>
        /// 迭代调用红点销毁函数
        /// </summary>
        public void DestroyAllRedTip() {
            Destroy();
            foreach (var keyValue in childDic) {
                keyValue.Value?.DestroyAllRedTip();
            }
            RedTipMgr.RemoveRedTipFromDic(name, id);
        }

        /// <summary>
        /// 判断是否激活
        /// </summary>
        public bool IsActive() {
            return active;
        }

        /// <summary>
        /// 设置是否激活
        /// </summary>
        public void SetActive(bool isActive) {
            if (active == isActive) {
                return;
            }
            // 当前红点未激活
            if (!GetIsOpen()) {
                return;
            }
            // 存在子节点的整体状态和当前设置状态不一致，则不处理
            int childActiveCount = 0;
            bool hasChild = false;
            foreach (var keyValue in childDic) {
                childActiveCount += keyValue.Value.GetIsOpen() && keyValue.Value.IsActive() ? 1 : 0;
                hasChild = true;
            }
            if (hasChild && (isActive && childActiveCount <= 0 || !isActive && childActiveCount > 0)) {
                Debug.LogWarning("SetActive value is invalid, RedTip name is :" + name);
                return;
            }
            active = isActive;
            // 刷新父节点红点激活状态
            if (parent != null && parent.IsActive() != isActive) {
                if (isActive) {
                    parent.SetActive(true);
                }else {
                    bool parentActive = false;
                    foreach (var keyValue in parent.childDic) {
                        if (keyValue.Value != null && keyValue.Value.IsActive() && keyValue.Value.GetIsOpen()) {
                            parentActive = true;
                            break;
                        }
                    }
                    if (!parentActive) {
                        parent.SetActive(false);
                    }
                }
            }
            EventMgr.DispatchEvent(EventBase.Frame.RedTipRefreshActive, name, IsActive(), id);
        }

        /// <summary>
        /// 迭代创建红点
        /// </summary>
        public void CreateAllChildRedTip() {
            var keysToUpdate = new List<string>();
            foreach (var keyValue in childDic) {
                if (keyValue.Value == null) {
                    keysToUpdate.Add(keyValue.Key);
                }
            }
            foreach (var key in keysToUpdate) {
                string name = RedTipMgr.GetNameByKey(key);
                int id = RedTipMgr.GetIdByKey(key);
                childDic[key] = RedTipTree.GetRedTipClass(name);
                childDic[key].SetNameAndParent(name, this, id);
                childDic[key].CreateAllChildRedTip();
            }
        }

        /// <summary>
        /// 获取子节点的数量
        /// </summary>
        public int GetChildNum(bool isOnlyActive = false) {
            int activeNum = 0;
            foreach (var keyValue in childDic) {
                if (keyValue.Value != null && (!isOnlyActive || keyValue.Value.IsActive())) {
                    activeNum += 1;
                }
            }
            return activeNum;
        }

        /// <summary>
        /// 创建子红点
        /// </summary>
        public void CreateChildRedTip(string name, RedTipBase redTip, int id = 0) {
            string key = RedTipMgr.GetKey(name, id);
            if (childDic.ContainsKey(key)) {
                return;
            }
            redTip.id = id;
            childDic.Add(key, redTip);
            EventMgr.DispatchEvent(EventBase.Frame.RedTipChildModify, this.name, true);
        }
        
        /// <summary>
        /// 移除子红点
        /// </summary>
        public void DestroyChildRedTip(string name, int id = 0) {
            string key = RedTipMgr.GetKey(name, id);
            if (!childDic.ContainsKey(key)) {
                return;
            }
            childDic.Remove(key);
            EventMgr.DispatchEvent(EventBase.Frame.RedTipChildModify, name, false);
        }

        /// <summary>
        /// 获取子节点列表
        /// </summary>
        public Dictionary<string, RedTipBase> GetChildDic() {
            return childDic;
        }

        /// <summary>
        /// 红点的名称
        /// </summary>
        public string name = "";
        /// <summary>
        /// 处理同一个红点名称，不同ID的情况
        /// </summary>
        public int id = 0;
        /// <summary>
        /// 红点类型
        /// </summary>
        public RedTipType type = RedTipType.Default;
        /// <summary>
        /// 父节点
        /// </summary>
        public RedTipBase parent;
        /// <summary>
        /// 是否启用红点（判断条件没满足就不初始化）
        /// </summary>
        protected bool isOpen = true;
        /// <summary>
        /// 红点是否激活
        /// </summary>
        private bool active = false;
        /// <summary>
        /// 红点子节点列表（key：遵守RedTipMgr.GetKey()返回值规则）
        /// </summary>
        private readonly Dictionary<string, RedTipBase> childDic = new Dictionary<string, RedTipBase>();
    }
}

