using System;
using DCFrame.Foundation;
using UnityEngine;

namespace DCFrame {
	public class MainFrame : MonoSingleton<MainFrame> {
		/// <summary>
		/// 应用前后台切换事件
		/// </summary>
		public event Action<bool> OnApplicationPause;
		/// <summary>
		/// 应用关闭事件
		/// </summary>
		public event Action OnApplicationQuitEvent;

		public Transform tsfActiveRoot; // 激活根节点
		public Transform tsfDeActiveRoot; // 不激活根节点
		public Camera uiCamera; // UI摄像机

		private void Awake() {
			GCCollect.Init();
			TextFilter.Init();
			UIMgr.Instance.Init(tsfActiveRoot, tsfDeActiveRoot, uiCamera);
			CacheMgr.Init();
		}

		private void Update() {
			if (Input.GetKeyDown(KeyCode.Escape)) {
				UIMgr.Instance.EscapeClose();
			}
		}
		
		private void FixedUpdate() {
			Asset.FixedUpdate();
			CacheMgr.FixedUpdate();
		}

		/// <summary>
		/// 退出游戏回调函数
		/// </summary>
		private void OnApplicationQuit() {
			isPaused = false;
			OnApplicationQuitEvent?.Invoke();
			Shutdown();
		}

		protected override void OnDestroy() {
			Shutdown();
			base.OnDestroy();
		}

		/// <summary>
		/// 释放框架运行时资源；应用退出与场景销毁共用，避免重复执行。
		/// </summary>
		private void Shutdown() {
			if (isShutdown) {
				return;
			}
			isShutdown = true;
			GCCollect.Destroy();
			TextFilter.Destroy();
			UIMgr.Instance.Shut();
			EventMgr.Clear();
			CacheMgr.Destroy();
		}

		/// <summary>
		/// 前后台切换回调函数
		/// </summary>
		private void OnApplicationFocus(bool hasFocus) {
			SetApplicationPause(!hasFocus);
		}

		/// <summary>
		/// 更新并派发应用暂停状态。
		/// </summary>
		private void SetApplicationPause(bool pauseStatus) {
			if (isPaused == pauseStatus) {
				return;
			}
			isPaused = pauseStatus;
			OnApplicationPause?.Invoke(isPaused);
		}

		/// <summary>
		/// 是否暂停中
		/// </summary>
		private bool isPaused = false;
		private bool isShutdown = false;
	}
}
