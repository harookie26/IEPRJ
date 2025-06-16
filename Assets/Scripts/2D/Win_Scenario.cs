using System.Collections.Generic;
using UnityEngine;

public class Win_Scenario : MonoBehaviour
{

    [SerializeField] private List<GameObject> ObjectivePrefabs;
    [SerializeField] private List<Vector3> ObjectivePositions;

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

        Debug.Log("I AM HERE");

        int count = Mathf.Min(ObjectivePrefabs.Count, ObjectivePositions.Count);
            
        for(int i = 0; i < count; i++)
        {
            GameObject spot = Instantiate(ObjectivePrefabs[i], ObjectivePositions[i], Quaternion.identity);
            Objectives.Add(spot);
            objectivePercentages.Add(0f);
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
            Debug.Log("QUEST IS COMPLETE");
        }

    }


    /// Check if there is a nearby objective Object nearby.
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
                isChanneling = true;
                PlayerChanneling();
                Debug.Log("Channeling an Object");
            }
        }
        else if (isChanneling && Input.anyKey && !Input.GetKey(KeyCode.Q)) 
        {
            isChanneling = false;
            PlayerDechanneling();
            Debug.Log("Player Stopped Channeling");
        }
    }

    private void PlayerChanneling()
    {
        var movement = player.GetComponent<PlayerMovement2D>();
        if (movement != null)
        {
            movement.enabled = false;
        }

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }

    private void PlayerDechanneling()
    {
        var movement = player.GetComponent<PlayerMovement2D>();
        if (movement != null)
        {
            movement.enabled = true;
        }
    }

    private void UpdateProgress()
    {
        timer += Time.deltaTime;
        if (isChanneling == true)
        {
            if (timer >= TimeTick && objectivePercentages[objectiveIndex] < TotalTime)
            {
                objectivePercentages[objectiveIndex] += 1f;
                timer = 0;
                Debug.Log("Current Progress: " + objectivePercentages[objectiveIndex] + " / " + TotalTime);

                if (objectivePercentages[objectiveIndex] == TotalTime)
                {
                    Debug.Log("Current Object is finished.");
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
        ObjectiveCompleted = true;
        for(int i = 0; i < objectivePercentages.Count; i++)
        {
            if (objectivePercentages[i] < TotalTime)
            {
                ObjectiveCompleted = false;
                break;
            }
        }
    }
}
