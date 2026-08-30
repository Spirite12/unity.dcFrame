using System;
using UnityEngine;
using UnityEngine.UI;

namespace DCFrame {
    [DisallowMultipleComponent]
    public class UIAdaptive : MonoBehaviour {
        public AdaptiveMode mode = AdaptiveMode.Middle;
        public enum AdaptiveMode {
            Middle = 1,
            Top = 2,
            Bottom = 3,
            OutTop = 4,
            OutBottom = 5,
            Camera = 6,
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
            vec2Origin = rect.anchoredPosition;
            topHeight = 0;
            centerHeight = ScreenHeight;
            bottomHeight = 0;
#if UNITY_EDITOR
            topHeight = vecEditorSafeArea.x;
            bottomHeight = vecEditorSafeArea.y;
            centerHeight = Screen.height - topHeight - bottomHeight;
#else
            bool hasSafeArea = Math.Abs(Screen.height - Screen.safeArea.height) > 20;
            if (!hasSafeArea || Screen.height <= 0){
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
                case AdaptiveMode.Middle:
                    rect.offsetMin = new Vector2(0, bottomHeight);
                    rect.offsetMax = new Vector2(0, -topHeight);
                    break;
                
                // 头部适配
                case AdaptiveMode.Top:
                    rect.offsetMin = new Vector2(0, 0);
                    rect.offsetMax = new Vector2(0, -topHeight);
                    break;

                // 底部适配
                case AdaptiveMode.Bottom:
                    rect.offsetMin = new Vector2(0, bottomHeight);
                    rect.offsetMax = new Vector2(0, 0);
                    break;

                // 超出顶部适配
                case AdaptiveMode.OutTop:
                    SetModeOutTop();
                    break;

                // 超出底部适配
                case AdaptiveMode.OutBottom:
                    SetModeOutBottom();
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
                rect.anchoredPosition = new Vector2(vec2Origin.x, vec2Origin.y + topHeight);
                var sizeDelta = rect.sizeDelta;
                sizeDelta = new Vector2(sizeDelta.x, sizeDelta.y + topHeight);
                rect.sizeDelta = sizeDelta;
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
                rect.anchoredPosition = new Vector2(vec2Origin.x, vec2Origin.y - bottomHeight);
                var sizeDelta = rect.sizeDelta;
                sizeDelta = new Vector2(sizeDelta.x, sizeDelta.y + bottomHeight);
                rect.sizeDelta = sizeDelta;
            }
        }

        /// <summary>
        /// 设置镜头模式
        /// </summary>
        private void SetModeCamera() {
            Canvas canvas = GetComponent<Canvas>();
            if (canvas != null && canvas.worldCamera != null) {
                Camera wc = canvas.worldCamera;
                wc.rect = new Rect(0, bottomHeight / ScreenHeight, 1, centerHeight / ScreenHeight);
            }
            CanvasScaler scaler = GetComponent<CanvasScaler>();
            if (scaler != null && centerHeight > 0) {
                Vector2 sr = scaler.referenceResolution;
                scaler.referenceResolution = new Vector2(sr.x, sr.y / centerHeight * ScreenHeight);
            }
        }

        private Vector2 vec2Origin;
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
