using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;

/// <summary>
/// Editor utility to procedurally generate the 5 Synthwave-themed training levels and the parallelized Curriculum Training arena.
/// Automatically handles custom HDR materials, lighting, and Post-Processing volumes.
/// </summary>
public class ParkourLevelBuilder : EditorWindow
{
    private const string SHADER_NAME = "Universal Render Pipeline/Lit";

    // Colors mapping to bright, fun, neon-candy arcade palette
    private static readonly Color ColorDarkBg = new Color(0.12f, 0.06f, 0.22f, 1f); // Rich warm neon indigo-violet
    private static readonly Color ColorStart = new Color(0f, 0.8f, 1f, 1f);       // Bright Electric Sky Blue
    private static readonly Color ColorGoal = new Color(0.2f, 1f, 0.2f, 1f);        // Electric Lime Green
    private static readonly Color ColorStaticPlatform = new Color(0.16f, 0.12f, 0.35f, 1f); // Vibrant Tech Indigo/Blue
    private static readonly Color ColorObstacle = new Color(1f, 0.05f, 0.6f, 1f);   // Hot Neon Pink / Magenta
    private static readonly Color ColorMovingPlatform = new Color(1f, 0.88f, 0f, 1f); // Bright Gold/Lemon Yellow
    private static readonly Color ColorAgent = new Color(0f, 1f, 0.9f, 1f);    // Beautiful Glowing Electric Turquoise-Cyan
    private static readonly Color ColorLava = new Color(1.0f, 0.35f, 0.0f, 1.0f);   // High-Vibrancy Tangy Orange Lava

    [MenuItem("Parkour/Build All Synthwave Levels")]
    public static void BuildAll()
    {
        Debug.Log("ParkourBuilder: Defining custom tags...");
        RegisterTagsIfMissing("Start", "Goal");

        Debug.Log("ParkourBuilder: Generating materials...");
        Material matStart = CreateGlowMaterial("NeonCyan", ColorStart, 2.5f);
        Material matGoal = CreateGlowMaterial("NeonGreen", ColorGoal, 3.0f);
        Material matStatic = CreateMaterial("SlatePlatform", ColorStaticPlatform, 0.2f, 0.6f);
        Material matObstacle = CreateGlowMaterial("NeonMagenta", ColorObstacle, 2.0f);
        Material matMoving = CreateGlowMaterial("NeonOrange", ColorMovingPlatform, 2.5f);
        Material matAgent = CreateGlowMaterial("NeonAgent", ColorAgent, 1.5f);
        Material matLava = CreateGlowMaterial("NeonLava", ColorLava, 3.0f);
        Material matEye = CreateMaterial("GlossyBlackEye", new Color(0.05f, 0.05f, 0.05f, 1f), 0.9f, 0.9f);

        // Build 5 separate scenes
        for (int level = 1; level <= 5; level++)
        {
            BuildSingleLevelScene(level, matStart, matGoal, matStatic, matObstacle, matMoving, matAgent, matLava);
        }

        // Build Unified Curriculum Training Scene (3x3 grid of 9 parallel environments)
        BuildCurriculumTrainingScene(matStart, matGoal, matStatic, matObstacle, matMoving, matAgent, matLava);

        // Build Panoramic YouTube Showcase Scene (All 5 levels aligned side-by-side with neon labels)
        BuildYoutubeShowcaseScene(matStart, matGoal, matStatic, matObstacle, matMoving, matAgent, matLava);

        Debug.Log("ParkourBuilder: Generation complete! All levels created and saved in Assets/Scenes/");
        EditorUtility.DisplayDialog("Synthwave Levels Built", "All 5 training levels, the CurriculumTraining scene, and the YouTube Showcase scene have been successfully generated with high-fidelity Synthwave visual aesthetics!", "Awesome!");
    }

    private static void RegisterTagsIfMissing(params string[] tags)
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");
        
