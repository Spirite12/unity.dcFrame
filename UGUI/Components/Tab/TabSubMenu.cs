using System.Collections.Generic;
using UnityEngine;

namespace DCFrame.UGUI {
    /// <summary>
    /// 单个一级 Tab 对应的二级菜单配置。
    /// </summary>
    public sealed class TabSubMenu {
        /// <summary>
        /// 构造一个子菜单配置。
        /// </summary>
        public TabSubMenu(IReadOnlyList<TabSubMenuOption> options, TabSubMenuListBase scrollList, GameObject arrowObject = null) {
            Options = options;
            ScrollList = scrollList;
            ArrowObject = arrowObject;
        }

        /// <summary>
        /// 子菜单数据集合。
        /// </summary>
        public IReadOnlyList<TabSubMenuOption> Options { get; private set; }

        /// <summary>
        /// 子菜单展开时隐藏、收起时显示的箭头节点。
        /// </summary>
        public GameObject ArrowObject { get; private set; }

        /// <summary>
        /// 子菜单列表组件。
        /// </summary>
        public TabSubMenuListBase ScrollList { get; private set; }
    }
}
