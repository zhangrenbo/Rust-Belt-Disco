using UnityEngine;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// ͨ�õ������� - �̳���MonoBehaviour�ĵ���ģʽ
/// �÷���public class MyManager : Singleton<MyManager> { }
/// </summary>
/// <typeparam name="T">�̳д��������</typeparam>
public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static readonly object _lock = new object();
    private static bool _applicationIsQuitting = false;

    /// <summary>
    /// ����ʵ�� - �̰߳�ȫ�ķ��ʵ�
    /// </summary>
    public static T Instance
    {
        get
        {
            // �����Ϸ�����˳�����������ʵ��
            if (_applicationIsQuitting)
            {
                Debug.LogWarning($"[Singleton] Instance '{typeof(T)}' already destroyed on application quit. Won't create again - returning null.");
                return null;
            }

            lock (_lock)
            {
                if (_instance == null)
                {
                    // �����ڳ����в�������ʵ��
                    _instance = Object.FindObjectOfType<T>(); // �޸�1: ʹ�� Object.FindObjectOfType

                    // ���������û�У�������ʵ��
                    if (_instance == null)
                    {
                        GameObject singleton = new GameObject();
                        _instance = singleton.AddComponent<T>();
                        singleton.name = $"(Singleton) {typeof(T).Name}";

                        // ��ֹ�����л�ʱ������
                        DontDestroyOnLoad(singleton);

                        Debug.Log($"[Singleton] An instance of {typeof(T)} is needed in the scene, so '{singleton}' was created with DontDestroyOnLoad.");
                    }
                    else
                    {
                        Debug.Log($"[Singleton] Using instance already created: {_instance.gameObject.name}");
                    }
                }

                return _instance;
            }
        }
    }

    /// <summary>
    /// ��鵥���Ƿ��Ѿ����ڣ����ᴥ��������
    /// </summary>
    public static bool HasInstance => _instance != null && !_applicationIsQuitting;

    /// <summary>
    /// ��ȫ��ȡʵ������������ڷ���null�����ᴴ����ʵ����
    /// </summary>
    public static T GetInstanceIfExists()
    {
        return _applicationIsQuitting ? null : _instance;
    }

    /// <summary>
    /// ����Awake���������������д
    /// ע�⣺������дʱ������� base.Awake()
    /// </summary>
    protected virtual void Awake()
    {
        // ȷ��ֻ��һ��ʵ������
        if (_instance == null)
        {
            _instance = this as T;

            // ������GameObject��û������DontDestroyOnLoad��������
            if (gameObject.scene.name != "DontDestroyOnLoad")
            {
                DontDestroyOnLoad(gameObject);
            }

            Debug.Log($"[Singleton] {typeof(T).Name} instance initialized: {gameObject.name}");
        }
        else if (_instance != this)
        {
            Debug.LogWarning($"[Singleton] Another instance of {typeof(T)} already exists: {_instance.gameObject.name}. Destroying this one: {gameObject.name}");

            // �����������ʱ�������ظ�ʵ������������
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                // �༭��ģʽ��ʹ��DestroyImmediate
                DestroyImmediate(gameObject);
            }
        }
    }

    /// <summary>
    /// ��Ϸ�˳�ʱ����
    /// </summary>
    protected virtual void OnApplicationQuit()
    {
        _applicationIsQuitting = true;
    }

    /// <summary>
    /// ����������ʱ����
    /// </summary>
    protected virtual void OnDestroy()
    {
        // ֻ�е�ǰʵ��������ʱ�������̬����
        if (_instance == this)
        {
            _instance = null;
        }
    }

    /// <summary>
    /// �ֶ����ٵ���ʵ��
    /// </summary>
    public static void DestroyInstance()
    {
        if (_instance != null)
        {
            if (Application.isPlaying)
            {
                Destroy(_instance.gameObject);
            }
            else
            {
                DestroyImmediate(_instance.gameObject);
            }
            _instance = null;
        }
    }

    /// <summary>
    /// ǿ�����´���ʵ��������ʹ�ã�
    /// </summary>
    public static void RecreateInstance()
    {
        DestroyInstance();
        // �´η���Instanceʱ���Զ�������ʵ��
    }
}

