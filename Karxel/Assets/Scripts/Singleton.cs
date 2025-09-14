using System;
using Mirror;
using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance = null!;

    public static T Instance
    {
        get
        {
            if (_instance != null) return _instance;
            
            _instance = FindAnyObjectByType<T>();
            return _instance;
        }
    }

    protected virtual void Awake()
    {
        if (_instance == null) _instance = this as T;
        else if (_instance != this)
        {
            Debug.LogError($"[Singleton] Found multiple instances of {typeof(T)}. Destroying self.");
            Destroy(gameObject);
        }
    }
}

public abstract class NetworkSingleton<T> : NetworkBehaviour where T : NetworkBehaviour
{
    private static T? _instance;

    public static event Action? OnReady;

    /// <summary>
    ///     Singular instance of this class within the whole loaded context.<br/>Singletons of <see cref="NetworkBehaviour"/>s
    ///     are only valid inside Start() or any other function after
    /// </summary>
    public static T? Instance
    {
        get
        {
            if (_instance != null) return _instance;
            
            _instance = FindAnyObjectByType<T>();
            return _instance;
        }
    }

    protected virtual void Awake()
    {
        if (_instance == null) _instance = this as T;
        else if (_instance != this)
        {
            Debug.LogError($"[Singleton] Found multiple instances of {typeof(T)}. Destroying self.");
            Destroy(gameObject);
        }
        
        OnReady?.Invoke();
    }
}