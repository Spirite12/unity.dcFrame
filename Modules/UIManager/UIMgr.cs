using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DCFrame {
	public class UIMgr : Singleton<UIMgr> {
		/// <summary>
		/// UI根结点
		/// </summary>
		public Transform uiRoot { get; private set; }
		/// <summary>
		/// 不激活根结点
		/// </summary>
		public Transform deActiveRoot { get; private set; }
		/// <summary>
		/// UI相机
		/// </summary>
		public Camera uiCamera { get; private set; }

		/// <summary>
		/// 初始化UIMgr。
		/// </summary>
		public void Init(Transform uiRoot, Transform deActiveRoot, Camera uiCamera) {
			this.uiRoot = uiRoot;
			this.deActiveRoot = deActiveRoot;
            this.uiCamera = uiCamera;
		}

		/// <summary>
		/// 关闭UIMgr，释放所有界面。
		/// </summary>
		public void Shut() {
			CloseAllUI();
		}

        /// <summary>
        /// Escape关闭窗口
        /// </summary>
        public void EscapeClose() {
            for (int i = uiStack.Count - 1; i > 0; i--) {
                if (uiStack[i].IsOpen()) {
                    uiStack[i].Close();
                    break;
                }
            }
        }

		public bool Open(UIBase ui) {
			if (uiStack.IndexOf(ui) >= 0) {
				return false;
			}

			//查找该UI插入的位置
			var inserted = false;
			for (var i = uiStack.Count - 1;i >= 0;--i) {
                if (uiStack[i].GetLayerIdx() > ui.GetLayerIdx()) {
					continue;
				}
				uiStack.Insert(i + 1, ui);
				inserted = true;
				break;
			}
            if (!inserted) {
				uiStack.Insert(0, ui);
			}

			if (ui.GetUIBaseType() == UIBase.UIBaseType.FullScene) {
				CloseBelowUI(ui);
			}
			return true;
		}

		public async UniTask Close(UIBase ui) {
			if (ui.GetUIBaseType() == UIBase.UIBaseType.FullScene) {
				await OpenBelowUI(ui);
			}
			int idx = uiStack.IndexOf(ui);
			uiStack.Remove(ui);
			if (idx < uiStack.Count) {
				SortAboveUI(uiStack[idx]);
			}
		}

		/// <summary>
		/// 获取最新打开界面的层级
		/// 如果已经在UI栈里则返回当前的值，不能嵌套重复打开
		/// </summary>
		/// <returns></returns>
		public int GetPanelOrder(UIBase uiBase) {
            if (uiBase.GetUIBaseType() == UIBase.UIBaseType.TopWindow) {
                return (int)(UIBase.UIBaseType.TopWindow);
            }

            int layerIncreaseRate = (int) (UIBase.UIBaseType.PopUpWindow);
            int windowsNum = 0;
            for (int i = 0; i < uiStack.Count; i++) {
                if (uiStack[i].Equals(uiBase)) {
					return (i + 1) * layerIncreaseRate;
				}
                if (uiStack[i].IsDefaultUIBaseType()) {
                    windowsNum += 1;
                }
            }
            return (windowsNum + 1) * layerIncreaseRate;
        }

        /// <summary>
		/// 判断指定UI是否在UI Stack里
		/// </summary>
		/// <param name="ui"></param>
		/// <returns></returns>
		public bool IsInStack(UIBase ui) {
			return uiStack.IndexOf(ui) >= 0;
		}

        /// <summary>
		/// 在target界面前/后插入insert界面
		/// 注意：界面开启关闭时序
		/// </summary>
		/// <param name="insert">插入界面</param>
		/// <param name="target">目标界面</param>
		/// <param name="isForward">是否在前面插入，否则是后面</param>
		public void InsertUI(UIBase insert, UIBase target,bool isForward) {
			for (int i = 0;i < uiStack.Count;i++) {
				var ui = uiStack[i];
				if (ui.Equals(target)) {
					uiStack.Insert(isForward ? i : ++i, insert);
					return;
				}
			}
			Debug.LogError($"Don't Find Need Insert Pos ，UIBase is：{target}");
		}


		/// <summary>
		/// 从场景树中移除
		/// </summary>
		/// <param name="ui">需要移除的UI</param>
		public void RemoveFromScene(UIBase ui) {
			var transform = ui.GetUIGameObject().transform;
			if (transform) {
				transform.SetParent(deActiveRoot, false);
			}
			ui.IsInScene = false;
		}

        /// <summary>
		/// 添加UI到场景
		/// </summary>
		/// <param name="ui">需要添加的UI</param>
		public void AddToScene(UIBase ui) {
			ui.IsInScene = true;
			var uiTransform = ui.GetUIGameObject().transform;
			
            var curLayer = ui.GetLayerIdx();
            uiTransform.SetParent(deActiveRoot, false);
			if (uiRoot.childCount < 1) {
                uiTransform.SetParent(uiRoot, false);
				return;
            }

            var targetIdx = 0;
            for (int i = uiRoot.childCount - 1; i >= 0; i--) {
                var go = uiRoot.GetChild(i);
                var childValue = go.GetComponent<UIOrder>().GetOrderValue();
                if (childValue <= curLayer) {
                    targetIdx = i + 1;
                    break;
                }
            }
            uiTransform.SetParent(uiRoot, false);
			uiTransform.SetSiblingIndex(targetIdx);
        }

		/// <summary>
		/// 关闭当前UI之后的UI
		/// </summary>
		public async UniTask CloseAboveUI(UIBase ui) {
            for (int i = uiStack.Count - 1; i >= 0; i--) {
                if (uiStack[i].Equals(ui)) {
                    if (uiStack[i].IsOpen()) {
						return;
                    }
					break;
                }
                if (uiStack[i].IsOpen() && uiStack[i].IsDefaultUIBaseType()) {
                    uiStack[i].Close(false);
                }
                if (uiStack[i].IsDefaultUIBaseType()) {
                    uiStack.RemoveAt(i);
				}
			}
			await OpenBelowUI(ui);
        }

		#region Private Methods

        /// <summary>
        /// 对当前UI之后的UI进行排序
        /// </summary>
        /// <param name="ui"></param>
        private void SortAboveUI(UIBase ui) {
            bool isEnd = false;
            for (int i = uiStack.Count - 1; i >= 0; i--) {
                if (isEnd) {
                    break;
                }
                if (uiStack[i].Equals(ui)) {
                    isEnd = true;
                }
                if (uiStack[i].IsDefaultUIBaseType()) {
                    uiStack[i].SetOrderInfo();
                }
            }
        }

        /// <summary>
        /// 打开当前UI之前的界面，直到打开一个全屏界面
        /// </summary>
        private async UniTask OpenBelowUI(UIBase ui) {
			var idx = uiStack.IndexOf(ui);
			if (idx <= 0) {
				return;
			}

			List<UniTask> taskList = new List<UniTask>();
			for(var i = idx - 1; i >= 0; --i) {
                if (!uiStack[i].IsDefaultUIBaseType()) {
					continue;
                }
                bool isFullScene = uiStack[i].GetUIBaseType() == UIBase.UIBaseType.FullScene;
                if(uiStack[i].IsOpen() && isFullScene) {
                    break;
				}
				var task = uiStack[i].Open(null,true);
				taskList.Add(task);
				if(isFullScene) {
					break;
				}
			}
			await UniTask.WhenAll(taskList);
		}

		/// <summary>
		/// 关闭当前界面之前的界面
		/// </summary>
		private void CloseBelowUI(UIBase ui) {
			var idx = uiStack.IndexOf(ui);
			if (idx <= 0) {
				return;
			}
			for (var i = idx - 1; i >= 0;--i) {
				if (!uiStack[i].IsReturnable()) {
					uiStack.RemoveAt(i);
				}
				if (!uiStack[i].IsOpen()) {
					continue;
				}
                uiStack[i].Close(false, false);
			}
		}

        /// <summary>
        /// 关闭所有未关闭的界面
        /// </summary>
        private void CloseAllUI() {
            var tmpUIs = new List<UIBase>();
            foreach (var t in uiStack) {
	            tmpUIs.Add(t);
            }
            // UIBase.Close接口会删除uiList数组元素，所以引用临时数据
            for (var i = tmpUIs.Count - 1; i >= 0; --i) {
	            if (tmpUIs[i].IsOpen()) {
		            tmpUIs[i].Close(false);
	            }
            }
            uiStack.Clear();
        }

    #endregion

        private readonly List<UIBase> uiStack = new List<UIBase>();
    }
}
