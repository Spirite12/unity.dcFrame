using System;
using System.Collections.Generic;
using UnityEngine;

namespace DCFrame {
	/// <summary>
	/// UI管理类，自动管理层级
	/// 术语定义：
	/// 1.显示关闭界面，在游戏逻辑中主动调用UIBase.Close方法；
	/// 2.隐式关闭界面，在UIMgr中因为层级关系被关闭。
	/// </summary>
	public class UIMgr : Singleton<UIMgr> {

        /// <summary>
        /// 关闭所有UI的回调，方便做资源回收
        /// </summary>
        public event Action OnCloseAllUI;
        /// <summary>
        /// 打开关闭游戏弹窗的通知
        /// </summary>
        public event Action OnShowClosePanel;
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
		/// <param name="root">UI根节点</param>
		/// <param name="deActiveRoot"></param>
		/// <param name="camera"></param>
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
            // 关闭游戏确认弹窗
            void openClosePanel() {
                OnShowClosePanel?.Invoke();
            }

            for (int i = uiStack.Count - 1; i > 0; i--) {
                if (uiStack[i].IsOpen()) {
                    uiStack[i].Close();
                    return;
                }
            }
            openClosePanel();
        }

        /// <summary>
		/// UIBase打开的时候通过这个函数通知UIMgr进行层级管理，
		/// 如果已经在UI栈里则返回false，不能重复打开同一个界面。
		/// </summary>
		/// <param name="ui">被打开的界面</param>
		/// <returns>是否打开成功</returns>
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

		/// <summary>
		/// UIBase显式关闭的时候通过这个函数通知UIMgr进行管理。
		/// </summary>
		/// <param name="ui">被关闭的界面</param>
		/// <param name="callback">关闭回调，因为关闭当前界面有可能需要异步返回之前被隐式关闭的界面，所以关闭也是异步的</param>
		public void Close(UIBase ui, Action<Exception> callback) {
            void doClose(Exception ex) {
                int idx = uiStack.IndexOf(ui);
                uiStack.Remove(ui);
                if (idx < uiStack.Count) {
                    SortAboveUI(uiStack[idx]);
                }
                callback(ex);
			}

			if (ui.GetUIBaseType() == UIBase.UIBaseType.FullScene) {
				OpenBelowUI(ui, doClose);
			} else {
				doClose(null);
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
		/// <param name="ui"></param
		/// <param name="callBack"></param>
		/// <returns>是否有UI被关闭</returns>
		public void CloseAboveUI(UIBase ui, Action<Exception> callBack) {
            for (int i = uiStack.Count - 1; i >= 0; i--) {
                if (uiStack[i].Equals(ui)) {
                    if (uiStack[i].IsOpen()) {
						return;
                    }
					break;
                }
                if (uiStack[i].IsOpen() && uiStack[i].IsDefaultUIBaseType()) {
                    uiStack[i].Close(null, false);
                }
                if (uiStack[i].IsDefaultUIBaseType()) {
                    uiStack.RemoveAt(i);
				}
			}
			OpenBelowUI(ui, callBack);
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
        /// 打开指定UI下的UI，直到打开一个全屏不透明界面
        /// </summary>
        /// <param name="ui"></param>
        /// <param name="callback"></param>
        private void OpenBelowUI(UIBase ui, Action<Exception> callback) {
			var idx = uiStack.IndexOf(ui);
			if (idx <= 0) {
				callback?.Invoke(null);
				return;
			}

			var expectedOpenCnt = 0;
			var actualOpenedCnt = 0;
			var loopFinished = false;
			Exception lastException = null;

			void NextCallback() {
				if (expectedOpenCnt != actualOpenedCnt || !loopFinished) {
					return;
				}
				callback?.Invoke(lastException);
				callback = null;// 置空，防止重复调用
			}

			for(var i = idx - 1; i >= 0; --i) {
                if (!uiStack[i].IsDefaultUIBaseType()) {
					continue;
                }
                if(uiStack[i].IsOpen() && uiStack[i].GetUIBaseType() == UIBase.UIBaseType.FullScene) {
                    loopFinished = true;
                    NextCallback();
					return;
				}
				++expectedOpenCnt;
                // 这个open如果界面常驻内存就有可能会变成同步操作，使用loopFinished防止回调被提前调用
                uiStack[i].Open(exception => {
					if(exception != null) {
						lastException = exception;
					}
					++actualOpenedCnt;
					NextCallback();
				},null,true);
				if(uiStack[i].GetUIBaseType() == UIBase.UIBaseType.FullScene) {
					break;
				}
			}
			loopFinished = true;
			NextCallback();
		}

		/// <summary>
		/// 关闭全屏之前以上的界面
		/// </summary>
		/// <param name="ui"></param>
		private void CloseBelowUI(UIBase ui) {
			var idx = uiStack.IndexOf(ui);
			if (idx <= 0) {
				return;
			}

			for (var i = idx - 1;i >= 0;--i) {
				if (!uiStack[i].IsReturnable()) {
					uiStack.RemoveAt(i);
				}
				if (!uiStack[i].IsOpen()) {
					continue;
				}
                uiStack[i].Close(null, false, false);
			}
		}

        /// <summary>
        /// 关闭所有未关闭的界面，根据参数决定是否强制卸载所有未卸载的界面。
        /// </summary>
        /// <param name="forceUnload"></param>
        private void CloseAllUI() {
            var tmpUIs = new List<UIBase>();
            for (var i = 0; i < uiStack.Count; ++i) {
                tmpUIs.Add(uiStack[i]);
            }

            // UIBase.Close接口会删除uiList数组元素，所以引用临时数据
            for (var i = tmpUIs.Count - 1; i >= 0; --i) {
                tmpUIs[i].Close(null, false);
            }

            uiStack.Clear();
            OnCloseAllUI?.Invoke();
        }

    #endregion

        private readonly List<UIBase> uiStack = new List<UIBase>();
    }
}
