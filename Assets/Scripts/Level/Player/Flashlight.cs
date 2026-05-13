using UnityEngine;

public class Flashlight : MonoBehaviour
{
    [SerializeField] private GameObject flashlightBeam;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (flashlightBeam != null)
            flashlightBeam.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyUp(KeyCode.F))
        {
            if (flashlightBeam != null)
            {
                flashlightBeam.SetActive(!flashlightBeam.activeSelf);
            }
        }
    }
}
