using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraSwitch : MonoBehaviour
{
    public CinemachineCamera thirdPersonCam;
    public CinemachineCamera firstPersonCam;

    private void Start()
    {
        ActivateCamera(thirdPersonCam);
    }

    private void Update()
    {
        if (Keyboard.current.vKey.wasPressedThisFrame)
        {
            if (thirdPersonCam.IsLive)
                ActivateCamera(firstPersonCam);
            else
                ActivateCamera(thirdPersonCam);
        }
    }

    void ActivateCamera(CinemachineCamera cam)
    {
        thirdPersonCam.Priority = 0;
        firstPersonCam.Priority = 0;

        cam.Priority = 10;
    }
}
