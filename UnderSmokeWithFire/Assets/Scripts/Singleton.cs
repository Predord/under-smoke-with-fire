using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static object _lock = new object();
    protected static T instance;

    public static T Instance
    {
        get
        {
            object @lock = _lock;
            T instance;
            lock (@lock)
            {
                instance = Singleton<T>.instance;
            }
            return instance;
        }
    }

    protected bool RegisterMe()
    {
        if (Instance == null)
        {
            instance = GetComponent<T>();
            return true;
        }
        else
        {
            Debug.LogWarning("Trying to register a new Singleton " + gameObject.name, gameObject);
            Destroy(gameObject);
            return false;
        }
    }
}
