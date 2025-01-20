using System;
using UnityEngine;
using UnityEngine.UI;

namespace DCFrame {
    [DisallowMultipleComponent]
    public class UIAdaptive : MonoBehaviour {
        public AdaptiveMode mode = AdaptiveMode.MiddleOffset;
        public enum AdaptiveMode {
            MiddleOffset = 1,
            TopSizeDelta = 2,
            BottomSizeDelta = 3,
            OutTopRect = 4,
            OutBottomRect = 5,
            NoTopOffset = 6,
            NoBottomOffset = 7,
            Camera = 8,
        }

        public void OnClickTakeEffect() {
            if (!Application.isPlaying) {
                InitSafeAreaData();
            }
            SetSafeAreaPos();
        }

        void Awake() {
            InitSafeAreaData();
            SetSafeAreaPos();
        }

        /// <summary>
        /// 实例化初始顶部，中间，底部高度
        /// </summary>
        private void InitSafeAreaData() {
            rect = GetComponent<RectTransform>();
#if UNITY_EDITOR
            topHeight = vecEditorSafeArea.x;
            bottomHeight = vecEditorSafeArea.y;
            centerHeight = Screen.height - topHeight - bottomHeight;
#else
            bool hasSafeAres = Math.Abs(Screen.height - Screen.safeArea.height) > 20;
            if (!hasSafeAres){
                return;
            }
            topHeight = (Screen.height - Screen.safeArea.height - Screen.safeArea.y) / Screen.height * ScreenHeight;
            centerHeight = Screen.safeArea.height / Screen.height * ScreenHeight;
            bottomHeight = ScreenHeight - topHeight - centerHeight;
#endif
        }

        private void SetSafeAreaPos() {
            switch (mode) {
                // 中间适配
                case AdaptiveMode.MiddleOffset:
                    rect.offsetMin = new Vector2(0, bottomHeight);
                    rect.offsetMax = new Vector2(0, -topHeight);
                    break;

                // 顶部适配
                case AdaptiveMode.TopSizeDelta:
                    rect.sizeDelta = new Vector2(rect.sizeDelta.x, topHeight);
                    break;

                // 底部适配
                case AdaptiveMode.BottomSizeDelta:
                    rect.sizeDelta = new Vector2(rect.sizeDelta.x, bottomHeight);
                    break;

                // 超出顶部高度适配
                case AdaptiveMode.OutTopRect:
                    SetModeOutTop();
                    break;

                // 超出底部高度适配
                case AdaptiveMode.OutBottomRect:
                    SetModeOutBottom();
                    break;

                // 只有头部缺少适配
                case AdaptiveMode.NoTopOffset:
                    rect.offsetMin = new Vector2(0, 0);
                    rect.offsetMax = new Vector2(0, -topHeight);
                    break;

                // 只有底部缺少适配
                case AdaptiveMode.NoBottomOffset:
                    rect.offsetMin = new Vector2(0, bottomHeight);
                    rect.offsetMax = new Vector2(0, 0);
                    break;

                // 修改镜头模式
                case AdaptiveMode.Camera:
                    SetModeCamera();
                    break;
            }
        }

        /// <summary>
        /// 设置顶部超出适配
        /// </summary>
        private void SetModeOutTop() {
            bool isFullStretch = !(Math.Abs(rect.anchorMin.y - rect.anchorMax.y) <= 0);
            if (isFullStretch) {
                rect.offsetMin = new Vector2(0, 0);
                rect.offsetMax = new Vector2(0, topHeight);
            } else {
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, rect.anchoredPosition.y + topHeight);
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, rect.sizeDelta.y + topHeight);
            }
        }

        /// <summary>
        /// 设置底部超出适配
        /// </summary>
        private void SetModeOutBottom() {
            bool isFullStretch = !(Math.Abs(rect.anchorMin.y - rect.anchorMax.y) <= 0);
            if (isFullStretch) {
                rect.offsetMin = new Vector2(0, -bottomHeight);
                rect.offsetMax = new Vector2(0, 0);
            } else {
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, rect.anchoredPosition.y + -topHeight);
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, rect.sizeDelta.y + topHeight);
            }
        }

        /// <summary>
        /// 设置镜头模式
        /// </summary>
        private void SetModeCamera() {
            Canvas canvas = GetComponent<Canvas>();
            CanvasScaler scaler = GetComponent<CanvasScaler>();
            if (canvas != null) {
                Camera wc = canvas.worldCamera;
                wc.rect = new Rect(0, bottomHeight / ScreenHeight, 1, centerHeight / ScreenHeight);
            }

            if (scaler != null) {
                Vector2 sr = scaler.referenceResolution;
                scaler.referenceResolution = new Vector2(sr.x, sr.y / centerHeight * ScreenHeight);
            }
        }

        private RectTransform rect;
        private float topHeight = 0;
        private float centerHeight = 0;
        private float bottomHeight = 0;
        private const float ScreenHeight = 1334;
        /// <summary>
        /// 以 Iphone 做的异形屏顶部与底部的安全区域值
        /// </summary>
        private readonly Vector2 vecEditorSafeArea = new Vector2(68, 48);
    }
}
