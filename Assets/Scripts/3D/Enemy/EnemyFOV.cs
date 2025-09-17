using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(MeshFilter))]
public class EnemyFOV : MonoBehaviour
{
    [SerializeField] private LayerMask layerMask;
    [SerializeField] private float fov = 90f;
    [SerializeField] private float viewDistance = 10f;
    [SerializeField] private int rayCount = 30;

    private Mesh mesh;
    private Vector3 origin;

    public bool PlayerInSight { get; private set; }


    private void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
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
    }

}
