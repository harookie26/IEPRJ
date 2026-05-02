using UnityEngine;

public class PaintBucketHintTrigger : MonoBehaviour
{
    private bool hasTriggered = false;


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if(hasTriggered == false) {

                 EventBroadcaster.Instance.PostEvent(EventNames.HintEvents.HINT3_START);
                 hasTriggered = true;
            }
        }
    }
}
