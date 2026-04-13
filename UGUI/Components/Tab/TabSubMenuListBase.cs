using System;
using System.Collections.Generic;
using UnityEngine;

namespace DCFrame.UGUI {
    /// <summary>
    /// 二级子菜单列表的抽象基类。
    /// 业务侧实现自己的列表渲染时，继承该类即可接入 Tab 控制器。
    /// </summary>
    public abstract class TabSubMenuListBase : MonoBehaviour {
        /// <summary>
        /// 绑定列表项点击回调，参数为二级索引。
        /// </summary>
        public abstract void SetItemClickCallback(Action<int> onItemClick);

        /// <summary>
        /// 根据参数刷新子菜单列表内容。
        /// </summary>
        public abstract void Render(IReadOnlyList<TabSubMenuOption> options, int moveToIndex, Func<int, bool> isSelected);

        /// <summary>
        /// 显示子菜单列表。
        /// </summary>
        public virtual void ShowSelf() {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// 隐藏子菜单列表。
        /// </summary>
        public virtual void HideSelf() {
            gameObject.SetActive(false);
        }
    }
}