/// <summary>
/// �򻯰浥������ - ���Զ�����ʵ��
/// ��������Ҫ�ֶ������ĵ�������
/// </summary>
/// <typeparam name="T">�̳д��������</typeparam>
public abstract class SimpleSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;

    /// <summary>
    /// ����ʵ�� - �������Ѵ��ڵ�ʵ���������Զ�����
    /// </summary>
    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Object.FindObjectOfType<T>(); // �޸�1: ʹ�� Object.FindObjectOfType
            }
            return _instance;
        }
    }

    /// <summary>
    /// ��鵥���Ƿ����
    /// </summary>
    public static bool HasInstance => _instance != null;

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Debug.LogWarning($"[SimpleSingleton] Multiple instances of {typeof(T)} found. Destroying duplicate: {gameObject.name}");
            Destroy(gameObject);
        }
    }

    protected virtual void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}

/// <summary>
/// �ɳ־û��������� - ֧�ֳ����л�ʱ��������
/// </summary>
/// <typeparam name="T">�̳д��������</typeparam>
public abstract class PersistentSingleton<T> : Singleton<T> where T : MonoBehaviour
{
    /// <summary>
    /// �Ƿ��ڳ����л�ʱ��������
    /// </summary>
    [Header("=== Persistent Singleton Settings ===")]
    [Tooltip("�Ƿ��ڳ����л�ʱ�����˵���ʵ��")]
    public bool persistAcrossScenes = true;

    protected override void Awake()
    {
        // �������Ϊ���־û����Ƴ�DontDestroyOnLoad
        if (!persistAcrossScenes && gameObject.scene.name == "DontDestroyOnLoad")
        {
            // �������ƻص�ǰ����
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(gameObject, UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        base.Awake();
    }

    /// <summary>
    /// �����Ƿ�־û�
    /// </summary>
    /// <param name="persistent">�Ƿ�־û�</param>
    public void SetPersistent(bool persistent)
    {
        persistAcrossScenes = persistent;

        if (persistent && gameObject.scene.name != "DontDestroyOnLoad")
        {
            DontDestroyOnLoad(gameObject);
        }
        else if (!persistent && gameObject.scene.name == "DontDestroyOnLoad")
        {
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(gameObject, UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }
    }
}

/// <summary>
/// ���������� - �ṩ�����ĵ����������
/// </summary>
public static class SingletonUtility
{
    /// <summary>
    /// ��ȡ���л�Ծ�ĵ���ʵ����Ϣ
    /// </summary>
    public static void LogAllSingletons()
    {
        Debug.Log("=== Active Singletons ===");

        var allMonoBehaviours = Object.FindObjectsOfType<MonoBehaviour>(); // �޸�1: ʹ�� Object.FindObjectsOfType
        foreach (var mb in allMonoBehaviours)
        {
            var type = mb.GetType();
            // ����Ƿ�̳���ĳ����������
            while (type != null)
            {
                if (type.IsGenericType &&
                    (type.GetGenericTypeDefinition() == typeof(Singleton<>) ||
                     type.GetGenericTypeDefinition() == typeof(SimpleSingleton<>) ||
                     type.GetGenericTypeDefinition() == typeof(PersistentSingleton<>)))
                {
                    Debug.Log($"Singleton found: {mb.GetType().Name} on {mb.gameObject.name}");
                    break;
                }
                type = type.BaseType;
            }
        }
    }

    /// <summary>
    /// �������DontDestroyOnLoad�ĵ����������ڳ������ã�
    /// </summary>
    public static void ClearAllPersistentSingletons()
    {
        // �޸�2��3: �ֶ�ʵ��Where��ToArray���ܣ�����ʹ��LINQ
        var allGameObjects = Object.FindObjectsOfType<GameObject>();
        var dontDestroyObjects = new List<GameObject>();

        // ��������DontDestroyOnLoad�Ķ���
        foreach (var go in allGameObjects)
        {
            if (go.scene.name == "DontDestroyOnLoad")
            {
                dontDestroyObjects.Add(go);
            }
        }

        // ������Щ����
        foreach (var obj in dontDestroyObjects)
        {
            if (obj.GetComponent<MonoBehaviour>() != null)
            {
                Debug.Log($"Destroying persistent object: {obj.name}");
                Object.Destroy(obj); // �޸�1: ʹ�� Object.Destroy
            }
        }
    }
}