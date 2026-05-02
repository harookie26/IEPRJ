using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class EnemyFOV : MonoBehaviour
{
    [SerializeField] private LayerMask layerMask;
    [SerializeField] private float fov = 90f;
    [SerializeField] private float viewDistance = 10f;
    [SerializeField] private int rayCount = 30;
    [SerializeField] private Material fovMaterial; // material used to render the FOV mesh

    private Mesh mesh;
    private Vector3 origin;

    public bool PlayerInSight { get; private set; }


    private void Start()
    {
        mesh = new Mesh();
        mesh.name = "EnemyFOV_Mesh";
        GetComponent<MeshFilter>().mesh = mesh;

        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (fovMaterial != null)
        {
            mr.material = fovMaterial;
        }
        else
        {
            // Create a simple semi-transparent unlit material if none provided
            Shader shader = Shader.Find("Unlit/Color");
            if (shader != null)
            {
                Material temp = new Material(shader);
                temp.color = new Color(1f, 0f, 0f, 0.2f);
                mr.material = temp;
            }
        }
    }

    private void LateUpdate()
    {
        GenerateFOVMesh();
    }

    private void GenerateFOVMesh()
    {
        origin = transform.position;
        float angleIncrease = fov / rayCount;
        float startAngle = -fov / 2f;

        Vector3[] vertices = new Vector3[rayCount + 2];
        Vector2[] uv = new Vector2[vertices.Length];
        int[] triangles = new int[rayCount * 3];

        vertices[0] = transform.InverseTransformPoint(origin);

        int vertexIndex = 1;
        int triangleIndex = 0;

        PlayerInSight = false;

        for (int i = 0; i <= rayCount; i++)
        {
            float currentAngle = startAngle + (angleIncrease * i);
            Vector3 dir = Quaternion.Euler(0, currentAngle, 0) * transform.forward;

            Vector3 vertexWorld;
            if (Physics.Raycast(origin, dir, out RaycastHit hit, viewDistance, layerMask))
            {
                vertexWorld = hit.point;

                if (hit.collider.CompareTag("Player"))
                {
                    Debug.Log("Player spotted in FOV!");
                    PlayerInSight = true;
                }
            }
            else
            {
                vertexWorld = origin + dir * viewDistance;
            }

            vertices[vertexIndex] = transform.InverseTransformPoint(vertexWorld);

            if (i > 0)
            {
                triangles[triangleIndex] = 0;
                triangles[triangleIndex + 1] = vertexIndex - 1;
                triangles[triangleIndex + 2] = vertexIndex;
                triangleIndex += 3;
            }

            vertexIndex++;
        }


        mesh.Clear();
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }

    // Draw helpful gizmos in the editor for visualization
    private void OnDrawGizmosSelected()
    {
        origin = transform.position;
        Gizmos.color = new Color(1f, 0f, 0f, 0.25f);
        // Draw outer arc by sampling points along the FOV
        int sampleCount = Mathf.Max(3, rayCount);
        float angleStep = fov / sampleCount;
        Vector3 prevPoint = origin + (Quaternion.Euler(0, -fov / 2f, 0) * transform.forward) * viewDistance;
        for (int i = 1; i <= sampleCount; i++)
        {
            float ang = -fov / 2f + angleStep * i;
            Vector3 nextPoint = origin + (Quaternion.Euler(0, ang, 0) * transform.forward) * viewDistance;
            Gizmos.DrawLine(origin, nextPoint);
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }

        // Draw radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin, viewDistance);
    }

}
