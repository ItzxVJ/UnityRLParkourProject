using UnityEngine;

/// <summary>
/// Moves a platform smoothly between two local offsets using physics (Rigidbody.MovePosition).
/// Accumulates physics time manually using Time.fixedDeltaTime to guarantee deterministic movement
/// that perfectly syncs with ML-Agents training physics steps, regardless of training timeScale speed.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class MovingPlatform : MonoBehaviour
{
    [Header("Movement Offsets (Relative to Start)")]
    public Vector3 startOffset = Vector3.zero;
    public Vector3 endOffset = Vector3.zero;

    [Header("Speed Settings")]
    public float speed = 1.5f;

    private Rigidbody rb;
    private Vector3 localStartPos;
    private float accumulatedPhysicsTime = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true; // Must be kinematic to prevent falling or gravity effects
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        // Record starting local position
        localStartPos = transform.localPosition;
    }

    private void FixedUpdate()
    {
        if (startOffset == Vector3.zero && endOffset == Vector3.zero) return;

        // Manually accumulate time based on physics ticks (Time.fixedDeltaTime).
        // This keeps the platform movements perfectly deterministic and independent of Time.time scale issues.
        accumulatedPhysicsTime += Time.fixedDeltaTime * speed;

        // Smooth Ping-Pong factor (0 to 1)
        float t = Mathf.PingPong(accumulatedPhysicsTime, 1.0f);
        
        // Interpolate local position
        Vector3 targetLocalPos = localStartPos + Vector3.Lerp(startOffset, endOffset, t);
        
        // Convert local position to world position for Rigidbody.MovePosition
        Vector3 targetWorldPos = transform.parent != null 
            ? transform.parent.TransformPoint(targetLocalPos) 
            : targetLocalPos;

        rb.MovePosition(targetWorldPos);
    }

    /// <summary>
    /// Teleports the platform back to its starting state (called at episode begin).
    /// </summary>
    public void ResetPlatform()
    {
        transform.localPosition = localStartPos;
        accumulatedPhysicsTime = 0f; // Reset time so moving platform starts in the identical phase
        
        if (rb != null)
        {
            Vector3 worldPos = transform.parent != null 
                ? transform.parent.TransformPoint(localStartPos) 
                : localStartPos;
            rb.position = worldPos;
        }
    }
}

