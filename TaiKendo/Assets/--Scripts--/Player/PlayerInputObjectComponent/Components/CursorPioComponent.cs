using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Users;

public class CursorPioComponent : PlayerInputObjectComponent
{
    private const string GamepadScheme = "Gamepad";
        
    private const string KeyboardScheme = "Keyboard&Mouse";
    
    [Header("Inscribed References")]
    
    [Tooltip("The prefab to spawn as player cursor")]
    [SerializeField]
    private GameObject cursorPrefab;
    
    [Header("Inscribed Settings")]
    
    [Tooltip("If true, default player cursor navigation is per player camera space." +
             " If false, default player cursor navigation is for whole screen space.")]
    [SerializeField] private bool defaultIsPerPlayerNavigation;
    
    [Header("Dynamic References - Don't modify in Inspector")]
    
    [SerializeField] private PlayerInput pioPlayerInput; // the PlayerInput attached to the PIO

    [Tooltip("The canvas the cursor will be used on")]
    [SerializeField] private Canvas canvas;
    
    [Tooltip(" The rect transform of the canvas the cursor will be used on")]
    [SerializeField] private RectTransform canvasRectTransform;

    [Tooltip("The camera used for drawing the cursor over screen space")]
    [SerializeField] private Camera drawCam;
    
    [Tooltip("The currently managed cursor")]
    [field: SerializeField] public RectTransform CursorInstance {get; private set;}

    [Tooltip("The mouse being managed. Either the real mouse of k&m player or virtual mouse of gamepad player")]
    private Mouse _mouse;

    [Header("Dynamic Settings - Don't modify in Inspector")]
    
    [Tooltip("The players specific min and max Vector2 screen bounds")]
    [SerializeField]
    private Vector2[] playerScreenBounds;
    
    [Tooltip("The main canvas min and max Vector2 screen bounds")]
    [SerializeField]
    private Vector2[] mainScreenBounds;
    
    [Tooltip("The most recently updated position of real mouse scheme")]
    [SerializeField]
    private Vector2 realMousePos;
    
    [Tooltip("The most recently updated direction of gamepad movement")]
    [SerializeField]
    private Vector2 gamepadDir;
    
    [Tooltip("The most recently updated bool if either control scheme click is pressed")]
    [SerializeField]
    private bool cursorPressed;
    
    [Tooltip("If true, player cursor navigation is per player camera space." +
             " If false, player cursor navigation is for whole screen space.")]
    [SerializeField] private bool isPerPlayerNavigation;
    
    /// <summary>
    /// Message to be received by PlayerInput Message Broadcast when real mouse moves on Keyboard&Mouse scheme.
    /// </summary>
    public void OnPoint(InputValue pointValue)
    {
        realMousePos = pointValue.Get<Vector2>();
    }
    
    /// <summary>
    /// Message to be received by PlayerInput Message Broadcast for moving cursor on Gamepad scheme.
    /// </summary>
    public void OnGamepadPoint(InputValue inputVector)
    {
        gamepadDir = inputVector.Get<Vector2>();
    }
    
    /// <summary>
    /// Message to be received by PlayerInput Message Broadcast for clicking on any scheme.
    /// </summary>
    public void OnClick(InputValue buttonValue)
    {
        cursorPressed = buttonValue.isPressed;
        
        // hide real mouse when using cursor
        if (cursorPressed && enabled && Initialized)
        {
            Cursor.visible = false;
        }
    }
    
    /// <summary>
    /// Message to be received by InputManager Message Broadcast when a player joins.
    /// </summary>
    public void OnPlayerJoined(PlayerInput joinedPlayerInput)
    {
        UpdateScreenBounds();
    }
    
    /// <summary>
    /// Message to be received by InputManager Message Broadcast when a player leaves.
    /// </summary>
    public void OnPlayerLeft(PlayerInput joinedPlayerInput)
    {
        UpdateScreenBounds();
    }
    
