using System;
using UnityEngine;

/// <summary>
/// Extends BaseStateMachine.cs for static instanced state managers. Supports a basic static instance, singleton,
/// and persistent singleton based on type set in abstract SetInstanceType method.
/// </summary>
public abstract class BaseStateManager<TSelf, EState> :
    BaseStateMachine<EState> 
    where TSelf : BaseStateManager<TSelf, EState>
    where EState : Enum
{
    
    /// <summary>
    /// Instance type for the state manager.
    /// A static instance overrides existing instances,
    /// the singleton destroys new instances,
    /// the persistent singleton extends the singleton to also not destroy itself on scene load.
    /// </summary>
    public enum EInstanceType
    {
        StaticInstance,
        Singleton,
        PersistentSingleton
    }
    
    /// <summary>
    /// The type of instance this state manager will create on Awake.
    /// </summary>
    protected EInstanceType InstanceType = EInstanceType.StaticInstance;
    
    /// <summary>
    /// The current instance of the state manager.
    /// </summary>
    public static TSelf Instance { get; private set; }

    /// <summary>
    /// Adds on top of base Awake to set the instance type and handle the corresponding instance behavior.
    /// </summary>
    protected override void Awake()
    {
        base.Awake();

        SetInstanceType();
        
        if (Instance != null)
        {
            // If the instance type is a singleton or persistent singleton, destroy this new instance
            if (InstanceType == EInstanceType.Singleton || InstanceType == EInstanceType.PersistentSingleton)
            {
                Destroy(gameObject);
                
                if (DebugMode)
                {
                    Debug.LogWarning($"An instance of {GetType().Name} already exists. " +
                                 $"New instance destroyed due to being a {InstanceType}.");
                }
                
                return;
            }
            
            // If the instance type is a static instance, override the existing instance
            Instance = (TSelf) this;
        }
        else
        {
            Instance = (TSelf) this;
        }

        if (InstanceType == EInstanceType.PersistentSingleton)
        {
            DontDestroyOnLoad(gameObject);
        }
    }
    
    /// <summary>
    /// Method to be defined in derived classes that determines the instance type created on Awake.
    /// </summary>
    protected abstract void SetInstanceType();

    /// <summary>
    /// Base method that handles resetting the static instance and destroying the game object.
    ///
    /// Can be added on top of, should not be overridden without calling base in derived classes.
    /// </summary>
    protected virtual void OnApplicationQuit()
    {
        if (DebugMode)
        {
            Debug.Log($"{GetType().Name}: On Application Quit");
        }
        
        // Reset the static instance on application quit
        Instance = null;
        
        Destroy(gameObject);
    }

    /// <summary>
    /// Unity Event called when the MonoBehaviour will be destroyed.
    /// Ensures instance is reset if this instance is destroyed.
    ///
    /// Can be added on top of, should not be overridden without calling base in derived classes.
    /// </summary>
    protected void OnDestroy()
    {
        if (DebugMode)
        {
            Debug.Log($"{GetType().Name}: On Destroy");
        }
        
        // Reset the static instance if this instance is being destroyed
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
