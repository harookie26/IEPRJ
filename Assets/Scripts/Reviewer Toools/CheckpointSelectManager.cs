using UnityEngine;
using UnityEngine.SceneManagement;
using static EventNames.GameStateEvents;

public class CheckpointSelectManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void LoadCheckpoint(int checkpointIndex)
    {
        switch (checkpointIndex) {

            case 0:
                // Load checkpoint 0
                Time.timeScale = 1f;
                EventBroadcaster.Instance.PostEvent(ON_GAME_RESUME);
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                break;
            // Add more cases for other checkpoints
        }
    }
}
