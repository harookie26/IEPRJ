using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class Enemy_Teleport_Handling : MonoBehaviour
{

    [SerializeField] private float minTeleportDistance;
    [SerializeField] private List<Vector3> listTeleportSpots;
    [SerializeField] private GameObject ghostObject;

    private readonly List<Vector3> updatedTeleportSpot = new();


    void Update()
    {
        UpdatePossibleTeleportations();
        
    }

    private void UpdatePossibleTeleportations()
    {
        updatedTeleportSpot.Clear();

        foreach(Vector3 Telepos in listTeleportSpots)
        {
            float distance = Vector3.Distance(ghostObject.transform.position, Telepos);

            if (distance > minTeleportDistance) 
            {
                updatedTeleportSpot.Add(Telepos);
            }
        }
    }


    public void CommenceTeleportation()
    {

        int teleportIndex;

        if (updatedTeleportSpot.Count > 0) 
        {
            teleportIndex = Random.Range(0,updatedTeleportSpot.Count);
            ghostObject.transform.position = updatedTeleportSpot[teleportIndex]; 

        }


    }


}
