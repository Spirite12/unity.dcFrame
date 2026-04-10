using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DCFrame.UGUI {
    [AddComponentMenu("UI/Effects/Gradient")]
    public class Gradient : BaseMeshEffect {
        [SerializeField]
        private Type gradientType;

        [SerializeField]
        private Blend blendMode = Blend.Override;

        [SerializeField]
        [Range(-1, 1)]
        private float offset;

        [SerializeField]
        private UnityEngine.Gradient effectGradient = new() {
            colorKeys = new[] {
                new GradientColorKey(Color.black, 0),
                new GradientColorKey(Color.white, 1),
            },
        };

        /// <summary>
        /// 获取或设置颜色混合模式。
        /// </summary>
        public Blend BlendMode {
            get => blendMode;
            set => blendMode = value;
        }

        /// <summary>
        /// 获取或设置用于采样的颜色渐变。
        /// </summary>
        public UnityEngine.Gradient EffectGradient {
            get => effectGradient;
            set => effectGradient = value;
        }

        /// <summary>
        /// 获取或设置渐变方向。
        /// </summary>
        public Type GradientType {
            get => gradientType;
            set => gradientType = value;
        }

        /// <summary>
        /// 获取或设置渐变偏移量。
        /// </summary>
        public float Offset {
            get => offset;
            set => offset = value;
        }

        /// <summary>
        /// 根据顶点坐标为 UI 网格写入渐变颜色。
        /// </summary>
        public override void ModifyMesh(VertexHelper helper) {
            if (!IsActive() || helper.currentVertCount == 0) {
                return;
            }

            List<UIVertex> vertexList = new();
            helper.GetUIVertexStream(vertexList);
            if (vertexList.Count == 0) {
                return;
            }

            switch (GradientType) {
                case Type.Horizontal:
                    ApplyHorizontalGradient(helper, vertexList);
                    break;
                case Type.Vertical:
                    ApplyVerticalGradient(helper, vertexList);
                    break;
            }
        }

        /// <summary>
        /// 按水平方向为顶点应用渐变。
        /// </summary>
        private void ApplyHorizontalGradient(VertexHelper helper, List<UIVertex> vertexList) {
            float left = vertexList[0].position.x;
            float right = vertexList[0].position.x;

            for (int i = vertexList.Count - 1; i >= 1; --i) {
                float x = vertexList[i].position.x;
                if (x > right) {
                    right = x;
                } else if (x < left) {
                    left = x;
                }
            }

            if (Mathf.Approximately(right, left)) {
                return;
            }

            float width = 1f / (right - left);
            UIVertex vertex = new();
            for (int i = 0; i < helper.currentVertCount; i++) {
                helper.PopulateUIVertex(ref vertex, i);
                vertex.color = BlendColor(vertex.color, EffectGradient.Evaluate((vertex.position.x - left) * width - Offset));
                helper.SetUIVertex(vertex, i);
            }
        }

        /// <summary>
        /// 按垂直方向为顶点应用渐变。
        /// </summary>
        private void ApplyVerticalGradient(VertexHelper helper, List<UIVertex> vertexList) {
            float bottom = vertexList[0].position.y;
            float top = vertexList[0].position.y;

            for (int i = vertexList.Count - 1; i >= 1; --i) {
                float y = vertexList[i].position.y;
                if (y > top) {
                    top = y;
                } else if (y < bottom) {
                    bottom = y;
                }
            }

            if (Mathf.Approximately(top, bottom)) {
                return;
            }

            float height = 1f / (top - bottom);
            UIVertex vertex = new();
            for (int i = 0; i < helper.currentVertCount; i++) {
                helper.PopulateUIVertex(ref vertex, i);
                vertex.color = BlendColor(vertex.color, EffectGradient.Evaluate((top - vertex.position.y) * height - Offset));
                helper.SetUIVertex(vertex, i);
            }
        }

        /// <summary>
        /// 根据设定的混色模式合并原色与渐变色。
        /// </summary>
        private Color BlendColor(Color colorA, Color colorB) {
            return BlendMode switch {
                Blend.Add => colorA + colorB,
                Blend.Multiply => colorA * colorB,
                _ => colorB,
            };
        }

        public enum Type {
            Vertical,
            Horizontal,
        }

        public enum Blend {
            Override,
            Add,
            Multiply,
        }
    }
}
