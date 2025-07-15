using System.Collections.Generic;
using UnityEngine;
using static EventNames;

public class Win_Scenario : MonoBehaviour
{

    [SerializeField] private List<GameObject> ObjectivePrefabs;
    [SerializeField] private List<Vector3> ObjectivePositions;

    [SerializeField] private float raiseSpeed = 0.5f;
    [SerializeField] private float TimeTick;
    [SerializeField] private float TotalTime;

    [SerializeField] private GameObject player;
    private readonly List<GameObject> Objectives = new();
    private readonly List<float> objectivePercentages = new();

    private float timer;
    private int objectiveIndex = -1;
    private float graceDistance = 0.4f;

    private bool canChannel;
    private bool isChanneling;
    private bool ObjectiveCompleted;

    void Start()
    {
        canChannel = false;
        isChanneling = false;
        timer = 0;
        ObjectiveCompleted = false;

        int count = Mathf.Min(ObjectivePrefabs.Count, ObjectivePositions.Count);
            
        for(int i = 0; i < count; i++)
        {
            GameObject spot = Instantiate(ObjectivePrefabs[i], ObjectivePositions[i], Quaternion.identity);
            Objectives.Add(spot);
            objectivePercentages.Add(0f);

            if (ObjectivePrefabs[i] != null && ObjectivePrefabs[i].scene.IsValid())
            {
                Destroy(ObjectivePrefabs[i]);
            }
        }
    }

    void Update()
    {
        if(ObjectiveCompleted == false)
        {
            ObserveObjectives();
            HandleObjectiveInputs();
            UpdateProgress();
            ProgressCheck();
        }
        else
        {
            EventBroadcaster.Instance.PostEvent(GameStateEvents.ON_LEVEL_COMPLETE);

        }

    }


    void ObserveObjectives()
    {
        canChannel = false;

        for (int i = 0; i < Objectives.Count; i++)
        {
            if (Mathf.Abs(Objectives[i].transform.position.x - player.transform.position.x) <= graceDistance)
            {
                objectiveIndex = i;
                canChannel = true;
                break;
            }
        }

        if(canChannel == false)
        {
            objectiveIndex = -1;
            Debug.Log("No Objectives.");
        }
        
    }

    private void HandleObjectiveInputs()
    {

        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (!canChannel)
            {
                isChanneling = false;
                Debug.Log("NO NEARBY OBJECtIVES TO CHANNEL");
            }
            else if (!isChanneling && canChannel)
            {
                EventBroadcaster.Instance.PostEvent(PlayerEvents.PLAYER_CHANNELING);

                isChanneling = true;
                Debug.Log("Channeling an Object");
            }
        }
        else if (isChanneling && Input.anyKey && !Input.GetKey(KeyCode.Q)) 
        {
            EventBroadcaster.Instance.PostEvent(PlayerEvents.PLAYER_DECHANNELING);

            isChanneling = false;
            Debug.Log("Player Stopped Channeling");
        }
    }

    private void UpdateProgress()
    {
        timer += Time.deltaTime;
        if (isChanneling == true)
        {
            // Slowly raise the objective while channeling
            if (objectiveIndex >= 0 && objectiveIndex < Objectives.Count)
            {
                var obj = Objectives[objectiveIndex];
                var rb = obj.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    Vector2 targetPosition = rb.position + Vector2.up * raiseSpeed * Time.deltaTime;
                    rb.MovePosition(targetPosition);
                }
                else
                {
                    // Fallback if no Rigidbody2D (should not happen in your case)
                    obj.transform.position += Vector3.up * raiseSpeed * Time.deltaTime;
                }
            }

            if (timer >= TimeTick && objectivePercentages[objectiveIndex] < TotalTime)
            {
                objectivePercentages[objectiveIndex] += 1f;
                timer = 0;
                Debug.Log("Current Progress: " + objectivePercentages[objectiveIndex] + " / " + TotalTime);

                if (objectivePercentages[objectiveIndex] >= TotalTime)
                {
                    Debug.Log("Current Object is finished.");
                    EventBroadcaster.Instance.PostEvent(PlayerEvents.PLAYER_DECHANNELING);
                    isChanneling = false;

                    Destroy(Objectives[objectiveIndex]);
                }
            }
        }
        else if (isChanneling == false)
        {
            if (timer >= TimeTick * 2)
            {
                Debug.Log("Current Progression is Deteriorating");
                for(int i = 0; i < objectivePercentages.Count; i++)
                {
                    if (objectivePercentages[i] > 0 && objectivePercentages[i] < TotalTime)
                    {
                        objectivePercentages[i] -= 1;
                    }
                }
                timer = 0;
            }
        }
    }

    private void ProgressCheck()
    {
        if (objectivePercentages.Count == 0)
        {
            ObjectiveCompleted = false;
            return;
        }

        ObjectiveCompleted = true;
        for (int i = 0; i < objectivePercentages.Count; i++)
        {
            if (objectivePercentages[i] < TotalTime)
            {
                ObjectiveCompleted = false;
                break;
            }
        }
    }
}
