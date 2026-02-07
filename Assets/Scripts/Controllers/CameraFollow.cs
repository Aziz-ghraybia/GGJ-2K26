using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("Camera Settings")]
    public Vector3 offset = new Vector3(0, 2, -10);
    public float smoothSpeed = 5f;

    [Header("Boundaries (Optional)")]
    public bool useBounds = false;
    public Vector2 minBounds = new Vector2(-50, 0);
    public Vector2 maxBounds = new Vector2(50, 10);

    void LateUpdate()
    {
        if (player == null) return;

        // Calculate target position
        Vector3 targetPosition = player.position + offset;

        // Apply boundaries if enabled
        if (useBounds)
        {
            targetPosition.x = Mathf.Clamp(targetPosition.x, minBounds.x, maxBounds.x);
            targetPosition.y = Mathf.Clamp(targetPosition.y, minBounds.y, maxBounds.y);
        }

        // Smooth follow
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
    }

    void OnDrawGizmosSelected()
    {
        if (useBounds)
        {
            // Draw boundary box
            Gizmos.color = Color.yellow;
            Vector3 center = new Vector3((minBounds.x + maxBounds.x) / 2, (minBounds.y + maxBounds.y) / 2, offset.z);
            Vector3 size = new Vector3(maxBounds.x - minBounds.x, maxBounds.y - minBounds.y, 0.1f);
            Gizmos.DrawWireCube(center, size);
        }
    }
}