        foreach (string tag in tags)
        {
            bool found = false;
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag)
                {
                    found = true;
                    break;
                }
            }
            
            if (!found)
            {
                tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
            }
        }
        tagManager.ApplyModifiedProperties();
    }

    private static Material CreateMaterial(string name, Color color, float metallic, float smoothness)
    {
        string dir = "Assets/Materials";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        string path = $"{dir}/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader uShader = Shader.Find(SHADER_NAME);
            if (uShader == null) uShader = Shader.Find("Standard");
            mat = new Material(uShader);
            AssetDatabase.CreateAsset(mat, path);
        }
        
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_Smoothness", smoothness);
        
        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();
        return mat;
    }

    private static Material CreateGlowMaterial(string name, Color color, float emissionIntensity)
    {
        string dir = "Assets/Materials";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        string path = $"{dir}/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader uShader = Shader.Find(SHADER_NAME);
            if (uShader == null) uShader = Shader.Find("Standard");
            mat = new Material(uShader);
            AssetDatabase.CreateAsset(mat, path);
        }
        
        mat.SetColor("_BaseColor", color * 0.4f); // Subdued base color
        mat.SetFloat("_Metallic", 0.1f);
        mat.SetFloat("_Smoothness", 0.5f);

        // Turn on emission keywords and HDR color
        mat.EnableKeyword("_EMISSION");
        Color emissionColor = color * Mathf.Pow(2f, emissionIntensity);
        mat.SetColor("_EmissionColor", emissionColor);
        
        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();
        return mat;
    }

    private static void BuildSingleLevelScene(int level, Material matStart, Material matGoal, Material matStatic, Material matObstacle, Material matMoving, Material matAgent, Material matLava)
    {
        string sceneDir = "Assets/Scenes";
        if (!Directory.Exists(sceneDir)) Directory.CreateDirectory(sceneDir);

        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        
        // 1. Create Core Setup
        SetupEnvironmentCommon(null, level, matStart, matGoal, matStatic, matObstacle, matMoving, matAgent, matLava);

        // Save
        string scenePath = $"{sceneDir}/Level{level}.unity";
        EditorSceneManager.SaveScene(newScene, scenePath);
        Debug.Log($"Generated Scene: {scenePath}");
    }

    private static void BuildCurriculumTrainingScene(Material matStart, Material matGoal, Material matStatic, Material matObstacle, Material matMoving, Material matAgent, Material matLava)
    {
        string sceneDir = "Assets/Scenes";
        if (!Directory.Exists(sceneDir)) Directory.CreateDirectory(sceneDir);

        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Create lighting & camera
        CreateGlobalLightingAndCamera();

        // Spawn a 3x3 Grid of 9 parallel training environments - widely spaced
        float gridSpacingX = 80f;
        float gridSpacingZ = 120f;
        int gridIndex = 0;

        GameObject trainingRoot = new GameObject("CurriculumTrainingGrid");

        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                Vector3 positionOffset = new Vector3(x * gridSpacingX, 0f, z * gridSpacingZ);
                GameObject envObj = new GameObject($"TrainingArena_{gridIndex}");
                envObj.transform.SetParent(trainingRoot.transform);
                envObj.transform.position = positionOffset;

                ParkourEnvironment env = envObj.AddComponent<ParkourEnvironment>();
                env.levelIndex = 1; // Default starting curriculum level index
                env.fallThreshold = -10f;

                // Create agent inside this arena
                GameObject agentObj = CreateAgentCube(envObj.transform, matAgent);
                env.agent = agentObj.transform;

                // Create the 5 sub-level containers inside this specific arena (these will toggle dynamically based on curriculum parameter)
                env.levelContainers = new GameObject[5];
                for (int level = 1; level <= 5; level++)
                {
                    GameObject levelContainer = new GameObject($"Level_{level}_Layout");
                    levelContainer.transform.SetParent(envObj.transform, false);

                    // Build layout specifically for this sub-level inside the container
                    BuildLevelGeometry(levelContainer.transform, level, matStart, matGoal, matStatic, matObstacle, matMoving, matLava);

                    env.levelContainers[level - 1] = levelContainer;
                }

                // Call initial curriculum switch logic to activate Level 1 structure by default
                env.UpdateActiveLevelFromCurriculum();

                gridIndex++;
            }
        }

        // Save Scene
        string scenePath = $"{sceneDir}/CurriculumTraining.unity";
        EditorSceneManager.SaveScene(newScene, scenePath);
        Debug.Log($"Generated Unified Curriculum Training Scene: {scenePath}");
    }

    private static void BuildYoutubeShowcaseScene(Material matStart, Material matGoal, Material matStatic, Material matObstacle, Material matMoving, Material matAgent, Material matLava)
    {
        string sceneDir = "Assets/Scenes";
        if (!Directory.Exists(sceneDir)) Directory.CreateDirectory(sceneDir);

        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Create lighting & camera
        CreateGlobalLightingAndCamera();

        // Spawn a row of 5 showcase environments side-by-side - spaced out wide
        float spacingX = 45f; // Spaced 45m apart on X-axis (was 18m)
        GameObject showcaseRoot = new GameObject("YoutubeShowcaseArena");

        string[] levelNames = new string[] { "L1: NAVIGATION", "L2: SMALL GAPS", "L3: PRECISION PADS", "L4: ADVANCED RAMPS", "L5: EXPERT COURSE" };
        Color[] labelColors = new Color[] { ColorStart, ColorObstacle, ColorMovingPlatform, ColorObstacle, ColorGoal };

        for (int i = 0; i < 5; i++)
        {
            int levelIndex = i + 1;
            Vector3 posOffset = new Vector3((i - 2) * spacingX, 0f, 0f); // Center Level 3 at X=0
            GameObject envObj = new GameObject($"Showcase_Level_{levelIndex}");
            envObj.transform.SetParent(showcaseRoot.transform);
            envObj.transform.position = posOffset;

            ParkourEnvironment env = envObj.AddComponent<ParkourEnvironment>();
            env.levelIndex = levelIndex;
            env.fallThreshold = -10f;

            // Instantiate Layout
            GameObject levelContainer = new GameObject($"Level_{levelIndex}_Layout");
            levelContainer.transform.SetParent(envObj.transform, false);
            BuildLevelGeometry(levelContainer.transform, levelIndex, matStart, matGoal, matStatic, matObstacle, matMoving, matLava);

            // Set up environment references
            Transform startPlat = levelContainer.transform.Find("StartPlatform");
            if (startPlat != null) env.startPlatform = startPlat;

            Transform goalPlat = levelContainer.transform.Find("GoalPlatform");
            if (goalPlat != null) env.goalPlatform = goalPlat;

            // Agent Setup
            GameObject agentObj = CreateAgentCube(envObj.transform, matAgent);
            agentObj.transform.localPosition = new Vector3(0f, 1.2f, 0f); // Position relative to layout start
            env.agent = agentObj.transform;
        }

        // Adjust Main Camera for the ultimate Showcase panoramic angle
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(0f, 22f, -28f);
            cam.transform.rotation = Quaternion.Euler(28f, 0f, 0f); // Panoramic high-angle face straight forward
            cam.fieldOfView = 65f;
        }

        // Save Scene
        string scenePath = $"{sceneDir}/YoutubeShowcase.unity";
        EditorSceneManager.SaveScene(newScene, scenePath);
        Debug.Log($"Generated Youtube Showcase Scene: {scenePath}");
    }

    private static void CreateFloatingShowcaseLabel(Transform parent, string text, Color emissiveColor, Material matStatic)
    {
        // 1. Create an unscaled Billboard Root to hold frame and text separately
        GameObject billboardRoot = new GameObject("ShowcaseBillboard_Root");
        billboardRoot.transform.SetParent(parent, false);
        billboardRoot.transform.localPosition = new Vector3(0f, 6.5f, -1f); // Float 6.5m above start platform, slightly back

        // 2. Create the backing frame
        GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
        frame.name = "BillboardFrame";
        frame.transform.SetParent(billboardRoot.transform, false);
        frame.transform.localPosition = Vector3.zero;
        frame.transform.localScale = new Vector3(8.5f, 1.4f, 0.2f);
        frame.GetComponent<Renderer>().sharedMaterial = matStatic;

        // Glowing border lines on the frame
        GameObject glowBorderTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
        glowBorderTop.name = "BorderTop";
        glowBorderTop.transform.SetParent(frame.transform, false);
        glowBorderTop.transform.localPosition = new Vector3(0f, 0.55f, 0f);
        glowBorderTop.transform.localScale = new Vector3(1f, 0.1f, 1.1f);
        
        Material matGlow = CreateGlowMaterial($"TempLabel_{emissiveColor.r:F2}_{emissiveColor.g:F2}", emissiveColor, 2.5f);
        glowBorderTop.GetComponent<Renderer>().sharedMaterial = matGlow;

        GameObject glowBorderBottom = GameObject.CreatePrimitive(PrimitiveType.Cube);
        glowBorderBottom.name = "BorderBottom";
        glowBorderBottom.transform.SetParent(frame.transform, false);
        glowBorderBottom.transform.localPosition = new Vector3(0f, -0.55f, 0f);
        glowBorderBottom.transform.localScale = new Vector3(1f, 0.1f, 1.1f);
        glowBorderBottom.GetComponent<Renderer>().sharedMaterial = matGlow;

        // 3. Create the TextMesh Component directly on the unscaled root
        GameObject textObj = new GameObject("LabelText");
        textObj.transform.SetParent(billboardRoot.transform, false);
        textObj.transform.localPosition = new Vector3(0f, -0.1f, -0.15f); // Slightly forward from the board
        textObj.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f); // Uniform unskewed local scale

        TextMesh tm = textObj.AddComponent<TextMesh>();
        tm.text = text;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.fontSize = 64; // High resolution font size
        tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Set the text color to white/glow
        MeshRenderer mr = textObj.GetComponent<MeshRenderer>();
        mr.sharedMaterial = matGlow;

        // Attach FaceCamera to the entire root so both board and text rotate together
        billboardRoot.AddComponent<FaceCamera>();
    }

    private static void SetupEnvironmentCommon(GameObject parent, int level, Material matStart, Material matGoal, Material matStatic, Material matObstacle, Material matMoving, Material matAgent, Material matLava)
    {
        GameObject envRootObj = new GameObject($"ParkourEnvironment_Level{level}");
        if (parent != null) envRootObj.transform.SetParent(parent.transform);

        ParkourEnvironment env = envRootObj.AddComponent<ParkourEnvironment>();
        env.levelIndex = level;
        env.fallThreshold = -8f;

        // Build Level Layout directly under envRootObj
        BuildLevelGeometry(envRootObj.transform, level, matStart, matGoal, matStatic, matObstacle, matMoving, matLava);

        // Instantiate Agent
        GameObject agentObj = CreateAgentCube(envRootObj.transform, matAgent);
        env.agent = agentObj.transform;

        // Find start position and goal to set environment properties (for non-grid direct reference)
        Transform startPlat = envRootObj.transform.Find("StartPlatform");
        if (startPlat != null) env.startPlatform = startPlat;

        Transform goalPlat = envRootObj.transform.Find("GoalPlatform");
        if (goalPlat != null) env.goalPlatform = goalPlat;

        // Set up scene-level camera/lighting if this is a single level scene (parent is null)
        if (parent == null)
        {
            CreateGlobalLightingAndCamera();
        }
    }

    private static GameObject CreateAgentCube(Transform parent, Material matAgent)
    {
        GameObject agentObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        agentObj.name = "ParkourAgent";
        agentObj.transform.SetParent(parent, false);
        agentObj.transform.localPosition = new Vector3(0f, 1.2f, 0f); // Reset relative to local platform start position (0, 0.5, 0) + offset
        
        // Add tag
        agentObj.tag = "Player";

        // Render visual
        agentObj.GetComponent<Renderer>().sharedMaterial = matAgent;

        // --- CUTE GLOWING ROBOT EYES SETUP ---
        Material matEye = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/GlossyBlackEye.mat");
        if (matEye == null)
        {
            matEye = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            matEye.color = new Color(0.05f, 0.05f, 0.05f, 1f);
        }

        Material matPupil = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/NeonCyan.mat");
        if (matPupil == null)
        {
            matPupil = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            matPupil.color = new Color(0f, 0.94f, 1f, 1f);
        }

        // Left Eye (Glossy Black backing sphere)
        GameObject leftEye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        leftEye.name = "LeftEye";
        leftEye.transform.SetParent(agentObj.transform, false);
        leftEye.transform.localPosition = new Vector3(-0.22f, 0.15f, 0.48f);
        leftEye.transform.localScale = new Vector3(0.18f, 0.18f, 0.18f);
        leftEye.GetComponent<Renderer>().sharedMaterial = matEye;

        // Left Pupil (Glowing Neon Cyan sphere)
        GameObject leftPupil = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        leftPupil.name = "LeftPupil";
        leftPupil.transform.SetParent(leftEye.transform, false);
        leftPupil.transform.localPosition = new Vector3(0f, 0f, 0.35f);
        leftPupil.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
        leftPupil.GetComponent<Renderer>().sharedMaterial = matPupil;

        // Right Eye (Glossy Black backing sphere)
        GameObject rightEye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        rightEye.name = "RightEye";
        rightEye.transform.SetParent(agentObj.transform, false);
        rightEye.transform.localPosition = new Vector3(0.22f, 0.15f, 0.48f);
        rightEye.transform.localScale = new Vector3(0.18f, 0.18f, 0.18f);
        rightEye.GetComponent<Renderer>().sharedMaterial = matEye;

        // Right Pupil (Glowing Neon Cyan sphere)
        GameObject rightPupil = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        rightPupil.name = "RightPupil";
        rightPupil.transform.SetParent(rightEye.transform, false);
        rightPupil.transform.localPosition = new Vector3(0f, 0f, 0.35f);
        rightPupil.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
        rightPupil.GetComponent<Renderer>().sharedMaterial = matPupil;
        // -------------------------------------

        // Rigidbody
        Rigidbody rb = agentObj.AddComponent<Rigidbody>();
        rb.mass = 1.0f;
        rb.linearDamping = 0.1f;
        rb.angularDamping = 0.05f;

        // Script
        agentObj.AddComponent<ParkourAgent>();

        // Decision Requester component for automatic MLAgents step decisions
        var decisionRequester = agentObj.GetComponent<Unity.MLAgents.DecisionRequester>();
        if (decisionRequester == null) decisionRequester = agentObj.AddComponent<Unity.MLAgents.DecisionRequester>();
        decisionRequester.DecisionPeriod = 5;

        // Behavior Parameters for ML-Agents config
        var behaviorParams = agentObj.GetComponent<Unity.MLAgents.Policies.BehaviorParameters>();
        if (behaviorParams == null) behaviorParams = agentObj.AddComponent<Unity.MLAgents.Policies.BehaviorParameters>();
        behaviorParams.BehaviorName = "ParkourCube";
        
        // Specify Action Spec: 3 Continuous actions
        behaviorParams.BrainParameters.ActionSpec = new Unity.MLAgents.Actuators.ActionSpec(3, new int[0]);
        // Specify Observation Vector: 11 float dimensions
        behaviorParams.BrainParameters.VectorObservationSize = 11;
        
        return agentObj;
    }

    private static void BuildLevelGeometry(Transform container, int level, Material matStart, Material matGoal, Material matStatic, Material matObstacle, Material matMoving, Material matLava)
    {
        // 1. Start Platform
        GameObject startObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        startObj.name = "StartPlatform";
        startObj.tag = "Start";
        startObj.transform.SetParent(container, false);
        startObj.transform.localPosition = new Vector3(0f, 0f, 0f);
        startObj.transform.localScale = new Vector3(6f, 1f, 6f);
        startObj.GetComponent<Renderer>().sharedMaterial = matStart;

        // Visual and physical boundaries (Solid Neon Walls and Glowing Lava Floor)
        BuildVisualFraming(container, level, matStatic, matObstacle, matLava);

        switch (level)
        {
            case 1:
                // Level 1: Basic Navigation
                // Flat wide course, one obstacle wall requiring lateral traversal.
                // Learning Goal: Move forward, discover the Goal, navigate around basic static obstacles.
                
                // Segment 1 Ground
                CreateStaticCube(container, "Ground_Section", new Vector3(0f, 0f, 12f), new Vector3(6f, 1f, 18f), matStatic);

                // Central Obstacle Wall
                CreateStaticCube(container, "WallObstacle", new Vector3(0f, 1.5f, 12f), new Vector3(3f, 2f, 1f), matObstacle);

                // Goal platform
                CreateGoalPlatform(container, new Vector3(0f, 0f, 24f), matGoal);
                break;

            case 2:
                // Level 2: Small Gaps
                // Introducing gaps, require timing jumps, large forgiving platforms.
                // Learning Goal: Jump timing, combining movement with a simple jump impulse over simple terrain gaps.
                
                // Platform 1 (Gap 1 is between start and this platform)
                CreateStaticCube(container, "JumpPlatform_1", new Vector3(0f, 0f, 8f), new Vector3(5f, 1f, 6f), matStatic);

                // Platform 2
                CreateStaticCube(container, "JumpPlatform_2", new Vector3(1.5f, 0f, 16f), new Vector3(5f, 1f, 6f), matStatic);

                // Goal Platform
                CreateGoalPlatform(container, new Vector3(0f, 0f, 24f), matGoal);
                break;

            case 3:
                // Level 3: Precision Platforming
                // Narrower, sequential column pads, high/low elevation steps.
                // Learning Goal: Combining precise movement with accurate jump magnitude adjustments on tight footprints.
                
                // Step 1 Column
                CreateStaticCube(container, "Column_1", new Vector3(-1.5f, 0.25f, 7f), new Vector3(2.5f, 1.5f, 2.5f), matStatic);

                // Step 2 Column
                CreateStaticCube(container, "Column_2", new Vector3(1.5f, 0.75f, 13f), new Vector3(2f, 2.5f, 2f), matStatic);

                // Step 3 Column
                CreateStaticCube(container, "Column_3", new Vector3(-1.0f, 0.5f, 19f), new Vector3(2.2f, 2f, 2.2f), matStatic);

                // Goal Platform
                CreateGoalPlatform(container, new Vector3(0f, 0.5f, 26f), matGoal);
                break;

            case 4:
                // Level 4: Advanced Parkour
                // Introduce moving platforms, angled sloped ramps, tall jump over walls.
                // Learning Goal: Master dynamic/moving obstacles, momentum translation, and slope climbing.
                
                // Angle Ramp up
                GameObject ramp = CreateStaticCube(container, "Ramp_Up", new Vector3(0f, 0.5f, 7f), new Vector3(3.5f, 0.5f, 6f), matStatic);
                ramp.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);

                // Flat rest platform
                CreateStaticCube(container, "MiddlePlatform", new Vector3(0f, 1.3f, 13f), new Vector3(4f, 1f, 4f), matStatic);

                // Dynamic Moving Platform (moves horizontally left-right)
                GameObject movPlat = CreateMovingPlatform(container, "HorizontalMovingPlatform", new Vector3(0f, 1.3f, 19f), new Vector3(3f, 0.5f, 3f), matMoving);
                MovingPlatform mp = movPlat.GetComponent<MovingPlatform>();
                mp.startOffset = new Vector3(-3.5f, 0f, 0f);
                mp.endOffset = new Vector3(3.5f, 0f, 0f);
                mp.speed = 1.2f;

                // Goal platform
                CreateGoalPlatform(container, new Vector3(0f, 1.0f, 26f), matGoal);
                break;

            case 5:
                // Level 5: Expert Course
                // Masterclass combining narrow beams, multiple moving platforms, vertical climbing, and a final deep precision jump.
                // Learning Goal: Complete synthesis of precision speed, dynamic obstacle timing, elevation climbing, and directional quick-turns.
                
                // Narrow Beam Bridge (width = 1m)
                CreateStaticCube(container, "NarrowBeam", new Vector3(0f, 0f, 7f), new Vector3(1.2f, 1f, 6f), matStatic);

                // Moving Platform 1 (Vertically moving platform)
                GameObject movPlatV = CreateMovingPlatform(container, "VerticalMovingPlatform", new Vector3(0f, 0.5f, 13f), new Vector3(3f, 0.5f, 3f), matMoving);
                MovingPlatform mpV = movPlatV.GetComponent<MovingPlatform>();
                mpV.startOffset = new Vector3(0f, -0.5f, 0f);
                mpV.endOffset = new Vector3(0f, 3.5f, 0f);
                mpV.speed = 1.0f;

                // High Staggered Shelf 1 (Left side climb)
                CreateStaticCube(container, "StaggeredClimb_1", new Vector3(-2.5f, 2f, 18f), new Vector3(2f, 0.5f, 2f), matStatic);

                // High Staggered Shelf 2 (Right side climb)
                CreateStaticCube(container, "StaggeredClimb_2", new Vector3(2.5f, 3.5f, 18f), new Vector3(2f, 0.5f, 2f), matStatic);

                // High Bridge
                CreateStaticCube(container, "HighWalkway", new Vector3(0f, 4.5f, 23f), new Vector3(3f, 0.5f, 4f), matStatic);

                // Moving Platform 2 (Fast horizontal gap crossing)
                GameObject movPlatH = CreateMovingPlatform(container, "HorizontalGapCrosser", new Vector3(0f, 4.5f, 29f), new Vector3(2.5f, 0.4f, 2.5f), matMoving);
                MovingPlatform mpH = movPlatH.GetComponent<MovingPlatform>();
                mpH.startOffset = new Vector3(-4f, 0f, 0f);
                mpH.endOffset = new Vector3(4f, 0f, 0f);
                mpH.speed = 1.8f;

                // Goal Platform (at a distance, requires a massive jump from the moving platform)
                CreateGoalPlatform(container, new Vector3(0f, 3.5f, 37f), matGoal);
                break;
        }
    }

    private static GameObject CreateStaticCube(Transform parent, string name, Vector3 localPos, Vector3 scale, Material mat)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = localPos;
        cube.transform.localScale = scale;
        cube.GetComponent<Renderer>().sharedMaterial = mat;
        return cube;
    }

    private static GameObject CreateGoalPlatform(Transform parent, Vector3 localPos, Material matGoal)
    {
        GameObject goalObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        goalObj.name = "GoalPlatform";
        goalObj.tag = "Goal";
        goalObj.transform.SetParent(parent, false);
        goalObj.transform.localPosition = localPos;
        goalObj.transform.localScale = new Vector3(5f, 1f, 5f);
        goalObj.GetComponent<Renderer>().sharedMaterial = matGoal;

        // Give it a child trigger zone slightly raised to catch floating collisions perfectly
        GameObject triggerObj = new GameObject("GoalTriggerZone");
        triggerObj.transform.SetParent(goalObj.transform, false);
        triggerObj.transform.localPosition = new Vector3(0f, 0.75f, 0f);
        triggerObj.transform.localScale = new Vector3(0.95f, 1.5f, 0.95f);
        triggerObj.tag = "Goal";

        BoxCollider boxCol = triggerObj.AddComponent<BoxCollider>();
        boxCol.isTrigger = true;

        return goalObj;
    }

    private static GameObject CreateMovingPlatform(Transform parent, string name, Vector3 localPos, Vector3 scale, Material matMoving)
    {
        GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
        platform.name = name;
        platform.transform.SetParent(parent, false);
        platform.transform.localPosition = localPos;
        platform.transform.localScale = scale;
        platform.GetComponent<Renderer>().sharedMaterial = matMoving;

        // Setup moving platform physics
        Rigidbody rb = platform.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        platform.AddComponent<MovingPlatform>();

        return platform;
    }

    private static void BuildVisualFraming(Transform container, int level, Material matStatic, Material matObstacle, Material matLava)
    {
        float courseWidth = 14f; 
        float roomLength = 54f;
        float roomCenterZ = 18.5f;
        float roomHeight = 18f;
        float roomCenterY = 5f;

        GameObject framingRoot = new GameObject("VisualArenaFraming");
        framingRoot.transform.SetParent(container, false);

        // 1. Solid Left Wall
        GameObject wallLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wallLeft.name = "SolidWall_Left";
        wallLeft.transform.SetParent(framingRoot.transform, false);
        wallLeft.transform.localPosition = new Vector3(-courseWidth, roomCenterY, roomCenterZ);
        wallLeft.transform.localScale = new Vector3(0.5f, roomHeight, roomLength);
        wallLeft.GetComponent<Renderer>().sharedMaterial = matStatic;

        // Glowing horizontal neon stripe on left wall
        GameObject stripLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stripLeft.name = "NeonStrip_Left";
        stripLeft.transform.SetParent(wallLeft.transform, false);
        stripLeft.transform.localPosition = new Vector3(0.06f, 0f, 0f); // Exposed inner edge
        stripLeft.transform.localScale = new Vector3(1.1f, 0.05f, 1.0f);
        stripLeft.GetComponent<Renderer>().sharedMaterial = matObstacle;

        // 2. Solid Right Wall
        GameObject wallRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wallRight.name = "SolidWall_Right";
        wallRight.transform.SetParent(framingRoot.transform, false);
        wallRight.transform.localPosition = new Vector3(courseWidth, roomCenterY, roomCenterZ);
        wallRight.transform.localScale = new Vector3(0.5f, roomHeight, roomLength);
        wallRight.GetComponent<Renderer>().sharedMaterial = matStatic;

        // Glowing horizontal neon stripe on right wall
        GameObject stripRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stripRight.name = "NeonStrip_Right";
        stripRight.transform.SetParent(wallRight.transform, false);
        stripRight.transform.localPosition = new Vector3(-0.06f, 0f, 0f); // Exposed inner edge
        stripRight.transform.localScale = new Vector3(1.1f, 0.05f, 1.0f);
        stripRight.GetComponent<Renderer>().sharedMaterial = matObstacle;

        // 3. Front Wall (behind start platform)
        GameObject wallFront = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wallFront.name = "SolidWall_Front";
        wallFront.transform.SetParent(framingRoot.transform, false);
        wallFront.transform.localPosition = new Vector3(0f, roomCenterY, -8.5f); // Behind start platform (Z=-3)
        wallFront.transform.localScale = new Vector3(courseWidth * 2f, roomHeight, 0.5f);
        wallFront.GetComponent<Renderer>().sharedMaterial = matStatic;

        // Glowing neon strip on front wall
        GameObject stripFront = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stripFront.name = "NeonStrip_Front";
        stripFront.transform.SetParent(wallFront.transform, false);
        stripFront.transform.localPosition = new Vector3(0f, 0f, 0.06f);
        stripFront.transform.localScale = new Vector3(1.0f, 0.05f, 1.1f);
        stripFront.GetComponent<Renderer>().sharedMaterial = matObstacle;

        // 4. Back Wall (behind goal platform)
        GameObject wallBack = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wallBack.name = "SolidWall_Back";
        wallBack.transform.SetParent(framingRoot.transform, false);
        wallBack.transform.localPosition = new Vector3(0f, roomCenterY, 45.5f); // Behind maximum goal platform (Z=37)
        wallBack.transform.localScale = new Vector3(courseWidth * 2f, roomHeight, 0.5f);
        wallBack.GetComponent<Renderer>().sharedMaterial = matStatic;

        // Glowing neon strip on back wall
        GameObject stripBack = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stripBack.name = "NeonStrip_Back";
        stripBack.transform.SetParent(wallBack.transform, false);
        stripBack.transform.localPosition = new Vector3(0f, 0f, -0.06f);
        stripBack.transform.localScale = new Vector3(1.0f, 0.05f, 1.1f);
        stripBack.GetComponent<Renderer>().sharedMaterial = matObstacle;

        // 5. Solid Roof (Ceiling)
        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.name = "SolidRoof";
        roof.transform.SetParent(framingRoot.transform, false);
        roof.transform.localPosition = new Vector3(0f, roomCenterY + (roomHeight / 2f), roomCenterZ);
        roof.transform.localScale = new Vector3(courseWidth * 2f, 0.5f, roomLength);
        roof.GetComponent<Renderer>().sharedMaterial = matStatic;

        // Glowing neon center stripe on the ceiling
        GameObject stripRoof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stripRoof.name = "NeonStrip_Roof";
        stripRoof.transform.SetParent(roof.transform, false);
        stripRoof.transform.localPosition = new Vector3(0f, -0.06f, 0f);
        stripRoof.transform.localScale = new Vector3(0.05f, 1.1f, 1.0f);
        stripRoof.GetComponent<Renderer>().sharedMaterial = matObstacle;

        // 6. Glowing Hot Lava Floor filling the entire base below the platforms
        GameObject lavaObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lavaObj.name = "GlowingLava";
        lavaObj.transform.SetParent(framingRoot.transform, false);
        // Position at Y=-4f so it sits beautifully below the platforms (at Y=0 to Y=4.5)
        lavaObj.transform.localPosition = new Vector3(0f, -4f, roomCenterZ);
        lavaObj.transform.localScale = new Vector3(courseWidth * 2f, 0.2f, roomLength);
        lavaObj.GetComponent<Renderer>().sharedMaterial = matLava;
    }

    private static void CreateGlobalLightingAndCamera()
    {
        // Ambient Light setup - made significantly brighter and warmer to align with retro-arcade aesthetic
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = ColorDarkBg * 0.45f;
        RenderSettings.skybox = null; // Clear skybox for deep solid black sci-fi grid aesthetics

        // Main camera configuration
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            cam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
        }
        
        cam.transform.position = new Vector3(10f, 15f, -12f);
        cam.transform.rotation = Quaternion.Euler(30f, -35f, 0f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = ColorDarkBg;
        cam.orthographic = false;
        cam.fieldOfView = 60f;

        // Automatically attach the professional free-look SpectatorCamera component
        if (cam.gameObject.GetComponent<SpectatorCamera>() == null)
        {
            cam.gameObject.AddComponent<SpectatorCamera>();
        }

        // Automatically spawn the Spectator HUD overlay if not already in scene
        if (GameObject.Find("SpectatorHUD") == null)
        {
            GameObject hudObj = new GameObject("SpectatorHUD");
            hudObj.AddComponent<ParkourHUD>();
        }

        // Directional Light
        Light light = null;
        Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (var l in lights)
        {
            if (l.type == LightType.Directional)
            {
                light = l;
                break;
            }
        }
        if (light == null)
        {
            GameObject lightObj = new GameObject("Directional Light");
            light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
        }
        light.transform.rotation = Quaternion.Euler(45f, 45f, 0f);
        light.color = new Color(0.85f, 0.88f, 1f);
        light.intensity = 1.3f; // Made significantly brighter (was 0.5f) to ensure clean, high-contrast visibility
        light.shadows = LightShadows.Soft;

        // Cinematic Post-Processing Bloom Volume
        GameObject ppVolume = new GameObject("Cinematic_Bloom_Volume");
        var volume = ppVolume.AddComponent<UnityEngine.Rendering.Volume>();
        volume.isGlobal = true;
        volume.weight = 1.0f;

        // Generate custom volume profile if required
        UnityEngine.Rendering.VolumeProfile profile = ScriptableObject.CreateInstance<UnityEngine.Rendering.VolumeProfile>();
        profile.name = "SynthwaveCinematicProfile";

        // Procedurally add Bloom & Vignette & ColorAdjustments to the Volume
        var bloom = profile.Add<UnityEngine.Rendering.Universal.Bloom>(true);
        bloom.active = true;
        bloom.threshold.Override(0.80f);
        bloom.intensity.Override(2.5f);
        bloom.scatter.Override(0.65f);
        bloom.tint.Override(Color.white);

        var vignette = profile.Add<UnityEngine.Rendering.Universal.Vignette>(true);
        vignette.active = true;
        vignette.intensity.Override(0.35f);
        vignette.smoothness.Override(0.45f);
        vignette.color.Override(Color.black);

        var tonemapping = profile.Add<UnityEngine.Rendering.Universal.Tonemapping>(true);
        tonemapping.active = true;
        tonemapping.mode.Override(UnityEngine.Rendering.Universal.TonemappingMode.ACES);

        var colorAdjustments = profile.Add<UnityEngine.Rendering.Universal.ColorAdjustments>(true);
        colorAdjustments.active = true;
        colorAdjustments.contrast.Override(15f);
        colorAdjustments.saturation.Override(15f);

        // Save Volume Profile to Assets so it persists
        string profileDir = "Assets/Settings";
        if (!Directory.Exists(profileDir)) Directory.CreateDirectory(profileDir);
        string profilePath = $"{profileDir}/SynthwaveCinematicProfile.asset";
        AssetDatabase.CreateAsset(profile, profilePath);
        AssetDatabase.SaveAssets();

        volume.sharedProfile = profile;
    }
}
