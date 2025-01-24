using System;
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

		/// <summary>
		/// 回收接口
		/// </summary>
		public void GcCollect() {
			if (!unhandledLowMemoryWarning || Time.time - lastCollectTime < CollectDuration) {
				return;
			}

			Resources.UnloadUnusedAssets();
			GC.Collect();
			lastCollectTime = Time.time;
			unhandledLowMemoryWarning = false;
		}

		private void Awake() {
			lastCollectTime = Time.time;
			AddListeners();
			UIMgr.Instance.Init(tsfActiveRoot, tsfDeActiveRoot, uiCamera);
		}

		private void Update() {
			if (Input.GetKeyDown(KeyCode.Escape)) {
				UIMgr.Instance.EscapeClose();
			}
		}

		/// <summary>
		/// 退出游戏回调函数
		/// </summary>
		private void OnApplicationQuit() {
			unhandledLowMemoryWarning = false;
			isPaused = false;
			OnApplicationQuitEvent?.Invoke();
			RemoveListener();
			UIMgr.Instance.Shut();
		}

		/// <summary>
		/// 前后台切换回调函数
		/// </summary>
		/// <param name="pauseStatus"></param>
		private void OnApplicationFocus(bool pauseStatus) {
			if (isPaused == pauseStatus) {
				return;
			}

			isPaused = pauseStatus;
			OnApplicationPause?.Invoke(isPaused);
		}

		private void AddListeners() {
			Application.lowMemory += OnLowMemory;
			UIBase.OnUnLoadUI += GcCollect;
		}

		private void RemoveListener() {
			Application.lowMemory -= OnLowMemory;
			UIBase.OnUnLoadUI -= GcCollect;
		}
		
		/// <summary>
		/// 接收到了低内存的事件
		/// </summary>
		private void OnLowMemory() {
			unhandledLowMemoryWarning = true;
			GcCollect();
		}

		private void GcCollect(UIBase uiBase) {
			GcCollect();
		}

		/// <summary>
		/// 回收周期，单位秒
		/// </summary>
		private const float CollectDuration = 10f;

		/// <summary>
		/// 是否有未处理的低内存警告
		/// </summary>
		private bool unhandledLowMemoryWarning = false;

		/// <summary>
		/// 上一次回收时刻
		/// </summary>
		private float lastCollectTime = 0;

		/// <summary>
		/// 是否暂停中
		/// </summary>
		private bool isPaused = false;
	}
}
