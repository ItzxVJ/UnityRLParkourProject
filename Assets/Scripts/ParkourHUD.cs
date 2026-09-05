using UnityEngine;

/// <summary>
/// A gorgeous retro-candy themed Spectator HUD.
/// Automatically detects and tracks the nearest active agent to the main camera,
/// displaying real-time metrics, a live neon timer, and dynamic reward states.
/// </summary>
public class ParkourHUD : MonoBehaviour
{
    private GUIStyle headerStyle;
    private GUIStyle labelStyle;
    private GUIStyle valueStyle;
    private GUIStyle boxStyle;
    private Texture2D bgTexture;
    private Texture2D progressBarTexture;
    private Texture2D progressBgTexture;

    private void Awake()
    {
        // Prevent duplicate HUDs
        var existing = FindObjectsByType<ParkourHUD>(FindObjectsInactive.Include);
        if (existing.Length > 1)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        InitTextures();
    }

    private void InitTextures()
    {
        // Semi-transparent dark warm violet background
        bgTexture = CreateSolidColorTexture(new Color(0.08f, 0.05f, 0.15f, 0.85f));
        // Hot Neon Pink progress bar
        progressBarTexture = CreateSolidColorTexture(new Color(1f, 0.05f, 0.6f, 1f));
        // Darker violet background for progress bar
        progressBgTexture = CreateSolidColorTexture(new Color(0.16f, 0.12f, 0.3f, 1f));
    }

