using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

public class ActiveUIManager : MonoBehaviour
{

    public static ActiveUIManager Instance { get; private set; }

    [SerializeField] private List<GameObject> keyList;
    [SerializeField] private List<Sprite> spritelist;


    private void Awake()
    {
        // Standard singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;

            if(keyList.Count != 5)
            {
                Debug.Log("UI Format Incorrect");
            }
        }
    }

    /// Visual updates whenever movement keys are held or released
    void Update()
    {
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D))
        {

            if (Input.GetKey(KeyCode.W))
            {
                onButtonHold(0);
            }
            else if (Input.GetKey(KeyCode.A))
            {
                onButtonHold(1);
            }
            else if (Input.GetKey(KeyCode.S))
            {
                onButtonHold(2);
            }
            else if (Input.GetKey(KeyCode.D)) 
            {
                onButtonHold(3);
            }

        } else if (Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.A) || Input.GetKeyUp(KeyCode.S) || Input.GetKeyUp(KeyCode.D))
        {
            if (Input.GetKeyUp(KeyCode.W))
            {
                onButtonRelease(0);
            }
            else if (Input.GetKeyUp(KeyCode.A))
            {
                onButtonRelease(1);
            }
            else if (Input.GetKeyUp(KeyCode.S))
            {
                onButtonRelease(2);
            }
            else if (Input.GetKeyUp(KeyCode.D))
            {
                onButtonRelease(3);
            }
        }
    }

    private void onButtonHold(int keyIndex)
    {
        keyList[keyIndex].GetComponent<Image>().sprite = spritelist[keyIndex + 4];

    }

    private void onButtonRelease(int keyIndex)
    {
        keyList[keyIndex].GetComponent<Image>().sprite = spritelist[keyIndex];
    }

}
