using UnityEngine;
using TMPro; // Add this for the battery text

public class Flashlight : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject flashlightBeam;
    [SerializeField] private TextMeshProUGUI batteryText; // Assign a UI Text element here
    [SerializeField] private Transform camTransform; // Assign the main camera or flashlight tip

    [Header("Battery Settings")]
    [SerializeField] private float maxBattery = 100f;
    [SerializeField] private float drainRate = 2f; // Percent per second
    private float currentBattery;

    [Header("Stun Settings")]
    [SerializeField] private float stunRange = 10f;
    [SerializeField] private LayerMask enemyLayer; // Set this to the layer your Ghost is on

    [Header("Audio")]
    [SerializeField] private AudioClip flashlightAudioClip;
    private AudioSource sfxAudioSource;

    private bool isOn = true;

    void Start()
    {
        currentBattery = maxBattery;
        if (camTransform == null) camTransform = Camera.main.transform;

        UpdateBeamState();

        sfxAudioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        HandleInput();

        if (isOn && currentBattery > 0)
        {
            DrainBattery();
            CheckForGhost();
        }
        else if (currentBattery <= 0 && isOn)
        {
            isOn = false;
            UpdateBeamState();
        }

        UpdateUI();
    }

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0) && currentBattery > 0)
        {
            if (flashlightAudioClip != null && sfxAudioSource != null)
            {
                sfxAudioSource.PlayOneShot(flashlightAudioClip);
            }
            isOn = !isOn;
            UpdateBeamState();
        }
    }

    private void DrainBattery()
    {
        currentBattery -= drainRate * Time.deltaTime;
        currentBattery = Mathf.Clamp(currentBattery, 0, maxBattery);
    }

    private void CheckForGhost()
    {
        int layerMask = enemyLayer | (1 << LayerMask.NameToLayer("Default"));

        if (Physics.Raycast(camTransform.position, camTransform.forward, out RaycastHit hit, stunRange, enemyLayer))
        {
            // Use GetComponentInParent in case the collider is on a child object
            var ghost = hit.collider.GetComponentInParent<EnemyStateMachine>();

            if (ghost != null)
            {
                // Call the Freeze function with your custom duration
                ghost.Freeze(ghost.StunDuration);
                Debug.Log("Ghost is caught in light - Stun timer paused.");
            }
        }
    }

    private void UpdateBeamState()
    {
        if (flashlightBeam != null)
            flashlightBeam.SetActive(isOn);
    }

    private void UpdateUI()
    {
        if (batteryText != null)
        {
            batteryText.text = $"Battery: {Mathf.CeilToInt(currentBattery)}%";
        }
    }
}