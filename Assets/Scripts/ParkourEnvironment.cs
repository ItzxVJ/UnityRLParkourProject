using UnityEngine;
using Unity.MLAgents;

/// <summary>
/// Controls a single environment setup (room/arena).
/// Manages level switching via ML-Agents Academy curriculum and resets obstacles.
/// </summary>
public class ParkourEnvironment : MonoBehaviour
{
    [Header("Environment References")]
    public Transform agent;
    public Transform goalPlatform;
    public Transform startPlatform;
    
    [Header("Level Settings")]
    [Tooltip("The manual level index. Overridden by curriculum parameter during training.")]
    public int levelIndex = 1;
    public float fallThreshold = -5f;

    [Header("Dynamic Sub-levels (Optional)")]
    [Tooltip("Container objects for Level 1, Level 2, etc. If defined, the active one is toggled on reset.")]
    public GameObject[] levelContainers;

    private MovingPlatform[] movingPlatforms;

    private void Awake()
    {
        // Cache all moving platform scripts in children
        movingPlatforms = GetComponentsInChildren<MovingPlatform>(true);
    }

    private void Start()
    {
        // Initial setup
        UpdateActiveLevelFromCurriculum();
    }

    /// <summary>
    /// Checks the ML-Agents Academy curriculum parameters and activates the appropriate sub-level geometry.
    /// </summary>
    public void UpdateActiveLevelFromCurriculum()
    {
        // Read "level_index" curriculum parameter if training via communicator
        if (Academy.Instance != null && Academy.Instance.IsCommunicatorOn)
        {
            float curriculumLevel = Academy.Instance.EnvironmentParameters.GetWithDefault("level_index", (float)levelIndex);
            levelIndex = Mathf.Clamp(Mathf.RoundToInt(curriculumLevel), 1, 5);
        }

        // Handle dynamic activation of sub-level models (highly useful for training scene)
        if (levelContainers != null && levelContainers.Length > 0)
        {
            for (int i = 0; i < levelContainers.Length; i++)
            {
                if (levelContainers[i] != null)
                {
                    // Level container array is 0-indexed, whereas levelIndex is 1-indexed (Level 1-5)
                    bool shouldBeActive = (i + 1) == levelIndex;
                    levelContainers[i].SetActive(shouldBeActive);

                    // Dynamically bind the start and goal reference points if child structures are active
                    if (shouldBeActive)
                    {
                        // Look for objects tagged as Goal/Start inside the active container
                        Transform levelGoal = FindDeepChildWithTag(levelContainers[i].transform, "Goal");
                        Transform levelStart = FindDeepChildWithTag(levelContainers[i].transform, "Start");
                        
                        if (levelGoal != null) goalPlatform = levelGoal;
                        if (levelStart != null) startPlatform = levelStart;
                    }
                }
            }
        }

        // Re-cache moving platforms for the newly active level structure
        movingPlatforms = GetComponentsInChildren<MovingPlatform>(true);
    }

    /// <summary>
    /// Resets all obstacles and moving platforms in this environment.
    /// </summary>
    public void ResetEnvironment()
    {
        // Check curriculum level again on reset to allow real-time transition to next lesson
        UpdateActiveLevelFromCurriculum();

        // Reset all moving platforms
        foreach (var platform in movingPlatforms)
        {
            if (platform != null)
            {
                platform.ResetPlatform();
            }
        }
    }

    /// <summary>
    /// Helper to find a child with a specific tag recursively.
    /// </summary>
    private Transform FindDeepChildWithTag(Transform parent, string tag)
    {
        if (parent.CompareTag(tag)) return parent;
        
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform result = FindDeepChildWithTag(parent.GetChild(i), tag);
            if (result != null) return result;
        }
        return null;
    }
}
