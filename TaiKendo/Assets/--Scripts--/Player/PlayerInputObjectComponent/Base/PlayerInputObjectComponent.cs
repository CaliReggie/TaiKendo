using UnityEngine;

[RequireComponent(typeof(PlayerInputObject))]
public abstract class PlayerInputObjectComponent : MonoBehaviour
{
    [Header("Base Settings")]

    [Tooltip("If true, component will log and enact debug behavior or information.")]
    [SerializeField] protected bool debugMode;
    
    [Header("Base Dynamic References - Don't Modify In Inspector")]

    [Tooltip("Exists for serialization purposes. No effect, click away!")]
    [SerializeField] private bool baseEmptyBool;
    
    /// <summary>
    /// The PlayerInputObject this component is associated with.
    /// </summary>
    [field: SerializeField] protected PlayerInputObject Pio { get; private set; }
    
    /// <summary>
    /// Flag indication initialization logic such as checking and setting references, along with any additional logic
    /// being completed. To be set to true at the end of Initialize().
    /// </summary>
    [field: SerializeField] public bool Initialized { get; protected set; }
    
    /// <summary>
    /// Base method handles initialization call and Pio event subscription.
    /// Initializes and turns off waiting for state changes. Make sure to set initialized to true in Initialize().
    /// </summary>
    protected virtual void Awake()
    {
        if (!Initialized)
        {
            Pio = GetComponent<PlayerInputObject>();
            
            Pio.OnBeforeStateChange += OnBeforePioStateChange;
            
            Pio.OnAfterStateChange += OnAfterPioStateChange;
            
            Initialize();
            
            enabled = false; // Disable script. Self is enabled or disabled based on Pio state changes
        }
    }
    
    /// <summary>
    /// Base methods handles event unsubscription.
    /// </summary>
    protected virtual void OnDestroy()
    {
        if (Pio != null)
        {
            Pio.OnBeforeStateChange -= OnBeforePioStateChange;

            Pio.OnAfterStateChange -= OnAfterPioStateChange;
        }
    }
    
    /// <summary>
    /// To be defined in derived classes. This is where references should be checked and set, default or base logic run,
    /// and whatever else is needed to get the component ready to be enabled and used. Make sure to set Initialized to true.
    /// </summary>
    protected abstract void Initialize();
    
    /// <summary>
    /// To be defined in derived classes. This will be used for necessary logic to be done right before changing
    /// to a new target state.
    /// </summary>
    /// <param name="toState"></param>
    protected abstract void OnBeforePioStateChange(PlayerInputObject.EPlayerInputObjectState toState);
    
    /// <summary>
    /// To be defined in derived classes. This will be used for necessary logic once a new target state has been
    /// switched to.
    /// </summary>
    /// <param name="toState"></param>
    protected abstract void OnAfterPioStateChange(PlayerInputObject.EPlayerInputObjectState toState);
}
