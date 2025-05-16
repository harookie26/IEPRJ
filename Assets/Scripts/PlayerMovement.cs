using UnityEngine;

public class PlayerMovement : MonoBehaviour
{

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        playerMovement();
    }

    private void playerMovement()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            // stuff here
        }
    }
}
