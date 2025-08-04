using UnityEngine;

public class GameStateManager3D : MonoBehaviour
{
    [SerializeField] private Transform playerTransform;

    private void Start()
    {
        playerTransform = GameObject.FindWithTag("Player")?.transform;

        if (PlayerPrefs.HasKey("Hub"))
        {
            string json = PlayerPrefs.GetString("Hub");
            SceneStateData sceneStateData = JsonUtility.FromJson<SceneStateData>(json);
            if (sceneStateData != null && playerTransform != null)
            {
                playerTransform.position = sceneStateData.playerPosition;
                playerTransform.rotation = sceneStateData.playerRotation;
            }
        }
    }

    private void OnApplicationQuit()
    {
        PlayerPrefs.DeleteKey("Hub");
        PlayerPrefs.Save();
    }
}
