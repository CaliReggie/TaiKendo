using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// General application state machine manager. SPE (Single Point of Entry) for application logic and state management.
/// </summary>
public class ApplicationManager : BaseStateManager<ApplicationManager, ApplicationManager.EApplicationState>
{
    /// <summary>
    /// Super states the application can be in.
    /// </summary>
    public enum EApplicationState
    {
        LoadingScene,
        Running,
        AlternateRunning,
        ExitingScene,
        Closing
    }
    
    [Header("Application State Manager Settings")]
    
    [Tooltip("Houses most all ApplicationManager logic and references as a bridge between the manager and states")]
    [SerializeField] private  ApplicationManagerContext context;
    
    /// <summary>
    /// Read only property for current scene SO (houses information and settings).
    /// </summary>
    public SceneSO ActiveSceneSO => context.activeSceneSO; 

    #region BaseMethods
    
    protected override void SetInstanceType()
    {
        InstanceType = EInstanceType.PersistentSingleton;
    }
    
    protected override void Start()
    {
        // delaying start to ensure all other managers or listeners are fully awake and enabled
        StartCoroutine(DelayedStart());
    }
    
    private IEnumerator DelayedStart()
    {
        yield return null; // Wait one frame to ensure all Awake/enable calls are done (possible issue in future?)

        base.Start();
    }
    
    protected override void Initialize()
    {
        // ensure scene so is set and valid
        if (context.targetSceneSO == null ||
            !SceneSO.IsValidScene(context.targetSceneSO))
        {
            Debug.LogError("TargetSceneSO is not set or invalid on initial Start. " +
                           "Please assign the valid SceneSO in the ApplicationManager context in Editor pre-runtime." +
                           "Destroying ApplicationManager Instance.");
            
            Destroy(gameObject);
            
            return;
        }
        
        States = context.ContextInitialize(this);
    }
    
    #endregion

    #region PublicMethods
    
    /// <summary>
    /// Takes in a Scene SO and attempts to begin exiting the current scene and loading the new one if valid.
    /// </summary>
    public void TryLoadNewScene(SceneSO targetSceneSO)
    {
        if (targetSceneSO == null) { Debug.LogWarning("Target SceneSO is null, cannot load new scene."); return; }
        
        if (DebugMode) { Debug.Log($"Trying to load scene: {targetSceneSO.TryGetScenePathAsName()}"); }
        
        //Has to have valid path and not be active scene
        if (SceneSO.IsValidScene(targetSceneSO) && !SceneSO.IsActiveScene(targetSceneSO))
        {
            context.LoadNewScene(targetSceneSO);
        }
        else
        {
            Debug.LogWarning($"Cannot currently switch to SceneSO: {targetSceneSO.name}.");
        }
    }
    
    /// <summary>
    /// Call when application is to be closed.
    /// </summary>
    public void Quit()
    {
        if (DebugMode) { Debug.Log("Quit called in ApplicationManager."); }
        
        ChangeState(EApplicationState.Closing);
    }
    
    /// <summary>
    /// Call to toggle between running and alternate running states. IE: Playing v Paused in game.
    /// (Currently, there is only one alternate state, more states could be added, logic would have to be modified).
    /// </summary>
    public void ToggleRunningState()
    {
        EApplicationState currentState = CurrentState.State;
        
        if (currentState == EApplicationState.Running)
        {
            ChangeState(EApplicationState.AlternateRunning);
        }
        else if (currentState == EApplicationState.AlternateRunning)
        {
            ChangeState(EApplicationState.Running);
        }else
        {
            Debug.LogWarning($"Cannot toggle running state from current state: {currentState}");
        }
    }

    #endregion
    
    /// <summary>
    /// Public method to request change game state. Internal denials can happen.
    /// </summary>
    public void RequestChangeState(EApplicationState toState)
    {
        ChangeState(toState);
    }
    
    [Serializable] 
    public class ApplicationManagerContext : BaseStateMachineContext
    {
        #region Context Declarations
        
        [Header("Inscribed References")]
        
        [Tooltip("SceneSO to load with on init; also set by self when loading a new scene. " +
                 "SET IN INSPECTOR as the corresponding sceneSO that this manager is starting in.")]
        public SceneSO targetSceneSO;
        
        [Header("Dynamic References - Don't Modify in Inspector")]
            
        [Tooltip("Current ApplicationManager instance, set by self on init.")]
        public ApplicationManager applicationManager;
        
        [Tooltip("SceneSO currently being managed by the AppManager, set by self on init or when loading new scene.")]
        public SceneSO activeSceneSO;
        
        #endregion
        
        #region BaseMethods
        
