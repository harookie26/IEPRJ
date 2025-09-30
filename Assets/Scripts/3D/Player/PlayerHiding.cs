using System.Collections.Generic;
using UnityEngine;


/// Implentation of this script requires additional Child Objects underneath each object that is hideable.
/// Make sure that there is at least one child object that has a Box Collider Component to be able to use this script.
/// Currently, the action that the player will do when inside of a hideable zone is that they teleport directly into the Child object's Transform.Position


public class PlayerHiding : MonoBehaviour
{

    [SerializeField] private GameObject player;
    [SerializeField] private List <GameObject> hideableObjects; 

    private GameObject closestHideable; /// Gamebeobject Variable closest to the player
    private BoxCollider closestCollider; /// Closest BoxCollider to the player.

    [SerializeField] private float hidingRange;

    /// CHECK IF ALL HIDEABLE OBJECTS INPUTTED FROM THE EDITOR IS CORRECT.
    void Start()
    {
        foreach(GameObject obj in hideableObjects)
        {
            ///Debugging Cases

            if(obj == null)
            {
                Debug.Log("NULL Object added in hiding. Hiding Feature is Disabled");
                enabled = false;
                return;

            } else if(obj.transform.childCount == 0)
            {
                Debug.Log("Hideable Object:  " + obj.name + " Has no Child Object to put a Box Collider in. Please set a child object to put the Box Collider in. Hiding Feature is Disabled");
                enabled = false;
                return;
            }
            else if (obj.transform.childCount > 0)
            {
                GameObject childCheck;
                BoxCollider colliderCheck;

                for (int i = 0; i < obj.transform.childCount; i++)
                {
                    childCheck = obj.transform.GetChild(i).gameObject;
                    colliderCheck = childCheck.GetComponent<BoxCollider>();

                    if(colliderCheck == null)
                    {
                        enabled = false;
                        Debug.Log(obj.name + "'s child object: " + childCheck.name + " has no box collider component inside of it. Hiding Featuere is Disabled");
                        return;
                    } else
                    {
                        colliderCheck.isTrigger = true;
                        Debug.Log("Hideable Object: " + obj.name + "  Children Object:  " + childCheck.name + "Box Colliders VERIFIED!");
                    }
                }
            }
        }
    }

    void Update()
    {
        UpdateClosestHideableObject();

        if (Input.GetKeyUp(KeyCode.E))
        {
            AttemptPlayerHide(); 
        }

    }

    private void AttemptPlayerHide()
    {
        if (closestCollider != null)
        {
            Debug.Log("Player is in a valid collider to hide unto");

            player.transform.position = closestCollider.gameObject.transform.position;



        } else
        {
            Debug.Log("No hideablable objects nearby");
        }
            
    }

    private void UpdateClosestHideableObject()
    {
        float closestDistance = Mathf.Infinity;

        GameObject nearestObject = null;
        BoxCollider objectCollider = null;

        foreach (GameObject obj in hideableObjects)  /// Get the closest Hideable Object to the player
        {
            float distance = Vector3.Distance(player.transform.position, obj.transform.position);

            if (distance < closestDistance && distance <= hidingRange)
            {
                closestDistance = distance;
                nearestObject = obj;
  
            }
        }


        if(nearestObject != null)   /// If there is an existing object that is close to the player, then get the closest 'Collider Object' to the player.
        {
            BoxCollider boxCollider = null;
            float distToCollider = Mathf.Infinity;

            for (int i = 0; i < nearestObject.transform.childCount; i++)
            {
                GameObject objCollider = nearestObject.transform.GetChild(i).gameObject;
                boxCollider = objCollider.GetComponent<BoxCollider>();
                if (boxCollider == null) continue;

                float distance = Vector3.Distance(player.transform.position, objCollider.transform.position);

                if(distance < distToCollider && boxCollider.bounds.Contains(player.transform.position))
                {
                    distToCollider = distance;
                    objectCollider = boxCollider;
                }

            }    
        }

        closestHideable = nearestObject;
        closestCollider = objectCollider;

        if (closestHideable != null && closestCollider != null)
        {
            Debug.Log("Closest hideable object: " + closestHideable.name + " , COLLIDER NAME:  " + closestCollider.name);
        }
        else
        {
            Debug.Log("No hideable object within range.");
        }

    }
}
