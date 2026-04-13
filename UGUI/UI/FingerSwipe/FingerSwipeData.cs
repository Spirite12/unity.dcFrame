using UnityEngine;

namespace DCFrame.UGUI {
    /// <summary>
    /// 滑动事件数据。
    /// </summary>
    public readonly struct SwipeData {
        /// <summary>
        /// 使用起点和终点构造滑动事件数据。
        /// </summary>
        /// <param name="direction">滑动方向</param>
        /// <param name="startPosition">起点屏幕坐标</param>
        /// <param name="endPosition">终点屏幕坐标</param>
        public SwipeData(SwipeDirection direction, Vector2 startPosition, Vector2 endPosition) {
            Direction = direction;
            StartPosition = startPosition;
            EndPosition = endPosition;
            Delta = endPosition - startPosition;
            Distance = Delta.magnitude;
        }

        /// <summary>
        /// 滑动方向。
        /// </summary>
        public SwipeDirection Direction { get; }

        /// <summary>
        /// 滑动起点。
        /// </summary>
        public Vector2 StartPosition { get; }

        /// <summary>
        /// 滑动终点。
        /// </summary>
        public Vector2 EndPosition { get; }

        /// <summary>
        /// 滑动位移。
        /// </summary>
        public Vector2 Delta { get; }

        /// <summary>
        /// 滑动距离。
        /// </summary>
        public float Distance { get; }
    }
}

