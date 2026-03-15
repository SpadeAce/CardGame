public class Singleton<T> where T : class, new()
{
    private static object _lock = new object();
    private static T _instance = null;
    public static T Instance
    {
        get
        {
            lock (_lock)
            {
                if (_instance == null)
                    _instance = new T();
                return _instance;
            }
        }
    }

    public static bool HasInstance
    {
        get
        {
            return _instance != null;
        }
    }
}
