using System.Linq;
using Game.ObjectTypes;
using UnityEngine;
using static EventNames;

public class Win_Scenario : MonoBehaviour
{
    [SerializeField] private GameObject player;

    private bool objectiveCompleted;

    void Update()
    {
        if (!objectiveCompleted)
        {
            ProgressCheck();
        }
        else
        {
            EventBroadcaster.Instance.PostEvent(GameStateEvents.ON_LEVEL_COMPLETE);
        }
    }

    private void ProgressCheck()
    {
        // If there are no more channelable objects in the scene, the objective is complete
        var remaining = Object.FindObjectsByType<ChannelObject>(FindObjectsSortMode.None)
            .Any(obj => !obj.IsComplete);
        objectiveCompleted = !remaining;
    }
}