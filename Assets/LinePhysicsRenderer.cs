using UnityEngine;

[DisallowMultipleComponent]
public class LinePhysicsRenderer : MonoBehaviour
{
    [Header("Platform (line rectangle)")]
    public Vector3 platformCenter = new Vector3(0f, -1f, 0f);
    public Vector3 platformSize = new Vector3(20f, 1f, 4f); 

    [Header("Cube (wire cube)")]
    public Vector3 cubeSize = Vector3.one;
    public Vector3 cubeStartPosition = new Vector3(0f, 2f, 0f);

    [Header("Physics")]
    public float gravity = -20f;
    public float jumpVelocity = 8f;
    public float terminalVelocity = -50f;
    public float horizontalSpeed = 5f;

    [Header("Rendering")]
    public float lineWidth = 0.02f; 
    public Color cubeColorIdle = Color.white;
    public Color cubeColorCollision = Color.green;
    public Color platformColor = Color.gray;

    // internal physics state
    Vector3 cubePosition;
    Vector3 velocity;
    bool isGrounded = false;
    bool collidedThisFrame = false;

    // Rendering resources
    Mesh cubeLineMesh;
    Mesh platformLineMesh;
    Material lineMaterial;

    static readonly int[] cubeEdgePairs = {
        0,1, 1,2, 2,3, 3,0, // bottom face
        4,5, 5,6, 6,7, 7,4, // top face
        0,4, 1,5, 2,6, 3,7  // vertical edges
    };

    void Awake()
    {
        // initial state
        cubePosition = cubeStartPosition;
        velocity = Vector3.zero;

        // create a simple Unlit color material (rendered without lighting)
        Shader s = Shader.Find("Unlit/Color");
        if (s == null)
        {
            // fallback if Unlit/Color is unavailable
            s = Shader.Find("Hidden/Internal-Colored");
        }
        lineMaterial = new Material(s);
        lineMaterial.hideFlags = HideFlags.HideAndDontSave;

        // prepare meshes
        cubeLineMesh = CreateCubeLineMesh(cubeSize);
        platformLineMesh = CreatePlatformLineMesh(platformCenter, platformSize);

    }

    void OnDestroy()
    {
        if (cubeLineMesh != null) DestroyImmediate(cubeLineMesh);
        if (platformLineMesh != null) DestroyImmediate(platformLineMesh);
        if (lineMaterial != null) DestroyImmediate(lineMaterial);
    }

