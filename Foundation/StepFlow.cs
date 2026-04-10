using System;
using System.Collections.Generic;
using UnityEngine;

namespace DCFrame.Utility {
    /// <summary>
    /// 顺序流程控制器。
    /// 注册多个步骤后，当前步骤在完成时调用 CompleteStep，即可自动推进到下一步。
    /// </summary>
    public class StepFlow  {
        /// <summary>
        /// 添加一个无参流程步骤，并自动记录函数名。
        /// </summary>
        /// <param name="stepAction">步骤执行回调</param>
        public StepFlow AddStep(Action stepAction) {
            if (stepAction == null) {
                throw new ArgumentNullException(nameof(stepAction));
            }

            stepList.Add(new Step(_ => stepAction.Invoke(), GetStepName(stepAction.Method.Name)));
            return this;
        }

        /// <summary>
        /// 添加一个带 Flow 参数的流程步骤，并自动记录函数名。
        /// 需要在步骤间传递数据时，推荐使用这个重载。
        /// </summary>
        /// <param name="stepAction">步骤执行回调，参数为当前流程实例</param>
        public StepFlow AddStep(Action<StepFlow> stepAction) {
            if (stepAction == null) {
                throw new ArgumentNullException(nameof(stepAction));
            }

            stepList.Add(new Step(stepAction, GetStepName(stepAction.Method.Name)));
            return this;
        }

        /// <summary>
        /// 启动流程，从第一个步骤开始执行。
        /// </summary>
        public void Start() {
            if (stepList.Count == 0) {
                Debug.LogWarning("Flow 启动失败，当前没有注册任何步骤。");
                return;
            }

            isRunning = true;
            isCompleted = false;
            currentStepIndex = 0;
            ExecuteCurrentStep();
        }

        /// <summary>
        /// 当前步骤完成后调用，流程会自动执行下一个步骤。
        /// </summary>
        public void CompleteStep() {
            if (!isRunning || !isWaitingComplete) {
                return;
            }

            isWaitingComplete = false;
            currentStepIndex++;
            if (currentStepIndex >= stepList.Count) {
                FinishFlow();
                return;
            }

            ExecuteCurrentStep();
        }

        /// <summary>
        /// 停止当前流程，保留已注册的步骤和上下文数据。
        /// </summary>
        public void Stop() {
            isRunning = false;
            isWaitingComplete = false;
        }

        /// <summary>
        /// 重置流程状态，下次可重新 Start。
        /// 默认保留上下文数据，便于外部决定何时清理。
        /// </summary>
        public void Reset() {
            Stop();
            isCompleted = false;
            currentStepIndex = -1;
        }

        /// <summary>
        /// 清空全部步骤并重置流程状态。
        /// </summary>
        public void Clear() {
            Reset();
            stepList.Clear();
            contextData.Clear();
        }

        /// <summary>
        /// 向流程上下文写入数据，后续步骤可按 key 读取。
        /// </summary>
        public StepFlow SetData<T>(string key, T value) {
            if (string.IsNullOrWhiteSpace(key)) {
                throw new ArgumentException("Flow 上下文 key 不能为空。", nameof(key));
            }

            contextData[key] = value;
            return this;
        }

        /// <summary>
        /// 从流程上下文读取数据。
        /// key 不存在或类型不匹配时会抛出异常。
        /// </summary>
        public T GetData<T>(string key) {
            if (string.IsNullOrWhiteSpace(key)) {
                throw new ArgumentException("Flow 上下文 key 不能为空。", nameof(key));
            }

            if (!contextData.TryGetValue(key, out var value)) {
                throw new KeyNotFoundException($"Flow 上下文中不存在 key：{key}");
            }

            if (value is T result) {
                return result;
            }

            throw new InvalidCastException($"Flow 上下文 key：{key} 的值无法转换为类型 {typeof(T).Name}");
        }

        /// <summary>
        /// 尝试从流程上下文读取数据，读取失败时返回 false。
        /// </summary>
        public bool TryGetData<T>(string key, out T value) {
            value = default;
            if (string.IsNullOrWhiteSpace(key)) {
                return false;
            }

            if (!contextData.TryGetValue(key, out var rawValue)) {
                return false;
            }

            if (rawValue is not T result) {
                return false;
            }

            value = result;
            return true;
        }

        /// <summary>
        /// 删除指定 key 的上下文数据。
        /// </summary>
        public bool RemoveData(string key) {
            if (string.IsNullOrWhiteSpace(key)) {
                return false;
            }

            return contextData.Remove(key);
        }

        /// <summary>
        /// 清空全部上下文数据。
        /// </summary>
        public void ClearData() {
            contextData.Clear();
        }

        /// <summary>
        /// 当前是否正在执行流程。
        /// </summary>
        public bool IsRunning => isRunning;

        /// <summary>
        /// 当前流程是否已全部执行完成。
        /// </summary>
        public bool IsCompleted => isCompleted;

        /// <summary>
        /// 当前执行到的步骤索引。
        /// </summary>
        public int CurrentStepIndex => currentStepIndex;

        /// <summary>
        /// 当前步骤名称，未开始时为空字符串。
        /// </summary>
        public string CurrentStepName => currentStepIndex >= 0 && currentStepIndex < stepList.Count
            ? stepList[currentStepIndex].StepName
            : string.Empty;

        /// <summary>
        /// 全部流程执行完成时触发。
        /// </summary>
        public event Action OnCompleted;

        /// <summary>
        /// 每次切换到新步骤时触发。
        /// </summary>
        public event Action<int, string> OnStepChanged;

        /// <summary>
        /// 执行当前步骤。
        /// </summary>
        private void ExecuteCurrentStep() {
            if (!isRunning || currentStepIndex < 0 || currentStepIndex >= stepList.Count) {
                return;
            }

            var step = stepList[currentStepIndex];
            isWaitingComplete = true;
            OnStepChanged?.Invoke(currentStepIndex, step.StepName);

            try {
                step.StepAction.Invoke(this);
            } catch (Exception ex) {
                Debug.LogError($"Flow 步骤执行失败，步骤：{step.StepName}，异常：{ex}");
                Stop();
            }
        }

        /// <summary>
        /// 获取步骤名称。
        /// 匿名函数或 lambda 会退回为默认步骤名，因此更推荐直接传方法组。
        /// </summary>
        private string GetStepName(string methodName) {
            if (string.IsNullOrWhiteSpace(methodName) || methodName.Contains("<")) {
                return $"Step_{stepList.Count}";
            }

            return methodName;
        }

        /// <summary>
        /// 结束整个流程并派发完成事件。
        /// </summary>
        private void FinishFlow() {
            isRunning = false;
            isWaitingComplete = false;
            isCompleted = true;
            OnCompleted?.Invoke();
            ClearData();
        }

        /// <summary>
        /// 单个流程步骤定义。
        /// </summary>
        private readonly struct Step {
            public Step(Action<StepFlow> stepAction, string stepName) {
                StepAction = stepAction;
                StepName = stepName;
            }

            public Action<StepFlow> StepAction { get; }
            public string StepName { get; }
        }

        private readonly Dictionary<string, object> contextData = new();
        private readonly List<Step> stepList = new();
        private bool isRunning;
        private bool isCompleted;
        private bool isWaitingComplete;
        private int currentStepIndex = -1;
    }
}
