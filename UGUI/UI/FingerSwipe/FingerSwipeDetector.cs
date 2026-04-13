using System;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace DCFrame.UGUI {
    /// <summary>
    /// 滑动方向。
    /// </summary>
    public enum SwipeDirection {
        Left,
        Right,
        Up,
        Down
    }
    /// <summary>
    /// 纯滑动检测器。
    /// 外部可在任意驱动点手动调用 Update，不依赖 MonoBehaviour 生命周期。
    /// </summary>
    public sealed class SwipeDetector {
        /// <summary>
        /// 左滑事件，仅在有订阅者时派发。
        /// </summary>
        public event Action<SwipeData> OnSwipeLeft;

        /// <summary>
        /// 右滑事件，仅在有订阅者时派发。
        /// </summary>
        public event Action<SwipeData> OnSwipeRight;

        /// <summary>
        /// 上滑事件，仅在有订阅者时派发。
        /// </summary>
        public event Action<SwipeData> OnSwipeUp;

        /// <summary>
        /// 下滑事件，仅在有订阅者时派发。
        /// </summary>
        public event Action<SwipeData> OnSwipeDown;

        /// <summary>
        /// 当前最小滑动距离。
        /// </summary>
        public float MinSwipeDistance => minSwipeDistance;

        /// <summary>
        /// 当前矩形区域。
        /// 仅在未使用自定义区域判定时有效。
        /// </summary>
        public Rect SwipeRegion => useDefaultRegion ? GetDefaultRegion() : swipeRegion;

        /// <summary>
        /// 是否已经完成初始化。
        /// </summary>
        public bool IsInitialized => isInitialized;

        /// <summary>
        /// 使用默认全屏区域初始化滑动检测器。
        /// </summary>
        /// <param name="minDistance">最小滑动距离</param>
        public void Init(float minDistance = DefaultMinDistance) {
            InitInternal(null, minDistance, null, true);
        }

        /// <summary>
        /// 使用指定屏幕矩形区域初始化滑动检测器。
        /// </summary>
        /// <param name="region">允许触发滑动的屏幕区域</param>
        /// <param name="minDistance">最小滑动距离</param>
        public void Init(Rect region, float minDistance = DefaultMinDistance) {
            InitInternal(region, minDistance, null, false);
        }

        /// <summary>
        /// 使用自定义区域判定函数初始化滑动检测器。
        /// 适合外部按 UI、Collider 或其他业务规则判断触摸点是否有效。
        /// </summary>
        /// <param name="regionChecker">自定义区域判定函数，传入屏幕坐标</param>
        /// <param name="minDistance">最小滑动距离</param>
        public void Init(Func<Vector2, bool> regionChecker, float minDistance = DefaultMinDistance) {
            InitInternal(null, minDistance, regionChecker, true);
        }
        
        /// <summary>
        /// 释放增强触摸支持,外部不再使用时可主动调用。
        /// </summary>
        public void Destroy() {
            if (!isInitialized) {
                return;
            }
            EnhancedTouchSupport.Disable();
            isInitialized = false;
        }

        /// <summary>
        /// 每帧手动调用一次，用于检测并派发滑动事件。
        /// 若未主动初始化，则首次调用时按默认参数自动初始化。
        /// </summary>
        /// <returns>本帧是否成功派发过至少一次滑动事件</returns>
        public bool Update() {
            if (!isInitialized) {
                return false;
            }

            if (!HasAnyListener()) {
                return false;
            }

            bool hasDispatched = false;
            var touches = Touch.activeTouches;
            for (int i = 0; i < touches.Count; i++) {
                Touch touch = touches[i];
                if (touch.phase != UnityEngine.InputSystem.TouchPhase.Ended) {
                    continue;
                }

                if (!IsInSwipeRegion(touch.startScreenPosition)) {
                    continue;
                }

                if (TryDispatchSwipe(touch.startScreenPosition, touch.screenPosition)) {
                    hasDispatched = true;
                }
            }

            return hasDispatched;
        }

        /// <summary>
        /// 根据起点和终点尝试派发一次滑动事件。
        /// </summary>
        /// <param name="startPosition">滑动起点屏幕坐标</param>
        /// <param name="endPosition">滑动终点屏幕坐标</param>
        /// <returns>是否成功派发事件</returns>
        public bool TryDispatchSwipe(Vector2 startPosition, Vector2 endPosition) {
            Vector2 delta = endPosition - startPosition;
            float distance = delta.magnitude;
            if (distance < minSwipeDistance) {
                return false;
            }

            SwipeDirection direction = GetSwipeDirection(delta);
            var eventData = new SwipeData(direction, startPosition, endPosition);
            return Dispatch(direction, eventData);
        }

        /// <summary>
        /// 设置自定义区域判定函数。
        /// 适合外部把物体区域换算成屏幕坐标后统一传入。
        /// </summary>
        /// <param name="regionChecker">区域判定函数，传入屏幕坐标</param>
        public void SetRegionChecker(Func<Vector2, bool> regionChecker) {
            customRegionChecker = regionChecker;
        }

        /// <summary>
        /// 使用新的矩形区域重新配置检测范围。
        /// </summary>
        /// <param name="region">新的屏幕矩形区域</param>
        public void SetSwipeRegion(Rect region) {
            swipeRegion = region;
            useDefaultRegion = false;
            customRegionChecker = null;
        }

        /// <summary>
        /// 执行统一初始化逻辑。
        /// </summary>
        /// <param name="region">矩形区域</param>
        /// <param name="minDistance">最小滑动距离</param>
        /// <param name="regionChecker">自定义区域判定函数</param>
        /// <param name="useDefault">是否使用默认全屏区域</param>
        private void InitInternal(Rect? region, float minDistance, Func<Vector2, bool> regionChecker, bool useDefault) {
            EnhancedTouchSupport.Enable();

            minSwipeDistance = Mathf.Max(0f, minDistance);
            customRegionChecker = regionChecker;
            useDefaultRegion = useDefault;
            swipeRegion = region ?? GetDefaultRegion();
            isInitialized = true;
        }

        /// <summary>
        /// 判断坐标是否位于允许滑动的区域内。
        /// </summary>
        /// <param name="screenPosition">屏幕坐标</param>
        private bool IsInSwipeRegion(Vector2 screenPosition) {
            if (customRegionChecker != null) {
                return customRegionChecker.Invoke(screenPosition);
            }

            Rect currentRegion = useDefaultRegion ? GetDefaultRegion() : swipeRegion;
            return currentRegion.Contains(screenPosition);
        }

        /// <summary>
        /// 判断当前是否存在任意滑动事件订阅者。
        /// 没有订阅者时可直接跳过本帧检测。
        /// </summary>
        private bool HasAnyListener() {
            return OnSwipeLeft != null || OnSwipeRight != null || OnSwipeUp != null || OnSwipeDown != null;
        }

        /// <summary>
        /// 根据位移计算滑动方向。
        /// </summary>
        /// <param name="delta">滑动位移</param>
        private static SwipeDirection GetSwipeDirection(Vector2 delta) {
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)) {
                return delta.x >= 0f ? SwipeDirection.Right : SwipeDirection.Left;
            }

            return delta.y >= 0f ? SwipeDirection.Up : SwipeDirection.Down;
        }

        /// <summary>
        /// 按方向派发具体事件。
        /// 仅当对应方向存在订阅者时才会触发。
        /// </summary>
        /// <param name="direction">滑动方向</param>
        /// <param name="eventData">滑动事件数据</param>
        private bool Dispatch(SwipeDirection direction, SwipeData eventData) {
            switch (direction) {
                case SwipeDirection.Left:
                    if (OnSwipeLeft == null) {
                        return false;
                    }

                    OnSwipeLeft.Invoke(eventData);
                    return true;
                case SwipeDirection.Right:
                    if (OnSwipeRight == null) {
                        return false;
                    }

                    OnSwipeRight.Invoke(eventData);
                    return true;
                case SwipeDirection.Up:
                    if (OnSwipeUp == null) {
                        return false;
                    }

                    OnSwipeUp.Invoke(eventData);
                    return true;
                case SwipeDirection.Down:
                    if (OnSwipeDown == null) {
                        return false;
                    }

                    OnSwipeDown.Invoke(eventData);
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 获取默认全屏区域。
        /// </summary>
        private static Rect GetDefaultRegion() {
            return new Rect(0f, 0f, Screen.width, Screen.height);
        }

        private Rect swipeRegion;
        private Func<Vector2, bool> customRegionChecker;
        private const float DefaultMinDistance = 100f;
        private float minSwipeDistance;
        private bool useDefaultRegion = true;
        private bool isInitialized;
    }
}