    void Update()
    {
        // Input: horizontal/vertical movement for XZ plane (WASD / arrows)
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 horiz = new Vector3(h, 0f, v) * horizontalSpeed * Time.deltaTime;
        cubePosition += horiz;

        // Jump input
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isGrounded)
            {
                velocity.y = jumpVelocity;
                isGrounded = false;
                collidedThisFrame = false;
            }
        }
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        // apply gravity
        velocity.y += gravity * dt;
        if (velocity.y < terminalVelocity) velocity.y = terminalVelocity;

        Vector3 pos = cubePosition;
        Vector3 nextPos = pos + velocity * dt;
        float cubeBottomNow = pos.y - cubeSize.y * 0.5f;
        float cubeBottomNext = nextPos.y - cubeSize.y * 0.5f;
        float platformTop = platformCenter.y + platformSize.y * 0.5f;

        // compute swept XZ AABB (min/max) between current & next positions
        float cubeMinX = Mathf.Min(pos.x - cubeSize.x * 0.5f, nextPos.x - cubeSize.x * 0.5f);
        float cubeMaxX = Mathf.Max(pos.x + cubeSize.x * 0.5f, nextPos.x + cubeSize.x * 0.5f);
        float cubeMinZ = Mathf.Min(pos.z - cubeSize.z * 0.5f, nextPos.z - cubeSize.z * 0.5f);
        float cubeMaxZ = Mathf.Max(pos.z + cubeSize.z * 0.5f, nextPos.z + cubeSize.z * 0.5f);

        float platMinX = platformCenter.x - platformSize.x * 0.5f;
        float platMaxX = platformCenter.x + platformSize.x * 0.5f;
        float platMinZ = platformCenter.z - platformSize.z * 0.5f;
        float platMaxZ = platformCenter.z + platformSize.z * 0.5f;

        bool overlapXZ = (cubeMaxX > platMinX) && (cubeMinX < platMaxX)
                       && (cubeMaxZ > platMinZ) && (cubeMinZ < platMaxZ);

        collidedThisFrame = false;

        if (overlapXZ && ((cubeBottomNow > platformTop && cubeBottomNext <= platformTop) || (cubeBottomNow < platformTop && cubeBottomNext >= platformTop)))
        {
            // compute t fraction where the bottom reaches platformTop
            float totalDy = cubeBottomNext - cubeBottomNow;
            float t = 0f;
            if (Mathf.Abs(totalDy) > 1e-6f)
                t = (platformTop - cubeBottomNow) / totalDy;
            t = Mathf.Clamp01(t);

            Vector3 contactPos = Vector3.Lerp(pos, nextPos, t);

            if (cubeBottomNow > platformTop && cubeBottomNext <= platformTop)
            {
                contactPos.y = platformTop + cubeSize.y * 0.5f;
                cubePosition = contactPos;
                velocity.y = 0f;
                isGrounded = true;
                collidedThisFrame = true;
            }
            else if (cubeBottomNow < platformTop && cubeBottomNext >= platformTop)
            {
                contactPos.y = platformTop - cubeSize.y * 0.5f;
                cubePosition = contactPos;
                velocity.y = 0f;
                isGrounded = false;
                collidedThisFrame = true;
            }
        }
        else
        {
            cubePosition = nextPos;
            float bottom = cubePosition.y - cubeSize.y * 0.5f;
            if (bottom > platformTop + 0.001f)
            {
                if (isGrounded)
                {
                    isGrounded = false;
                }
            }
        }
    }

    void LateUpdate()
    {
        if (cubeLineMesh == null) cubeLineMesh = CreateCubeLineMesh(cubeSize);
        UpdateCubeMeshVertices(cubeLineMesh, cubePosition, cubeSize);
        if (platformLineMesh == null) platformLineMesh = CreatePlatformLineMesh(platformCenter, platformSize);
    }

    void OnRenderObject()
    {
        if (lineMaterial == null) return;
        lineMaterial.SetColor("_Color", platformColor);
        lineMaterial.SetPass(0);
        Graphics.DrawMeshNow(platformLineMesh, Matrix4x4.identity);
        Color c = collidedThisFrame ? cubeColorCollision : cubeColorIdle;
        lineMaterial.SetColor("_Color", c);
        lineMaterial.SetPass(0);
        Graphics.DrawMeshNow(cubeLineMesh, Matrix4x4.identity);
    }


    Mesh CreateCubeLineMesh(Vector3 size)
    {
        Mesh m = new Mesh();
        m.name = "CubeLineMesh";

        Vector3 half = size * 0.5f;

        Vector3[] verts = new Vector3[8];

        verts[0] = new Vector3(-half.x, -half.y, -half.z);
        verts[1] = new Vector3(half.x, -half.y, -half.z);
        verts[2] = new Vector3(half.x, -half.y, half.z);
        verts[3] = new Vector3(-half.x, -half.y, half.z);
        verts[4] = new Vector3(-half.x, half.y, -half.z);
        verts[5] = new Vector3(half.x, half.y, -half.z);
        verts[6] = new Vector3(half.x, half.y, half.z);
        verts[7] = new Vector3(-half.x, half.y, half.z);

        int[] indices = {
            0,1, 1,2, 2,3, 3,0,
            4,5, 5,6, 6,7, 7,4,
            0,4, 1,5, 2,6, 3,7
        };

        m.vertices = verts;
        m.SetIndices(indices, MeshTopology.Lines, 0);
        m.RecalculateBounds();
        m.hideFlags = HideFlags.HideAndDontSave;
        return m;
    }

    void UpdateCubeMeshVertices(Mesh mesh, Vector3 worldCenter, Vector3 size)
    {
        Vector3 half = size * 0.5f;

        Vector3[] verts = new Vector3[8];
        verts[0] = new Vector3(-half.x, -half.y, -half.z) + worldCenter;
        verts[1] = new Vector3(half.x, -half.y, -half.z) + worldCenter;
        verts[2] = new Vector3(half.x, -half.y, half.z) + worldCenter;
        verts[3] = new Vector3(-half.x, -half.y, half.z) + worldCenter;
        verts[4] = new Vector3(-half.x, half.y, -half.z) + worldCenter;
        verts[5] = new Vector3(half.x, half.y, -half.z) + worldCenter;
        verts[6] = new Vector3(half.x, half.y, half.z) + worldCenter;
        verts[7] = new Vector3(-half.x, half.y, half.z) + worldCenter;

        mesh.vertices = verts;
        mesh.RecalculateBounds();
    }

    Mesh CreatePlatformLineMesh(Vector3 center, Vector3 size)
    {
        Mesh m = new Mesh();
        m.name = "PlatformLineMesh";

        Vector3 half = size * 0.5f;
        Vector3 topCenter = new Vector3(center.x, center.y + half.y, center.z);

        Vector3[] verts = new Vector3[8];
        verts[0] = new Vector3(-half.x, 0f, -half.z) + topCenter;
        verts[1] = new Vector3(half.x, 0f, -half.z) + topCenter;
        verts[2] = new Vector3(half.x, 0f, half.z) + topCenter;
        verts[3] = new Vector3(-half.x, 0f, half.z) + topCenter;
        float t = Mathf.Min(0.05f, half.y);
        verts[4] = verts[0] + Vector3.down * t;
        verts[5] = verts[1] + Vector3.down * t;
        verts[6] = verts[2] + Vector3.down * t;
        verts[7] = verts[3] + Vector3.down * t;

        int[] indices = {
            0,1, 1,2, 2,3, 3,0, 
            0,4, 1,5, 2,6, 3,7, 
            4,5, 5,6, 6,7, 7,4 
        };

        m.vertices = verts;
        m.SetIndices(indices, MeshTopology.Lines, 0);
        m.RecalculateBounds();
        m.hideFlags = HideFlags.HideAndDontSave;
        return m;
    }
}
