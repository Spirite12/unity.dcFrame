using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DCFrame.UGUI {
    /// <summary>
    /// 状态节点行为接口，负责进入、离开以及合法跳转判断。
    /// </summary>
    public interface ISimpleFSMBehaviour {
        /// <summary>
        /// 进入状态时触发。
        /// </summary>
        void OnEnter(SimpleFSMNode node);

        /// <summary>
        /// 离开状态时触发。
        /// </summary>
        void OnOut(SimpleFSMNode node);

        /// <summary>
        /// 判断当前状态是否允许跳到目标状态。
        /// </summary>
        bool ValidJump(SimpleFSMNode targetNode, SimpleFSMNode currentNode);
    }

    /// <summary>
    /// 简单状态机节点。
    /// </summary>
    public sealed class SimpleFSMNode {
        /// <summary>
        /// 节点整型标识。
        /// </summary>
        public int IntKey { get; internal set; }

        /// <summary>
        /// 业务层自定义数据。
        /// </summary>
        public object CustomData { get; set; }

        /// <summary>
        /// 当前节点绑定的状态行为。
        /// </summary>
        public ISimpleFSMBehaviour FSMBehaviour { get; private set; }

        /// <summary>
        /// 进入状态时的提示文案。
        /// </summary>
        public string OnEnterTip { get; private set; }

        /// <summary>
        /// 构造一个状态节点。
        /// </summary>
        public SimpleFSMNode(int key, object customData = null) {
            IntKey = key;
            CustomData = customData;
        }

        /// <summary>
        /// 绑定节点行为。
        /// </summary>
        public void AddFSMBehaviour(ISimpleFSMBehaviour fsmBehaviour) {
            FSMBehaviour = fsmBehaviour;
        }

        /// <summary>
        /// 设置进入状态时的提示文案。
        /// </summary>
        public void SetOnEnterTip(string tip) {
            OnEnterTip = tip;
        }

        /// <summary>
        /// 清理节点行为引用。
        /// </summary>
        public void Destroy() {
            FSMBehaviour = null;
            CustomData = null;
            OnEnterTip = null;
        }
    }

    /// <summary>
    /// 单个状态的超时配置。
    /// </summary>
    public sealed class SimpleFSMTimeout {
        /// <summary>
        /// 超时时长。
        /// </summary>
        public TimeSpan Delay { get; private set; }

        /// <summary>
        /// 超时后的异步回调。
        /// </summary>
        public Func<CancellationToken, UniTask> AsyncCallback { get; private set; }

        /// <summary>
        /// 构造一个超时配置。
        /// </summary>
        public SimpleFSMTimeout(TimeSpan delay, Func<CancellationToken, UniTask> asyncCallback = null) {
            Delay = delay;
            AsyncCallback = asyncCallback;
        }

        /// <summary>
        /// 用同步回调快速创建一个超时配置。
        /// </summary>
        public static SimpleFSMTimeout Create(TimeSpan delay, Action callback) {
            return new SimpleFSMTimeout(
                delay,
                delegate(CancellationToken _) {
                    callback?.Invoke();
                    return UniTask.CompletedTask;
                });
        }
    }

    /// <summary>
    /// 简单状态机，支持状态跳转、超时检测以及节点销毁。
    /// </summary>
    public sealed class FSM : IDisposable {
        private readonly Dictionary<int, SimpleFSMNode> stateMap = new Dictionary<int, SimpleFSMNode>();
        private readonly Dictionary<int, CancellationTokenSource> timerMap = new Dictionary<int, CancellationTokenSource>();

        private IReadOnlyDictionary<int, SimpleFSMTimeout> timeOutValueMap;

        /// <summary>
        /// 当前状态节点。
        /// </summary>
        public SimpleFSMNode CurrentNode { get; private set; }

        /// <summary>
        /// 是否已经停止工作。
        /// </summary>
        public bool Halted { get; private set; }

        /// <summary>
        /// 设置各状态的超时配置。
        /// </summary>
        public void SetStateTimeOut(IReadOnlyDictionary<int, SimpleFSMTimeout> timeOutValue) {
            timeOutValueMap = timeOutValue;
        }

        /// <summary>
        /// 注册一个状态节点。
        /// </summary>
        public void AddStateNode(int intKey, SimpleFSMNode node) {
            if (node == null) {
                throw new ArgumentNullException(nameof(node));
            }

            node.IntKey = intKey;
            stateMap[intKey] = node;
        }

        /// <summary>
        /// 获取指定状态节点。
        /// </summary>
        public SimpleFSMNode GetStateNode(int intKey) {
            SimpleFSMNode node;
            return stateMap.TryGetValue(intKey, out node) ? node : null;
        }

        /// <summary>
        /// 切换到指定状态。
        /// </summary>
        public void ChangeState(int intKey, bool noCheck = false) {
            if (Halted) {
                return;
            }

            SimpleFSMNode node;
            if (!stateMap.TryGetValue(intKey, out node)) {
                return;
            }

            if (!noCheck && !CanJump(intKey)) {
                return;
            }

            int? lastIntKey = null;
            if (CurrentNode != null) {
                lastIntKey = CurrentNode.IntKey;
                if (CurrentNode.FSMBehaviour != null) {
                    CurrentNode.FSMBehaviour.OnOut(CurrentNode);
                }
            }

            CurrentNode = node;
            if (node.FSMBehaviour != null) {
                node.FSMBehaviour.OnEnter(node);
            }

            TryStateTimeOut(lastIntKey, node.IntKey);
        }

        /// <summary>
        /// 根据离开状态和进入状态切换超时定时器。
        /// </summary>
        public void TryStateTimeOut(int? leaveIntKey, int? enterIntKey) {
            if (timeOutValueMap == null) {
                return;
            }

            if (leaveIntKey.HasValue) {
                DestroySingleTimeOutTimer(leaveIntKey.Value);
            }

            if (!enterIntKey.HasValue) {
                return;
            }

            SimpleFSMTimeout timeoutContext;
            if (timeOutValueMap.TryGetValue(enterIntKey.Value, out timeoutContext) && timeoutContext != null) {
                CreateTimeOutTimer(enterIntKey.Value, timeoutContext);
            }
        }

        /// <summary>
        /// 判断当前状态是否允许跳转到目标状态。
        /// </summary>
        public bool CanJump(int intKey) {
            SimpleFSMNode node;
            if (!stateMap.TryGetValue(intKey, out node)) {
                return false;
            }

            if (CurrentNode == null || CurrentNode.FSMBehaviour == null) {
                return true;
            }

            return CurrentNode.FSMBehaviour.ValidJump(node, CurrentNode);
        }

        /// <summary>
        /// 获取当前状态节点。
        /// </summary>
        public SimpleFSMNode GetCurrentState() {
            return CurrentNode;
        }

        /// <summary>
        /// 清理全部状态节点。
        /// </summary>
        public void DestroyNode() {
            foreach (KeyValuePair<int, SimpleFSMNode> pair in stateMap) {
                if (pair.Value != null) {
                    pair.Value.Destroy();
                }
            }

            stateMap.Clear();
            CurrentNode = null;
        }

        /// <summary>
        /// 销毁单个超时定时器。
        /// </summary>
        public void DestroySingleTimeOutTimer(int timerKey) {
            ReleaseTimer(timerKey, true);
        }

        /// <summary>
        /// 销毁全部超时定时器。
        /// </summary>
        public void DestroyTimerOutTimer() {
            List<int> timerKeys = new List<int>(timerMap.Keys);
            for (int i = 0; i < timerKeys.Count; i++) {
                ReleaseTimer(timerKeys[i], true);
            }
        }

        /// <summary>
        /// 销毁整个状态机。
        /// </summary>
        public void Destroy() {
            Halted = true;
            DestroyNode();
            DestroyTimerOutTimer();
            timeOutValueMap = null;
        }

        /// <summary>
        /// 实现 IDisposable，方便统一释放。
        /// </summary>
        public void Dispose() {
            Destroy();
        }

        /// <summary>
        /// 创建状态超时定时器。
        /// </summary>
        private void CreateTimeOutTimer(int timerKey, SimpleFSMTimeout timeoutContext) {
            DestroySingleTimeOutTimer(timerKey);

            CancellationTokenSource tokenSource = new CancellationTokenSource();
            timerMap[timerKey] = tokenSource;
            RunTimeOutAsync(timerKey, timeoutContext, tokenSource.Token).Forget();
        }

        /// <summary>
        /// 异步等待状态超时，并在超时后执行回调。
        /// </summary>
        private async UniTaskVoid RunTimeOutAsync(int timerKey, SimpleFSMTimeout timeoutContext, CancellationToken token) {
            try {
                await UniTask.Delay(timeoutContext.Delay, DelayType.UnscaledDeltaTime, PlayerLoopTiming.Update, token);
                if (token.IsCancellationRequested || Halted) {
                    return;
                }

                if (timeoutContext.AsyncCallback != null) {
                    await timeoutContext.AsyncCallback.Invoke(token);
                }
            } catch (OperationCanceledException) {
                // 定时器被主动取消时无需额外处理。
            } catch (Exception ex) {
                Debug.LogException(ex);
            } finally {
                if (!token.IsCancellationRequested) {
                    ReleaseTimer(timerKey, false);
                }
            }
        }

        /// <summary>
        /// 从定时器字典中释放指定句柄。
        /// </summary>
        private void ReleaseTimer(int timerKey, bool cancel) {
            CancellationTokenSource tokenSource;
            if (!timerMap.TryGetValue(timerKey, out tokenSource)) {
                return;
            }

            timerMap.Remove(timerKey);
            if (cancel && !tokenSource.IsCancellationRequested) {
                tokenSource.Cancel();
            }

            tokenSource.Dispose();
        }
    }

    /// <summary>
    /// 状态机过渡所需的时间源接口。
    /// </summary>
    public interface ISimpleFSMTimeProvider {
        /// <summary>
        /// 当前时间，单位为秒。
        /// </summary>
        float Time { get; }
    }

    /// <summary>
    /// 默认使用 Unity 非缩放时间的时间源。
    /// </summary>
    public sealed class UnitySimpleFSMTimeProvider : ISimpleFSMTimeProvider {
        /// <summary>
        /// 默认实例。
        /// </summary>
        public static readonly UnitySimpleFSMTimeProvider Instance = new UnitySimpleFSMTimeProvider();

        /// <summary>
        /// 当前时间，使用非缩放时间避免过渡受 Time.timeScale 影响。
        /// </summary>
        public float Time {
            get { return UnityEngine.Time.unscaledTime; }
        }
    }

    /// <summary>
    /// 单个过渡队列元素。
    /// </summary>
    public sealed class SimpleFSMTransitionItem {
        /// <summary>
        /// 目标状态。
        /// </summary>
        public int State { get; private set; }

        /// <summary>
        /// 过渡持续时间，负数表示需要等待外部条件。
        /// </summary>
        public float Duration { get; private set; }

        /// <summary>
        /// 条件控制标记。
        /// </summary>
        public bool? ConditionFlag { get; set; }

        /// <summary>
        /// 构造一个过渡项。
        /// </summary>
        public SimpleFSMTransitionItem(int state, float duration, bool? conditionFlag = null) {
            State = state;
            Duration = duration;
            ConditionFlag = conditionFlag;
        }
    }

    /// <summary>
    /// 状态机过渡队列，负责按时间或条件推进状态切换。
    /// </summary>
    public sealed class SimpleFSMTransition : IDisposable {
        private readonly List<SimpleFSMTransitionItem> fsmQueue = new List<SimpleFSMTransitionItem>();
        private readonly FSM fsm;
        private readonly ISimpleFSMTimeProvider timeProvider;

        private CancellationTokenSource stateTimer;
        private Func<bool> haltPredicate;
        private float startTick = -1f;

        /// <summary>
        /// 当前是否处于待执行状态。
        /// </summary>
        public bool Pending { get; private set; }

        /// <summary>
        /// 构造一个过渡控制器。
        /// </summary>
        public SimpleFSMTransition(FSM fsm, ISimpleFSMTimeProvider timeProvider = null) {
            if (fsm == null) {
                throw new ArgumentNullException(nameof(fsm));
            }

            this.fsm = fsm;
            this.timeProvider = timeProvider ?? UnitySimpleFSMTimeProvider.Instance;
            Pending = true;
        }

        /// <summary>
        /// 销毁过渡控制器。
        /// </summary>
        public void Destroy() {
            StopTransition();
            haltPredicate = null;
        }

        /// <summary>
        /// 实现 IDisposable，方便统一释放。
        /// </summary>
        public void Dispose() {
            Destroy();
        }

        /// <summary>
        /// 推进一步状态，满足时间或条件时会立即触发切换。
        /// </summary>
        public void NextAnimState(bool isForce = false) {
            if (fsmQueue.Count == 0) {
                Pending = true;
                return;
            }

            bool isChange = false;
            SimpleFSMTransitionItem headElem = fsmQueue[0];
            if (startTick < 0f) {
                isChange = true;
            } else {
                bool conditionFlag = !headElem.ConditionFlag.HasValue || headElem.ConditionFlag.Value;
                if (isForce) {
                    isChange = true;
                } else if (headElem.Duration < 0f) {
                    isChange = headElem.ConditionFlag.HasValue && headElem.ConditionFlag.Value;
                } else if (timeProvider.Time > startTick + headElem.Duration && conditionFlag) {
                    isChange = true;
                }
            }

            if (!isChange) {
                Pending = false;
                return;
            }

            SimpleFSMTransitionItem elem = PopAnimState();
            if (elem == null) {
                Pending = true;
                return;
            }

            fsm.ChangeState(elem.State);
            startTick = timeProvider.Time;
            Pending = fsmQueue.Count == 0;
        }

        /// <summary>
        /// 获取当前队头过渡项。
        /// </summary>
        public SimpleFSMTransitionItem GetCurrentTransition() {
            return fsmQueue.Count > 0 ? fsmQueue[0] : null;
        }

        /// <summary>
        /// 压入一个新的过渡项。
        /// </summary>
        public SimpleFSMTransitionItem PushAnimState(int animState, float duration, bool? conditionFlag = null) {
            SimpleFSMTransitionItem item = new SimpleFSMTransitionItem(animState, duration, conditionFlag);
            fsmQueue.Add(item);
            Pending = false;
            return item;
        }

        /// <summary>
        /// 修改某个过渡项的条件标记。
        /// </summary>
        public void SetTransitionConditionFlag(SimpleFSMTransitionItem transition, bool flag) {
            if (transition == null) {
                return;
            }

            transition.ConditionFlag = flag;
        }

        /// <summary>
        /// 弹出队头过渡项。
        /// </summary>
        public SimpleFSMTransitionItem PopAnimState() {
            if (fsmQueue.Count == 0) {
                return null;
            }

            SimpleFSMTransitionItem firstElem = fsmQueue[0];
            fsmQueue.RemoveAt(0);
            return firstElem;
        }

        /// <summary>
        /// 开始自动推进过渡队列。
        /// </summary>
        /// <param name="intervalSeconds">轮询间隔，单位秒</param>
        /// <param name="haltPredicate">返回 true 时，本轮不会推进状态</param>
        public void StartTransition(float intervalSeconds, Func<bool> haltPredicate = null) {
            if (stateTimer != null) {
                if (!stateTimer.IsCancellationRequested) {
                    stateTimer.Cancel();
                }

                stateTimer.Dispose();
                stateTimer = null;
            }

            this.haltPredicate = haltPredicate;
            stateTimer = new CancellationTokenSource();
            startTick = -1f;
            Pending = fsmQueue.Count == 0;
            RunTransitionLoopAsync(Mathf.Max(0.01f, intervalSeconds), stateTimer.Token).Forget();
        }

        /// <summary>
        /// 停止自动推进，并清空队列。
        /// </summary>
        public void StopTransition() {
            CleanQueue();
            if (stateTimer != null) {
                if (!stateTimer.IsCancellationRequested) {
                    stateTimer.Cancel();
                }

                stateTimer.Dispose();
                stateTimer = null;
            }

            startTick = -1f;
            Pending = true;
        }

        /// <summary>
        /// 清空当前过渡队列。
        /// </summary>
        public void CleanQueue() {
            fsmQueue.Clear();
        }

        /// <summary>
        /// 强制推进到下一状态。
        /// </summary>
        public void ForceTransition() {
            NextAnimState(true);
        }

        /// <summary>
        /// 异步轮询过渡队列。
        /// </summary>
        private async UniTaskVoid RunTransitionLoopAsync(float intervalSeconds, CancellationToken token) {
            try {
                while (!token.IsCancellationRequested) {
                    if (haltPredicate == null || !haltPredicate.Invoke()) {
                        NextAnimState();
                    }

                    await UniTask.Delay(TimeSpan.FromSeconds(intervalSeconds), true, PlayerLoopTiming.Update, token);
                }
            } catch (OperationCanceledException) {
                // 停止轮询时无需额外处理。
            } catch (Exception ex) {
                Debug.LogException(ex);
            }
        }
    }
}
