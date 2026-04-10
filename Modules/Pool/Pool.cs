using System;
using System.Collections.Generic;

namespace DCFrame {
    /// <summary>
    /// 通用泛型对象池。
    /// 负责对象创建、回收和缓存管理，不关心具体业务逻辑。
    /// </summary>
    public class Pool<T> {
        /// <summary>
        /// 创建一个对象池。
        /// </summary>
        public Pool(Func<T> createFunc, Action<T> actionOnGet = null, Action<T> actionOnRecycle = null, Action<T> actionOnDestroy = null) {
            this.createFunc = createFunc ?? throw new ArgumentNullException(nameof(createFunc));
            this.actionOnGet = actionOnGet;
            this.actionOnRecycle = actionOnRecycle;
            this.actionOnDestroy = actionOnDestroy;
        }

        /// <summary>
        /// 直接创建一个新对象。
        /// </summary>
        public T Create() {
            return createFunc.Invoke();
        }

        /// <summary>
        /// 从对象池中取出一个对象。
        /// </summary>
        public T Get() {
            T element = cacheStack.Count == 0 ? Create() : cacheStack.Pop();
            if (element is null) {
                return default;
            }
            actionOnGet?.Invoke(element);
            return element;
        }

        /// <summary>
        /// 回收对象到对象池中。
        /// </summary>
        public void Recycle(T element) {
            if (cacheStack.Count > 0 && !typeof(T).IsValueType && ReferenceEquals(cacheStack.Peek(), element)) {
                throw new InvalidOperationException("回收对象失败，重复回收了同一个对象。");
            }

            actionOnRecycle?.Invoke(element);
            cacheStack.Push(element);
        }

        /// <summary>
        /// 销毁一个不再进入池子的对象。
        /// </summary>
        public void Destroy(T element) {
            actionOnDestroy?.Invoke(element);
        }

        /// <summary>
        /// 移除并清理缓存中的无效对象。
        /// </summary>
        public void RemoveInvalid(Predicate<T> match) {
            if (match == null || cacheStack.Count == 0) {
                return;
            }

            Stack<T> tempStack = new Stack<T>(cacheStack.Count);
            while (cacheStack.Count > 0) {
                T element = cacheStack.Pop();
                if (match.Invoke(element)) {
                    actionOnDestroy?.Invoke(element);
                    continue;
                }

                tempStack.Push(element);
            }

            while (tempStack.Count > 0) {
                cacheStack.Push(tempStack.Pop());
            }
        }

        /// <summary>
        /// 清空当前缓存中的所有对象。
        /// </summary>
        public void Clear() {
            while (cacheStack.Count > 0) {
                actionOnDestroy?.Invoke(cacheStack.Pop());
            }
        }

        /// <summary>
        /// 当前缓存中的对象数量。
        /// </summary>
        public int InactiveCount => cacheStack.Count;

        private readonly Stack<T> cacheStack = new();
        private readonly Func<T> createFunc;
        private readonly Action<T> actionOnGet;
        private readonly Action<T> actionOnRecycle;
        private readonly Action<T> actionOnDestroy;
    }
}
