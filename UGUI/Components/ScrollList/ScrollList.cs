using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DCFrame.UGUI {
    /// <summary>
    /// ScrollList 运行时控制器，负责虚拟列表的节点复用、布局计算与滚动刷新。
    /// </summary>
    public sealed class ScrollList {
        private const float TweenDurationPerStep = 0.1f;

        /// <summary>
        /// 列表控制器构造入口。
        /// </summary>
        public ScrollList(ScrollListMono owner) {
            this.owner = owner;
        }

        /// <summary>
        /// 当前数据数量。
        /// </summary>
        public int Count => dataList?.Count ?? 0;

        /// <summary>
        /// 设置数据源，并在需要时更新绑定回调。
        /// </summary>
        public void InitData(IList list) {
            dataList = list ?? EmptyList;
            Refresh();
        }

        /// <summary>
        /// 基于当前数据源重新刷新列表。
        /// </summary>
        public void Refresh() {
            if (!EnsureInitialized()) {
                return;
            }

            KillScrollTween();
            UpdateItemSize();
            UpdateContentSize();
            lastVisibleKey = int.MinValue;
            UpdateVisibleItems(true);
        }

        /// <summary>
        /// 清空当前列表。
        /// </summary>
        public void Clear() {
            dataList = EmptyList;
            if (!EnsureInitialized()) {
                return;
            }

            KillScrollTween();
            UpdateContentSize();
            HideUnusedItems(0);
            lastVisibleKey = int.MinValue;
            ResetScrollPosition();
        }

        /// <summary>
        /// 将列表滚动到指定索引。
        /// </summary>
        public void ScrollToIndex(int index, bool useTween = false) {
            if (!EnsureInitialized() || Count <= 0) {
                return;
            }

            index = Mathf.Clamp(index, 0, Count - 1);
            KillScrollTween();

            switch (owner.layoutType) {
                case ScrollListMono.LayoutType.Horizontal:
                    ScrollHorizontal(index, useTween);
                    break;
                case ScrollListMono.LayoutType.Grid:
                    ScrollGrid(index, useTween);
                    break;
                default:
                    ScrollVertical(index, useTween);
                    break;
            }

            if (!useTween) {
                UpdateVisibleItems(true);
            }
        }

        /// <summary>
        /// 释放滚动监听。
        /// </summary>
        public void Dispose() {
            KillScrollTween();

            if (scrollRect && scrollHandler != null && isScrollSubscribed) {
                scrollRect.onValueChanged.RemoveListener(scrollHandler);
            }

            isScrollSubscribed = false;
        }

        /// <summary>
        /// 确保运行依赖完整可用。
        /// </summary>
        private bool EnsureInitialized() {
            if (!owner) {
                return false;
            }

            scrollRect = owner.ScrollRect;
            if (!scrollRect) {
                Debug.LogError("ScrollListMono 缺少 ScrollRect 组件。", owner);
                return false;
            }

            content = scrollRect.content;
            if (!content) {
                Debug.LogError("ScrollRect.content 未设置，无法初始化 ScrollList。", owner);
                return false;
            }

            viewport = scrollRect.viewport ? scrollRect.viewport : scrollRect.transform as RectTransform;
            if (!viewport) {
                Debug.LogError("ScrollRect 缺少可用的 Viewport。", owner);
                return false;
            }

            if (!owner.goItem) {
                Debug.LogError("ScrollListMono.goItem 未设置，无法创建列表项。", owner);
                return false;
            }

            templateRect = owner.goItem.GetComponent<RectTransform>();
            if (!templateRect) {
                Debug.LogError("ScrollListMono.goItem 必须是带 RectTransform 的 UI 节点。", owner.goItem);
                return false;
            }

            if (owner.goItem.transform.parent == content) {
                owner.goItem.SetActive(false);
            }

            EnsureScrollListener();
            return true;
        }

        /// <summary>
        /// 注册滚动监听，在视口滚动时刷新当前可见区。
        /// </summary>
        private void EnsureScrollListener() {
            if (isScrollSubscribed) {
                return;
            }

            scrollHandler ??= OnScrollValueChanged;
            scrollRect.onValueChanged.AddListener(scrollHandler);
            isScrollSubscribed = true;
        }

        /// <summary>
        /// 同步模板尺寸，作为布局计算的基础。
        /// </summary>
        private void UpdateItemSize() {
            itemSize = templateRect.rect.size;
            stepSize = new Vector2(
                Mathf.Max(1f, itemSize.x + owner.spacing.x),
                Mathf.Max(1f, itemSize.y + owner.spacing.y));
        }

        /// <summary>
        /// 按当前布局计算 Content 尺寸。
        /// </summary>
        private void UpdateContentSize() {
            Vector2 size = CalculateContentSize(Count);
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
        }

        /// <summary>
        /// 根据滚动位置刷新当前可见区的节点。
        /// </summary>
        private void UpdateVisibleItems(bool forceRefresh) {
            visibleDataIndices.Clear();
            visiblePositions.Clear();

            if (Count <= 0) {
                HideUnusedItems(0);
                return;
            }

            GridMetrics metrics = CalculateGridMetrics(Count);
            int visibleKey = BuildVisibleEntries(metrics);
            if (!forceRefresh && visibleKey == lastVisibleKey) {
                return;
            }

            lastVisibleKey = visibleKey;
            EnsurePoolSize(visibleDataIndices.Count);

            for (int i = 0; i < visibleDataIndices.Count; i++) {
                ScrollListItemMono item = itemPool[i];
                int dataIndex = visibleDataIndices[i];
                item.gameObject.SetActive(true);
                item.RectTransform.anchoredPosition = visiblePositions[i];

                if (item.Index != dataIndex || forceRefresh) {
                    object itemData = dataList[dataIndex];
                    item.Refresh(dataIndex, itemData);
                }
            }

            HideUnusedItems(visibleDataIndices.Count);
        }

        /// <summary>
        /// 按当前布局构建可见区中的数据索引与坐标。
        /// </summary>
        private int BuildVisibleEntries(GridMetrics metrics) {
            switch (owner.layoutType) {
                case ScrollListMono.LayoutType.Horizontal:
                    return BuildHorizontalEntries();
                case ScrollListMono.LayoutType.Grid:
                    return BuildGridEntries(metrics);
                default:
                    return BuildVerticalEntries();
            }
        }

        /// <summary>
        /// 构建垂直列表可见区。
        /// </summary>
        private int BuildVerticalEntries() {
            int startDisplayIndex = Mathf.Max(0, Mathf.FloorToInt(GetVerticalOffset() / stepSize.y));
            int visibleCount = Mathf.CeilToInt(viewport.rect.height / stepSize.y) + 1 + Mathf.Max(0, owner.extraItemCount);
            int endDisplayIndex = Mathf.Min(Count, startDisplayIndex + visibleCount);

            for (int displayIndex = startDisplayIndex; displayIndex < endDisplayIndex; displayIndex++) {
                int dataIndex = owner.isTopToBottom ? displayIndex : Count - 1 - displayIndex;

                if (dataIndex < 0 || dataIndex >= Count) {
                    continue;
                }

                visibleDataIndices.Add(dataIndex);
                visiblePositions.Add(CalculateVerticalPositionByDisplayIndex(displayIndex));
            }

            return HashVisibleKey(startDisplayIndex, visibleDataIndices.Count);
        }

        /// <summary>
        /// 构建水平列表可见区。
        /// </summary>
        private int BuildHorizontalEntries() {
            int startIndex = Mathf.Max(0, Mathf.FloorToInt(GetHorizontalOffset() / stepSize.x));
            int visibleCount = Mathf.CeilToInt(viewport.rect.width / stepSize.x) + 1 + Mathf.Max(0, owner.extraItemCount);
            int endIndex = Mathf.Min(Count, startIndex + visibleCount);

            for (int index = startIndex; index < endIndex; index++) {
                visibleDataIndices.Add(index);
                visiblePositions.Add(CalculateHorizontalPosition(index));
            }

            return HashVisibleKey(startIndex, visibleDataIndices.Count);
        }

        /// <summary>
        /// 构建网格列表可见区。
        /// </summary>
        private int BuildGridEntries(GridMetrics metrics) {
            int extraCount = Mathf.Max(0, owner.extraItemCount);

            if (owner.axis == GridLayoutGroup.Axis.Horizontal) {
                int startRow = Mathf.Max(0, Mathf.FloorToInt(GetVerticalOffset() / stepSize.y));
                int visibleRows = Mathf.CeilToInt(viewport.rect.height / stepSize.y) + 1;
                int extraRows = Mathf.CeilToInt((float)extraCount / metrics.columns);
                int endRow = Mathf.Min(metrics.rows, startRow + visibleRows + extraRows);

                for (int row = startRow; row < endRow; row++) {
                    for (int column = 0; column < metrics.columns; column++) {
                        int dataIndex = GetGridDataIndex(row, column, metrics);
                        if (dataIndex < 0 || dataIndex >= Count) {
                            continue;
                        }

                        visibleDataIndices.Add(dataIndex);
                        visiblePositions.Add(CalculateGridPosition(row, column));
                    }
                }

                return HashVisibleKey(startRow, visibleDataIndices.Count);
            }

            int startColumn = Mathf.Max(0, Mathf.FloorToInt(GetHorizontalOffset() / stepSize.x));
            int visibleColumns = Mathf.CeilToInt(viewport.rect.width / stepSize.x) + 1;
            int extraColumns = Mathf.CeilToInt((float)extraCount / metrics.rows);
            int endColumn = Mathf.Min(metrics.columns, startColumn + visibleColumns + extraColumns);

            for (int column = startColumn; column < endColumn; column++) {
                for (int row = 0; row < metrics.rows; row++) {
                    int dataIndex = GetGridDataIndex(row, column, metrics);
                    if (dataIndex < 0 || dataIndex >= Count) {
                        continue;
                    }

                    visibleDataIndices.Add(dataIndex);
                    visiblePositions.Add(CalculateGridPosition(row, column));
                }
            }

            return HashVisibleKey(startColumn, visibleDataIndices.Count);
        }

        /// <summary>
        /// 获取网格中某个显示单元格对应的数据索引。
        /// </summary>
        private int GetGridDataIndex(int displayRow, int column, GridMetrics metrics) {
            int logicalRow = owner.isTopToBottom ? displayRow : metrics.rows - 1 - displayRow;

            if (logicalRow < 0 || logicalRow >= metrics.rows) {
                return -1;
            }

            return owner.axis == GridLayoutGroup.Axis.Horizontal
                ? logicalRow * metrics.columns + column
                : column * metrics.rows + logicalRow;
        }

        /// <summary>
        /// 确保对象池数量满足当前可见区需求。
        /// </summary>
        private void EnsurePoolSize(int requiredCount) {
            while (itemPool.Count < requiredCount) {
                ScrollListItemMono item = CreateItem(itemPool.Count);
                if (item == null) {
                    break;
                }

                itemPool.Add(item);
            }
        }

        /// <summary>
        /// 创建单个可复用的列表项。
        /// </summary>
        private ScrollListItemMono CreateItem(int poolIndex) {
            GameObject itemObject = UnityEngine.Object.Instantiate(owner.goItem, content);
            itemObject.name = owner.goItem.name + "_" + poolIndex;
            itemObject.SetActive(false);

            RectTransform itemRect = itemObject.GetComponent<RectTransform>();
            if (itemRect == null) {
                Debug.LogError("列表项预制件缺少 RectTransform。", itemObject);
                UnityEngine.Object.Destroy(itemObject);
                return null;
            }

            ScrollListItemMono item = itemObject.GetComponent<ScrollListItemMono>();
            if (item == null) {
                item = itemObject.AddComponent<ScrollListItemMono>();
            }

            return item;
        }

        /// <summary>
        /// 隐藏未参与当前可见区刷新的节点。
        /// </summary>
        private void HideUnusedItems(int usedCount) {
            for (int i = usedCount; i < itemPool.Count; i++) {
                if (!itemPool[i].gameObject.activeSelf) {
                    continue;
                }

                itemPool[i].Recycle();
                itemPool[i].gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 计算垂直列表项位置。
        /// </summary>
        private Vector2 CalculateVerticalPositionByDisplayIndex(int displayIndex) {
            float x = owner.padding.left;
            float y = -owner.padding.top - displayIndex * stepSize.y;
            return new Vector2(x, y);
        }

        /// <summary>
        /// 计算水平列表项位置。
        /// </summary>
        private Vector2 CalculateHorizontalPosition(int index) {
            float x = owner.padding.left + index * stepSize.x;
            float y = -owner.padding.top;
            return new Vector2(x, y);
        }

        /// <summary>
        /// 计算网格布局下的列表项位置。
        /// </summary>
        private Vector2 CalculateGridPosition(int row, int column) {
            float x = owner.padding.left + column * stepSize.x;
            float y = -owner.padding.top - row * stepSize.y;
            return new Vector2(x, y);
        }

        /// <summary>
        /// 计算当前数据量对应的 Content 尺寸。
        /// </summary>
        private Vector2 CalculateContentSize(int totalCount) {
            float width = owner.padding.horizontal;
            float height = owner.padding.vertical;

            switch (owner.layoutType) {
                case ScrollListMono.LayoutType.Horizontal:
                    width += totalCount * itemSize.x + Mathf.Max(0, totalCount - 1) * owner.spacing.x;
                    height += itemSize.y;
                    break;
                case ScrollListMono.LayoutType.Grid:
                    GridMetrics metrics = CalculateGridMetrics(totalCount);
                    width += metrics.columns * itemSize.x + Mathf.Max(0, metrics.columns - 1) * owner.spacing.x;
                    height += metrics.rows * itemSize.y + Mathf.Max(0, metrics.rows - 1) * owner.spacing.y;
                    break;
                default:
                    width += itemSize.x;
                    height += totalCount * itemSize.y + Mathf.Max(0, totalCount - 1) * owner.spacing.y;
                    break;
            }

            width = Mathf.Max(width, viewport.rect.width);
            height = Mathf.Max(height, viewport.rect.height);
            return new Vector2(width, height);
        }

        /// <summary>
        /// 计算网格行列信息，供布局与滚动定位复用。
        /// </summary>
        private GridMetrics CalculateGridMetrics(int totalCount) {
            GridMetrics metrics = new GridMetrics();
            int safeCount = Mathf.Max(totalCount, 1);
            int safeConstraintCount = Mathf.Max(1, owner.constraintCount);

            switch (owner.constraint) {
                case GridLayoutGroup.Constraint.FixedColumnCount:
                    metrics.columns = safeConstraintCount;
                    metrics.rows = Mathf.CeilToInt((float)safeCount / metrics.columns);
                    break;
                case GridLayoutGroup.Constraint.FixedRowCount:
                    metrics.rows = safeConstraintCount;
                    metrics.columns = Mathf.CeilToInt((float)safeCount / metrics.rows);
                    break;
                default:
                    if (owner.axis == GridLayoutGroup.Axis.Horizontal) {
                        float availableWidth = Mathf.Max(0f, viewport.rect.width - owner.padding.horizontal + owner.spacing.x);
                        metrics.columns = Mathf.Max(1, Mathf.FloorToInt(availableWidth / stepSize.x));
                        metrics.rows = Mathf.CeilToInt((float)safeCount / metrics.columns);
                    } else {
                        float availableHeight = Mathf.Max(0f, viewport.rect.height - owner.padding.vertical + owner.spacing.y);
                        metrics.rows = Mathf.Max(1, Mathf.FloorToInt(availableHeight / stepSize.y));
                        metrics.columns = Mathf.CeilToInt((float)safeCount / metrics.rows);
                    }
                    break;
            }

            metrics.columns = Mathf.Max(1, metrics.columns);
            metrics.rows = Mathf.Max(1, metrics.rows);
            return metrics;
        }

        /// <summary>
        /// 滚动到垂直列表中的指定位置。
        /// </summary>
        private void ScrollVertical(int index, bool useTween) {
            int displayIndex = owner.isTopToBottom ? index : Count - 1 - index;
            float normalized = CalculateVerticalNormalizedPosition(displayIndex);
            SetVerticalNormalizedPosition(normalized, useTween);
        }

        /// <summary>
        /// 滚动到水平列表中的指定位置。
        /// </summary>
        private void ScrollHorizontal(int index, bool useTween) {
            float normalized = CalculateHorizontalNormalizedPosition(index);
            SetHorizontalNormalizedPosition(normalized, useTween);
        }

        /// <summary>
        /// 滚动到网格中的指定位置。
        /// </summary>
        private void ScrollGrid(int index, bool useTween) {
            GridMetrics metrics = CalculateGridMetrics(Count);
            float normalized;
            if (owner.axis == GridLayoutGroup.Axis.Horizontal) {
                int logicalRow = index / metrics.columns;
                int displayRow = owner.isTopToBottom ? logicalRow : metrics.rows - 1 - logicalRow;
                normalized = CalculateVerticalNormalizedPosition(displayRow);
                SetVerticalNormalizedPosition(normalized, useTween);
                return;
            }

            int column = index / metrics.rows;
            normalized = CalculateHorizontalNormalizedPosition(column);
            SetHorizontalNormalizedPosition(normalized, useTween);
        }

        /// <summary>
        /// 获取当前垂直滚动偏移。
        /// </summary>
        private float GetVerticalOffset() {
            float maxOffset = GetMaxVerticalScrollDistance();
            if (maxOffset <= 0f) {
                return 0f;
            }

            return (1f - scrollRect.verticalNormalizedPosition) * maxOffset;
        }

        /// <summary>
        /// 获取当前水平滚动偏移。
        /// </summary>
        private float GetHorizontalOffset() {
            float maxOffset = GetMaxHorizontalScrollDistance();
            if (maxOffset <= 0f) {
                return 0f;
            }

            return scrollRect.horizontalNormalizedPosition * maxOffset;
        }

        /// <summary>
        /// 获取垂直方向的最大滚动距离。
        /// </summary>
        private float GetMaxVerticalScrollDistance() {
            return Mathf.Max(0f, content.rect.height - viewport.rect.height);
        }

        /// <summary>
        /// 获取水平方向的最大滚动距离，避免依赖 Content 的水平对齐方式。
        /// </summary>
        private float GetMaxHorizontalScrollDistance() {
            return Mathf.Max(0f, content.rect.width - viewport.rect.width);
        }

        /// <summary>
        /// 计算垂直方向的目标归一化滚动值。
        /// </summary>
        private float CalculateVerticalNormalizedPosition(int displayIndex) {
            float contentHeight = GetMaxVerticalScrollDistance();
            if (contentHeight <= 0f) {
                return 1f;
            }

            float targetY = displayIndex * stepSize.y;
            return 1f - Mathf.Clamp01(targetY / contentHeight);
        }

        /// <summary>
        /// 计算水平方向的目标归一化滚动值。
        /// </summary>
        private float CalculateHorizontalNormalizedPosition(int displayIndex) {
            float contentWidth = GetMaxHorizontalScrollDistance();
            if (contentWidth <= 0f) {
                return 0f;
            }

            float targetX = displayIndex * stepSize.x;
            return Mathf.Clamp01(targetX / contentWidth);
        }

        /// <summary>
        /// 设置垂直滚动位置，必要时使用 DoTween 平滑滚动。
        /// </summary>
        private void SetVerticalNormalizedPosition(float normalized, bool useTween) {
            if (!useTween) {
                scrollRect.verticalNormalizedPosition = normalized;
                return;
            }

            float duration = CalculateScrollTweenDuration(
                Mathf.Abs(scrollRect.verticalNormalizedPosition - normalized) * GetMaxVerticalScrollDistance());
            if (duration <= 0f) {
                scrollRect.verticalNormalizedPosition = normalized;
                UpdateVisibleItems(true);
                return;
            }

            scrollTween = DOTween
                .To(() => scrollRect.verticalNormalizedPosition, value => scrollRect.verticalNormalizedPosition = value, normalized, duration)
                .SetTarget(scrollRect)
                .SetEase(Ease.OutCubic)
                .OnUpdate(() => UpdateVisibleItems(false))
                .OnComplete(() => {
                    scrollTween = null;
                    UpdateVisibleItems(true);
                });
        }

        /// <summary>
        /// 设置水平滚动位置，必要时使用 DoTween 平滑滚动。
        /// </summary>
        private void SetHorizontalNormalizedPosition(float normalized, bool useTween) {
            if (!useTween) {
                scrollRect.horizontalNormalizedPosition = normalized;
                return;
            }

            float duration = CalculateScrollTweenDuration(
                Mathf.Abs(scrollRect.horizontalNormalizedPosition - normalized) * GetMaxHorizontalScrollDistance());
            if (duration <= 0f) {
                scrollRect.horizontalNormalizedPosition = normalized;
                UpdateVisibleItems(true);
                return;
            }

            scrollTween = DOTween
                .To(() => scrollRect.horizontalNormalizedPosition, value => scrollRect.horizontalNormalizedPosition = value, normalized, duration)
                .SetTarget(scrollRect)
                .SetEase(Ease.OutCubic)
                .OnUpdate(() => UpdateVisibleItems(false))
                .OnComplete(() => {
                    scrollTween = null;
                    UpdateVisibleItems(true);
                });
        }

        /// <summary>
        /// 按滑动距离计算滚动 Tween 时长，每个距离步长固定耗时 0.1 秒。
        /// </summary>
        private float CalculateScrollTweenDuration(float distance) {
            if (distance <= 0f) {
                return 0f;
            }

            float distanceStep = Mathf.Max(1f, owner.tweenDistanceStep);
            return distance / distanceStep * TweenDurationPerStep;
        }

        /// <summary>
        /// 终止当前滚动 Tween，避免连续定位时状态冲突。
        /// </summary>
        private void KillScrollTween() {
            if (scrollTween == null || !scrollTween.IsActive()) {
                scrollTween = null;
                return;
            }

            scrollTween.Kill();
            scrollTween = null;
        }

        /// <summary>
        /// 响应 ScrollRect 滚动事件。
        /// </summary>
        private void OnScrollValueChanged(Vector2 _) {
            UpdateVisibleItems(false);
        }

        /// <summary>
        /// 生成当前可见区的缓存键，便于快速判断是否需要重绑。
        /// </summary>
        private static int HashVisibleKey(int startIndex, int visibleCount) {
            unchecked {
                return startIndex * 397 ^ visibleCount;
            }
        }

        /// <summary>
        /// 重置滚动条位置，避免清空后保留旧偏移。
        /// </summary>
        private void ResetScrollPosition() {
            scrollRect.horizontalNormalizedPosition = 0f;
            scrollRect.verticalNormalizedPosition = 1f;
        }

        private static readonly IList EmptyList = Array.Empty<object>();
        private readonly ScrollListMono owner;
        private readonly List<ScrollListItemMono> itemPool = new();
        private readonly List<int> visibleDataIndices = new();
        private readonly List<Vector2> visiblePositions = new();

        private IList dataList = EmptyList;
        private ScrollRect scrollRect;
        private RectTransform content;
        private RectTransform viewport;
        private RectTransform templateRect;
        private Vector2 itemSize;
        private Vector2 stepSize;
        private UnityAction<Vector2> scrollHandler;
        private Tween scrollTween;
        private bool isScrollSubscribed;
        private int lastVisibleKey = int.MinValue;

        /// <summary>
        /// 网格布局的行列信息。
        /// </summary>
        private struct GridMetrics {
            public int columns;
            public int rows;
        }
    }
}
