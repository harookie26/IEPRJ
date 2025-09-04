using UnityEngine;
using static EventNames;

public class CutsceneEventFlag : MonoBehaviour
{
    [SerializeField] private string cutsceneName;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the object that entered the trigger is on the "Player" layer.
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            EventBroadcaster.Instance.PostEvent(CutsceneEvents.CUTSCENE_START);
            CutsceneManager.Instance.PlayCutscene(cutsceneName);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // Check if the object that exited the trigger is on the "Player" layer.
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            EventBroadcaster.Instance.PostEvent(CutsceneEvents.CUTSCENE_END);
            Destroy(gameObject);
        }
    }
}
