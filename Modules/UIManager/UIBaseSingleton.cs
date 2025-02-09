namespace DCFrame {
    public abstract class UIBaseSingleton<T> : UIBase where T : class, new() {

        protected UIBaseSingleton() { }

        public static T Instance { get; } = new T();
    }
}