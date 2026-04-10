using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DCFrame.UGUI {
    /// <summary>
    /// 让文本或图片网格沿曲线排布显示。
    /// </summary>
    [AddComponentMenu("UI/Effects/Extensions/Text Curved")]
    public class TextCurved : BaseMeshEffect {
        /// <summary>
        /// 控制弯曲形态的曲线。
        /// </summary>
        public AnimationCurve curveForText = AnimationCurve.Linear(0, 0, 1, 10);

        /// <summary>
        /// 控制曲线位移强度。
        /// </summary>
        public float curveMultiplier = 1f;

        /// <summary>
        /// 控制每个字符最终附加旋转角度。
        /// </summary>
        public float eulerAngle;

        private RectTransform rectTrans;
        private List<Character> characters;

        private class Character {
            public float width;
            public float height;
            public Vector2 pos0;
            public Vector2 pos1;
            public Vector2 pos2;
            public Vector2 pos3;
            public Vector2 pos4;
            public Vector2 pos5;
        }

#if UNITY_EDITOR
        /// <summary>
        /// 在编辑器参数变化时同步曲线边界与当前宽度。
        /// </summary>
        protected override void OnValidate() {
            base.OnValidate();
            if (curveForText.length == 0) {
                return;
            }

            if (curveForText[0].time != 0) {
                Keyframe firstKey = curveForText[0];
                firstKey.time = 0;
                curveForText.MoveKey(0, firstKey);
            }

            if (rectTrans == null) {
                rectTrans = GetComponent<RectTransform>();
            }

            if (rectTrans != null && curveForText[curveForText.length - 1].time != rectTrans.rect.width) {
                OnRectTransformDimensionsChange();
            }
        }
#endif

        /// <summary>
        /// 初始化尺寸缓存并同步曲线尾点。
        /// </summary>
        protected override void Awake() {
            base.Awake();
            rectTrans = GetComponent<RectTransform>();
            OnRectTransformDimensionsChange();
        }

        /// <summary>
        /// 组件启用时重新同步曲线尾点。
        /// </summary>
        protected override void OnEnable() {
            base.OnEnable();
            rectTrans = GetComponent<RectTransform>();
            OnRectTransformDimensionsChange();
        }

        /// <summary>
        /// 计算点绕指定中心旋转后的 X 坐标。
        /// </summary>
        public float rotateX(float rx0, float ry0, float x, float y, float a) {
            return (x - rx0) * Mathf.Cos(a) - (y - ry0) * Mathf.Sin(a) + rx0;
        }

        /// <summary>
        /// 计算点绕指定中心旋转后的 Y 坐标。
        /// </summary>
        public float rotateY(float rx0, float ry0, float x, float y, float a) {
            return (x - rx0) * Mathf.Sin(a) + (y - ry0) * Mathf.Cos(a) + ry0;
        }

        /// <summary>
        /// 计算点绕指定中心旋转后的二维坐标。
        /// </summary>
        public Vector2 rotateXY(float rx0, float ry0, float x, float y, float a) {
            float x0 = (x - rx0) * Mathf.Cos(a) - (y - ry0) * Mathf.Sin(a) + rx0;
            float y0 = (x - rx0) * Mathf.Sin(a) + (y - ry0) * Mathf.Cos(a) + ry0;
            return new Vector2(x0, y0);
        }

        /// <summary>
        /// 按字符重新排布顶点，使网格沿曲线弯折。
        /// </summary>
        public override void ModifyMesh(VertexHelper vh) {
            if (!IsActive()) {
                return;
            }

            List<UIVertex> verts = new();
            vh.GetUIVertexStream(verts);
            if (verts.Count == 0) {
                return;
            }

            Vector2 pos2 = Vector2.zero;
            Vector2 pos3 = Vector2.zero;
            Vector2 pos4 = Vector2.zero;
            characters = new List<Character>();

            for (int index = 0; index < verts.Count; index++) {
                if (index % 6 != 5) {
                    continue;
                }

                Character character = new() {
                    pos0 = verts[index - 5].position,
                    pos1 = verts[index - 4].position,
                    pos2 = verts[index - 3].position,
                    pos3 = verts[index - 2].position,
                    pos4 = verts[index - 1].position,
                    pos5 = verts[index].position,
                };
                character.width = Mathf.Abs(character.pos3.x - character.pos4.x);
                character.height = Mathf.Abs(character.pos1.y - character.pos2.y);
                characters.Add(character);
            }

            for (int index = 0; index < verts.Count; index++) {
                UIVertex uiVertex = verts[index];
                int indexMod = index % 6;

                if (indexMod >= 2 && indexMod <= 4) {
                    uiVertex.position.y += curveForText.Evaluate(rectTrans.rect.width * rectTrans.pivot.x + uiVertex.position.x) * curveMultiplier;
                    if (indexMod == 2) {
                        pos2 = uiVertex.position;
                    }
                    if (indexMod == 3) {
                        pos3 = uiVertex.position;
                    }
                    if (indexMod == 4) {
                        pos4 = uiVertex.position;
                    }
                }

                verts[index] = uiVertex;

                if (indexMod != 5) {
                    continue;
                }

                int charNum = index / 6;
                Vector2 anchor = (pos3 + pos4) / 2f;
                Vector2 rightDirection = (pos3 - pos4).normalized * (characters[charNum].width / 2f);
                pos3 = anchor + rightDirection;
                pos4 = anchor - rightDirection;
                pos2 = pos3;

                Vector2 origin = pos3 - pos4;
                float length = characters[charNum].height;
                Vector2 vertical = new(1f, -origin.x / (origin.y == 0 ? 1 : origin.y));
                vertical = vertical.normalized * length;
                if (vertical.y < 0) {
                    vertical = new Vector2(-vertical.x, -vertical.y);
                }

                float radianAngle = eulerAngle / 180f * Mathf.PI;

                UIVertex uiVertex0 = verts[index - 5];
                uiVertex0.position = pos4 + vertical;
                if (eulerAngle != 0) {
                    uiVertex0.position = rotateXY(anchor.x, anchor.y, uiVertex0.position.x, uiVertex0.position.y, radianAngle);
                }
                verts[index - 5] = uiVertex0;

                UIVertex uiVertex1 = verts[index - 4];
                uiVertex1.position = pos3 + vertical;
                if (eulerAngle != 0) {
                    uiVertex1.position = rotateXY(anchor.x, anchor.y, uiVertex1.position.x, uiVertex1.position.y, radianAngle);
                }
                verts[index - 4] = uiVertex1;

                UIVertex uiVertex5 = verts[index];
                uiVertex5.position = pos4 + vertical;
                if (eulerAngle != 0) {
                    uiVertex5.position = rotateXY(anchor.x, anchor.y, uiVertex5.position.x, uiVertex5.position.y, radianAngle);
                }
                verts[index] = uiVertex5;

                UIVertex uiVertex2 = verts[index - 3];
                uiVertex2.position = pos2;
                if (eulerAngle != 0) {
                    uiVertex2.position = rotateXY(anchor.x, anchor.y, uiVertex2.position.x, uiVertex2.position.y, radianAngle);
                }
                verts[index - 3] = uiVertex2;

                UIVertex uiVertex3 = verts[index - 2];
                uiVertex3.position = pos3;
                if (eulerAngle != 0) {
                    uiVertex3.position = rotateXY(anchor.x, anchor.y, uiVertex3.position.x, uiVertex3.position.y, radianAngle);
                }
                verts[index - 2] = uiVertex3;

                UIVertex uiVertex4 = verts[index - 1];
                uiVertex4.position = pos4;
                if (eulerAngle != 0) {
                    uiVertex4.position = rotateXY(anchor.x, anchor.y, uiVertex4.position.x, uiVertex4.position.y, radianAngle);
                }
                verts[index - 1] = uiVertex4;
            }

            vh.Clear();
            vh.AddUIVertexTriangleStream(verts);
        }

        /// <summary>
        /// 尺寸变化时同步曲线最后一个关键点到当前宽度。
        /// </summary>
        protected override void OnRectTransformDimensionsChange() {
            if (curveForText.length == 0 || rectTrans == null) {
                return;
            }

            Keyframe lastKey = curveForText[curveForText.length - 1];
            lastKey.time = rectTrans.rect.width;
            curveForText.MoveKey(curveForText.length - 1, lastKey);
        }
    }
}
