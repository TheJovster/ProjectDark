using UnityEngine;

[RequireComponent(typeof(MeshFilter))][RequireComponent(typeof(MeshRenderer))]
public class CircularQuadMeshGeneration : MonoBehaviour
{
    [Header("Circular Quad Properties")]
    [SerializeField, Range(3, 64)] private int numberOfSegments = 3;
    [SerializeField] private float radius = 1.0f;

    [Header("Mesh Generation Options")] 
    [SerializeField] private bool invertNormals = false;
    [SerializeField] private bool centerOrigin = true;

    [Header("Material Options")] 
    [SerializeField] private Material baseMaterial;
    [SerializeField] private Material renderTextureMaterial;

    private void OnEnable()
    {
        GenerateCircularQuad();
    }

    public void GenerateCircularQuad()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        Mesh mesh = new Mesh();

        meshFilter.mesh = mesh;

        Vector3[] vertices = new Vector3[numberOfSegments + 1];
        Vector2[] uvs = new Vector2[numberOfSegments + 1];

        vertices[0] = Vector3.zero;
        uvs[0] = new Vector2(0.5f, 0.5f);

        for (int i = 0; i < numberOfSegments; i++)
        {
            float angle = (2 * Mathf.PI / numberOfSegments) * i;
            vertices[i + 1] = new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius,
                0.0f);

            uvs[i + 1] = new Vector2(
                (Mathf.Cos(angle) + 1.0f) * 0.5f,
                (Mathf.Sin(angle) + 1.0f) * 0.5f
            );
        }

        int[] triangles = new int[numberOfSegments * 3];
        for (int i = 0; i < numberOfSegments; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = (i + 1) % numberOfSegments + 1;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;

        mesh.RecalculateNormals();
        if (invertNormals)
        {
            Vector3[] normals = mesh.normals;
            for (int i = 0; i < normals.Length; i++)
            {
                normals[i] = -normals[i];
            }
            mesh.normals = normals;
        }
        mesh.RecalculateBounds();
;    }
}
