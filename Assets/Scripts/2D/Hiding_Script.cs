using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;

public class Hiding_Script : MonoBehaviour
{
    [SerializeField] private GameObject player;
    [SerializeField] private float graceDistance = 0.2f; // Adjust as needed
    [SerializeField] private List<GameObject> hidingSpotPrefabs; // Assign different prefabs in Inspector
    [SerializeField] private List<Vector3> hidingSpotPositions;  // Assign positions in Inspector

    [SerializeField] private float hidingDuration; /// Time for how long the player can hide inside of the object.
    private float timer = 0f;
    private GameObject targetObject;  /// Object the player is currently hiding in
    private int targetIndex = -1;


    private SpriteRenderer playerSpriteRenderer;
    private List<GameObject> hidingSpots = new List<GameObject>();

    private bool canHide = false;
    private bool isHiding = false;


    private void Start()
    {
        for (int i = 0; i < hidingSpotPrefabs.Count && i < hidingSpotPositions.Count; i++)
        {
            GameObject spot = Instantiate(hidingSpotPrefabs[i], hidingSpotPositions[i], Quaternion.identity);
            hidingSpots.Add(spot);
        }

        playerSpriteRenderer = player.GetComponent<SpriteRenderer>();

        canHide = false;
        isHiding = false;
    }

    private void CheckHidingSpot()
    {
        int tempval = -1;
        canHide = false;
        foreach (GameObject spot in hidingSpots)
        {
            tempval++;

            if (Mathf.Abs(spot.transform.position.x - player.transform.position.x) <= graceDistance)
            {
                targetObject = spot;
                targetIndex = tempval;
                canHide = true;
                break;
            }
        }
    }


    private void Update()
    {
        CheckHidingSpot();

        if (Input.GetKeyDown(KeyCode.W))
        {
            if (canHide && !isHiding)
            {
                isHiding = true;
                Debug.Log("Player is hiding");
            }
            else if (isHiding)
            {
                isHiding = false;
                timer = 0f;
                targetObject = null;
                Debug.Log("Player stopped hiding");
            }
            else
            {
                Debug.Log("No hiding spot available");
            }
        }

        if (isHiding)
        {
            player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
            player.GetComponent<PlayerMovement2D>().enabled = false;
            if (playerSpriteRenderer != null)
                playerSpriteRenderer.enabled = false;
        }
        else
        {
            player.GetComponent<PlayerMovement2D>().enabled = true;
            if (playerSpriteRenderer != null)
                playerSpriteRenderer.enabled = true;
        }

        if (targetObject != null && isHiding)
        {
            timer += Time.deltaTime;

            Debug.Log(timer);


            if (timer >= hidingDuration)
            {
                isHiding = false;
                targetObject = null;

                Debug.Log(hidingSpotPositions.Count + " , " + hidingSpotPrefabs.Count);

                hidingSpotPositions.RemoveAt(targetIndex);
                hidingSpotPrefabs.RemoveAt(targetIndex);

                targetIndex = -1;
                timer = 0f;

            }
        }

    }


}
