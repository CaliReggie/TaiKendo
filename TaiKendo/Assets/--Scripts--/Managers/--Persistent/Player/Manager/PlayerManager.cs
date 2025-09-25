using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInputManager))]
public class PlayerManager : BaseStateManagerApplicationListener<PlayerManager, PlayerManager.EPlayerManagementState>
{
    public enum EPlayerManagementState
    {
        Default,
        InsufficientPlayers,
        ExcessPlayers
    }
    
    [SerializeField] private PlayerManagerContext context;
    
    /// <summary>
    /// Event to subscribe to receive notifications after the target player state changes.
    /// </summary>
    public event Action<PlayerInputObject.EPlayerInputObjectState> OnAfterTargetPlayerStateChange;
    
    /// <summary>
    /// The current target state for all players to be in.
    /// </summary>
    public PlayerInputObject.EPlayerInputObjectState TargetPlayerState => context.targetPlayerState;
    
    /// <summary>
    /// True if player cursor navigation should be limited to player screen space specific portions.
    /// Otherwise, can traverse whole main canvas. 
    /// </summary>
    public bool TargetIsPerPlayerNavigation => context.targetIsPerPlayerNavigation;
    
    /// <summary>
    /// The Canvas that player cursors will be parented to when managed by PlayerManager and using cursors.
    /// </summary>
    public Canvas CursorsCanvas => context.cursorsCanvas;

    #region BaseMethods

    protected override void SetInstanceType()
    {
        InstanceType = EInstanceType.PersistentSingleton;
    }

    protected override void Initialize()
    {
        States = context.ContextInitialize(this);
    }
    
    protected override void OnBeforeApplicationStateChange(ApplicationManager.EApplicationState toState)
    {
        if (DebugMode) 
        {
            Debug.Log($"PlayerManager: OnBeforeApplicationStateChange called with state: {toState}");
        }
    }
    
    protected override void OnAfterApplicationStateChange(ApplicationManager.EApplicationState toState)
    {
        if (DebugMode) 
        {
            Debug.Log($"PlayerManager: OnAfterApplicationStateChange called with state: {toState}");
        }
        
        switch (toState)
        {
            case ApplicationManager.EApplicationState.Running:
                
                // if just came from loading scene set target players from ActiveSceneSO
                if ((ApplicationManager.Instance.PreviousState.State ==
                ApplicationManager.EApplicationState.LoadingScene))
                {
                    context.SetTargetPlayers(ApplicationManager.Instance.ActiveSceneSO.TargetPlayers);
                }
                
                // set target player state from ActiveSceneSO
                context.SetTargetPlayerState(ApplicationManager.Instance.ActiveSceneSO.TargetPlayerStateInRunning);
                
                // determine cursor navigation limitation from ActiveSceneSO
                context.targetIsPerPlayerNavigation =
                    ApplicationManager.Instance.ActiveSceneSO.PerPlayerNavigationInRunning;
                
                break;
            case ApplicationManager.EApplicationState.AlternateRunning:
                
                // set target player state from ActiveSceneSO
                context.SetTargetPlayerState(ApplicationManager.Instance.ActiveSceneSO.TargetPlayerStateInAlternateRunning);
                
                // determine cursor navigation limitation from ActiveSceneSO
                context.targetIsPerPlayerNavigation =
                    ApplicationManager.Instance.ActiveSceneSO.PerPlayerNavigationInAlternateRunning;
                
                break;
            
            case ApplicationManager.EApplicationState.ExitingScene:

                // set managed players inactive when exiting scene
                context.SetTargetPlayerState(PlayerInputObject.EPlayerInputObjectState.Inactive);
                
                break;
        }
    }

    #endregion

    #region PublicMethods
    
    /// <summary>
    /// Way to publicly change target player count during runtime
    /// </summary>
    public void SetTargetPlayers(int targetPlayers)
    {
        context.SetTargetPlayers(targetPlayers);
    }

