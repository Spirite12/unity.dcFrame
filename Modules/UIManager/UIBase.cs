using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DCFrame {
	/// <summary>
	/// 所有UI的基类，会被UIMgr管理
	/// </summary>
	public abstract class UIBase{
		/// <summary>
		/// 界面类型,值是界面层级的初始值，PopUpWindow跟随FullScene
		/// </summary>
		public enum UIBaseType {
            /// <summary>
            /// 全屏
            /// </summary>
            FullScene = 0,
			/// <summary>
			/// 弹窗
			/// </summary>
			PopUpWindow = 100,
			/// <summary>
			/// 顶部弹窗
			/// </summary>
			TopWindow = 2000,
        }
		/// <summary>
		/// UI打开事件，只要有界面打开就会触发
		/// </summary>
		public static event Action<UIBase> OnUIOpened;
		/// <summary>
		/// UI关闭事件，只要有界面关闭就会触发
		/// </summary>
		public static event Action<UIBase> OnUIClosed;
		/// <summary>
		/// 卸载UI后的回调，方便做资源回收
		/// </summary>
        public static event Action<UIBase> OnUnLoadUI;
		/// <summary>
		/// 当前UI打开时会触发
		/// </summary>
		public event Action<UIBaseType> OnThisUIOpened;
		/// <summary>
		/// 当前UI关闭时会触发
		/// </summary>
		public event Action<UIBaseType> OnThisUIClosed;
		/// <summary>
		/// 当前结界实例的根结点
		/// </summary>
		public GameObject uiGameObject;

		#region Public Methods

		/// <summary>
		/// 显示当前界面
		/// </summary>
		/// <param name="closeCallback">关闭界面回调</param>
		public async UniTask Open(Action closeCallback = null) {
			if (isOpen) {
				Debug.LogError("Already opened.");
				return;
			}
			isOpen = true;
			await LoadPrefab();
			if (!isOpen) {
				Debug.LogError($"UIBase.Close was called during loading, Prefab Name is {AssetName}");
				return;
			}
			SetOrderInfo();
			OnShow();
			if (!IsInScene) {
				UIMgr.Instance.AddToScene(this);
			}
			closeAction = closeCallback;
			UIMgr.Instance.Open(this);
			OnThisUIOpened?.Invoke(GetUIBaseType());
			OnUIOpened?.Invoke(this);
		}


		/// <summary>
		/// 关闭当前界面，会隐藏界面，但是否释放取决于ShouldUnloadOnClose方法的返回值
		/// </summary>
		/// <param name="notifyUIMgr">是否通知UI管理器，这个通常使用默认值即可，无需设置</param>
		/// <param name="isRelease">是否需要走释放操作，默认需要</param>
		public async UniTask Close(bool notifyUIMgr = true, bool isRelease = true) {
			if (!isOpen) {
				Debug.LogError($"closing not opened ui: {AssetName}");
				return;
			}
			if (notifyUIMgr) {
				await UIMgr.Instance.Close(this);
			}
			isOpen = false;
			OnHide();
			if (isRelease) {
				OnRelease();
			}
			UIMgr.Instance.RemoveFromScene(this);
			if (ShouldUnloadOnClose()) {
				Unload();
			}
			closeAction?.Invoke();
			closeAction = null;
			OnThisUIClosed?.Invoke(GetUIBaseType());
			OnUIClosed?.Invoke(this);
		}

        /// <summary>
        /// 获取界面类型
        /// </summary>
        public UIBaseType GetUIBaseType() {
            return SetUIBaseType();
        }

		/// <summary>
		/// 是否是默认界面类型
		/// </summary>
		/// <returns></returns>
        public bool IsDefaultUIBaseType() {
            return GetUIBaseType() == UIBaseType.FullScene || GetUIBaseType() == UIBaseType.PopUpWindow;
        }

		/// <summary>
		/// 设置界面类型
		/// 默认全屏，可设置为其他类型
		/// </summary>
		public virtual UIBaseType SetUIBaseType() {
			return UIBaseType.FullScene;
		}

		/// <summary>
		/// 是否可返回，不可返回的界面在任意其它界面打开的时候会被自动关闭
		/// </summary>
		/// <returns>返回true表示当前界面可返回</returns>
		public virtual bool IsReturnable() {
			return true;
		}

		/// <summary>
		/// 是否已经打开
		/// </summary>
		/// <returns>返回true时有可能当前界面实例正在加载即将显示或者已经显示</returns>
		public bool IsOpen() {
			return isOpen;
		}

		/// <summary>
		/// 是否当前界面的GameObject处于场景树中(UIMgr设置值)
		/// </summary>
		public bool IsInScene { get; set; }

		/// <summary>
		/// 是否已经加载完成
		/// </summary>
		public bool IsLoaded() {
			return uiGameObject != null;
		}

		/// <summary>
		/// 获取实例化节点的根节点物体
		/// </summary>
		/// <returns>当前界面根结点的Root Transform</returns>
		public GameObject GetUIGameObject() {
            return uiGameObject;
        }

        /// <summary>
		/// 获取当前界面指定层内的层级，值越大显示越前面
		/// </summary>
		/// <returns>层内索引int值</returns>
		public int GetLayerIdx() {
			return layerIndex;
		}

        /// <summary>
        /// 设置层级的信息
        /// </summary>
        public void SetOrderInfo() {
            layerIndex = UIMgr.Instance.GetPanelOrder(this);
            var uiOrder = uiGameObject.GetComponent<UIOrder>();
            if (!uiOrder) {
                return;
            }
            uiOrder.SetOrder(layerIndex);
        }

        #endregion

        #region Protect Methods
        
        /// <summary>
        /// 是否关闭的时候释放界面资源，默认为释放
        /// </summary>
        protected virtual bool ShouldUnloadOnClose() {
	        return true;
        }

        /// <summary>
        /// 正在加载界面的回调
        /// </summary>
        protected virtual UniTask OnLoading() {
	        return UniTask.CompletedTask;
        }

        /// <summary>
        /// 界面显示的时候回调
        /// </summary>
        protected abstract void OnShow();

		/// <summary>
		/// 界面释放的时候回调
		/// </summary>
		protected abstract void OnRelease();

		/// <summary>
		/// 界面隐藏的时候回调,主要做事件监听移除
		/// </summary>
		protected abstract void OnHide();

		/// <summary>
		/// 实现这个函数提供资源路径
		/// </summary>
		protected abstract string AssetPath { get; }

		/// <summary>
		/// 实现这个函数提供资源文件名
		/// </summary>
		protected abstract string AssetName { get; }

        #endregion

		#region private methods

		/// <summary>
		/// 加载预制体
		/// </summary>
		private async UniTask LoadPrefab() {
			if (IsLoaded()) {
				return;
			}

			var address = Asset.GetPrefabPath(AssetPath + "/" + AssetName);
			var taskLoadPrefab = Asset.LoadAsset(address);
			var taskOnLoading = OnLoading();
			var (prefabObj, _) = await UniTask.WhenAll(taskLoadPrefab, taskOnLoading.AsAsyncUnitUniTask());
			var prefab = prefabObj as GameObject;
			if (prefab == null) {
				Debug.LogError($"load failed: assetPath = {AssetPath}, assetName = {AssetName}");
				return;
			}
			uiGameObject = Object.Instantiate(prefab, UIMgr.Instance.deActiveRoot);
		}

		/// <summary>
		/// 卸载预制体
		/// </summary>
		private void Unload() {
            Object.Destroy(uiGameObject);
			uiGameObject = null;
            OnUnLoadUI?.Invoke(this);
		}

        #endregion

        private Action closeAction; // 关闭界面的回调
        private bool isOpen;// 当前界面是否处于打开状态
		private int layerIndex = 0;// 当前界面在层内的索引
	}
}