    protected override void Initialize()
    {
        // make sure inscribed refs are good
        if (!CheckInscribedReferences()) return;
        
        // ensure dynamic refs set
        if (!SetDynamicReferences()) return;
        
        // update screen bounds
        UpdateScreenBounds();
        
        // get mouse ref or add if gamepad
        ConfigureMouseReference();
        
        // ensure starts off
        ToggleCursorInstance(false);
        
        // configure cursor defaults
        ConfigureCursorSettings(CursorLockMode.Locked,
            false,
            defaultIsPerPlayerNavigation);
        
        Initialized = true;
        
        return;
        
        // true if all inscribe references are good
        bool CheckInscribedReferences()
        {
            if (cursorPrefab == null)
            {
                Debug.LogError($"{GetType().Name}: Inscribed references not set.");
                
                return false;
            }
            else
            {
                return true;
            }
        }
        
        // true if all dynamic references are good
        bool SetDynamicReferences()
        {
            try
            {
                pioPlayerInput = Pio.GetComponent<PlayerInput>();
                
                // if main ui manager exists use its canvas as ref for logic
                if (PlayerManager.Instance != null)
                {
                    canvas = PlayerManager.Instance.CursorsCanvas;
                    
                    canvasRectTransform = canvas.GetComponent<RectTransform>();
                }
                // if not, see if player has a camera and ui component to sample from
                else if (GetComponent<UIPioComponent>() != null)
                {
                    UIPioComponent UIPioComponent = GetComponent<UIPioComponent>();
                    
                    canvas = UIPioComponent.PlayerUICanvas;
                    
                    canvasRectTransform = canvas.GetComponent<RectTransform>();
                }
                // if neither, ain't gon work
                else
                {
                    Debug.LogError("PlayerCursor: No canvas found to sample canvas from");

                    return false;
                }
                
                // getting camera from camera pio component
                CameraPioComponent cameraPioComponent = GetComponent<CameraPioComponent>();
                    
                drawCam = cameraPioComponent.MainPlayerCam;
                
                // spawning cursor from prefab if null, starting with gameObject inactive.
                if (CursorInstance == null)
                {
                    GameObject cursorGO = Instantiate(cursorPrefab, transform);
                    
                    CursorInstance = cursorGO.GetComponent<RectTransform>();

                    int playerCursorIndex = pioPlayerInput.splitScreenIndex;
                    
                    CursorInstance.name = "Player" + playerCursorIndex + "Cursor";
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("PlayerCursor: Exception getting dynamic references: " + e);

                return false;
            }
        }
        
        // getting mouse ref or adding if gamepad
        void ConfigureMouseReference()
        {
            //if player is on keyboard
            if (pioPlayerInput.currentControlScheme == KeyboardScheme)
            {
                // assigning real mouse if not already present
                if (_mouse == null)
                {
                    foreach (var device in pioPlayerInput.devices)
                    {
                        if (device is Mouse)
                        {
                            _mouse = device as Mouse;
                            
                            break;
                        }
                    }
                }
                // adding device if not already added
                else if (!_mouse.added)
                {
                    InputSystem.AddDevice(_mouse);
                }
                
            }
            //if player is on gamepad
            else if (pioPlayerInput.currentControlScheme == GamepadScheme)
            {
                //adding virtual mouse if not already present
                if (_mouse == null)
                {
                    _mouse = (Mouse) InputSystem.AddDevice("VirtualMouse");
                }
                // adding device if not already added
                else if (!_mouse.added)
                {
                    InputSystem.AddDevice(_mouse);
                }
                
                //pairing virtual mouse to player
                InputUser.PerformPairingWithDevice(_mouse, pioPlayerInput.user);
            }
            // unhandled control scheme
            else
            {
                Debug.LogError("PlayerCursor: Unhandled control scheme: " + pioPlayerInput.currentControlScheme);
            }
        }
    }
    
    /// <summary>
    /// Call to update the screen bounds for this player initially or after their camera space changes. 
    /// </summary>
    private void UpdateScreenBounds()
    {
        // sampling main bounds for whole cursor space
        Rect mainAreaRect;
        
        // use canvasRectTransform to sample if exists
        if (canvasRectTransform != null)
        {
            mainAreaRect = canvasRectTransform.rect;
        }
        // if not use whole display
        else
        {
            float width = Display.main.renderingWidth;
            
            float height = Display.main.renderingHeight;
            
            mainAreaRect = new Rect(0, 0, width, height);
        }
        
        //setting main bounds
        mainScreenBounds = new Vector2[2];

        mainScreenBounds[0] = Vector2.zero;

        mainScreenBounds[1] = new Vector2(mainAreaRect.width, mainAreaRect.height);
        
        //setting player bounds
        playerScreenBounds = new Vector2[2];
        
        if (drawCam != null)
        {
            Rect camRect = drawCam.rect;
            
            playerScreenBounds[0] = new Vector2(camRect.xMin * mainAreaRect.width,
                camRect.yMin * mainAreaRect.height); // minimum position
            
            playerScreenBounds[1] = new Vector2(camRect.xMax * mainAreaRect.width,
                camRect.yMax * mainAreaRect.height); // maximum position
        }
        else
        {
            playerScreenBounds[0] = mainScreenBounds[0]; // min pos

            playerScreenBounds[1] = mainScreenBounds[1]; // max pos
        }
    }
    
    protected override void OnDestroy()
    {
        // unsub to input system update
        InputSystem.onAfterUpdate -= UpdateCursor;

        // if gamepad player remove virtual mouse
        if (pioPlayerInput.currentControlScheme == GamepadScheme)
        {
            InputSystem.RemoveDevice(_mouse);
        }
        
        //if cursor instance exists, destroy it
        if (CursorInstance != null)
        {
            Destroy(CursorInstance.gameObject);
        }
        
        base.OnDestroy();
    }
    
    protected override void OnBeforePioStateChange(PlayerInputObject.EPlayerInputObjectState toState)
    {
        switch (toState)
        {   
            case PlayerInputObject.EPlayerInputObjectState.ObjectInitialize:
            case PlayerInputObject.EPlayerInputObjectState.Inactive:
            case PlayerInputObject.EPlayerInputObjectState.Player:
                
                ConfigureCursorSettings(CursorLockMode.Locked,
                    false,
                    isPerPlayerNavigation);
                
                // if PlayerManager exists, remove from its list of player cursors
                if (PlayerManager.Instance != null)
                {
                    // When disabled, remove this player's cursor from the MainUIManager.Instance's list of player cursors.
                    PlayerManager.Instance.PlayerRequestRemoveCursor(Pio);
                }
                // if not, unchild from canvas if available
                else if (canvas != null)
                {
                    CursorInstance.SetParent(transform, false);
                }
                else
                {
                    Debug.LogWarning("PlayerCursor: No MainUIManager or Canvas found to un parent cursor from.");
                }
                
                //turn off cursor Go
                if (CursorInstance != null)
                {
                    ToggleCursorInstance(false);
                }
                
                enabled = false;
                
                break;
        }
    }
    
    protected override void OnAfterPioStateChange(PlayerInputObject.EPlayerInputObjectState toState)
    {
        switch (toState)
        {   
            case PlayerInputObject.EPlayerInputObjectState.PlayerUI:
            case PlayerInputObject.EPlayerInputObjectState.ApplicationUI:
                
                bool pmExists = PlayerManager.Instance != null;
                
                // want hardware cursor free, mouse hidden, and then navigation is variable:
                // if Player Manager exists use its setting, if not stick with current setting
                ConfigureCursorSettings(CursorLockMode.None,
                        false,
                        pmExists ? 
                            PlayerManager.Instance.TargetIsPerPlayerNavigation : isPerPlayerNavigation);
                
                // if PlayerManager exists, request to add player cursor
                if (pmExists)
                {
                    PlayerManager.Instance.PlayerRequestAddCursor(Pio);
                }
                // if not, if canvas is available, child to canvas
                else if (canvas != null)
                {
                    CursorInstance.SetParent(canvas.transform, false);
                    
                    CursorInstance.SetAsLastSibling();
                }
                else
                {
                    Debug.LogWarning("PlayerCursor: No MainUIManager or Canvas found to parent cursor to.");
                }
                
                StartCursorInCenter();
        
                // ensuring click is false on enable
                cursorPressed = false;
                
                //turn on cursor Go
                ToggleCursorInstance(true);
                
                enabled = true;
                
                break;
                
                void StartCursorInCenter()
                {
                    //placing in center of screen
                    Vector2 startingPos = (playerScreenBounds[0] + playerScreenBounds[1]) / 2;
                    
                    AnchorCursor(startingPos);
                    
                    // if k&m
                    if (pioPlayerInput.currentControlScheme == KeyboardScheme)
                    {
                        //matching mouse to cursor
                        _mouse.WarpCursorPosition(startingPos);
                        
                        realMousePos = startingPos;
                    }
                    // if Gp
                    else if (pioPlayerInput.currentControlScheme == GamepadScheme)
                    {
                        //matching virtual mouse to cursor
                        InputState.Change(_mouse.position, startingPos);

                        gamepadDir = Vector2.zero;
                    }
                }
        }
    }

    private void OnEnable()
    {
        if (!Initialized) return;
        
        // sub to input system update
        InputSystem.onAfterUpdate += UpdateCursor;
    }

    protected void OnDisable()
    {
        if (!Initialized) return;
        
        // unsub to input system update
        InputSystem.onAfterUpdate -= UpdateCursor;
    }
    
    /// <summary>
    /// Toggles the visible cursor gameObject on or off
    /// </summary>
    private void ToggleCursorInstance(bool active)
    {
        if (CursorInstance == null)
        {
            Debug.LogError("PlayerCursor: Cursor Instance null on toggle.");
            
            return;
        }
        
        CursorInstance.gameObject.SetActive(active);
    }
    
    /// <summary>
    /// Configures the cursor settings
    /// </summary>
    /// <param name="hardwareLockMode"></param> The real mouse lock mode to set
    /// <param name="hardwareCursorVisible"></param> If true, real mouse cursor is visible
    /// <param name="targetIsPerPlayerNavigation"></param> If true, player cursor navigation is per player camera space.
    /// If false, player cursor navigation is for whole display space.
    private void ConfigureCursorSettings(CursorLockMode hardwareLockMode, bool hardwareCursorVisible,
        bool targetIsPerPlayerNavigation)
    {
        Cursor.lockState = hardwareLockMode;
        
        Cursor.visible = hardwareCursorVisible;
        
        isPerPlayerNavigation = targetIsPerPlayerNavigation;
    }
    
    /// <summary>
    /// Subscribed with input system update, cursor will be moved and managed depending on control scheme type
    /// </summary>
    private void UpdateCursor()
    {
        if (!Initialized || !enabled)
        {
            return;
        }
        
        //if player in on keyboard
        if (pioPlayerInput.currentControlScheme == KeyboardScheme)
        {
            //clamping mouse pos based on desired player cursor space (player portion or whole screen)
            Vector2 clampedMousePos;
            
            if (isPerPlayerNavigation)
            {
                clampedMousePos = ClampedByBounds(realMousePos, playerScreenBounds[0], playerScreenBounds[1]);
            }
            else
            {
                clampedMousePos = ClampedByBounds(realMousePos, mainScreenBounds[0], mainScreenBounds[1]);
            }
            
            //updating cursor position
            AnchorCursor(clampedMousePos);
            
            // did real mouse go too far?
            bool mouseOffBounds = realMousePos != clampedMousePos;
            
            if (mouseOffBounds)
            {
                // how far is it off
                Vector2 diff = clampedMousePos - realMousePos;
                
                // bring it back that much
                Vector2 warpedCursorPos = clampedMousePos + diff.normalized * 10;
                
                //matching mouse to cursor
                _mouse.WarpCursorPosition(warpedCursorPos);
            }
        }
        //if player is on gamepad
        else if (pioPlayerInput.currentControlScheme == GamepadScheme)
        {
            if (_mouse == null)
            {
                Debug.LogError("PlayerCursor: Mouse is null while on GamepadScheme. Disabling PlayerCursor.");
                
                enabled = false;
                
                return;
            }
            
            //reading current virtual mouse position
            Vector2 gamepadMousePos = _mouse.position.ReadValue();
            
            //calculating new position based on current position and input direction
            Vector2 targetGpMousePos = gamepadMousePos + gamepadDir * Time.unscaledDeltaTime;
            
            //clamping new pos based on desired player cursor space (player portion or whole screen)
            Vector2 clampedTargetGpPos;
            
            if (isPerPlayerNavigation)
            {
                clampedTargetGpPos = ClampedByBounds(targetGpMousePos, playerScreenBounds[0], playerScreenBounds[1]);
            }
            else
            {
                clampedTargetGpPos = ClampedByBounds(targetGpMousePos, mainScreenBounds[0], mainScreenBounds[1]);
            }
            
            //updating virtual mouse position
            InputState.Change(_mouse.position, clampedTargetGpPos);
            
            // calculating delta for potential use
            Vector2 delta = clampedTargetGpPos - gamepadMousePos;
            
            //using delta to update virtual mouse delta
            InputState.Change(_mouse.delta, delta);
            
            //updating cursor position
            AnchorCursor(clampedTargetGpPos);
            
            //handling gamepad click state
            _mouse.CopyState<MouseState>(out var gamepadMouseState);
            
            //setting left button state based on cursorPressed
            gamepadMouseState = cursorPressed
                ? gamepadMouseState.WithButton(MouseButton.Left)
                : gamepadMouseState.WithButton(MouseButton.Left, false);
            
            //applying state change
            InputState.Change(_mouse, gamepadMouseState);
        }
        // unhandled control scheme
        else
        {
            Debug.LogError("PlayerCursor: Unhandled control scheme: " + pioPlayerInput.currentControlScheme);
        }
    }
    
    /// <summary>
    /// Takes a V2 position, min, and max and clamps within bounds
    /// </summary>
    private Vector2 ClampedByBounds(Vector2 pos, Vector2 min, Vector2 max)
    {
        return new Vector2(Mathf.Clamp(pos.x, min.x, max.x), Mathf.Clamp(pos.y, min.y, max.y));
    }
    
    // TODO: Remove reliance on main camera. Maybe make a main camera instance class
    /// <summary>
    /// Takes a target pos and moves the cursor GO anchor to that place
    /// </summary>
    private void AnchorCursor(Vector2 newPos)
    {
        Vector2 anchoredPos;
        
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, newPos, 
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : drawCam , out anchoredPos);
        
        CursorInstance.anchoredPosition = anchoredPos;
    }
}
    