    #region PlayerCalledMethods
    
    /// <summary>
    /// Takes an playerInputObject and adds its CursorPioComponent CursorInstance to internal childed list. From there it is
    /// kept as a child till the same playerInputObject removes its cursor. 
    /// </summary>
    public void PlayerRequestAddCursor(PlayerInputObject playerInputObject)
    {
        context.AddPlayersCursor(playerInputObject);
    }
    
    /// <summary>
    /// Takes an playerInputObject and removes its CursorPioComponent CursorInstance from internal childed list.
    /// From there it is returned as child to playerInputObject.
    /// </summary>
    public void PlayerRequestRemoveCursor(PlayerInputObject playerInputObject)
    {
        context.RemovePlayersCursor(playerInputObject);
    }
    
    /// <summary>
    /// Called by players attempting to switch major target state. (Think Play / Pause).
    /// Logic is variable.
    /// With an App Manager, it will request a toggle of the Application State and in turn respond to that change.
    /// Without, it will toggle between default values set in the inspector.
    /// </summary>
    public void PlayerRequestToggleTargetPlayerState()
    {
        if (IsApplicationManager)
        {
            ApplicationManager.Instance.ToggleRunningState();
        }
        else
        {
            PlayerInputObject.EPlayerInputObjectState currentTargetState = context.targetPlayerState;
            
            PlayerInputObject.EPlayerInputObjectState defaultState = context.defaultPlayerState;
            
            PlayerInputObject.EPlayerInputObjectState defaultAlternateState = context.defaultAlternatePlayerState;
            
            // swapping from default to alternate
            if (currentTargetState == defaultState)
            {
                context.SetTargetPlayerState(defaultAlternateState);
            }
            // swapping from alternate to default
            else if (currentTargetState == defaultAlternateState)
            {
                context.SetTargetPlayerState(defaultState);
            }
            // swapping from something else to default
            else
            {
                context.SetTargetPlayerState(defaultState);
            }
        }
    }

    #endregion
    
    #region PlayerInputManagerBroadcastMethods

    /// <summary>
    /// Message sent by PlayerInputManager when a new player joins (manually or automatically)
    /// </summary>
    /// <param name="playerInput"></param> The PlayerInput component of the joined player,
    /// should also have a PlayerInputObject component on the same gameobject to be added to context.
    public void OnPlayerJoined(PlayerInput playerInput)
    {
        context.AddJoinedPlayer(playerInput);
    }
    
    /// <summary>
    /// Message sent by PlayerInputManager when a player leaves
    /// (PlayerInput component/gameobject is destroyed/removed/disabled, manually or automatically)
    /// </summary>
    /// <param name="playerInput"></param> The PlayerInput component of the player that left, will be removed from context
    /// by finding associated "PlayerInputObject" if it exists. (It likely won't as this is happening after destruction)
    public void OnPlayerLeft(PlayerInput playerInput)
    {
        if (context.GetPlayer(playerInput) != null) 
        {
            context.RemovePlayer(playerInput);
        }
    }

    #endregion
    
    #endregion
    
    [Serializable]
    public class PlayerManagerContext : BaseStateMachineContext
    {
        #region ContextDeclarations
        
        public const int MaxPlayers = 4;
        
        [Header("Inscribed References")]
        
        public GameObject playerInputObjectPrefab;
        
        public Transform playersParent;
        
        public Canvas cursorsCanvas;
        
        public Transform cursorsParent;

        [Header("Inscribed Settings")]
        
        [Tooltip("The default target number of players set at start if no ApplicationManager and ActiveSceneSO found.")]
        [Range(1,4)] public int defaultTargetPlayers = 1;
        
        [Tooltip("The default target player state for all players to be in.")]
        public PlayerInputObject.EPlayerInputObjectState defaultPlayerState =
            PlayerInputObject.EPlayerInputObjectState.Player;
        
