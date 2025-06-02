using UnityEngine;

public class SceneHandler : MonoBehaviour
{
    [SerializeField] private Canvas uiCanvas;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        SwitchTo2DScene();
    }

    void SwitchTo2DScene()
    {
        if (uiCanvas.enabled == true)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("2DSample");
            }
        }
    }
}
