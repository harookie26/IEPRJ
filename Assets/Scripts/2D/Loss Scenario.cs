using UnityEngine;

public class LossScenario : MonoBehaviour
{

    [SerializeField] private GameObject player;
    [SerializeField] private GameObject ghost;
    [SerializeField] private float hauntingDistance;
    [SerializeField] private float hauntingTick;
    [SerializeField] private float maxSanity;

    private float hauntedPoints;
    private float timer = 0f;
    private bool isHaunted = false;
    private bool isLost;



    void Start()
    {
        hauntedPoints = 0f;
        isLost = false;
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime; 
        if (isLost == false)
        {
            CheckHaunting();
            UpdateHaunting();
        }
    }

    private void CheckHaunting()
    {
        isHaunted = false;
        if(Mathf.Abs(player.transform.position.x - ghost.transform.position.x) <= hauntingDistance)
        {
         
            isHaunted = true;
            Debug.Log("Being Haunted");
        } else
        {
            Debug.Log("Not Haunted");
        }
    }

    private void UpdateHaunting()
    {
       
            if (isHaunted == true && hauntedPoints < maxSanity)
            {
                if (timer >= hauntingTick)
                {
                    hauntedPoints += 1;
                    Debug.Log("Current Haunted Points: " + hauntedPoints + "/" + maxSanity);
                    timer = 0f;
                }
            }  else if (isHaunted == false && hauntedPoints < maxSanity)
            {

                if(timer >= hauntingTick * 2)
                {
                    if (hauntedPoints < maxSanity && hauntedPoints > 0f)
                    {
               
                        hauntedPoints -= 1;
                        timer = 0f;
                        Debug.Log("Current Haunted Points: " + hauntedPoints + "/" + maxSanity);
                    }
                }

            }

        

        if(hauntedPoints >= maxSanity)
        {
            isLost = true;
            OnDefeat();
        }

    }


    private void OnDefeat()
    {
        
        var movement = player.GetComponent<PlayerMovement2D>();
        if(movement != null)
        {
            movement.moveSpeed = 0f;
        }
        Debug.Log("Boo!");
    }




}
