namespace DCFrame {
    public abstract class Singleton<T> where T : class, new() {

        protected Singleton() { }

        public static T Instance { get; } = new T();
    }
}
