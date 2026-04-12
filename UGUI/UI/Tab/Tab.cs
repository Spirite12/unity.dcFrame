using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DCFrame.UGUI {
    /// <summary>
    /// Tab 控制器，负责一级 Tab、二级子菜单列表以及选中态刷新。
    /// </summary>
    public sealed class Tab : IDisposable {
        /// <summary>
        /// Tab 切换回调，参数依次为上一个索引、当前索引、是否由点击触发。
        /// </summary>
        public event Action<int, int, bool> OnSelect;

        /// <summary>
        /// 一级 Tab 切换回调，参数依次为一级索引、换算后的扁平索引。
        /// </summary>
        public event Action<int, int> OnFirstIndexChange;

        /// <summary>
        /// 点击任意 Tab 时触发的音效回调。
        /// </summary>
        public event Action OnClickAudio;

        /// <summary>
        /// 一级 Tab 切换前的拦截判断。
        /// 返回 false 时，本次点击不会继续执行。
        /// </summary>
        public Func<int, bool> JudgeHandler { get; set; }

        /// <summary>
        /// 当前扁平化选中索引。
        /// </summary>
        public int SelectIndex {
            get { return selectIndex; }
        }

        /// <summary>
        /// 当前一级 Tab 索引。
        /// </summary>
        public int SelectFirstIndex {
            get { return GetSelectFirstIndex(); }
        }

        /// <summary>
        /// 当前二级子菜单索引。
        /// </summary>
        public int SelectSecondIndex {
            get { return selectSecondIndex; }
        }

        /// <summary>
        /// 是否禁止点击音效。
        /// </summary>
        public bool IsForbidClickAudio { get; private set; }

        /// <summary>
        /// 最大可点击的一级 Tab 索引。
        /// 为 null 时表示全部可点击。
        /// </summary>
        public int? MaxIndex { get; private set; }

        /// <summary>
        /// 初始化 Tab。
        /// </summary>
        /// <param name="tabItems">一级 Tab 节点集合。</param>
        /// <param name="defaultIndex">默认选中索引。</param>
        /// <param name="maxIndex">最大可点击的一级 Tab 索引。</param>
        /// <param name="subContext">二级子菜单配置。</param>
        /// <param name="subMaskGo">点击空白关闭二级菜单的遮罩节点。</param>
        public void Init(
            IReadOnlyList<TabItem> tabItems,
            int defaultIndex = 0,
            int? maxIndex = null,
            IReadOnlyDictionary<int, TabSubMenu> subContext = null,
            GameObject subMaskGo = null) {
            if (tabItems == null || tabItems.Count == 0) {
                throw new ArgumentException("Tab 节点集合不能为空。", nameof(tabItems));
            }

            ClearTabBindings();
            tabs.Clear();
            ClearSubContext();
            ApplySubMask(null);

            for (int i = 0; i < tabItems.Count; i++) {
                TabItem item = tabItems[i];
                if (item == null) {
                    throw new ArgumentException(string.Format("第 {0} 个 Tab 节点为空。", i), nameof(tabItems));
                }

                tabs.Add(item);
                int tabIndex = i;
                item.BindClick(delegate { OnClick(tabIndex); });
            }

            MaxIndex = maxIndex;
            IsForbidClickAudio = false;

            ApplySubContext(subContext);
            ApplySubMask(subMaskGo);

            selectIndex = Mathf.Clamp(defaultIndex, 0, GetMaxSelectableFlattenIndex());
            selectFirstIndex = selectIndex;
            selectSecondIndex = 0;

            PreOnSelect(selectIndex);
            RenderTabs();
            if (OnSelect != null) {
                OnSelect.Invoke(-1, selectIndex, false);
            }
        }

        /// <summary>
        /// 运行时更新二级子菜单配置。
        /// </summary>
        public void BindSubContext(IReadOnlyDictionary<int, TabSubMenu> subContext) {
            ApplySubContext(subContext);
            selectIndex = Mathf.Clamp(selectIndex, 0, GetMaxSelectableFlattenIndex());
            PreOnSelect(selectIndex);
            RenderTabs();
        }

        /// <summary>
        /// 运行时更新点击空白关闭子菜单的遮罩节点。
        /// </summary>
        public void BindSubMask(GameObject subMaskGo) {
            ApplySubMask(subMaskGo);
        }

        /// <summary>
        /// 获取某个一级 Tab 下的二级子菜单是否处于选中状态。
        /// </summary>
        public bool GetSubMenuListSelect(int tabIndex, int listIndex) {
            if (!subMenuClickContext.ContainsKey(tabIndex)) {
                return false;
            }

            return listIndex == selectSecondIndex;
        }

        /// <summary>
        /// 关闭当前展开的二级子菜单。
        /// </summary>
        public void CancelSecondRect() {
            SwitchSubMenuList(selectFirstIndex, false);
        }

        /// <summary>
        /// 主动设置扁平化选中索引，并触发切换回调。
        /// </summary>
        public void SetSelectIndex(int value, int? lastIndex = null) {
            SwitchSubMenuList(selectFirstIndex, false);
            SwitchSubMenuMask(false);

            int finalLastIndex = lastIndex ?? selectIndex;
            selectIndex = Mathf.Clamp(value, 0, GetMaxSelectableFlattenIndex());
            PreOnSelect(selectIndex);
            RenderTabs();
            if (OnSelect != null) {
                OnSelect.Invoke(finalLastIndex, selectIndex, false);
            }
        }

        /// <summary>
        /// 仅刷新当前索引对应的渲染状态，不触发切换回调。
        /// </summary>
        public void RefreshSelectIndex(int value) {
            SwitchSubMenuList(selectFirstIndex, false);
            SwitchSubMenuMask(false);

            selectIndex = Mathf.Clamp(value, 0, GetMaxSelectableFlattenIndex());
            PreOnSelect(selectIndex);
            RenderTabs();
        }

        /// <summary>
        /// 记录某个一级 Tab 最近一次选中的二级索引。
        /// </summary>
        public bool SetChangedIndex(int firstIndex, int secondIndex) {
            if (!subMenuClickContext.ContainsKey(firstIndex)) {
                return false;
            }

            changedIndexContext[firstIndex] = ClampSecondIndex(firstIndex, secondIndex);
            return true;
        }

        /// <summary>
        /// 获取某个一级 Tab 最近一次选中的二级索引。
        /// </summary>
        public int? GetChangedIndex(int firstIndex) {
            if (!subMenuClickContext.ContainsKey(firstIndex)) {
                return null;
            }

            int changedIndex;
            if (!changedIndexContext.TryGetValue(firstIndex, out changedIndex)) {
                return null;
            }

            return changedIndex;
        }

        /// <summary>
        /// 获取当前选中的一级索引。
        /// 未绑定二级子菜单时，直接返回扁平化索引。
        /// </summary>
        public int GetSelectFirstIndex() {
            return subMenuClickContext.Count > 0 ? selectFirstIndex : selectIndex;
        }

        /// <summary>
        /// 设置一级索引，并尽可能恢复此前缓存的二级索引。
        /// </summary>
        public void SetSelectFirstIndex(int index) {
            index = Mathf.Clamp(index, 0, GetMaxSelectableFirstIndex());
            int lastFirstIndex = selectFirstIndex;
            if (subMenuClickContext.Count == 0) {
                if (lastFirstIndex == index) {
                    return;
                }

                selectIndex = index;
                selectFirstIndex = index;
                selectSecondIndex = 0;
                if (OnFirstIndexChange != null) {
                    OnFirstIndexChange.Invoke(index, index);
                }

                return;
            }

            int lastSecondIndex = selectSecondIndex;
            selectFirstIndex = index;
            if (lastFirstIndex == index) {
                return;
            }

            SetChangedIndex(lastFirstIndex, lastSecondIndex);
            int? changedIndexCache = GetChangedIndex(index);
            selectSecondIndex = ClampSecondIndex(index, changedIndexCache ?? 0);
            if (OnFirstIndexChange != null) {
                int changedIndex = FirstSecondToIndex(index, selectSecondIndex);
                OnFirstIndexChange.Invoke(index, changedIndex);
            }
        }

        /// <summary>
        /// 根据一级、二级索引重新计算扁平化索引。
        /// </summary>
        public void UpdateIndexFromFirstSecond() {
            int index = FirstSecondToIndex(selectFirstIndex, selectSecondIndex);
            if (index >= 0) {
                selectIndex = index;
            }
        }

        /// <summary>
        /// 将一级索引和二级索引换算为扁平化索引。
        /// </summary>
        public int FirstSecondToIndex(int firstIndex, int secondIndex) {
            if (tabs.Count == 0) {
                return -1;
            }

            if (subMenuClickContext.Count == 0) {
                return Mathf.Clamp(firstIndex, 0, GetMaxFirstIndex());
            }

            int safeFirstIndex = Mathf.Clamp(firstIndex, 0, GetMaxFirstIndex());
            int safeSecondIndex = Mathf.Max(0, secondIndex);
            int index = 0;
            for (int i = 0; i < tabs.Count; i++) {
                TabSubMenu secondInfo;
                if (i >= safeFirstIndex) {
                    if (subMenuClickContext.TryGetValue(i, out secondInfo)) {
                        index += Mathf.Clamp(safeSecondIndex, 0, secondInfo.Options.Count - 1);
                    }

                    break;
                }

                if (subMenuClickContext.TryGetValue(i, out secondInfo)) {
                    index += secondInfo.Options.Count;
                } else {
                    index += 1;
                }
            }

            return index;
        }

        /// <summary>
        /// 禁用点击音效回调。
        /// </summary>
        public void ForbidClickAudio() {
            IsForbidClickAudio = true;
        }

        /// <summary>
        /// 释放点击监听与运行时引用。
        /// </summary>
        public void Destroy() {
            SwitchSubMenuMask(false);
            ClearTabBindings();
            ClearSubContext();
            ApplySubMask(null);
            tabs.Clear();
            subMaskGo = null;
            OnSelect = null;
            OnFirstIndexChange = null;
            OnClickAudio = null;
            JudgeHandler = null;
            selectIndex = 0;
            selectFirstIndex = 0;
            selectSecondIndex = 0;
            MaxIndex = null;
            IsForbidClickAudio = false;
        }

        /// <summary>
        /// 实现 IDisposable，方便外部统一回收。
        /// </summary>
        public void Dispose() {
            Destroy();
        }

        /// <summary>
        /// 根据扁平化索引反推当前一级与二级索引。
        /// </summary>
        private void PreOnSelect(int flattenIndex) {
            if (tabs.Count == 0) {
                selectFirstIndex = 0;
                selectSecondIndex = 0;
                return;
            }

            if (subMenuClickContext.Count == 0) {
                selectFirstIndex = Mathf.Clamp(flattenIndex, 0, GetMaxFirstIndex());
                selectSecondIndex = 0;
                return;
            }

            int tempSelect = Mathf.Clamp(flattenIndex, 0, GetMaxFlattenIndex());
            for (int i = 0; i < tabs.Count; i++) {
                TabSubMenu secondInfo;
                if (subMenuClickContext.TryGetValue(i, out secondInfo)) {
                    int secondLength = secondInfo.Options.Count;
                    if (tempSelect < secondLength) {
                        selectFirstIndex = i;
                        selectSecondIndex = Mathf.Clamp(tempSelect, 0, secondLength - 1);
                        return;
                    }

                    tempSelect -= secondLength;
                } else {
                    if (tempSelect == 0) {
                        selectFirstIndex = i;
                        selectSecondIndex = 0;
                        return;
                    }

                    tempSelect -= 1;
                }
            }

            throw new IndexOutOfRangeException(string.Format("Tab 索引越界，selectIndex = {0}", flattenIndex));
        }

        /// <summary>
        /// 刷新所有一级 Tab 的显示状态。
        /// </summary>
        private void RenderTabs() {
            int currentFirstIndex = GetSelectFirstIndex();
            for (int i = 0; i < tabs.Count; i++) {
                int tabIndex = i;
                bool isSelected = currentFirstIndex == tabIndex;
                bool isDisabled = MaxIndex.HasValue && MaxIndex.Value < tabIndex;
                tabs[i].SetState(isSelected, isDisabled);
            }
        }

        /// <summary>
        /// 一级 Tab 点击处理。
        /// </summary>
        private void OnClick(int tabIndex) {
            if (!IsForbidClickAudio && OnClickAudio != null) {
                OnClickAudio.Invoke();
            }

            if (tabs.Count == 0) {
                return;
            }

            if (MaxIndex.HasValue && MaxIndex.Value < tabIndex) {
                return;
            }

            if (tabIndex == GetSelectFirstIndex()) {
                SwitchSubMenuList(tabIndex, true);
            } else {
                if (JudgeHandler != null && !JudgeHandler.Invoke(tabIndex)) {
                    return;
                }

                SwitchSubMenuList(selectFirstIndex, false);

                int lastIndex = selectIndex;
                SetSelectFirstIndex(tabIndex);
                UpdateIndexFromFirstSecond();
                RenderTabs();
                if (OnSelect != null) {
                    OnSelect.Invoke(lastIndex, selectIndex, true);
                }

                SwitchSubMenuMask(false);
            }
        }

        /// <summary>
        /// 二级子菜单项点击处理。
        /// </summary>
        private void OnClickSecond(int index) {
            SwitchSubMenuList(selectFirstIndex, false);
            selectSecondIndex = ClampSecondIndex(selectFirstIndex, index);

            int lastIndex = selectIndex;
            UpdateIndexFromFirstSecond();
            RenderTabs();
            if (OnSelect != null) {
                OnSelect.Invoke(lastIndex, selectIndex, true);
            }
        }

        /// <summary>
        /// 应用二级子菜单配置，并清理旧的子菜单显示状态。
        /// </summary>
        private void ApplySubContext(IReadOnlyDictionary<int, TabSubMenu> subContext) {
            ClearSubContext();
            SwitchSubMenuMask(false);
            if (subContext == null) {
                return;
            }

            foreach (KeyValuePair<int, TabSubMenu> pair in subContext) {
                int tabIndex = pair.Key;
                TabSubMenu subMenuInfo = pair.Value;
                if (tabIndex < 0 || tabIndex >= tabs.Count) {
                    throw new ArgumentOutOfRangeException(
                        nameof(subContext),
                        string.Format("一级 Tab {0} 超出范围，合法范围为 0 到 {1}。", tabIndex, GetMaxFirstIndex()));
                }

                if (subMenuInfo == null) {
                    throw new ArgumentException(string.Format("一级 Tab {0} 的子菜单配置为空。", tabIndex), nameof(subContext));
                }

                if (subMenuInfo.Options == null || subMenuInfo.Options.Count <= 0) {
                    throw new ArgumentException(string.Format("一级 Tab {0} 的子菜单配置不能为空。", tabIndex), nameof(subContext));
                }

                ValidateSubMenuOptions(tabIndex, subMenuInfo.Options, nameof(subContext));

                subMenuClickContext[tabIndex] = subMenuInfo;
                if (subMenuInfo.Options.Count == 1) {
                    SwitchSubMenuList(tabIndex, false);
                    if (subMenuInfo.ArrowObject != null) {
                        subMenuInfo.ArrowObject.SetActive(false);
                    }
                } else {
                    if (subMenuInfo.ScrollList == null) {
                        throw new ArgumentException(
                            string.Format("一级 Tab {0} 配置了多个子菜单，但未提供子菜单列表组件。", tabIndex),
                            nameof(subContext));
                    }

                    subMenuInfo.ScrollList.SetItemClickCallback(OnClickSecond);
                    SwitchSubMenuList(tabIndex, false);
                    SwitchArrowGo(subMenuInfo.ArrowObject, false);
                }
            }
        }

        /// <summary>
        /// 应用子菜单遮罩节点，并重建点击关闭回调。
        /// </summary>
        private void ApplySubMask(GameObject newSubMaskGo) {
            UnbindMaskClick();
            subMaskGo = newSubMaskGo;
            if (subMaskGo == null) {
                return;
            }

            BindClick(
                subMaskGo,
                CancelSecondRect,
                out subMaskButton,
                out subMaskButtonHandler);
            SwitchSubMenuMask(false);
        }

        /// <summary>
        /// 展开或收起某个一级 Tab 对应的二级子菜单列表。
        /// </summary>
        private bool SwitchSubMenuList(int index, bool flag) {
            TabSubMenu subMenuInfo;
            if (!subMenuClickContext.TryGetValue(index, out subMenuInfo)) {
                return false;
            }

            if (subMenuInfo.Options.Count <= 1) {
                if (subMenuInfo.ScrollList != null) {
                    subMenuInfo.ScrollList.HideSelf();
                }

                SwitchSubMenuMask(false);
                return false;
            }

            if (flag) {
                subMenuInfo.ScrollList.ShowSelf();
                subMenuInfo.ScrollList.Render(
                    subMenuInfo.Options,
                    selectSecondIndex,
                    delegate(int listIndex) { return GetSubMenuListSelect(index, listIndex); });
            } else {
                subMenuInfo.ScrollList.HideSelf();
            }

            SwitchSubMenuMask(flag);
            SwitchArrowGo(subMenuInfo.ArrowObject, flag);
            return true;
        }

        /// <summary>
        /// 切换子菜单箭头显示状态。
        /// flag 为 true 表示列表已展开，此时隐藏箭头。
        /// </summary>
        private static void SwitchArrowGo(GameObject arrowObject, bool flag) {
            if (arrowObject == null) {
                return;
            }

            arrowObject.SetActive(!flag);
        }

        /// <summary>
        /// 切换遮罩显隐。
        /// </summary>
        private void SwitchSubMenuMask(bool flag) {
            if (subMaskGo != null) {
                subMaskGo.SetActive(flag);
            }
        }

        /// <summary>
        /// 获取当前扁平化 Tab 总数。
        /// </summary>
        private int GetFlattenTabCount() {
            if (subMenuClickContext.Count == 0) {
                return tabs.Count;
            }

            int count = 0;
            for (int i = 0; i < tabs.Count; i++) {
                TabSubMenu subMenuInfo;
                if (subMenuClickContext.TryGetValue(i, out subMenuInfo)) {
                    count += Mathf.Max(1, subMenuInfo.Options.Count);
                } else {
                    count += 1;
                }
            }

            return count;
        }

        /// <summary>
        /// 将二级索引限制在当前一级 Tab 的有效范围内。
        /// </summary>
        private int ClampSecondIndex(int firstIndex, int secondIndex) {
            TabSubMenu subMenuInfo;
            if (!subMenuClickContext.TryGetValue(firstIndex, out subMenuInfo)) {
                return 0;
            }

            return Mathf.Clamp(secondIndex, 0, subMenuInfo.Options.Count - 1);
        }

        /// <summary>
        /// 校验子菜单项配置，保证子菜单项本身不为空。
        /// </summary>
        private static void ValidateSubMenuOptions(int tabIndex, IReadOnlyList<TabSubMenuOption> options, string paramName) {
            for (int i = 0; i < options.Count; i++) {
                if (options[i] == null) {
                    throw new ArgumentException(string.Format("一级 Tab {0} 的第 {1} 个子菜单项为空。", tabIndex, i), paramName);
                }
            }
        }

        /// <summary>
        /// 解绑当前全部 Tab 的点击监听。
        /// </summary>
        private void ClearTabBindings() {
            for (int i = 0; i < tabs.Count; i++) {
                if (tabs[i] != null) {
                    tabs[i].UnbindClick();
                }
            }
        }

        /// <summary>
        /// 清理当前已绑定的二级子菜单显示状态与运行时引用。
        /// </summary>
        private void ClearSubContext() {
            foreach (KeyValuePair<int, TabSubMenu> pair in subMenuClickContext) {
                TabSubMenu subMenuInfo = pair.Value;
                if (subMenuInfo == null) {
                    continue;
                }

                if (subMenuInfo.ScrollList != null) {
                    subMenuInfo.ScrollList.HideSelf();
                }

                SwitchArrowGo(subMenuInfo.ArrowObject, false);
            }

            subMenuClickContext.Clear();
            changedIndexContext.Clear();
        }

        /// <summary>
        /// 解绑遮罩点击监听。
        /// </summary>
        private void UnbindMaskClick() {
            UnbindClick(subMaskButton, subMaskButtonHandler);
            subMaskButton = null;
            subMaskButtonHandler = null;
        }

        /// <summary>
        /// 为节点绑定点击事件，优先使用 Button，缺失时自动补组件。
        /// </summary>
        private static void BindClick(
            GameObject gameObject,
            Action onClick,
            out Button button,
            out UnityAction buttonHandler) {
            button = null;
            buttonHandler = null;

            if (gameObject == null || onClick == null) {
                return;
            }

            button = gameObject.GetComponent<Button>();
            if (button == null) {
                button = gameObject.AddComponent<Button>();
            }

            buttonHandler = delegate { onClick.Invoke(); };
            button.onClick.AddListener(buttonHandler);
        }

        /// <summary>
        /// 移除通过 BindClick 注册的点击监听。
        /// </summary>
        private static void UnbindClick(Button button, UnityAction buttonHandler) {
            if (button != null && buttonHandler != null) {
                button.onClick.RemoveListener(buttonHandler);
            }
        }

        /// <summary>
        /// 获取当前一级索引的最大合法值。
        /// </summary>
        private int GetMaxFirstIndex() {
            return Mathf.Max(0, tabs.Count - 1);
        }

        /// <summary>
        /// 获取当前允许选中的一级索引最大值。
        /// </summary>
        private int GetMaxSelectableFirstIndex() {
            if (!MaxIndex.HasValue) {
                return GetMaxFirstIndex();
            }

            return Mathf.Clamp(MaxIndex.Value, 0, GetMaxFirstIndex());
        }

        /// <summary>
        /// 获取当前扁平化索引的最大合法值。
        /// </summary>
        private int GetMaxFlattenIndex() {
            return Mathf.Max(0, GetFlattenTabCount() - 1);
        }

        /// <summary>
        /// 获取当前允许选中的扁平化索引最大值。
        /// </summary>
        private int GetMaxSelectableFlattenIndex() {
            return FirstSecondToIndex(GetMaxSelectableFirstIndex(), int.MaxValue);
        }

        private readonly List<TabItem> tabs = new List<TabItem>();
        private readonly Dictionary<int, TabSubMenu> subMenuClickContext = new Dictionary<int, TabSubMenu>();
        private readonly Dictionary<int, int> changedIndexContext = new Dictionary<int, int>();

        private GameObject subMaskGo;
        private Button subMaskButton;
        private UnityAction subMaskButtonHandler;

        private int selectIndex;
        private int selectFirstIndex;
        private int selectSecondIndex;
    }
}
