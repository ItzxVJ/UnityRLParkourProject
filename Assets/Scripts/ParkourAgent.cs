using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

/// <summary>
/// Reinforcement Learning Agent representing the Parkour Cube.
/// Implements continuous movement force, jumping, and observations.
/// Features manual Heuristic, 20-second timeout punishment, and automatic trial/time sign display updates.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ParkourAgent : Agent
{
    [Header("Movement Settings")]
    public float moveSpeed = 6f;
    public float maxSpeed = 6f;
    public float acceleration = 19f;
    public float jumpForce = 6.5f;
    public float jumpCooldown = 0.15f;

    private float lastJumpTime = -999f;

    [Header("Debugging / Metrics")]
    [Tooltip("Read-only view of grounded state")]
    [SerializeField] private bool isGrounded;
    [SerializeField] private int trialCount = 0;
    [SerializeField] private float episodeTimer = 0f;

    [Header("Sign Reference")]
    [Tooltip("Optional billboard text mesh to display stats")]
    public TextMesh infoBillboardText;

    private Rigidbody rb;
    private ParkourEnvironment environment;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private float previousDistance;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();

        // Freeze Rigidbody X and Z rotations to keep the cube upright
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Find parent environment
        environment = GetComponentInParent<ParkourEnvironment>();
        if (environment == null)
        {
            Debug.LogWarning("ParkourAgent: Parent ParkourEnvironment not found! Ensure agent is placed within an environment root.");
        }

        // Cache initial positions relative to parent/world
        initialPosition = transform.localPosition;
        initialRotation = transform.localRotation;
    }

    public override void OnEpisodeBegin()
    {
        // Increment trial counter
        trialCount++;

        // Reset episode timer
        episodeTimer = 0f;

        // Clear billboard reference to force finding the active layout billboard on reset
        infoBillboardText = null;

        // Reset velocity and position
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.localPosition = initialPosition;
        transform.localRotation = initialRotation;

        // Reset the environment obstacles
        if (environment != null)
        {
            environment.ResetEnvironment();

            if (environment.goalPlatform != null)
            {
                previousDistance = Vector3.Distance(transform.localPosition, environment.goalPlatform.localPosition);
            }
            else
            {
                previousDistance = 0f;
            }
        }
    }

    private void Update()
    {
        // Track running elapsed time
        episodeTimer += Time.unscaledDeltaTime;

        // Perform extremely robust ground check
        isGrounded = CheckGrounded();

        // Update the billboard sign if assigned
        UpdateSignBillboard();

        // 20-Second Timeout Punishment: force efficiency or get penalized
        if (episodeTimer >= 20.0f)
        {
            AddReward(-1.0f); // Punish significantly
            EndEpisode();
            if (!Academy.Instance.IsCommunicatorOn)
            {
                OnEpisodeBegin();
            }
        }
    }

    private bool CheckGrounded()
    {
        // Simple downward raycast from the cube center. Since cube is 1x1x1, half-height is 0.5.
        // We cast a tiny bit further (0.55) to detect the ground robustly.
        // We ensure we only ground-check against other colliders, not ourselves.
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 0.55f))
        {
            if (hit.collider != null && hit.collider.gameObject != gameObject)
            {
                return true;
            }
        }
        return false;
    }

    private void UpdateSignBillboard()
    {
        if (infoBillboardText != null)
        {
            infoBillboardText.text = $"TRIAL: {trialCount}\nTIME: {episodeTimer:F1}s / 20.0s";
        }
        else if (environment != null)
        {
            // Search strictly for the child named "StatsText" to avoid grabbing the level name billboard
            Transform statsTextTransform = FindDeepChildWithName(environment.transform, "StatsText");
            if (statsTextTransform != null)
            {
                infoBillboardText = statsTextTransform.GetComponent<TextMesh>();
                if (infoBillboardText != null)
                {
                    infoBillboardText.text = $"TRIAL: {trialCount}\nTIME: {episodeTimer:F1}s / 20.0s";
                }
            }
        }
    }

    private Transform FindDeepChildWithName(Transform parent, string name)
    {
        // Ignore inactive sub-layouts so we don't accidentally select inactive billboards
        if (!parent.gameObject.activeInHierarchy) return null;

        if (parent.name == name) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform result = FindDeepChildWithName(parent.GetChild(i), name);
            if (result != null) return result;
        }
        return null;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (environment == null || environment.goalPlatform == null)
        {
            // Fallback observation sizes (total of 11 floats)
            sensor.AddObservation(Vector3.zero); // Velocity (3)
            sensor.AddObservation(Vector3.zero); // Agent Position (3)
            sensor.AddObservation(Vector3.zero); // Relative Vector to Goal (3)
            sensor.AddObservation(0f);           // Goal Distance (1)
            sensor.AddObservation(false);        // Grounded (1)
            return;
        }

        // 1. Agent Local Velocity (3 floats) - teaches agent speed and direction limits
        Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);
        sensor.AddObservation(localVelocity);

        // 2. Agent Relative Position to environment root/start (3 floats)
        sensor.AddObservation(transform.localPosition);

        // 3. Goal Relative Vector (3 floats) - teaches agent direction to reach goal
        Vector3 relativeGoalPos = environment.goalPlatform.localPosition - transform.localPosition;
        sensor.AddObservation(relativeGoalPos);

        // 4. Goal distance (1 float) - teaches agent progress metric
        sensor.AddObservation(Vector3.Distance(transform.localPosition, environment.goalPlatform.localPosition));

        // 5. Grounded state (1 float/bool) - teaches agent when it's valid to jump
        sensor.AddObservation(isGrounded);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveX = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        float moveZ = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);
        float jumpAction = actions.ContinuousActions[2];

        // Runtime caps protect you from old Inspector overrides like moveSpeed=15/maxSpeed=10.
        float speedLimit = Mathf.Min(maxSpeed, 6f);
        float accelLimit = Mathf.Min(acceleration, 30f);

        if (environment != null && environment.levelIndex == 1)
        {
            speedLimit *= 1.05f;
        }

        Vector3 input = new Vector3(moveX, 0f, moveZ);
        if (input.sqrMagnitude > 1f)
        {
            input.Normalize();
        }

        Vector3 desiredVelocity = input * speedLimit;
        Vector3 currentHorizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        Vector3 velocityChange = desiredVelocity - currentHorizontalVelocity;
        velocityChange = Vector3.ClampMagnitude(velocityChange, accelLimit * Time.fixedDeltaTime);

        rb.AddForce(new Vector3(velocityChange.x, 0f, velocityChange.z), ForceMode.VelocityChange);

        Vector3 clampedHorizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        if (clampedHorizontalVelocity.magnitude > speedLimit)
        {
            clampedHorizontalVelocity = clampedHorizontalVelocity.normalized * speedLimit;
            rb.linearVelocity = new Vector3(
                clampedHorizontalVelocity.x,
                rb.linearVelocity.y,
                clampedHorizontalVelocity.z
            );
        }

        bool groundedNow = CheckGrounded();
        bool canJump = environment == null || environment.levelIndex != 1;
        bool jumpReady = Time.time - lastJumpTime >= jumpCooldown;

        if (jumpAction > 0.5f && groundedNow && canJump && jumpReady)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            lastJumpTime = Time.time;
        }

        if (environment != null && environment.goalPlatform != null)
        {
            float currentDistance = Vector3.Distance(transform.localPosition, environment.goalPlatform.localPosition);
            float progress = previousDistance - currentDistance;

            AddReward(progress * 0.1f);
            previousDistance = currentDistance;
        }

        if (environment != null && transform.localPosition.y < environment.fallThreshold)
        {
            AddReward(-1.0f);
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;
        
        float moveX = 0f;
        float moveZ = 0f;
        float jump = -1.0f;

        // Use New Input System Keyboard direct access to safely read states without legacy errors
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) moveZ = 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) moveZ = -1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) moveX = -1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) moveX = 1f;

            if (keyboard.spaceKey.isPressed) jump = 1.0f;
        }

        continuousActions[0] = moveX;
        continuousActions[1] = moveZ;
        continuousActions[2] = jump;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Safe check for goal platform contact
        if (collision.gameObject.CompareTag("Goal"))
        {
            AddReward(2.0f);
            EndEpisode();
            
        }
        else if (collision.gameObject.name == "GlowingLava")
        {
            // Lava touching penalty
            AddReward(-1.0f); // Standardized to -1.0f as requested
            EndEpisode();
            
        }
        else if (collision.gameObject.name.Contains("Obstacle"))
        {
            // Obstacle touching penalty (Level 1 Central Wall is WallObstacle)
            AddReward(-1.0f); // Standardized to -1.0f as requested
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Also support Goal contact via Trigger zones
        if (other.gameObject.CompareTag("Goal"))
        {
            AddReward(2.0f);
            EndEpisode();
           
            
        }
        else if (other.gameObject.name == "GlowingLava")
        {
            // Lava touching trigger penalty
            AddReward(-1.0f); // Standardized to -1.0f as requested
            EndEpisode();
           
        }
        else if (other.gameObject.name.Contains("Obstacle"))
        {
            // Obstacle trigger penalty
            AddReward(-1.0f); // Standardized to -1.0f as requested
            EndEpisode();
   
        }
    }
}