        [Tooltip("The default alternate target player state for all players to be in")]
        public PlayerInputObject.EPlayerInputObjectState defaultAlternatePlayerState =
            PlayerInputObject.EPlayerInputObjectState.PlayerUI;
        
        public bool defaultIsPerPlayerNavigation;
        
        [Header("Dynamic References - Don't Modify In Inspector")]
        
        public PlayerManager playerManager;
        
        public PlayerInputManager inputManagerComponent;

        public List<PlayerInputObject> playerInputObjects;
        
        public Dictionary<PlayerInputObject, Transform> PlayersCursors;
        
        [Header("Dynamic Settings - Don't Modify In Inspector")]
        
        [Range(1,4)] public int targetPlayers = 1;

        [Tooltip("The current target state for all players to be in.")]
        public PlayerInputObject.EPlayerInputObjectState targetPlayerState;
        
        [Tooltip("If true, player cursors will only move on their respective canvas space. " +
                 "If false, player cursors will be able to traverse the whole canvas.")]
        public bool targetIsPerPlayerNavigation;
        
        /// <summary>
        /// The current number of existing players, cleans null entries whenever queried. Should ALWAYS use this
        /// when needing to know current player count.
        /// </summary>
        public int NumPlayers
        {
            get
            {
                playerInputObjects.RemoveAll(playerInputObject => playerInputObject == null);
                
                return playerInputObjects.Count;
            }
        }
        
        public bool NeedMorePlayers => NumPlayers < targetPlayers;
    
        public bool NeedLessPlayers => NumPlayers > targetPlayers;
        
        #endregion
       
        #region BaseMethods

        protected override Dictionary<EPlayerManagementState, EPlayerManagementState[]> StatesDict()
        {
            return new Dictionary<EPlayerManagementState, EPlayerManagementState[]>
            {
                { EPlayerManagementState.Default, new EPlayerManagementState[] {}}, // No invalid transitions from Default
                { EPlayerManagementState.InsufficientPlayers, new EPlayerManagementState[] {} }, // No invalid transitions from InsufficientPlayers
                { EPlayerManagementState.ExcessPlayers, new EPlayerManagementState[] {} } // No invalid transitions from ExcessPlayers
            };
        }

        protected override Dictionary<EPlayerManagementState, BaseState<EPlayerManagementState>> InitializedStates()
        {
            Dictionary<EPlayerManagementState, BaseState<EPlayerManagementState>> states =
                new Dictionary<EPlayerManagementState, BaseState<EPlayerManagementState>>();

            foreach (var state in StatesDict())
            {
                switch (state.Key)
                {
                    case EPlayerManagementState.Default:
                        states.Add(state.Key, new PlayerManagerDefault(this, state.Key, state.Value));
                        break;
                    case EPlayerManagementState.InsufficientPlayers:
                        states.Add(state.Key, new PlayerManagerAddingInput(this, state.Key, state.Value));
                        break;
                    case EPlayerManagementState.ExcessPlayers:
                        states.Add(state.Key, new PlayerManagerRemovingInput(this, state.Key, state.Value));
                        break;
                }
            }
            
            return states;
        }

