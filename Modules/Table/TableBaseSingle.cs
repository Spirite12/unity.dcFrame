namespace DCFrame {
    public abstract class TableBaseSingle<T> : TableBase where T : class, new() {

        protected TableBaseSingle() { }

        public static T Instance { get; } = new T();
    }
}
