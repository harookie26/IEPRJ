using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PaintSplash : MonoBehaviour
{
    [Header("References")]
    public GameObject paintDecalPrefab;    

    [Header("Settings")]
    public float decalOffset = 0.01f;        // Push decal slightly above surface

    void OnCollisionEnter(Collision collision)
    {
        ContactPoint contact = collision.contacts[0];
        Vector3 hitPoint = contact.point;
        Vector3 hitNormal = contact.normal;

        Quaternion rotation = Quaternion.LookRotation(-hitNormal);
        Vector3 spawnPos = hitPoint + hitNormal * decalOffset;

        // Spawn decal
        GameObject decalObj = Instantiate(paintDecalPrefab, spawnPos, rotation);
        DecalProjector decal = decalObj.GetComponent<DecalProjector>();

        if (decal != null)
        {
            // Random rotation (just around Z axis to spin decal)
            decal.transform.Rotate(Vector3.forward, Random.Range(0f, 360f));

            float scale = Random.Range(0.8f, 1.3f);
            decal.size = new Vector3(scale, scale, decal.size.z);
        }

        Destroy(gameObject);
    }
}
