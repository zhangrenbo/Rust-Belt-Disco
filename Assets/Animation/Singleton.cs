using UnityEngine;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// 通用单例基类 - 继承自MonoBehaviour的单例模式
/// 用法：public class MyManager : Singleton<MyManager> { }
/// </summary>
/// <typeparam name="T">继承此类的类型</typeparam>
public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static readonly object _lock = new object();
    private static bool _applicationIsQuitting = false;

    /// <summary>
    /// 单例实例 - 线程安全的访问点
    /// </summary>
    public static T Instance
    {
        get
        {
            // 如果游戏正在退出，不创建新实例
            if (_applicationIsQuitting)
            {
                Debug.LogWarning($"[Singleton] Instance '{typeof(T)}' already destroyed on application quit. Won't create again - returning null.");
                return null;
            }

            lock (_lock)
            {
                if (_instance == null)
                {
                    // 尝试在场景中查找现有实例
                    _instance = Object.FindObjectOfType<T>(); // 修复1: 使用 Object.FindObjectOfType

                    // 如果场景中没有，创建新实例
                    if (_instance == null)
                    {
                        GameObject singleton = new GameObject();
                        _instance = singleton.AddComponent<T>();
                        singleton.name = $"(Singleton) {typeof(T).Name}";

                        // 防止场景切换时被销毁
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
    /// 检查单例是否已经存在（不会触发创建）
    /// </summary>
    public static bool HasInstance => _instance != null && !_applicationIsQuitting;

    /// <summary>
    /// 安全获取实例（如果不存在返回null，不会创建新实例）
    /// </summary>
    public static T GetInstanceIfExists()
    {
        return _applicationIsQuitting ? null : _instance;
    }

    /// <summary>
    /// 虚拟Awake方法，子类可以重写
    /// 注意：子类重写时必须调用 base.Awake()
    /// </summary>
    protected virtual void Awake()
    {
        // 确保只有一个实例存在
        if (_instance == null)
        {
            _instance = this as T;

            // 如果这个GameObject还没有设置DontDestroyOnLoad，则设置
            if (gameObject.scene.name != "DontDestroyOnLoad")
            {
                DontDestroyOnLoad(gameObject);
            }

            Debug.Log($"[Singleton] {typeof(T).Name} instance initialized: {gameObject.name}");
        }
        else if (_instance != this)
        {
            Debug.LogWarning($"[Singleton] Another instance of {typeof(T)} already exists: {_instance.gameObject.name}. Destroying this one: {gameObject.name}");

            // 如果是在运行时创建的重复实例，立即销毁
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                // 编辑器模式下使用DestroyImmediate
                DestroyImmediate(gameObject);
            }
        }
    }

    /// <summary>
    /// 游戏退出时调用
    /// </summary>
    protected virtual void OnApplicationQuit()
    {
        _applicationIsQuitting = true;
    }

    /// <summary>
    /// 当对象被销毁时调用
    /// </summary>
    protected virtual void OnDestroy()
    {
        // 只有当前实例被销毁时才清除静态引用
        if (_instance == this)
        {
            _instance = null;
        }
    }

    /// <summary>
    /// 手动销毁单例实例
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
    /// 强制重新创建实例（慎重使用）
    /// </summary>
    public static void RecreateInstance()
    {
        DestroyInstance();
        // 下次访问Instance时会自动创建新实例
    }
}

/// <summary>
/// 简化版单例基类 - 不自动创建实例
/// 仅用于需要手动创建的单例对象
/// </summary>
/// <typeparam name="T">继承此类的类型</typeparam>
public abstract class SimpleSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;

    /// <summary>
    /// 单例实例 - 仅返回已存在的实例，不会自动创建
    /// </summary>
    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Object.FindObjectOfType<T>(); // 修复1: 使用 Object.FindObjectOfType
            }
            return _instance;
        }
    }

    /// <summary>
    /// 检查单例是否存在
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
/// 可持久化单例基类 - 支持场景切换时持续存在
/// </summary>
/// <typeparam name="T">继承此类的类型</typeparam>
public abstract class PersistentSingleton<T> : Singleton<T> where T : MonoBehaviour
{
    /// <summary>
    /// 是否在场景切换时持续单例
    /// </summary>
    [Header("=== Persistent Singleton Settings ===")]
    [Tooltip("是否在场景切换时持续此单例实例")]
    public bool persistAcrossScenes = true;

    protected override void Awake()
    {
        // 如果设置为不持久化，移除DontDestroyOnLoad
        if (!persistAcrossScenes && gameObject.scene.name == "DontDestroyOnLoad")
        {
            // 将对象移回当前场景
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(gameObject, UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        base.Awake();
    }

    /// <summary>
    /// 设置是否持久化
    /// </summary>
    /// <param name="persistent">是否持久化</param>
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
/// 单例工具类 - 提供便利的单例管理方法
/// </summary>
public static class SingletonUtility
{
    /// <summary>
    /// 获取所有活跃的单例实例信息
    /// </summary>
    public static void LogAllSingletons()
    {
        Debug.Log("=== Active Singletons ===");

        var allMonoBehaviours = Object.FindObjectsOfType<MonoBehaviour>(); // 修复1: 使用 Object.FindObjectsOfType
        foreach (var mb in allMonoBehaviours)
        {
            var type = mb.GetType();
            // 检查是否继承自某个单例基类
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
    /// 清除所有DontDestroyOnLoad的单例对象（用于场景重置）
    /// </summary>
    public static void ClearAllPersistentSingletons()
    {
        // 修复2和3: 手动实现Where和ToArray功能，避免使用LINQ
        var allGameObjects = Object.FindObjectsOfType<GameObject>();
        var dontDestroyObjects = new List<GameObject>();

        // 查找所有DontDestroyOnLoad的对象
        foreach (var go in allGameObjects)
        {
            if (go.scene.name == "DontDestroyOnLoad")
            {
                dontDestroyObjects.Add(go);
            }
        }

        // 销毁这些对象
        foreach (var obj in dontDestroyObjects)
        {
            if (obj.GetComponent<MonoBehaviour>() != null)
            {
                Debug.Log($"Destroying persistent object: {obj.name}");
                Object.Destroy(obj); // 修复1: 使用 Object.Destroy
            }
        }
    }
}