    private Texture2D CreateSolidColorTexture(Color col)
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, col);
        tex.Apply();
        return tex;
    }

    private void OnGUI()
    {
        InitializeStyles();

        // 1. Locate the nearest active agent to the spectator camera
        ParkourAgent activeAgent = GetNearestAgent();
        if (activeAgent == null) return;

        // Retrieve properties safely
        int trial = GetPrivateField<int>(activeAgent, "trialCount");
        float timer = GetPrivateField<float>(activeAgent, "episodeTimer");
        float cumulativeReward = activeAgent.GetCumulativeReward();
        
        string levelName = "UNKNOWN";
        int levelIndex = 1;
        var env = activeAgent.GetComponentInParent<ParkourEnvironment>();
        if (env != null)
        {
            levelIndex = env.levelIndex;
            string[] names = { "NAVIGATION", "SMALL GAPS", "PRECISION PADS", "ADVANCED RAMPS", "EXPERT COURSE" };
            if (levelIndex >= 1 && levelIndex <= 5) levelName = names[levelIndex - 1];
        }

        var behaviorParams = activeAgent.GetComponent<Unity.MLAgents.Policies.BehaviorParameters>();
        bool isHeuristic = behaviorParams != null && 
                            behaviorParams.BehaviorType == Unity.MLAgents.Policies.BehaviorType.HeuristicOnly;

        // 2. Render HUD Frame (Sleek corner panel)
        float panelWidth = 340f;
        float panelHeight = 210f;
        float margin = 20f;
        Rect panelRect = new Rect(margin, margin, panelWidth, panelHeight);

        GUI.Box(panelRect, "", boxStyle);

        // Header Title
        Rect headerRect = new Rect(panelRect.x + 15, panelRect.y + 15, panelWidth - 30, 25);
        GUI.Label(headerRect, "TRAINING BOB", headerStyle);

        // Active Level tracking
        Rect trackingRect = new Rect(panelRect.x + 15, panelRect.y + 45, panelWidth - 30, 20);
        labelStyle.normal.textColor = new Color(0f, 0.8f, 1f); // Electric Sky Blue
        GUI.Label(trackingRect, $"TRACKING: LEVEL {levelIndex} - {levelName}", labelStyle);

        // Reset label color for general labels
        labelStyle.normal.textColor = Color.white;

        // Stats rows
        float currentY = panelRect.y + 75;
        float rowHeight = 22f;

        RenderStatsRow("TRIAL NUMBER:", $"#{trial}", ref currentY, rowHeight, panelRect.x, panelWidth);
        RenderStatsRow("CUMULATIVE REWARD:", $"{cumulativeReward:F3}", ref currentY, rowHeight, panelRect.x, panelWidth);
        RenderStatsRow("CONTROL MODE:", isHeuristic ? "HEURISTIC (PLAYTEST)" : "AI BRAIN (INFERENCE)", ref currentY, rowHeight, panelRect.x, panelWidth);

        // 3. Render Custom Neon Timer Bar
        float barX = panelRect.x + 15;
        float barY = currentY + 10;
        float barWidth = panelWidth - 30;
        float barHeight = 16f;

        // Bar container
        GUI.DrawTexture(new Rect(barX, barY, barWidth, barHeight), progressBgTexture);
        
        // Fills up to 20 seconds
        float fillRatio = Mathf.Clamp01(timer / 20.0f);
        if (fillRatio > 0)
        {
            GUI.DrawTexture(new Rect(barX, barY, barWidth * fillRatio, barHeight), progressBarTexture);
        }

        // Live Timer Text Overlay inside/above the bar
        Rect timerTextRect = new Rect(barX, barY + 20, barWidth, 20);
        string timerText = $"TIME ELAPSED: {timer:F1}s / 20.0s";
        if (timer > 15.0f)
        {
            valueStyle.normal.textColor = new Color(1f, 0.2f, 0.2f); // Alert Red warning
        }
        else
        {
            valueStyle.normal.textColor = new Color(1f, 0.88f, 0f); // Lemon yellow
        }
        GUI.Label(timerTextRect, timerText, valueStyle);
    }

    private void RenderStatsRow(string label, string value, ref float currentY, float rowHeight, float panelX, float panelWidth)
    {
        Rect rowLabelRect = new Rect(panelX + 15, currentY, panelWidth - 30, rowHeight);
        Rect rowValueRect = new Rect(panelX + 175, currentY, panelWidth - 190, rowHeight);

        GUI.Label(rowLabelRect, label, labelStyle);
        GUI.Label(rowValueRect, value, valueStyle);

        currentY += rowHeight;
    }

    private void InitializeStyles()
    {
        if (boxStyle != null) return; // Already initialized

        // Create standard styles
        boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.normal.background = bgTexture;

        headerStyle = new GUIStyle();
        headerStyle.fontSize = 18;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.normal.textColor = new Color(1f, 0.05f, 0.6f); // Hot pink
        headerStyle.alignment = TextAnchor.MiddleLeft;

        labelStyle = new GUIStyle();
        labelStyle.fontSize = 13;
        labelStyle.fontStyle = FontStyle.Bold;
        labelStyle.normal.textColor = Color.white;
        labelStyle.alignment = TextAnchor.MiddleLeft;

        valueStyle = new GUIStyle();
        valueStyle.fontSize = 13;
        valueStyle.fontStyle = FontStyle.Bold;
        valueStyle.normal.textColor = new Color(1f, 0.88f, 0f); // Bright yellow
        valueStyle.alignment = TextAnchor.MiddleLeft;
    }

    private ParkourAgent GetNearestAgent()
    {
        ParkourAgent[] agents = FindObjectsByType<ParkourAgent>(FindObjectsInactive.Exclude);
        if (agents == null || agents.Length == 0) return null;

        ParkourAgent nearest = null;
        float minDistance = float.MaxValue;
        Vector3 camPos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;

        foreach (var agent in agents)
        {
            float dist = Vector3.Distance(agent.transform.position, camPos);
            if (dist < minDistance)
            {
                minDistance = dist;
                nearest = agent;
            }
        }
        return nearest;
    }

    /// <summary>
    /// Helper to grab serialized fields safely at runtime.
    /// </summary>
    private T GetPrivateField<T>(object target, string fieldName)
    {
        var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            return (T)field.GetValue(target);
        }
        return default;
    }
}
