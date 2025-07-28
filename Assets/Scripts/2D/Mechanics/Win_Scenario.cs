using System.Linq;
using Game.ObjectTypes;
using UnityEngine;
using static EventNames;

public class Win_Scenario : MonoBehaviour
{
    [SerializeField] private GameObject player;

    private IChannelable currentObjective;
    private bool isChanneling;
    private bool objectiveCompleted;

    void Update()
    {
        if (!objectiveCompleted)
        {
            ObserveObjectives();
            HandleObjectiveInputs();
            UpdateChanneling();
            ProgressCheck();
        }
        else
        {
            EventBroadcaster.Instance.PostEvent(GameStateEvents.ON_LEVEL_COMPLETE);
        }
    }

    void ObserveObjectives()
    {
        // Find the nearest available channelable object
        var channelables = Object.FindObjectsByType<ChannelObject>(FindObjectsSortMode.None)
            .Where(obj => !obj.IsComplete && obj.CanChannel(player))
            .ToList();

        currentObjective = channelables.FirstOrDefault();
    }

    private void HandleObjectiveInputs()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (currentObjective == null)
            {
                isChanneling = false;
                Debug.Log("NO NEARBY OBJECTIVES TO CHANNEL");
            }
            else if (!isChanneling)
            {
                currentObjective.StartChannel(player);
                isChanneling = true;
                Debug.Log("Channeling an Object");
            }
        }
        else if (isChanneling && Input.anyKey && !Input.GetKey(KeyCode.Q))
        {
            if (currentObjective != null)
                currentObjective.StopChannel();
            isChanneling = false;
            Debug.Log("Player Stopped Channeling");
        }
    }

    private void UpdateChanneling()
    {
        if (isChanneling && currentObjective != null)
        {
            currentObjective.ChannelTick(Time.deltaTime);
            if (currentObjective.IsComplete)
            {
                isChanneling = false;
                currentObjective = null;
            }
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