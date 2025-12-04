using UnityEngine;

/// <summary>
/// Generates a simple horizontal platform and a cube. No Unity physics or colliders used.
/// </summary>
public class Scene : MonoBehaviour
{
    [Header("Platform")]
    public Vector3 platformSize = new Vector3(20f, 1f, 4f); // full extents (scale)
    public Vector3 platformPosition = new Vector3(0f, -1f, 0f);

    [Header("Cube")]
    public Vector3 cubeSize = Vector3.one;
    public Vector3 cubeStartPosition = new Vector3(0f, 2f, 0f);

    void Start()
    {
        // Create platform (visual only)
        GameObject platform = new GameObject("Platform_NoPhysics");
        platform.transform.position = platformPosition;
        platform.transform.localScale = Vector3.one; // mesh is unit, we will scale visual via transform

        MeshFilter pf = platform.AddComponent<MeshFilter>();
        pf.mesh = CreateUnitCubeMesh();
        MeshRenderer pr = platform.AddComponent<MeshRenderer>();
        pr.material = new Material(Shader.Find("Standard"));
        pr.material.color = new Color(0.65f, 0.65f, 0.65f);
        platform.transform.localScale = platformSize;

        // We store platform size/position in a simple helper so the cube controller can read it (no colliders)
        PlatformData pd = platform.AddComponent<PlatformData>();
        pd.center = platform.transform.position;
        // platformSize is transform.localScale for unit cube mesh -> actual size:
        pd.size = platform.transform.lossyScale;

        // Create cube (visual only)
        GameObject cube = new GameObject("PlayerCube_NoPhysics");
        cube.transform.position = cubeStartPosition;
        cube.transform.localScale = cubeSize;

        MeshFilter cf = cube.AddComponent<MeshFilter>();
        cf.mesh = CreateUnitCubeMesh();
        MeshRenderer cr = cube.AddComponent<MeshRenderer>();
        cr.material = new Material(Shader.Find("Standard"));
        cr.material.color = Color.white;

        // Add controller that implements all physics by math
        CubeControllerNoPhysics ctl = cube.AddComponent<CubeControllerNoPhysics>();
        ctl.cubeSize = cube.transform.lossyScale;
        ctl.platform = pd; // reference to platform data

        // Camera helper (optional): put main camera above and behind
        if (Camera.main != null)
        {
            Camera.main.transform.position = new Vector3(0f, 4f, -8f);
            Camera.main.transform.LookAt(new Vector3(0f, 0f, 0f));
        }
    }

    // Helper component to expose platform extents/center
    public class PlatformData : MonoBehaviour
    {
        public Vector3 center;
        public Vector3 size; // full size in x,y,z
        public float TopY => center.y + size.y * 0.5f;
        public float LeftX => center.x - size.x * 0.5f;
        public float RightX => center.x + size.x * 0.5f;
        public float FrontZ => center.z + size.z * 0.5f;
        public float BackZ => center.z - size.z * 0.5f;
    }

    // Unit cube mesh centered at origin with size 1x1x1
    private Mesh CreateUnitCubeMesh()
    {
        Mesh m = new Mesh();
        Vector3[] v = {
            // 24 vertices (4 per face)
            new Vector3(-0.5f,-0.5f, 0.5f), new Vector3(0.5f,-0.5f, 0.5f), new Vector3(0.5f,0.5f, 0.5f), new Vector3(-0.5f,0.5f, 0.5f),
            new Vector3(0.5f,-0.5f,-0.5f), new Vector3(-0.5f,-0.5f,-0.5f), new Vector3(-0.5f,0.5f,-0.5f), new Vector3(0.5f,0.5f,-0.5f),
            new Vector3(-0.5f,-0.5f,-0.5f), new Vector3(-0.5f,-0.5f,0.5f), new Vector3(-0.5f,0.5f,0.5f), new Vector3(-0.5f,0.5f,-0.5f),
            new Vector3(0.5f,-0.5f,0.5f), new Vector3(0.5f,-0.5f,-0.5f), new Vector3(0.5f,0.5f,-0.5f), new Vector3(0.5f,0.5f,0.5f),
            new Vector3(-0.5f,0.5f,0.5f), new Vector3(0.5f,0.5f,0.5f), new Vector3(0.5f,0.5f,-0.5f), new Vector3(-0.5f,0.5f,-0.5f),
            new Vector3(-0.5f,-0.5f,-0.5f), new Vector3(0.5f,-0.5f,-0.5f), new Vector3(0.5f,-0.5f,0.5f), new Vector3(-0.5f,-0.5f,0.5f)
        };
        int[] t = {
            0,1,2, 0,2,3,
            4,5,6, 4,6,7,
            8,9,10, 8,10,11,
            12,13,14, 12,14,15,
            16,17,18, 16,18,19,
            20,21,22, 20,22,23
        };
        m.vertices = v;
        m.triangles = t;
        m.RecalculateNormals();
        return m;
    }
}
