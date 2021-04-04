using UnityEngine;

public class Singleton : MonoBehaviour
{
    public static T GetSingleton<T>(GameObject gameObject, T instance) where T : MonoBehaviour
    {
        if (instance != null && instance != gameObject.GetComponent<T>())
        {
            Destroy(gameObject);
        }
        else
        {
            instance = gameObject.GetComponent<T>();
        }
        return instance;
    }
}
