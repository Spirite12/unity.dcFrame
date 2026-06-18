using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DCFrame.Editor {
    /// <summary>
    /// Editor 窗口内使用的通用表格布局。
    /// </summary>
    public class EditorGUILayoutTable<TRow, TColumn> {
        public EditorGUILayoutTable(float defaultColumnWidth = 40f, float defaultRowHeight = 18f, float rowSpacing = 2f) {
            this.defaultColumnWidth = defaultColumnWidth;
            this.defaultRowHeight = defaultRowHeight;
            this.rowSpacing = rowSpacing;
        }

        /// <summary>
        /// 新增一列表格配置。
        /// </summary>
        public void AddColumn(TColumn id, string title, Action<TRow, TColumn> drawCell, float width = 0f) {
            if (columnDic.ContainsKey(id)) {
                return;
            }

            Column column = new Column(id, title, width, drawCell);
            columnList.Add(column);
            columnDic.Add(id, column);
        }

        /// <summary>
        /// 新增一行表格数据。
        /// </summary>
        public void AddRow(TRow id, float height = 0f) {
            if (rowDic.ContainsKey(id)) {
                return;
            }

            Row row = new Row(id, height);
            rowList.Add(row);
            rowDic.Add(id, row);
        }

        /// <summary>
        /// 设置指定列的期望宽度，最终列宽会取所有来源的最大值。
        /// </summary>
        public void SetColumnWidth(TColumn id, float width) {
            if (!columnDic.TryGetValue(id, out Column column)) {
                return;
            }

            column.Width = width;
        }

        /// <summary>
        /// 设置指定行的期望高度，最终行高会取所有来源的最大值。
        /// </summary>
        public void SetRowHeight(TRow id, float height) {
            if (!rowDic.TryGetValue(id, out Row row)) {
                return;
            }

            row.Height = height;
        }

        /// <summary>
        /// 设置指定单元格的期望宽高，参与该列宽度与该行高度的最大值计算。
        /// </summary>
        public void SetCellSize(TRow rowId, TColumn columnId, float width = 0f, float height = 0f) {
            CellKey key = new CellKey(rowId, columnId);
            if (cellSizeDic.TryGetValue(key, out CellSize cellSize)) {
                cellSize.Width = width;
                cellSize.Height = height;
                return;
            }

            cellSizeDic.Add(key, new CellSize(width, height));
        }

        /// <summary>
        /// 清空所有行数据。
        /// </summary>
        public void ClearRows() {
            rowList.Clear();
            rowDic.Clear();
            cellSizeDic.Clear();
        }

        /// <summary>
        /// 绘制完整表格。
        /// </summary>
        public void Draw() {
            DrawHeader();
            for (int i = 0; i < rowList.Count; i++) {
                DrawRow(rowList[i]);
                if (rowSpacing > 0f && i < rowList.Count - 1) {
                    GUILayout.Space(rowSpacing);
                }
            }
        }

        /// <summary>
        /// 绘制表头。
        /// </summary>
        private void DrawHeader() {
            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < columnList.Count; i++) {
                Column column = columnList[i];
                GUILayout.Label(column.Title, GUILayout.Width(GetColumnWidth(column.Id)));
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 绘制单行内容。
        /// </summary>
        private void DrawRow(Row row) {
            float rowHeight = GetRowHeight(row.Id);
            EditorGUILayout.BeginHorizontal(GUILayout.Height(rowHeight));
            for (int i = 0; i < columnList.Count; i++) {
                Column column = columnList[i];
                EditorGUILayout.BeginVertical(GUILayout.Width(GetColumnWidth(column.Id)), GUILayout.Height(rowHeight));
                column.DrawCell?.Invoke(row.Id, column.Id);
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 获取指定列最终宽度。
        /// </summary>
        private float GetColumnWidth(TColumn columnId) {
            float width = defaultColumnWidth;
            if (columnDic.TryGetValue(columnId, out Column column)) {
                width = Mathf.Max(width, column.Width);
            }

            foreach (var keyValue in cellSizeDic) {
                if (EqualityComparer<TColumn>.Default.Equals(keyValue.Key.ColumnId, columnId)) {
                    width = Mathf.Max(width, keyValue.Value.Width);
                }
            }

            return width;
        }

        /// <summary>
        /// 获取指定行最终高度。
        /// </summary>
        private float GetRowHeight(TRow rowId) {
            float height = defaultRowHeight;
            if (rowDic.TryGetValue(rowId, out Row row)) {
                height = Mathf.Max(height, row.Height);
            }

            foreach (var keyValue in cellSizeDic) {
                if (EqualityComparer<TRow>.Default.Equals(keyValue.Key.RowId, rowId)) {
                    height = Mathf.Max(height, keyValue.Value.Height);
                }
            }

            return height;
        }

        private readonly Dictionary<TColumn, Column> columnDic = new Dictionary<TColumn, Column>();
        private readonly Dictionary<TRow, Row> rowDic = new Dictionary<TRow, Row>();
        private readonly Dictionary<CellKey, CellSize> cellSizeDic = new Dictionary<CellKey, CellSize>();
        private readonly List<Column> columnList = new List<Column>();
        private readonly List<Row> rowList = new List<Row>();
        private readonly float defaultColumnWidth;
        private readonly float defaultRowHeight;
        private readonly float rowSpacing;

        /// <summary>
        /// 表格列配置。
        /// </summary>
        private class Column {
            public Column(TColumn id, string title, float width, Action<TRow, TColumn> drawCell) {
                Id = id;
                Title = title;
                Width = width;
                DrawCell = drawCell;
            }

            public TColumn Id { get; }
            public string Title { get; }
            public float Width { get; set; }
            public Action<TRow, TColumn> DrawCell { get; }
        }

        /// <summary>
        /// 表格行配置。
        /// </summary>
        private class Row {
            public Row(TRow id, float height) {
                Id = id;
                Height = height;
            }

            public TRow Id { get; }
            public float Height { get; set; }
        }

        /// <summary>
        /// 单元格尺寸索引。
        /// </summary>
        private struct CellKey {
            public CellKey(TRow rowId, TColumn columnId) {
                RowId = rowId;
                ColumnId = columnId;
            }

            public TRow RowId { get; }
            public TColumn ColumnId { get; }
        }

        /// <summary>
        /// 单元格期望尺寸。
        /// </summary>
        private class CellSize {
            public CellSize(float width, float height) {
                Width = width;
                Height = height;
            }

            public float Width { get; set; }
            public float Height { get; set; }
        }
    }
}
