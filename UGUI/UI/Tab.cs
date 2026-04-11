using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DCFrame.UGUI {
    /// <summary>
    /// Tab 控制器，负责一级 Tab、二级下拉列表以及选中态刷新。
    /// 该实现保留了 Lua 版本的主要行为，并将外部依赖改成可注入的回调。
    /// </summary>
    public sealed class Tab : IDisposable {
        /// <summary>
        /// Tab 切换回调，参数依次为上一个索引、当前索引、是否由点击触发。
        /// </summary>
        public Action<int, int, bool> OnSelect { get; set; }

        /// <summary>
        /// 切换一级 Tab 前的拦截判断。
        /// 返回 false 时，本次点击不会继续执行。
        /// </summary>
        public Func<int, bool> OnJudge { get; set; }

        /// <summary>
        /// 一级 Tab 切换时的回调，参数依次为一级索引、扁平化后的选中索引。
        /// </summary>
        public Action<int, int> OnFirstIndexChange { get; set; }

        /// <summary>
        /// 点击任意 Tab 时触发的音效回调。
        /// </summary>
        public Action OnClickAudio { get; set; }

        /// <summary>
        /// 点击成功切换后触发的音效回调。
        /// </summary>
        public Action OnSelectAudio { get; set; }

        /// <summary>
        /// 一级 Tab 切换前触发的引导回调。
        /// </summary>
        public Action OnGuideNextByTab { get; set; }

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
        /// 当前二级下拉索引。
        /// </summary>
        public int SelectSecondIndex {
            get { return selectSecondIndex; }
        }

        /// <summary>
        /// 是否跳过基础选中态渲染。
        /// </summary>
        public bool IsBaseRender { get; set; }

        /// <summary>
        /// 是否禁用点击音效。
        /// </summary>
        public bool IsForbidClickAudio { get; private set; }

        /// <summary>
        /// 最大允许点击的一级 Tab 索引。
        /// 为 null 时表示全部可点击。
        /// </summary>
        public int? MaxIndex { get; private set; }

        private readonly List<TabItem> tabs = new List<TabItem>();
        private readonly Dictionary<int, TabDropdownInfo> dropdownClickContext = new Dictionary<int, TabDropdownInfo>();

        private GameObject dropdownMaskObject;
        private Button dropdownMaskButton;
        private UnityAction dropdownMaskButtonHandler;
        private TabClickRelay dropdownMaskRelay;
        private Action dropdownMaskRelayHandler;

        private int selectIndex = 1;
        private int selectFirstIndex = 1;
        private int selectSecondIndex = 1;

        /// <summary>
        /// 初始化 Tab 以及点击回调。
        /// </summary>
        /// <param name="tabItems">Tab 节点集合，索引从 1 开始语义化处理</param>
        /// <param name="selectHandler">切换回调</param>
        /// <param name="defaultIndex">默认选中索引</param>
        /// <param name="maxSelectableIndex">最大可点击一级索引</param>
        /// <param name="judgeHandler">点击拦截回调</param>
        /// <param name="isBaseRender">是否禁用基础渲染</param>
        public void Init(
            IReadOnlyList<TabItem> tabItems,
            Action<int, int, bool> selectHandler,
            int defaultIndex = 1,
            int? maxSelectableIndex = null,
            Func<int, bool> judgeHandler = null,
            bool isBaseRender = false) {
            if (tabItems == null || tabItems.Count == 0) {
                throw new ArgumentException("Tab 节点集合不能为空。", nameof(tabItems));
            }

            ClearTabBindings();
            tabs.Clear();
            dropdownClickContext.Clear();

            for (int i = 0; i < tabItems.Count; i++) {
                TabItem item = tabItems[i];
                if (item == null) {
                    throw new ArgumentException(string.Format("第 {0} 个 Tab 节点为空。", i + 1), nameof(tabItems));
                }

                tabs.Add(item);
                int tabIndex = i + 1;
                item.BindClick(delegate { OnClick(tabIndex); });
            }

            OnSelect = selectHandler;
            OnJudge = judgeHandler;
            MaxIndex = maxSelectableIndex;
            IsBaseRender = isBaseRender;
            selectIndex = Mathf.Clamp(defaultIndex, 1, tabs.Count);
            selectFirstIndex = selectIndex;
            selectSecondIndex = 1;

            PreOnSelect(selectIndex);
            RenderTabs();
            if (OnSelect != null) {
                OnSelect.Invoke(0, selectIndex, false);
            }
        }

        /// <summary>
        /// 绑定带有二级下拉列表的 Tab 配置。
        /// key 为一级 Tab 的 1 基索引。
        /// </summary>
        public void BindDropdownClick(IReadOnlyDictionary<int, TabDropdownInfo> dropdownContext) {
            dropdownClickContext.Clear();
            if (dropdownContext == null) {
                return;
            }

            foreach (KeyValuePair<int, TabDropdownInfo> pair in dropdownContext) {
                int tabIndex = pair.Key;
                TabDropdownInfo dropdownInfo = pair.Value;
                if (dropdownInfo == null) {
                    throw new ArgumentException(string.Format("一级 Tab {0} 的下拉配置为空。", tabIndex), nameof(dropdownContext));
                }

                if (dropdownInfo.Options == null || dropdownInfo.Options.Count <= 0) {
                    throw new ArgumentException(string.Format("一级 Tab {0} 的下拉配置不能为空。", tabIndex), nameof(dropdownContext));
                }

                dropdownClickContext[tabIndex] = dropdownInfo;
                if (dropdownInfo.Options.Count == 1) {
                    SwitchDropdownList(tabIndex, false);
                    if (dropdownInfo.ArrowObject != null) {
                        dropdownInfo.ArrowObject.SetActive(false);
                    }
                } else {
                    if (dropdownInfo.ScrollList == null) {
                        throw new ArgumentException(string.Format("一级 Tab {0} 配置了多项下拉，但未提供下拉列表组件。", tabIndex), nameof(dropdownContext));
                    }

                    dropdownInfo.ScrollList.SetItemClickCallback(OnClickSecond);
                    SwitchDropdownList(tabIndex, false);
                    SwitchArrowGo(dropdownInfo.ArrowObject, false);
                }
            }

            PreOnSelect(selectIndex);
            RenderTabs();
        }

        /// <summary>
        /// 绑定点击空白区域关闭下拉的遮罩节点。
        /// </summary>
        public void BindDropdownMaskUI(GameObject dropdownMask) {
            UnbindMaskClick();
            dropdownMaskObject = dropdownMask;
            if (dropdownMaskObject == null) {
                return;
            }

            BindClick(dropdownMaskObject, CancelSecondRect, out dropdownMaskButton, out dropdownMaskButtonHandler, out dropdownMaskRelay, out dropdownMaskRelayHandler);
            SwitchDropdownMask(false);
        }

        /// <summary>
        /// 绑定一级 Tab 切换回调。
        /// </summary>
        public void BindFirstIndexChange(Action<int, int> onFirstIndexChange) {
            OnFirstIndexChange = onFirstIndexChange;
        }

        /// <summary>
        /// 获取某个一级 Tab 下的二级下拉是否处于选中状态。
        /// </summary>
        public bool GetDropdownListSelect(int tabIndex, int listIndex) {
            if (!dropdownClickContext.ContainsKey(tabIndex)) {
                return false;
            }

            return listIndex == selectSecondIndex;
        }

        /// <summary>
        /// 关闭当前展开的二级下拉。
        /// </summary>
        public void CancelSecondRect() {
            SwitchDropdownList(selectFirstIndex, false);
        }

        /// <summary>
        /// 主动设置扁平化选中索引，并触发切换回调。
        /// </summary>
        public void SetSelectIndex(int value, int? lastIndex = null) {
            int finalLastIndex = lastIndex ?? selectIndex;
            selectIndex = Mathf.Clamp(value, 1, GetFlattenTabCount());
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
            selectIndex = Mathf.Clamp(value, 1, GetFlattenTabCount());
            PreOnSelect(selectIndex);
            RenderTabs();
        }

        /// <summary>
        /// 记录某个一级 Tab 最近一次选中的二级索引。
        /// </summary>
        public bool SetChangedIndex(int firstIndex, int secondIndex) {
            TabDropdownInfo dropdownInfo;
            if (!dropdownClickContext.TryGetValue(firstIndex, out dropdownInfo)) {
                return false;
            }

            dropdownInfo.ChangedIndex = Mathf.Max(1, secondIndex);
            return true;
        }

        /// <summary>
        /// 获取某个一级 Tab 最近一次选中的二级索引。
        /// </summary>
        public int? GetChangedIndex(int firstIndex) {
            TabDropdownInfo dropdownInfo;
            if (!dropdownClickContext.TryGetValue(firstIndex, out dropdownInfo)) {
                return null;
            }

            return dropdownInfo.ChangedIndex;
        }

        /// <summary>
        /// 获取当前选中的一级索引。
        /// 未绑定二级下拉时，直接返回扁平化索引。
        /// </summary>
        public int GetSelectFirstIndex() {
            return dropdownClickContext.Count > 0 ? selectFirstIndex : selectIndex;
        }

        /// <summary>
        /// 设置一级索引，并尽可能恢复此前缓存的二级索引。
        /// </summary>
        public void SetSelectFirstIndex(int index) {
            index = Mathf.Clamp(index, 1, tabs.Count);
            if (dropdownClickContext.Count == 0) {
                selectIndex = index;
                return;
            }

            int lastFirstIndex = selectFirstIndex;
            int lastSecondIndex = selectSecondIndex;
            selectFirstIndex = index;
            if (lastFirstIndex == index) {
                return;
            }

            if (SetChangedIndex(lastFirstIndex, lastSecondIndex) && OnFirstIndexChange != null) {
                int changedIndex = FirstSecondToIndex(index, lastSecondIndex);
                OnFirstIndexChange.Invoke(index, changedIndex);
            }

            int? changedIndexCache = GetChangedIndex(index);
            selectSecondIndex = changedIndexCache ?? 1;
        }

        /// <summary>
        /// 根据一级、二级索引重新计算扁平化索引。
        /// </summary>
        public void UpdateIndexFromFirstSecond() {
            int index = FirstSecondToIndex(selectFirstIndex, selectSecondIndex);
            if (index > 0) {
                selectIndex = index;
            }
        }

        /// <summary>
        /// 将一级索引和二级索引换算为扁平化索引。
        /// </summary>
        public int FirstSecondToIndex(int firstIndex, int secondIndex) {
            if (dropdownClickContext.Count == 0) {
                return Mathf.Clamp(firstIndex, 1, tabs.Count);
            }

            int index = 0;
            int safeSecondIndex = Mathf.Max(1, secondIndex);
            for (int i = 1; i <= tabs.Count; i++) {
                TabDropdownInfo secondInfo;
                if (i >= firstIndex) {
                    if (dropdownClickContext.TryGetValue(i, out secondInfo)) {
                        index += Mathf.Clamp(safeSecondIndex, 1, secondInfo.Options.Count);
                    } else {
                        index += 1;
                    }

                    break;
                }

                if (dropdownClickContext.TryGetValue(i, out secondInfo)) {
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
            SwitchDropdownMask(false);
            ClearTabBindings();
            UnbindMaskClick();
            tabs.Clear();
            dropdownClickContext.Clear();
            dropdownMaskObject = null;
            OnSelect = null;
            OnJudge = null;
            OnFirstIndexChange = null;
            OnClickAudio = null;
            OnSelectAudio = null;
            OnGuideNextByTab = null;
            selectIndex = 1;
            selectFirstIndex = 1;
            selectSecondIndex = 1;
            MaxIndex = null;
        }

        /// <summary>
        /// 实现 IDisposable，方便外部用 using 或生命周期统一回收。
        /// </summary>
        public void Dispose() {
            Destroy();
        }

        /// <summary>
        /// 根据扁平化索引反推当前一级与二级索引。
        /// </summary>
        private void PreOnSelect(int flattenIndex) {
            if (dropdownClickContext.Count == 0) {
                selectFirstIndex = Mathf.Clamp(flattenIndex, 1, tabs.Count);
                selectSecondIndex = 1;
                return;
            }

            int tempSelect = flattenIndex;
            for (int i = 1; i <= tabs.Count; i++) {
                TabDropdownInfo secondInfo;
                if (dropdownClickContext.TryGetValue(i, out secondInfo)) {
                    int secondLength = secondInfo.Options.Count;
                    if (tempSelect <= secondLength) {
                        selectFirstIndex = i;
                        selectSecondIndex = Mathf.Clamp(tempSelect, 1, secondLength);
                        return;
                    }

                    tempSelect -= secondLength;
                } else {
                    if (tempSelect <= 1) {
                        selectFirstIndex = i;
                        selectSecondIndex = 1;
                        return;
                    }

                    tempSelect -= 1;
                }
            }

            throw new IndexOutOfRangeException(string.Format("Tab 索引越界，selectIndex = {0}", flattenIndex));
        }

        /// <summary>
        /// 刷新所有一级 Tab 的显隐状态。
        /// </summary>
        private void RenderTabs() {
            if (IsBaseRender) {
                return;
            }

            int currentFirstIndex = GetSelectFirstIndex();
            for (int i = 0; i < tabs.Count; i++) {
                int tabIndex = i + 1;
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

            bool isOkClick = false;
            if (tabIndex == GetSelectFirstIndex()) {
                isOkClick = SwitchDropdownList(tabIndex, true);
            } else {
                if (OnJudge != null && !OnJudge.Invoke(tabIndex)) {
                    return;
                }

                SwitchDropdownList(selectFirstIndex, false);
                if (OnGuideNextByTab != null) {
                    OnGuideNextByTab.Invoke();
                }

                int lastIndex = selectIndex;
                SetSelectFirstIndex(tabIndex);
                UpdateIndexFromFirstSecond();
                RenderTabs();
                if (OnSelect != null) {
                    OnSelect.Invoke(lastIndex, selectIndex, true);
                }

                SwitchDropdownMask(false);
                isOkClick = true;
            }

            if (isOkClick && OnSelectAudio != null) {
                OnSelectAudio.Invoke();
            }
        }

        /// <summary>
        /// 二级下拉项点击处理。
        /// </summary>
        private void OnClickSecond(int index) {
            SwitchDropdownList(selectFirstIndex, false);
            selectSecondIndex = Mathf.Max(1, index);

            int lastIndex = selectIndex;
            UpdateIndexFromFirstSecond();
            RenderTabs();
            if (OnSelect != null) {
                OnSelect.Invoke(lastIndex, selectIndex, true);
            }
        }

        /// <summary>
        /// 展开或收起某个一级 Tab 对应的二级下拉列表。
        /// </summary>
        private bool SwitchDropdownList(int index, bool flag) {
            TabDropdownInfo dropdownInfo;
            if (!dropdownClickContext.TryGetValue(index, out dropdownInfo)) {
                return false;
            }

            if (dropdownInfo.Options.Count <= 1) {
                if (dropdownInfo.ScrollList != null) {
                    dropdownInfo.ScrollList.HideSelf();
                }

                SwitchDropdownMask(false);
                return false;
            }

            if (flag) {
                dropdownInfo.ScrollList.ShowSelf();
                TabDropdownRenderContext context = new TabDropdownRenderContext(
                    dropdownInfo.Options,
                    selectSecondIndex,
                    delegate(int listIndex) { return GetDropdownListSelect(index, listIndex); });
                dropdownInfo.ScrollList.Render(context);
            } else {
                dropdownInfo.ScrollList.HideSelf();
            }

            SwitchDropdownMask(flag);
            SwitchArrowGo(dropdownInfo.ArrowObject, flag);
            return true;
        }

        /// <summary>
        /// 切换下拉箭头显隐。
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
        private void SwitchDropdownMask(bool flag) {
            if (dropdownMaskObject != null) {
                dropdownMaskObject.SetActive(flag);
            }
        }

        /// <summary>
        /// 获取当前扁平化 Tab 总数。
        /// </summary>
        private int GetFlattenTabCount() {
            if (dropdownClickContext.Count == 0) {
                return tabs.Count;
            }

            int count = 0;
            for (int i = 1; i <= tabs.Count; i++) {
                TabDropdownInfo dropdownInfo;
                if (dropdownClickContext.TryGetValue(i, out dropdownInfo)) {
                    count += Mathf.Max(1, dropdownInfo.Options.Count);
                } else {
                    count += 1;
                }
            }

            return count;
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
        /// 解绑遮罩点击监听。
        /// </summary>
        private void UnbindMaskClick() {
            UnbindClick(dropdownMaskButton, dropdownMaskButtonHandler, dropdownMaskRelay, dropdownMaskRelayHandler);
            dropdownMaskButton = null;
            dropdownMaskButtonHandler = null;
            dropdownMaskRelay = null;
            dropdownMaskRelayHandler = null;
        }

        /// <summary>
        /// 为节点绑定点击事件，优先使用 Button，缺失时回退到 PointerClick。
        /// </summary>
        private static void BindClick(
            GameObject gameObject,
            Action onClick,
            out Button button,
            out UnityAction buttonHandler,
            out TabClickRelay relay,
            out Action relayHandler) {
            button = null;
            buttonHandler = null;
            relay = null;
            relayHandler = null;

            if (gameObject == null || onClick == null) {
                return;
            }

            button = gameObject.GetComponent<Button>();
            if (button != null) {
                buttonHandler = delegate { onClick.Invoke(); };
                button.onClick.AddListener(buttonHandler);
                return;
            }

            relay = gameObject.GetComponent<TabClickRelay>();
            if (relay == null) {
                relay = gameObject.AddComponent<TabClickRelay>();
            }

            relayHandler = onClick;
            relay.Clicked += relayHandler;
        }

        /// <summary>
        /// 移除通过 BindClick 注册的点击监听。
        /// </summary>
        private static void UnbindClick(Button button, UnityAction buttonHandler, TabClickRelay relay, Action relayHandler) {
            if (button != null && buttonHandler != null) {
                button.onClick.RemoveListener(buttonHandler);
            }

            if (relay != null && relayHandler != null) {
                relay.Clicked -= relayHandler;
            }
        }
    }

    /// <summary>
    /// 单个一级 Tab 需要的节点引用。
    /// </summary>
    [Serializable]
    public sealed class TabItem {
        /// <summary>
        /// 根节点，点击事件和状态节点查找都以它为入口。
        /// </summary>
        public GameObject Root;

        /// <summary>
        /// 可选的 Button 组件。
        /// 若为空，会自动回退到 PointerClick 监听。
        /// </summary>
        public Button Button;

        /// <summary>
        /// 选中态节点，默认自动查找名为 press 的子节点。
        /// </summary>
        public GameObject PressObject;

        /// <summary>
        /// 普通态节点，默认自动查找名为 normal 的子节点。
        /// </summary>
        public GameObject NormalObject;

        /// <summary>
        /// 禁用态节点，默认自动查找名为 none 的子节点。
        /// </summary>
        public GameObject DisabledObject;

        private UnityAction buttonHandler;
        private TabClickRelay clickRelay;
        private Action relayHandler;

        /// <summary>
        /// 绑定点击回调。
        /// </summary>
        public void BindClick(Action onClick) {
            UnbindClick();
            if (Root == null || onClick == null) {
                return;
            }

            ResolveStateNodes();
            Button = Button != null ? Button : Root.GetComponent<Button>();
            if (Button != null) {
                buttonHandler = delegate { onClick.Invoke(); };
                Button.onClick.AddListener(buttonHandler);
                return;
            }

            clickRelay = Root.GetComponent<TabClickRelay>();
            if (clickRelay == null) {
                clickRelay = Root.AddComponent<TabClickRelay>();
            }

            relayHandler = onClick;
            clickRelay.Clicked += relayHandler;
        }

        /// <summary>
        /// 移除当前 Tab 的点击回调。
        /// </summary>
        public void UnbindClick() {
            if (Button != null && buttonHandler != null) {
                Button.onClick.RemoveListener(buttonHandler);
            }

            if (clickRelay != null && relayHandler != null) {
                clickRelay.Clicked -= relayHandler;
            }

            buttonHandler = null;
            relayHandler = null;
        }

        /// <summary>
        /// 根据选中态和禁用态刷新显示节点。
        /// </summary>
        public void SetState(bool isSelected, bool isDisabled) {
            ResolveStateNodes();
            if (PressObject != null) {
                PressObject.SetActive(isSelected && !isDisabled);
            }

            if (NormalObject != null) {
                NormalObject.SetActive(!isSelected && !isDisabled);
            }

            if (DisabledObject != null) {
                DisabledObject.SetActive(isDisabled);
            }
        }

        /// <summary>
        /// 延迟解析默认命名的状态节点，减少外部手工配置。
        /// </summary>
        private void ResolveStateNodes() {
            if (Root == null) {
                return;
            }

            if (PressObject == null) {
                Transform press = Root.transform.Find("press");
                PressObject = press != null ? press.gameObject : null;
            }

            if (NormalObject == null) {
                Transform normal = Root.transform.Find("normal");
                NormalObject = normal != null ? normal.gameObject : null;
            }

            if (DisabledObject == null) {
                Transform none = Root.transform.Find("none");
                DisabledObject = none != null ? none.gameObject : null;
            }
        }
    }

    /// <summary>
    /// 二级下拉列表配置。
    /// </summary>
    public sealed class TabDropdownInfo {
        /// <summary>
        /// 下拉数据集合。
        /// </summary>
        public IReadOnlyList<TabDropdownOption> Options { get; private set; }

        /// <summary>
        /// 展开时隐藏、收起时显示的箭头节点。
        /// </summary>
        public GameObject ArrowObject { get; private set; }

        /// <summary>
        /// 下拉列表组件。
        /// </summary>
        public TabDropdownListBase ScrollList { get; private set; }

        /// <summary>
        /// 最近一次切走前缓存的二级索引。
        /// </summary>
        public int ChangedIndex { get; set; }

        /// <summary>
        /// 构造一个下拉列表配置。
        /// </summary>
        public TabDropdownInfo(IReadOnlyList<TabDropdownOption> options, TabDropdownListBase scrollList, GameObject arrowObject = null) {
            Options = options;
            ScrollList = scrollList;
            ArrowObject = arrowObject;
            ChangedIndex = 1;
        }
    }

    /// <summary>
    /// 单个二级下拉项的数据结构。
    /// </summary>
    public sealed class TabDropdownOption {
        /// <summary>
        /// 二级索引，默认从 1 开始。
        /// </summary>
        public int Index { get; private set; }

        /// <summary>
        /// 展示文案。
        /// </summary>
        public string Label { get; private set; }

        /// <summary>
        /// 业务侧自定义数据。
        /// </summary>
        public object CustomData { get; private set; }

        /// <summary>
        /// 构造一个二级下拉项。
        /// </summary>
        public TabDropdownOption(int index, string label, object customData = null) {
            Index = Mathf.Max(1, index);
            Label = label;
            CustomData = customData;
        }
    }

    /// <summary>
    /// 二级下拉列表渲染所需的上下文。
    /// </summary>
    public sealed class TabDropdownRenderContext {
        /// <summary>
        /// 下拉数据集合。
        /// </summary>
        public IReadOnlyList<TabDropdownOption> Options { get; private set; }

        /// <summary>
        /// 建议滚动到的索引。
        /// </summary>
        public int MoveToIndex { get; private set; }

        /// <summary>
        /// 判断某个索引是否处于选中状态的回调。
        /// </summary>
        public Func<int, bool> IsSelected { get; private set; }

        /// <summary>
        /// 构造一个下拉渲染上下文。
        /// </summary>
        public TabDropdownRenderContext(IReadOnlyList<TabDropdownOption> options, int moveToIndex, Func<int, bool> isSelected) {
            Options = options;
            MoveToIndex = Mathf.Max(1, moveToIndex);
            IsSelected = isSelected;
        }
    }

    /// <summary>
    /// 二级下拉列表的抽象基类。
    /// 业务侧实现自己的列表渲染时，继承该类即可接入 Tab 控制器。
    /// </summary>
    public abstract class TabDropdownListBase : MonoBehaviour {
        /// <summary>
        /// 绑定列表项点击回调，参数为二级索引。
        /// </summary>
        public abstract void SetItemClickCallback(Action<int> onItemClick);

        /// <summary>
        /// 根据上下文刷新下拉列表内容。
        /// </summary>
        public abstract void Render(TabDropdownRenderContext context);

        /// <summary>
        /// 显示下拉列表。
        /// </summary>
        public virtual void ShowSelf() {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// 隐藏下拉列表。
        /// </summary>
        public virtual void HideSelf() {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 为非 Button 节点补充点击能力。
    /// </summary>
    public sealed class TabClickRelay : MonoBehaviour, IPointerClickHandler {
        /// <summary>
        /// 节点被点击时触发。
        /// </summary>
        public event Action Clicked;

        /// <summary>
        /// 将 PointerClick 转发为普通 Action 事件。
        /// </summary>
        public void OnPointerClick(PointerEventData eventData) {
            if (!isActiveAndEnabled || eventData == null) {
                return;
            }

            Clicked?.Invoke();
        }
    }
}
