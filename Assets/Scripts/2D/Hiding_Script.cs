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
        canHide = false;
        foreach (GameObject spot in hidingSpots)
        {
            if (Mathf.Abs(spot.transform.position.x - player.transform.position.x) <= graceDistance)
            {
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
    }


}
