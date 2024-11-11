using UnityEngine;

public class MassCalculator : MonoBehaviour
{
    public bool updateMass;
    [Tooltip("Density of the object in kg/m^3. Common materials:\n- Wood: 700\n- Water: 1000\n- Stone: 2500\n- Iron: 7900\n- Gold: 20000\n- Aluminum: 2700")]
    public float density = 1000.0f; // The density of the object in kg/m^3, default is water

    void OnValidate()
    {
        if (updateMass)
        {
            updateMass = false;
            CalculateAndUpdateMass();
        }
    }

    void CalculateAndUpdateMass()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody component not found on the object.");
            return;
        }

        float totalVolume = CalculateVolume(transform);
        float totalMass = totalVolume * density;
        rb.mass = totalMass;
    }

    float CalculateVolume(Transform objTransform)
    {
        MeshFilter meshFilter = objTransform.GetComponent<MeshFilter>();
        float volume = 0f;

        // Calculate volume of the current object if it has a mesh
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            volume += CalculateMeshVolume(meshFilter.sharedMesh, objTransform);
        }

        // Recursively calculate volume for all children
        foreach (Transform child in objTransform)
        {
            volume += CalculateVolume(child);
        }

        return volume;
    }

    float CalculateMeshVolume(Mesh mesh, Transform objTransform)
    {
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        float volume = 0f;

        // Using the Tetrahedron method to calculate the volume of the mesh
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 p1 = objTransform.TransformPoint(vertices[triangles[i]]);
            Vector3 p2 = objTransform.TransformPoint(vertices[triangles[i + 1]]);
            Vector3 p3 = objTransform.TransformPoint(vertices[triangles[i + 2]]);

            volume += SignedVolumeOfTriangle(p1, p2, p3);
        }

        volume = Mathf.Abs(volume) * objTransform.localScale.x * objTransform.localScale.y * objTransform.localScale.z;

        return volume;
    }

    float SignedVolumeOfTriangle(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        return Vector3.Dot(Vector3.Cross(p1, p2), p3) / 6.0f;
    }
}
