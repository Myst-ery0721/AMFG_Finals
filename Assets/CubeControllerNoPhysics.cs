using UnityEngine;

/// <summary>
/// Pure-math cube controller (no colliders, no Rigidbody).
/// - Gravity applied in FixedUpdate
/// - Jump with Space
/// - Swept vertical collision test to prevent tunneling (works for downward & upward moves)
/// - Changes color to green on collision
/// </summary>
public class CubeControllerNoPhysics : MonoBehaviour
{
    [Header("References / sizes (set by generator)")]
    public Scene.PlatformData platform;
    public Vector3 cubeSize = Vector3.one; // full size in x,y,z

    [Header("Physics parameters")]
    public float gravity = -20f;               // stronger gravity for games feel
    public float jumpVelocity = 8f;
    public float terminalVelocity = -50f;

    [Header("Movement")]
    public Vector3 velocity = Vector3.zero;

    // internals
    private Renderer rend;
    private bool isGrounded = false;

    private void Awake()
    {
        rend = GetComponent<Renderer>();
        if (rend == null) enabled = false;
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        // apply gravity
        velocity.y += gravity * dt;
        if (velocity.y < terminalVelocity) velocity.y = terminalVelocity;

        Vector3 pos = transform.position;
        Vector3 nextPos = pos + velocity * dt;

        // Perform swept vertical collision against the single horizontal platform.
        // We'll only check vertical crossing and XZ overlap to keep it simple and robust for a horizontal platform.
        if (platform != null)
        {
            float cubeBottomNow = pos.y - cubeSize.y * 0.5f;
            float cubeBottomNext = nextPos.y - cubeSize.y * 0.5f;
            float platformTop = platform.TopY;

            // XZ AABB corners of cube (projected)
            float cubeMinX = Mathf.Min(pos.x - cubeSize.x * 0.5f, nextPos.x - cubeSize.x * 0.5f);
            float cubeMaxX = Mathf.Max(pos.x + cubeSize.x * 0.5f, nextPos.x + cubeSize.x * 0.5f);
            float cubeMinZ = Mathf.Min(pos.z - cubeSize.z * 0.5f, nextPos.z - cubeSize.z * 0.5f);
            float cubeMaxZ = Mathf.Max(pos.z + cubeSize.z * 0.5f, nextPos.z + cubeSize.z * 0.5f);

            // Platform XZ extents
            float platMinX = platform.center.x - platform.size.x * 0.5f;
            float platMaxX = platform.center.x + platform.size.x * 0.5f;
            float platMinZ = platform.center.z - platform.size.z * 0.5f;
            float platMaxZ = platform.center.z + platform.size.z * 0.5f;

            // Check XZ overlap between swept cube and platform
            bool overlapXZ = (cubeMaxX > platMinX) && (cubeMinX < platMaxX)
                           && (cubeMaxZ > platMinZ) && (cubeMinZ < platMaxZ);

            // If the cube crosses the platform top between frames and XZ overlaps, we have a collision
            if (overlapXZ && ((cubeBottomNow > platformTop && cubeBottomNext <= platformTop) // falling onto platform
                             || (cubeBottomNow < platformTop && cubeBottomNext >= platformTop))) // moving up into platform (jump hitting underside)
            {
                // compute fraction t along movement where the bottom hits the platform
                float totalDy = cubeBottomNext - cubeBottomNow;
                float t = 0f;
                if (Mathf.Abs(totalDy) > 1e-6f)
                    t = (platformTop - cubeBottomNow) / totalDy;
                t = Mathf.Clamp01(t);

                // move to contact point
                Vector3 contactPos = Vector3.Lerp(pos, nextPos, t);

                // set new position so cube sits exactly on platform top (if landing) OR just under if hitting underside
                if (cubeBottomNow > platformTop && cubeBottomNext <= platformTop)
                {
                    // landing on top
                    contactPos.y = platformTop + cubeSize.y * 0.5f;
                    transform.position = contactPos;

                    // stop vertical motion
                    velocity.y = 0f;
                    isGrounded = true;

                    // color change to green on collision
                    if (rend != null) rend.material.color = Color.green;
                }
                else if (cubeBottomNow < platformTop && cubeBottomNext >= platformTop)
                {
                    // hitting underside of platform - we stop upward movement and push below platform
                    contactPos.y = platformTop - cubeSize.y * 0.5f;
                    transform.position = contactPos;
                    velocity.y = 0f;
                    // optional color change as well
                    if (rend != null) rend.material.color = Color.green;
                    isGrounded = false;
                }

                // early return (we handled movement by placing at contact)
                return;
            }
        }

        // No collision -> just move normally
        transform.position = nextPos;

        // If we moved away from platform in Y, clear grounded state
        if (platform != null)
        {
            float bottom = transform.position.y - cubeSize.y * 0.5f;
            if (bottom > platform.TopY + 0.001f) // small tolerance
            {
                if (isGrounded)
                {
                    isGrounded = false;
                    // reset color when leaving (optional)
                    if (rend != null) rend.material.color = Color.white;
                }
            }
        }
    }

    private void Update()
    {
        // Jump input handled in Update for responsive input; it modifies velocity which FixedUpdate uses.
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isGrounded)
            {
                velocity.y = jumpVelocity;
                isGrounded = false;
                if (rend != null) rend.material.color = Color.white; // reset color on jump
            }
        }

        // Optional: simple horizontal control (so you can jump on/off platform)
        float h = Input.GetAxis("Horizontal"); // A/D or Left/Right
        float v = Input.GetAxis("Vertical");   // W/S or Up/Down

        // Apply simple horizontal translation (not physics-based)
        Vector3 horizontal = new Vector3(h, 0f, v) * 5f * Time.deltaTime;
        transform.position += horizontal;
    }
}
