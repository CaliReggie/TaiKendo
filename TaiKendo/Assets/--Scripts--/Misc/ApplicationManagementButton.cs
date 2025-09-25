using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ApplicationManagementButton : MonoBehaviour
{
    public enum ESceneManagementButtonType
    {
        LoadScene,
        QuitApplication
    }
    
    [Header("Inscribed References")]
    
    [SerializeField] private ESceneManagementButtonType buttonBehaviour = ESceneManagementButtonType.LoadScene;
    
    [SerializeField] private SceneSO sceneToLoad;
    
    
    [Header("Dynamic References - Don't Modify In Inspector")]
    
    [SerializeField] private Button button;

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        
        if (sceneToLoad == null && buttonBehaviour == ESceneManagementButtonType.LoadScene)
        {
            Debug.LogError($"{GetType().Name}: No SceneSO assigned to {nameof(sceneToLoad)}.");
        }
    }
    
    private void Start()
    {
        button.onClick.AddListener(OnButtonPressed);
    }
    
    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonPressed);
        }
    }
    
    private void OnButtonPressed()
    {
        switch (buttonBehaviour)
        {
            case ESceneManagementButtonType.LoadScene:
                
                if (ApplicationManager.Instance != null)
                {
                    ApplicationManager.Instance.TryLoadNewScene(sceneToLoad);
                }
                else
                {
                    Debug.LogWarning($"{GetType().Name}: No ApplicationManager instance found in scene. " +
                                   $"Attempting manual load.");
                    
                    if (SceneSO.IsValidScene(sceneToLoad) && !SceneSO.IsActiveScene(sceneToLoad))
                    {
                        SceneManager.LoadScene(sceneToLoad.TryGetScenePathAsName());
                        
                    }
                    else
                    {
                        Debug.LogError($"Cannot manual load SceneSO: {sceneToLoad.name}.");
                    }
                }
                
                break;
            
                case ESceneManagementButtonType.QuitApplication:
                
                if (ApplicationManager.Instance != null)
                {
                    ApplicationManager.Instance.Quit();
                }
                else
                {
                    Debug.LogWarning($"{GetType().Name}: No ApplicationManager instance found in scene. " +
                                   $"Attempting manual quit.");
                    
                    Application.Quit();
                }
                
                break;
        }
    }
}
