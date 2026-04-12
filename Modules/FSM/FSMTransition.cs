using System;
using System.Collections.Generic;
using UnityEngine;
using Timer = UnityTimer.Timer;

namespace DCFrame {
    /// <summary>
    /// 状态切换使用的时间模式。
    /// </summary>
    public enum FSMTimeMode {
        /// <summary>
        /// 使用受 Time.timeScale 影响的游戏时间。
        /// </summary>
        ScaledTime,
        /// <summary>
        /// 使用不受 Time.timeScale 影响的非缩放时间。
        /// </summary>
        UnscaledTime,
        /// <summary>
        /// 使用实时启动时间。
        /// </summary>
        RealtimeSinceStartup
    }

    /// <summary>
    /// 状态机过渡队列，负责按时间或条件推进状态切换。
    /// </summary>
    public sealed class FSMTransition : IDisposable {
        /// <summary>
        /// 构造一个过渡控制器。
        /// </summary>
        public FSMTransition(FSM fsm, FSMTimeMode timeMode = FSMTimeMode.UnscaledTime) {
            if (fsm == null) {
                throw new ArgumentNullException(nameof(fsm));
            }

            this.fsm = fsm;
            this.timeMode = timeMode;
            Pending = true;
        }

        /// <summary>
        /// 当前是否处于等待执行状态。
        /// </summary>
        public bool Pending { get; private set; }

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
        /// 推进一步状态，满足时间或条件时会立刻触发切换。
        /// </summary>
        public void NextAnimState(bool isForce = false) {
            if (fsmQueue.Count == 0) {
                Pending = true;
                return;
            }

            bool isChange = false;
            FSMTransitionItem headElem = fsmQueue[0];
            if (startTick < 0f) {
                isChange = true;
            } else {
                bool conditionFlag = !headElem.ConditionFlag.HasValue || headElem.ConditionFlag.Value;
                if (isForce) {
                    isChange = true;
                } else if (headElem.Duration < 0f) {
                    isChange = headElem.ConditionFlag.HasValue && headElem.ConditionFlag.Value;
                } else if (GetCurrentTime() > startTick + headElem.Duration && conditionFlag) {
                    isChange = true;
                }
            }

            if (!isChange) {
                Pending = false;
                return;
            }

            if (!fsm.TryChangeState(headElem.State)) {
                Pending = false;
                return;
            }

            PopAnimState();
            startTick = GetCurrentTime();
            Pending = fsmQueue.Count == 0;
        }

        /// <summary>
        /// 获取当前队头过渡项。
        /// </summary>
        public FSMTransitionItem GetCurrentTransition() {
            return fsmQueue.Count > 0 ? fsmQueue[0] : null;
        }

        /// <summary>
        /// 压入一个新的过渡项。
        /// </summary>
        public FSMTransitionItem PushAnimState(int animState, float duration, bool? conditionFlag = null) {
            FSMTransitionItem item = new FSMTransitionItem(animState, duration, conditionFlag);
            fsmQueue.Add(item);
            Pending = false;
            return item;
        }

        /// <summary>
        /// 修改某个过渡项的条件标记。
        /// </summary>
        public void SetTransitionConditionFlag(FSMTransitionItem transition, bool flag) {
            if (transition == null) {
                return;
            }

            transition.ConditionFlag = flag;
        }

        /// <summary>
        /// 弹出队头过渡项。
        /// </summary>
        public FSMTransitionItem PopAnimState() {
            if (fsmQueue.Count == 0) {
                return null;
            }

            FSMTransitionItem firstElem = fsmQueue[0];
            fsmQueue.RemoveAt(0);
            return firstElem;
        }

        /// <summary>
        /// 开始自动推进过渡队列。
        /// </summary>
        /// <param name="intervalSeconds">轮询间隔，单位秒。</param>
        /// <param name="haltPredicate">返回 true 时，本轮不会推进状态。</param>
        public void StartTransition(float intervalSeconds, Func<bool> haltPredicate = null) {
            StopTransitionTimer();
            this.haltPredicate = haltPredicate;
            startTick = -1f;
            Pending = fsmQueue.Count == 0;
            stateTimer = Timer.Register(
                Mathf.Max(0.01f, intervalSeconds),
                RunTransitionTick,
                isLooped: true,
                useRealTime: ShouldUseRealTimeTimer());
        }

        /// <summary>
        /// 停止自动推进，并清空队列。
        /// </summary>
        public void StopTransition() {
            CleanQueue();
            StopTransitionTimer();
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
        /// 强制推进到下一个状态。
        /// </summary>
        public void ForceTransition() {
            NextAnimState(true);
        }

        /// <summary>
        /// 读取当前时间，供过渡条件判断使用。
        /// </summary>
        private float GetCurrentTime() {
            switch (timeMode) {
                case FSMTimeMode.ScaledTime:
                    return Time.time;
                case FSMTimeMode.RealtimeSinceStartup:
                    return Time.realtimeSinceStartup;
                case FSMTimeMode.UnscaledTime:
                default:
                    return Time.unscaledTime;
            }
        }

        /// <summary>
        /// 判断轮询计时器是否需要使用真实时间。
        /// </summary>
        private bool ShouldUseRealTimeTimer() {
            return timeMode != FSMTimeMode.ScaledTime;
        }

        /// <summary>
        /// 执行一次过渡轮询，满足条件时推进状态队列。
        /// </summary>
        private void RunTransitionTick() {
            try {
                if (haltPredicate == null || !haltPredicate.Invoke()) {
                    NextAnimState();
                }
            } catch (Exception ex) {
                Debug.LogException(ex);
            }
        }

        /// <summary>
        /// 停止当前的过渡轮询计时器。
        /// </summary>
        private void StopTransitionTimer() {
            if (stateTimer == null) {
                return;
            }

            Timer.Cancel(stateTimer);
            stateTimer = null;
        }

        private readonly List<FSMTransitionItem> fsmQueue = new List<FSMTransitionItem>();
        private readonly FSM fsm;
        private readonly FSMTimeMode timeMode;

        private Timer stateTimer;
        private Func<bool> haltPredicate;
        private float startTick = -1f;
    }
}
