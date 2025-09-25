using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

#if UNITY_EDITOR

[CustomEditor(typeof(SceneSO))]
public class SceneSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.LabelField("Make sure to include in build settings!", EditorStyles.helpBox);
        EditorGUILayout.LabelField("Scene Asset - Set In Inspector", EditorStyles.boldLabel);
        
        SceneSO sceneSO = (SceneSO)target;
        
        sceneSO.SceneAsset = (SceneAsset)EditorGUILayout.ObjectField("Scene Asset",
            sceneSO.SceneAsset, typeof(SceneAsset),
            false);
        
        DrawDefaultInspector();
    }
}

#endif

[CreateAssetMenu(fileName = "NewSceneSO", menuName = "ScriptableObjects/SceneSO")]
public class SceneSO : ScriptableObject
{
    #if UNITY_EDITOR
    public SceneAsset SceneAsset
    { 
        get // Gets the SceneAsset from the scenePath if previously set
        {
            if (string.IsNullOrEmpty(scenePath))
            {
                Debug.LogWarning("Scene path is not set.");
                
                return null;
            }
            
            try { return AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath); }
            
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to find SceneAsset from path '{scenePath}': {e.Message}");
                
                return null;
            }
        }
        set { scenePath = AssetDatabase.GetAssetPath(value); } // Sets the scenePath from the SceneAsset
    }
    
    #endif
    
    [Header("Player Settings")]
    
    [Tooltip("The target number of players (1-4) that this scene is designed for. " +
             "This is used to determine the number of players that can join in this scene.")]
    [Range(1, 4)]
    [SerializeField] private int targetPlayers = 1;
    
    [Header("Running State Settings")]

    [Tooltip("The target scene player state for players to be in when scene in running state (default state on scene load).")]
    [SerializeField] private PlayerInputObject.EPlayerInputObjectState targetPlayerStateInRunning =
        PlayerInputObject.EPlayerInputObjectState.Player;

    [Tooltip("If true, PlayerCursors will only move on their respective canvas space. " +
             "If false, PlayerCursors will be able to traverse the whole canvas.")]
    [SerializeField] private bool perPlayerNavigationInRunning;
    
    [Header("Alternate Running State Settings")]
    
    [Tooltip("If true, time will be paused (Time.timeScale = 0) when the scene is in menu state. " +
             "If false, time will continue running normally.")]
    [SerializeField] private bool pauseTimeInAlternateRunning;
    
    [Tooltip("The target scene player state for players to be in when scene in menu state. (when players press ")]
    [SerializeField] private PlayerInputObject.EPlayerInputObjectState targetPlayerStateInAlternateRunning =
        PlayerInputObject.EPlayerInputObjectState.PlayerUI;
    
    [Tooltip("If true, PlayerCursors will only move on their respective canvas space. " +
             "If false, PlayerCursors will be able to traverse the whole canvas.")]
    [SerializeField] private bool perPlayerNavigationInAlternateRunning;
    
    [Header("Debug")]

    [Tooltip("Press when done making changes to the scenePath")]
    [SerializeField] private bool writeChanges;
    
    [Tooltip("The raw path to the scene asset. " +
             "This is set automatically when assigning a SceneAsset in the inspector.")]
    [SerializeField] private string scenePath;

    /// <summary>
    /// The target number of players (1-4) that this scene is designed for.
    /// </summary>
    public int TargetPlayers => targetPlayers;
    
    /// <summary>
    /// The target scene player state for players to be in when scene in running state.
    /// </summary>
    public PlayerInputObject.EPlayerInputObjectState TargetPlayerStateInRunning => targetPlayerStateInRunning;
    
    /// <summary>
    /// If true, PlayerCursors will only move on their respective canvas space.
    /// If false, PlayerCursors will be able to traverse the whole canvas.
    /// </summary>
    public bool PerPlayerNavigationInRunning => perPlayerNavigationInRunning;
    
    /// <summary>
    /// If true, time will be paused (Time.timeScale = 0) when the scene is in menu state.
    /// </summary>
    public bool PauseTimeInAlternateRunning => pauseTimeInAlternateRunning;
    
    /// <summary>
    /// The target scene player state for players to be in when scene in menu state.
    /// </summary>
    public PlayerInputObject.EPlayerInputObjectState TargetPlayerStateInAlternateRunning => targetPlayerStateInAlternateRunning;
    
    /// <summary>
    /// If true, PlayerCursors will only move on their respective canvas space.
    /// If false, PlayerCursors will be able to traverse the whole canvas.
    /// </summary>
    public bool PerPlayerNavigationInAlternateRunning => perPlayerNavigationInAlternateRunning;
    
    /// <summary>
    /// For some reason, changing values elsewhere is required to save the serialized path once set to something.
    /// Hence, the "writeChanges" boolean is used to trigger this method.
    /// </summary>
    private void OnValidate()
    {
        if (writeChanges)
        {
            writeChanges = false;
            
            Debug.Log($"Wrote changes to SceneSO: {name}");
        }
    }

    /// <summary>
    /// Takes the raw path of a scene asset and converts it to a name to load from and returns it, or nothing
    /// </summary>
    public String TryGetScenePathAsName()
    {
        int startIndex = scenePath.LastIndexOf('/') + 1;
        
        int endIndex = scenePath.LastIndexOf('.');
        
        if (startIndex >= 0 || endIndex > startIndex)
        {
            return scenePath.Substring(startIndex, endIndex - startIndex);
        }
        else
        {
            Debug.LogWarning($"Got invalid scene path: {scenePath}");

            return null;
        }
    }
    
    /// <summary>
    /// Ensures a path is set and valid in a target SceneSO. Will not check for build setting inclusion/enablement.
    /// </summary>
    public static bool IsValidScene (SceneSO sceneSO)
    {
        try
        {
            string scenePathAsName = sceneSO.TryGetScenePathAsName();
            
            // true if the scene path isn't null or empty
            if (!String.IsNullOrEmpty(scenePathAsName))
            {
                return true;
            }
            else
            {
                Debug.LogWarning($"SceneSO '{sceneSO.name}' is invalid for runtime use. Check it's path or inclusion" +
                                 $"in build settings.");
                
                return false;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to validate SceneSO: {e.Message}");
            
            return false;
        }
        
    }
    
    /// <summary>
    /// True if the current active scene matches the one set in a SceneSO.
    /// </summary>
    public static bool IsActiveScene(SceneSO sceneSO)
    {
        return SceneManager.GetActiveScene().name == sceneSO.TryGetScenePathAsName();
    }
}
