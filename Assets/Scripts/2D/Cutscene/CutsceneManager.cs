using UnityEngine;
using static EventNames;

public class CutsceneManager : MonoBehaviour
{
    private bool isCutsceneActive = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (isCutsceneActive)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                OnCutsceneEnd();
            }
        }
    }

    private void Awake()
    {
        EventBroadcaster.Instance.AddObserver(CutsceneEvents.CUTSCENE_START, OnCutsceneStart);
        // Assuming you have a CUTSCENE_END event
        EventBroadcaster.Instance.AddObserver(CutsceneEvents.CUTSCENE_END, OnCutsceneEnd);
    }

    private void OnDestroy()
    {
        EventBroadcaster.Instance.RemoveActionAtObserver(CutsceneEvents.CUTSCENE_START, OnCutsceneStart);
        EventBroadcaster.Instance.RemoveActionAtObserver(CutsceneEvents.CUTSCENE_END, OnCutsceneEnd);
    }

    private void OnCutsceneStart()
    {
        // Logic to handle the start of a cutscene
        Debug.Log("Cutscene started.");
        isCutsceneActive = true;
        GameState.IsCutsceneActive = true;

        // You can add more functionality here, such as playing animations, changing camera angles, etc.
    }

    private void OnCutsceneEnd()
    {
        Debug.Log("Cutscene ended.");
        isCutsceneActive = false;
        GameState.IsCutsceneActive = false;
        // Add any cleanup logic here
    }
}