        public override Dictionary<EPlayerManagementState, BaseState<EPlayerManagementState>>
            ContextInitialize(BaseStateMachine<EPlayerManagementState> targetStateMachine)
        {
            playerManager = (PlayerManager) targetStateMachine;
            
            inputManagerComponent = playerManager.GetComponent<PlayerInputManager>(); // guaranteed to exist
            
            //set the prefab for PlayerInputManager
            inputManagerComponent.playerPrefab = playerInputObjectPrefab; 
            
            //todo: maybe ensure more settings on that manager component here?
            
            //ensure players parent exists and is parented if not set
            if (playersParent == null)
            {
                playersParent = new GameObject("Player(s)Parent").transform;
                
                playersParent.SetParent(playerManager.transform); 
            }
            
            // Initialize the list of PlayerInput objects
            playerInputObjects = new List<PlayerInputObject>(); 
            
            // setting defaults if no ApplicationManager/ActiveSceneSO
            if (!playerManager.IsApplicationManager)
            {
                SetTargetPlayers(defaultTargetPlayers);

                SetTargetPlayerState(defaultPlayerState);
                
                targetIsPerPlayerNavigation = defaultIsPerPlayerNavigation;
            }
            
            // ensure cursor canvas set if not set (errors could happen if other canvases are added as children)
            if (cursorsCanvas == null)
            {
                cursorsCanvas = playerManager.GetComponentInChildren<Canvas>();
            }
            
            //ensure cursor parent set as last child of cursor canvas if not set
            if (cursorsParent == null)
            {
                cursorsParent = new GameObject("Cursor(s)Holder").transform;
                
                cursorsParent.SetParent(cursorsCanvas.transform);

                cursorsParent.SetAsLastSibling();
                
                cursorsParent.localPosition = Vector3.zero;
            }
            
            //initialize dictionary for player cursors
            PlayersCursors = new Dictionary<PlayerInputObject, Transform>();
            
            //set cursor lock state
            Cursor.lockState = CursorLockMode.Locked;

            return InitializedStates();
        }
        
        public override void ContextCallChangeState(EPlayerManagementState newState)
        {
            playerManager.ChangeState(newState);
        }

        #endregion

        #region PlayerMethods
        
        /// <summary>
        /// Public method to set target players, clamped between 1 and 4.
        /// </summary>
        /// <param name="targetPlayersToSet"></param>
        public void SetTargetPlayers(int targetPlayersToSet)
        {
            if (targetPlayersToSet < 1 || targetPlayersToSet > MaxPlayers)
            {
                Debug.LogWarning($"Invalid target players count: {targetPlayersToSet}. Must be between 1 and 4.");
                
                return;
            }
            
            targetPlayers = targetPlayersToSet;
            
            if (playerManager.DebugMode)
            {
                Debug.Log($"Target players set to {this.targetPlayers} in {playerManager.GetType().Name}.");
            }
        }

        #region GettingPlayers
        
        /// <summary>
        /// Private method used by GetPlayer(int) to validate index input when querying players by index.
        /// </summary>
        private bool IsValidPlayerIndex(int index)
        {
            bool isValid = index > 0 && index <= playerInputObjects.Count;
            
            if (!isValid)
            {
                Debug.LogWarning($"Index {index} is out of bounds for playerInputObjects. " +
                                 $"Must be between 1 and {playerInputObjects.Count}.");
            }
            
            return isValid;
        }
        
        /// <summary>
        /// Public method to get a PlayerInputObject by its 1-based index(1-4) in the playerInputObjects list.
        /// Null if invalid.
        /// </summary>
        public PlayerInputObject GetPlayer(int index)
        {
            if (IsValidPlayerIndex(index))
            {
                PlayerInputObject targetPlayer = playerInputObjects[index - 1]; // Convert to zero-based index
                
                if (targetPlayer != null)
                {
                    if (playerManager.DebugMode)
                    {
                        Debug.Log($"Getting player at index {index}: {playerInputObjects[index].name}");
                        
                    }

                    return targetPlayer;
                }
                else
                {
                    Debug.LogWarning($"No player found at index {index}.");
                    
                    return null;
                }
            }
            else
            {
                return null;
            }
        }
        
        /// <summary>
        /// Public method to get a PlayerInputObject by its associated PlayerInput component. Null if invalid.
        /// </summary>
        public PlayerInputObject GetPlayer(PlayerInput playerInput)
        {
            if (playerInput == null)
            {
                Debug.LogWarning("Provided PlayerInput is null.");
                
                return null;
            }
            
            PlayerInputObject targetPlayerInputObject = playerInput.GetComponent<PlayerInputObject>();
            
            if (targetPlayerInputObject != null && playerInputObjects.Contains(targetPlayerInputObject))
            {
                if (playerManager.DebugMode)
                {
                    Debug.Log($"Getting player: {targetPlayerInputObject.name}");
                }
                
                return targetPlayerInputObject;
            }
            else
            {
                return null;
            }
        }
        
