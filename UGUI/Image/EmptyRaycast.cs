using UnityEngine.UI;

namespace DCFrame.UGUI {
    /// <summary>
    /// 提供一个不可见但可参与射线检测的占位 Graphic。
    /// </summary>
    public class EmptyRaycast : MaskableGraphic {
        /// <summary>
        /// 初始化时关闭旧版网格生成流程。
        /// </summary>
        protected EmptyRaycast() {
            useLegacyMeshGeneration = false;
        }

        /// <summary>
        /// 不生成任何可见顶点，仅保留射线拦截能力。
        /// </summary>
        protected override void OnPopulateMesh(VertexHelper toFill) {
            toFill.Clear();
        }
    }
}
