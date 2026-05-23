using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class EndingSequence : MonoBehaviour
{
    [Header("Scene Settings")]
    public string targetSceneName;

    [Tooltip("How many seconds to wait before changing scenes.")]
    public float delayInSeconds = 5f;

    void Start()
    {
        // Start the countdown as soon as this object enters the scene
        StartCoroutine(WaitAndChangeScene());
    }

    private IEnumerator WaitAndChangeScene()
    {
        yield return new WaitForSeconds(delayInSeconds);

        if (!string.IsNullOrEmpty(targetSceneName))
        {
            SceneManager.LoadScene(targetSceneName);
        }
        else
        {
            Debug.LogError("Target Scene Name is empty on " + gameObject.name);
        }
    }
}