        /// <summary>
        /// Public method to get a PlayerInputObject by its own reference. Null if invalid.
        /// (Seems redundant but useful for validation)
        /// </summary>
        public PlayerInputObject GetPlayer(PlayerInputObject playerInputObject)
        {
            if (playerInputObject == null)
            {
                Debug.LogWarning("GetPlayer: Provided PlayerInputObject is null.");
                
                return null;
            }
            
            if (playerInputObjects.Contains(playerInputObject))
            {
                if (playerManager.DebugMode)
                {
                    Debug.Log($"GetPlayer: Returning player: {playerInputObject.name}");
                }
                
                return playerInputObject;
            }
            else
            {
                return null;
            }
        }

        #endregion
        
        #region AddingPlayers
        
        /// <summary>
        /// Public method to catch and add a newly added Player Input component to the managed list as part of a
        /// PlayerInputObject after being added by PlayerInputManager.
        /// </summary>
        public void AddJoinedPlayer(PlayerInput playerInput)
        {
            if (playerInput == null)
            {
                Debug.LogWarning("Provided PlayerInput is null.");
                
                return;
            }
            
            if (NumPlayers >= MaxPlayers)
            {
                Debug.LogWarning("Cannot add more players. Maximum limit reached.");
                
                Destroy(playerInput.gameObject);
                
                return;
            }
            
            PlayerInputObject attachedPlayerInputObject = playerInput.GetComponent<PlayerInputObject>();
            
            if (attachedPlayerInputObject == null)
            {
                Debug.LogWarning("Provided PlayerInput does not have a PlayerInputObject component.");
                
                Destroy(playerInput.gameObject);
                
                return;
            }
            
            if (playerInputObjects.Contains(attachedPlayerInputObject))
            {
                Debug.LogWarning("Provided PlayerInput is already managed by this PlayerManager.");
                
                return;
            }
            
            playerInputObjects.Add(attachedPlayerInputObject);
            
            attachedPlayerInputObject.transform.SetParent(playersParent); // Set parent to the holder from context
            
            if (playerManager.DebugMode)
            {
                Debug.Log($"Added player: {attachedPlayerInputObject.name}. Total players now: {NumPlayers}");
            }
        }

        #endregion

        #region RemovingPlayers

        /// <summary>
        /// Public method for removing a PlayerInputObject by its 1-based index(1-4) in the playerInputObjects list.
        /// </summary>
        public void RemovePlayer(int index)
        {
            PlayerInputObject targetPlayer = GetPlayer(index);
            
            if (targetPlayer != null)
            {
                playerInputObjects.Remove(targetPlayer);
                
                Destroy(targetPlayer.gameObject);
                
                if (playerManager.DebugMode)
                {
                    Debug.Log($"Removed player at index {index}: {targetPlayer.name}");
                }
            }
            else
            {
                Debug.LogWarning($"No player found at index {index}.");
            }
        }
        
        /// <summary>
        /// Public method for removing a PlayerInputObject by its associated PlayerInput component.
        /// </summary>
        public void RemovePlayer(PlayerInput playerInput)
        {
            if (playerInput == null)
            {
                Debug.LogWarning("Provided PlayerInput is null.");
                
                return;
            }
            PlayerInputObject targetPlayerInputObject = GetPlayer(playerInput);
            
            if (targetPlayerInputObject != null)
            {
                playerInputObjects.Remove(targetPlayerInputObject);
                
                Destroy(targetPlayerInputObject.gameObject);
                
                if (playerManager.DebugMode)
                {
                    Debug.Log($"Removed player: {targetPlayerInputObject.name}");
                }
            }
            else
            {
                Debug.LogWarning("Provided PlayerInput is not managed by this PlayerManager.");
            }
        }
        
