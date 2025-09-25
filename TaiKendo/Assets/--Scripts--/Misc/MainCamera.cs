using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Camera))]
[RequireComponent(typeof(CinemachineBrain))]
public class MainCamera : StaticInstance<MainCamera>
{
    [Header("Inscribed References")]

    [SerializeField] private CinemachineBrain cinBrain;
    [field: SerializeField] public Camera Camera { get; private set; }
    
    protected override void Awake()
    {
        if (!CheckInscribedReferences())
        {
            ToggleCamera(false);
            
            return;
        }
        
        base.Awake();
        
        return;
        
        bool CheckInscribedReferences()
        {
            if (Camera == null)
            {
                Debug.LogError($"{GetType().Name}: Error checking inscribed references.");
                
                return false;
            }
            
            return true;
        }
    }
    private void ToggleCamera(bool active)
    {
        if (Camera != null)
        {
            Camera.enabled = active;
        }
        
        if (cinBrain != null)
        {
            cinBrain.enabled = active;
        }
        
        gameObject.SetActive(active);
    }
}
