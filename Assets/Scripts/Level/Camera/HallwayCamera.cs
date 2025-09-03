using UnityEngine;

public class HallwayTrigger : MonoBehaviour
{
    private LevelCamera levelCamera;

    private void Start()
    {
        // Find the LevelCamera in the scene (or assign via inspector)
        levelCamera = FindFirstObjectByType<LevelCamera>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            levelCamera.HallwayZoomIn();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            levelCamera.HallwayZoomOut();
        }
    }
}