        /// <summary>
        /// Public method for removing a PlayerInputObject by its own reference.
        /// </summary>
        public void RemovePlayer(PlayerInputObject playerInputObject)
        {
            if (playerInputObject == null)
            {
                Debug.LogWarning("Provided PlayerInputObject is null.");
                
                return;
            }
            
            PlayerInputObject targetPlayerInputObject = GetPlayer(playerInputObject);
            
            if (targetPlayerInputObject != null)
            {
                playerInputObjects.Remove(targetPlayerInputObject);
                
                Destroy(targetPlayerInputObject.gameObject);
                
                if (playerManager.DebugMode)
                {
                    Debug.Log($"Removed player: {targetPlayerInputObject.name}");
                }
            }
            else
            {
                Debug.LogWarning("Provided PlayerInputObject is not managed by this PlayerManager.");
            }
        }
        
        #endregion

        #region PlayerStateMethods

        /// <summary>
        /// Sets the target player state for all managed players.
        /// </summary>
        public void SetTargetPlayerState(PlayerInputObject.EPlayerInputObjectState newState)
        {
            targetPlayerState = newState;
            
            playerManager.OnAfterTargetPlayerStateChange?.Invoke(newState);
            
            if (playerManager.DebugMode)
            {
                Debug.Log($"Set target player state to {newState} in {playerManager.GetType().Name}.");
            }
        }
        
        /// <summary>
        ///  If set to true, player cursors will only move on their respective canvas space. If not, no limits. 
        /// </summary>
        public void SetTargetCursorNavigation(bool isPerPlayerNavigation)
        {
            targetIsPerPlayerNavigation = isPerPlayerNavigation;
            
            if (playerManager.DebugMode)
            {
                Debug.Log($"Set cursor settings to PerPlayerNavigation: {isPerPlayerNavigation}, " +
                          $" in {playerManager.GetType().Name}.");
            }
        }

        #endregion

        #region CursorMethods
        
        /// <summary>
        /// Takes an playerInputObject and adds its CursorPioComponent CursorInstance to internal childed list.
        /// From there it is kept as a child till the same playerInputObject removes its cursor. 
        /// </summary>
        public void AddPlayersCursor(PlayerInputObject playerInputObject)
        {
            CursorPioComponent playerCursor = playerInputObject.GetComponent<CursorPioComponent>();
            
            if (playerCursor != null && !PlayersCursors.ContainsKey(playerInputObject))
            {
                PlayersCursors.Add(playerInputObject, playerCursor.CursorInstance.transform);
                
                playerCursor.CursorInstance.SetParent(cursorsParent);
            }
        }
        
        /// <summary>
        /// Takes an playerInputObject and removes its CursorPioComponent CursorInstance from internal childed list.
        /// From there it is returned as child to playerInputObject.
        /// </summary>
        public void RemovePlayersCursor(PlayerInputObject playerInputObject)
        {
            CursorPioComponent playerCursor = playerInputObject.GetComponent<CursorPioComponent>();
            
            if (playerCursor != null && PlayersCursors.ContainsKey(playerInputObject))
            {
                PlayersCursors.Remove(playerInputObject);
                
                playerCursor.CursorInstance.SetParent(playerCursor.transform);
            }
        }
        
        #endregion
        
        #endregion
    }
    
    public abstract class InputManagerState : BaseState<EPlayerManagementState>
    {
        protected InputManagerState(PlayerManagerContext context, 
            EPlayerManagementState key, 
            EPlayerManagementState[] invalidTransitions) 
            : base(key, invalidTransitions)
        {
            Context = context;
        }
        
        protected PlayerManagerContext Context { get; }
    }
}