using UnityEngine;

public class UIPioComponent : PlayerInputObjectComponent
{
    [Header("Inscribed References")]

    [Tooltip("Exists for serialization purposes, no effect. Click away!")]
    [SerializeField] private bool emptyBool;
    
    [field: SerializeField] public Canvas PlayerUICanvas { get; private set; }
    
    protected override void Initialize()
    {
        if (!CheckInscribedReferences())
        {
            return;
        }
        
        // ensure starts off
        TogglePlayerUI(false);
        
        Initialized = true;
        
        return;
        
        bool CheckInscribedReferences()
        {
            if (PlayerUICanvas == null)
            {
                Debug.LogError($"{GetType().Name}: Error checking inscribed references.");
                
                return false;
            }
            
            return true;
        }
    }
    
    protected override void OnBeforePioStateChange(PlayerInputObject.EPlayerInputObjectState toState)
    {
        switch (toState)
        {   
            case PlayerInputObject.EPlayerInputObjectState.ObjectInitialize:
            case PlayerInputObject.EPlayerInputObjectState.Inactive:
            case PlayerInputObject.EPlayerInputObjectState.Player:
            case PlayerInputObject.EPlayerInputObjectState.ApplicationUI:
                
                TogglePlayerUI(false);
                
                enabled = false;
                break;
        }
    }
    
    protected override void OnAfterPioStateChange(PlayerInputObject.EPlayerInputObjectState toState)
    {
        switch (toState)
        {   
            case PlayerInputObject.EPlayerInputObjectState.PlayerUI:
                
                TogglePlayerUI(true);
                
                enabled = true;
                break;
        }
    }

    private void TogglePlayerUI(bool active)
    {
        if (PlayerUICanvas != null)
        {
            PlayerUICanvas.enabled = active;
            
            PlayerUICanvas.gameObject.SetActive(active);
        }
    }
}