        protected override Dictionary<EApplicationState, EApplicationState[]> StatesDict()
        {
            return new Dictionary<EApplicationState, EApplicationState[]>
            {
                { EApplicationState.LoadingScene, new [] { EApplicationState.AlternateRunning } }, // Cannot go to menu from loading scene, must be running first
                { EApplicationState.Running, new [] { EApplicationState.LoadingScene } }, // Cannot go to loading new scene from running, must exit first
                { EApplicationState.AlternateRunning, new [] { EApplicationState.LoadingScene } }, // Cannot go to loading new scene from menu, must exit first
                { EApplicationState.ExitingScene, new [] { EApplicationState.Running, EApplicationState.AlternateRunning } }, // Cannot go to running or menu from exiting scene, must load first
                { EApplicationState.Closing, new [] { EApplicationState.LoadingScene, EApplicationState.Running, EApplicationState.AlternateRunning, EApplicationState.ExitingScene } } // Cannot go anywhere else from closing
            };
        }
        
        protected override Dictionary<EApplicationState, BaseState<EApplicationState>> InitializedStates()
        {
            Dictionary<EApplicationState, BaseState<EApplicationState>> states = 
                new Dictionary<EApplicationState, BaseState<EApplicationState>>();
            
            foreach (var state in StatesDict())
            {
                switch (state.Key)
                {
                    case EApplicationState.LoadingScene:
                        states.Add(state.Key, new ApplicationManagerLoadingScene(this, state.Key, state.Value));
                        break;
                    case EApplicationState.Running:
                        states.Add(state.Key, new ApplicationManagerRunning(this, state.Key, state.Value));
                        break;
                    case EApplicationState.AlternateRunning:
                        states.Add(state.Key, new ApplicationManagerAlternateRunning(this, state.Key, state.Value));
                        break;
                    case EApplicationState.ExitingScene:
                        states.Add(state.Key, new ApplicationManagerExitingScene(this, state.Key, state.Value));
                        break;
                    case EApplicationState.Closing:
                        states.Add(state.Key, new ApplicationManagerClosing(this, state.Key, state.Value));
                        break;
                }
            }
            
            return states;
            
        }
        
        public override Dictionary<EApplicationState, BaseState<EApplicationState>> 
                        ContextInitialize(BaseStateMachine<EApplicationState> targetStateMachine)
        {
            applicationManager = (ApplicationManager) targetStateMachine; // Set the application manager reference

            return InitializedStates();
        }
        
        public override void ContextCallChangeState(EApplicationState newState)
        {
            applicationManager.ChangeState(newState);
        }

        #endregion

        #region SceneMethods
        
        /// <summary>
        /// Sets the current scene SO to the one passed in, but does not initiate a load, state, or scene change.
        /// </summary>
        public void SetActiveSceneSO(SceneSO sceneSO)
        {
            if (!SceneSO.IsValidScene(sceneSO))
            {
                Debug.LogWarning($"Unable to set current sceneSO {sceneSO.name}.");
                
                return;
            }
            
            if (applicationManager.DebugMode) { Debug.Log($"Set activeSceneSO to: {sceneSO.TryGetScenePathAsName()} " +
                                                          $"in {applicationManager.GetType().Name}"); }
            
            activeSceneSO = sceneSO;
        }
        
        /// <summary>
        /// Sets the target scene SO to the one passed in, but does not initiate a load, state, or scene change.
        /// </summary>
        public void SetTargetSceneSO(SceneSO sceneSO)
        {
            if (!SceneSO.IsValidScene(sceneSO))
            {
                Debug.LogWarning($"Unable to set target sceneSO {sceneSO.name}.");
                
                return;
            }
            
            if (applicationManager.DebugMode) { Debug.Log($"Set targetSceneSO to: {sceneSO.TryGetScenePathAsName()} " +
                                                          $"in {applicationManager.GetType().Name}"); }
            
            targetSceneSO = sceneSO;
        }
        
        /// <summary>
        /// Attempts to set a target scene and change state to exiting scene if valid.
        /// </summary>
        public void LoadNewScene(SceneSO newSceneSO)
        {
            if (!applicationManager.CanChangeToState(EApplicationState.ExitingScene)) return;
            
            if (!SceneSO.IsValidScene(newSceneSO)) { return;}
            
            if (applicationManager.DebugMode) { Debug.Log($"Exiting to scene: {newSceneSO.TryGetScenePathAsName()}"); }

            SetTargetSceneSO(newSceneSO);
            
            ContextCallChangeState(EApplicationState.ExitingScene);
        }

        #endregion
    }
    
    public abstract class ApplicationManagerState : BaseState<EApplicationState>
    { 
        protected ApplicationManagerState(ApplicationManagerContext context, 
            EApplicationState key, 
            EApplicationState[] invalidTransitions) 
            : base(key, invalidTransitions)
        {
            Context = context;
        }
        
        protected ApplicationManagerContext Context { get; }
    }
}
