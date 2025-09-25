using System;
using UnityEngine;

/// <summary>
/// Extends BaseStateManager.cs to add functionality for listening to ApplicationManager event state changes.
/// Relies on an ApplicationManager being present in the scene.
/// </summary>
public abstract class BaseStateManagerApplicationListener<TSelf, EState> :
    BaseStateManager<TSelf, EState>
    where TSelf : BaseStateManagerApplicationListener<TSelf, EState>
    where EState : Enum
{
    /// <summary>
    /// Flag indicating if an ApplicationManager is present or not.
    /// </summary>
    public bool IsApplicationManager => ApplicationManager.Instance != null;
    
    /// <summary>
    /// Adds on top of base Start to check for an ApplicationManager instance in the scene.
    /// Executes additional logic if found.
    /// </summary>
    protected override void Start()
    {
        base.Start();
        
        if (IsApplicationManager)
        {
            ApplicationManager.Instance.OnBeforeStateChange += OnBeforeApplicationStateChange;
            
            ApplicationManager.Instance.OnAfterStateChange += OnAfterApplicationStateChange;
        }
        else
        {
            Debug.LogWarning($"{GetType().Name}: No ApplicationManager instance found in scene. " +
                             $"Functionality will be limited without.");
        }
    }
    
    /// <summary>
    /// Method to be defined in derived classes to handle logic before the application state changes
    /// if an ApplicationManager is present.
    /// </summary>
    protected abstract void OnBeforeApplicationStateChange(ApplicationManager.EApplicationState toState);
    
    /// <summary>
    /// Method to be defined in derived classes to handle logic after the application state changes
    /// if an ApplicationManager is present.
    /// </summary>
    protected abstract void OnAfterApplicationStateChange(ApplicationManager.EApplicationState toState);
}
