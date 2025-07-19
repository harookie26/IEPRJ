using UnityEngine;
using static EventNames;

public class CutsceneManager : MonoBehaviour
{

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void Awake()
    {
        EventBroadcaster.Instance.AddObserver(CutsceneEvents.CUTSCENE_START, OnCutsceneStart);
    }

    private void OnDestroy()
    {
        EventBroadcaster.Instance.RemoveActionAtObserver(CutsceneEvents.CUTSCENE_START, OnCutsceneStart);
    }

    private void OnCutsceneStart()
    {
        // Logic to handle the start of a cutscene
        Debug.Log("Cutscene started.");

        // You can add more functionality here, such as playing animations, changing camera angles, etc.
    }
}
