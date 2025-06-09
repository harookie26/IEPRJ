using TMPro;
using Unity.Cinemachine;
using UnityEngine;

public class LossScenario : MonoBehaviour
{

    [SerializeField] private GameObject player;
    [SerializeField] private CinemachineCamera mainCam;

    bool isLost = false;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void onDefeat()
    {
        player.GetComponent<PlayerMovement2D>().enabled = false; 

    }


}
