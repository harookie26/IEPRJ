using UnityEngine;
using static EventNames;

public class CutsceneEventFlag : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the object that entered the trigger is on the "Player" layer.
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            EventBroadcaster.Instance.PostEvent(CutsceneEvents.CUTSCENE_START);
            CutsceneManager.Instance.PlayCutscene("Intro");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // Check if the object that exited the trigger is on the "Player" layer.
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            EventBroadcaster.Instance.PostEvent(CutsceneEvents.CUTSCENE_END);
        }
    }
}
