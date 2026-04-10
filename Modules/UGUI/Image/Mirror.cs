using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace DCFrame.UGUI {
    /// <summary>
    /// 为 UI 图形追加水平、垂直或四分镜像效果。
    /// </summary>
    [AddComponentMenu("UI/Effects/Mirror", 20)]
    [RequireComponent(typeof(Graphic))]
    public class Mirror : BaseMeshEffect {
        private static readonly Pool<List<UIVertex>> VertexListPool = new(
            () => new List<UIVertex>(),
            actionOnRecycle: list => list.Clear());

        public enum MirrorType {
            /// <summary>
            /// 水平镜像。
            /// </summary>
            Horizontal,

            /// <summary>
            /// 垂直镜像。
            /// </summary>
            Vertical,

            /// <summary>
            /// 先水平再垂直，得到四分镜像。
            /// </summary>
            Quarter,
        }

        /// <summary>
        /// 当前镜像模式。
        /// </summary>
        [SerializeField]
        private MirrorType m_MirrorType = MirrorType.Horizontal;

        [NonSerialized]
        private RectTransform m_RectTransform;

        /// <summary>
        /// 获取或设置镜像模式，并在变化时刷新顶点。
        /// </summary>
        public MirrorType mirrorType {
            get => m_MirrorType;
            set {
                if (m_MirrorType == value) {
                    return;
                }

                m_MirrorType = value;
                if (graphic != null) {
                    graphic.SetVerticesDirty();
                }
            }
        }

        /// <summary>
        /// 获取当前节点的 RectTransform。
        /// </summary>
        public RectTransform rectTransform => m_RectTransform ??= GetComponent<RectTransform>();

        /// <summary>
        /// 按当前镜像模式设置图片原生尺寸。
        /// </summary>
        public void SetNativeSize() {
            if (graphic is not Image image) {
                return;
            }

            Sprite overrideSprite = image.overrideSprite;
            if (overrideSprite == null) {
                return;
            }

            float width = overrideSprite.rect.width / image.pixelsPerUnit;
            float height = overrideSprite.rect.height / image.pixelsPerUnit;
            rectTransform.anchorMax = rectTransform.anchorMin;

            switch (m_MirrorType) {
                case MirrorType.Horizontal:
                    rectTransform.sizeDelta = new Vector2(width * 2, height);
                    break;
                case MirrorType.Vertical:
                    rectTransform.sizeDelta = new Vector2(width, height * 2);
                    break;
                case MirrorType.Quarter:
                    rectTransform.sizeDelta = new Vector2(width * 2, height * 2);
                    break;
            }

            graphic.SetVerticesDirty();
        }

        /// <summary>
        /// 复制并翻转 UI 顶点，生成镜像后的网格。
        /// </summary>
        public override void ModifyMesh(VertexHelper vh) {
            if (!IsActive()) {
                return;
            }

            List<UIVertex> output = VertexListPool.Get();
            try {
                vh.GetUIVertexStream(output);
                int count = output.Count;

                if (graphic is Image image) {
                    switch (image.type) {
                        case Image.Type.Simple:
                            DrawSimple(output, count);
                            break;
                        case Image.Type.Sliced:
                            DrawSliced(output, count);
                            break;
                        case Image.Type.Tiled:
                            DrawTiled(output, count);
                            break;
                    }
                } else {
                    DrawSimple(output, count);
                }

                vh.Clear();
                vh.AddUIVertexTriangleStream(output);
            } finally {
                VertexListPool.Recycle(output);
            }
        }

        /// <summary>
        /// 处理 Simple 类型图片的镜像。
        /// </summary>
        protected void DrawSimple(List<UIVertex> output, int count) {
            Rect rect = graphic.GetPixelAdjustedRect();
            SimpleScale(rect, output, count);

            switch (m_MirrorType) {
                case MirrorType.Horizontal:
                    ExtendCapacity(output, count);
                    MirrorVerts(rect, output, count, true);
                    break;
                case MirrorType.Vertical:
                    ExtendCapacity(output, count);
                    MirrorVerts(rect, output, count, false);
                    break;
                case MirrorType.Quarter:
                    ExtendCapacity(output, count * 3);
                    MirrorVerts(rect, output, count, true);
                    MirrorVerts(rect, output, count * 2, false);
                    break;
            }
        }

        /// <summary>
        /// 处理 Sliced 类型图片的镜像。
        /// </summary>
        protected void DrawSliced(List<UIVertex> output, int count) {
            if (!(graphic as Image).hasBorder) {
                DrawSimple(output, count);
                return;
            }

            Rect rect = graphic.GetPixelAdjustedRect();
            SlicedScale(rect, output, count);
            count = SliceExcludeVerts(output, count);

            switch (m_MirrorType) {
                case MirrorType.Horizontal:
                    ExtendCapacity(output, count);
                    MirrorVerts(rect, output, count, true);
                    break;
                case MirrorType.Vertical:
                    ExtendCapacity(output, count);
                    MirrorVerts(rect, output, count, false);
                    break;
                case MirrorType.Quarter:
                    ExtendCapacity(output, count * 3);
                    MirrorVerts(rect, output, count, true);
                    MirrorVerts(rect, output, count * 2, false);
                    break;
            }
        }

        /// <summary>
        /// 处理 Tiled 类型图片的 UV 翻转。
        /// </summary>
        protected void DrawTiled(List<UIVertex> verts, int count) {
            Sprite overrideSprite = (graphic as Image).overrideSprite;
            if (overrideSprite == null) {
                return;
            }

            Rect rect = graphic.GetPixelAdjustedRect();
            Vector4 inner = DataUtility.GetInnerUV(overrideSprite);
            float width = overrideSprite.rect.width / (graphic as Image).pixelsPerUnit;
            float height = overrideSprite.rect.height / (graphic as Image).pixelsPerUnit;
            int triangleCount = count / 3;

            for (int i = 0; i < triangleCount; i++) {
                UIVertex v1 = verts[i * 3];
                UIVertex v2 = verts[i * 3 + 1];
                UIVertex v3 = verts[i * 3 + 2];

                float centerX = GetCenter(v1.position.x, v2.position.x, v3.position.x);
                float centerY = GetCenter(v1.position.y, v2.position.y, v3.position.y);

                if ((m_MirrorType == MirrorType.Horizontal || m_MirrorType == MirrorType.Quarter)
                    && Mathf.FloorToInt((centerX - rect.xMin) / width) % 2 == 1) {
                    v1.uv0 = GetOverturnUV(v1.uv0, inner.x, inner.z, true);
                    v2.uv0 = GetOverturnUV(v2.uv0, inner.x, inner.z, true);
                    v3.uv0 = GetOverturnUV(v3.uv0, inner.x, inner.z, true);
                }

                if ((m_MirrorType == MirrorType.Vertical || m_MirrorType == MirrorType.Quarter)
                    && Mathf.FloorToInt((centerY - rect.yMin) / height) % 2 == 0) {
                    v1.uv0 = GetOverturnUV(v1.uv0, inner.y, inner.w, false);
                    v2.uv0 = GetOverturnUV(v2.uv0, inner.y, inner.w, false);
                    v3.uv0 = GetOverturnUV(v3.uv0, inner.y, inner.w, false);
                }

                verts[i * 3] = v1;
                verts[i * 3 + 1] = v2;
                verts[i * 3 + 2] = v3;
            }
        }

        /// <summary>
        /// 为列表预留足够的顶点容量。
        /// </summary>
        protected void ExtendCapacity(List<UIVertex> verts, int addCount) {
            int neededCapacity = verts.Count + addCount;
            if (verts.Capacity < neededCapacity) {
                verts.Capacity = neededCapacity;
            }
        }

        /// <summary>
        /// 将 Simple 顶点缩放到镜像前的半边区域。
        /// </summary>
        protected void SimpleScale(Rect rect, List<UIVertex> verts, int count) {
            for (int i = 0; i < count; i++) {
                UIVertex vertex = verts[i];
                Vector3 position = vertex.position;

                if (m_MirrorType == MirrorType.Horizontal || m_MirrorType == MirrorType.Quarter) {
                    position.x = (position.x + rect.x) * 0.5f;
                }

                if (m_MirrorType == MirrorType.Vertical || m_MirrorType == MirrorType.Quarter) {
                    position.y = (position.y + rect.y) * 0.5f + rect.height / 2;
                }

                vertex.position = position;
                verts[i] = vertex;
            }
        }

        /// <summary>
        /// 将 Sliced 顶点缩放到镜像前的半边区域，同时保持边框比例。
        /// </summary>
        protected void SlicedScale(Rect rect, List<UIVertex> verts, int count) {
            Vector4 border = GetAdjustedBorders(rect);
            float halfWidth = rect.width * 0.5f;
            float halfHeight = rect.height * 0.5f;

            for (int i = 0; i < count; i++) {
                UIVertex vertex = verts[i];
                Vector3 position = vertex.position;

                if (m_MirrorType == MirrorType.Horizontal || m_MirrorType == MirrorType.Quarter) {
                    if (halfWidth < border.x && position.x >= rect.center.x) {
                        position.x = rect.center.x;
                    } else if (position.x >= border.x) {
                        position.x = (position.x + rect.x) * 0.5f;
                    }
                }

                if (m_MirrorType == MirrorType.Vertical || m_MirrorType == MirrorType.Quarter) {
                    if (halfHeight < border.y && position.y >= rect.center.y) {
                        position.y = rect.center.y;
                    } else {
                        position.y = (position.y + rect.y) * 0.5f + halfHeight;
                    }
                }

                vertex.position = position;
                verts[i] = vertex;
            }
        }

        /// <summary>
        /// 复制已有顶点并沿指定轴做镜像翻转。
        /// </summary>
        protected void MirrorVerts(Rect rect, List<UIVertex> verts, int count, bool isHorizontal = true) {
            for (int i = 0; i < count; i++) {
                UIVertex vertex = verts[i];
                Vector3 position = vertex.position;

                if (isHorizontal) {
                    position.x = rect.center.x * 2 - position.x;
                } else {
                    position.y = rect.center.y * 2 - position.y;
                }

                vertex.position = position;
                verts.Add(vertex);
            }
        }

        /// <summary>
        /// 移除无法构成三角面的退化顶点。
        /// </summary>
        protected int SliceExcludeVerts(List<UIVertex> verts, int count) {
            int realCount = count;
            int i = 0;
            while (i < realCount) {
                UIVertex v1 = verts[i];
                UIVertex v2 = verts[i + 1];
                UIVertex v3 = verts[i + 2];

                if (v1.position == v2.position || v2.position == v3.position || v3.position == v1.position) {
                    verts[i] = verts[realCount - 3];
                    verts[i + 1] = verts[realCount - 2];
                    verts[i + 2] = verts[realCount - 1];
                    realCount -= 3;
                    continue;
                }

                i += 3;
            }

            if (realCount < count) {
                verts.RemoveRange(realCount, count - realCount);
            }

            return realCount;
        }

        /// <summary>
        /// 根据当前显示区域修正九宫格边框宽度。
        /// </summary>
        protected Vector4 GetAdjustedBorders(Rect rect) {
            Sprite overrideSprite = (graphic as Image).overrideSprite;
            Vector4 border = overrideSprite.border / (graphic as Image).pixelsPerUnit;

            for (int axis = 0; axis <= 1; axis++) {
                float combinedBorders = border[axis] + border[axis + 2];
                if (rect.size[axis] < combinedBorders && combinedBorders != 0) {
                    float borderScaleRatio = rect.size[axis] / combinedBorders;
                    border[axis] *= borderScaleRatio;
                    border[axis + 2] *= borderScaleRatio;
                }
            }

            return border;
        }

        /// <summary>
        /// 计算三个点包围盒的中心坐标。
        /// </summary>
        protected float GetCenter(float p1, float p2, float p3) {
            float max = Mathf.Max(Mathf.Max(p1, p2), p3);
            float min = Mathf.Min(Mathf.Min(p1, p2), p3);
            return (max + min) / 2;
        }

        /// <summary>
        /// 沿指定方向翻转 UV 坐标。
        /// </summary>
        protected Vector2 GetOverturnUV(Vector2 uv, float start, float end, bool isHorizontal = true) {
            if (isHorizontal) {
                uv.x = end - uv.x + start;
            } else {
                uv.y = end - uv.y + start;
            }

            return uv;
        }
    }
}
