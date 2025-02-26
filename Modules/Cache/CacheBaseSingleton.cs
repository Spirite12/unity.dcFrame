namespace DCFrame {
    public abstract class CacheBaseSingleton<T> : CacheBase where T : class, new() {

        protected CacheBaseSingleton() { }

        public static T Instance { get; } = new T();
    }
}


