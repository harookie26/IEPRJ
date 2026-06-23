using System.Collections;
using UnityEngine;

public class MainMenuSceneryManager : MonoBehaviour
{
    [Header("Target Lights")]
    [SerializeField] private Light[] lightsToFlicker;

    [Header("Timing Settings")]
    [Tooltip("Minimum time (seconds) before the next flicker event.")]
    [SerializeField] private float minWaitTime = 2f;
    [Tooltip("Maximum time (seconds) before the next flicker event.")]
    [SerializeField] private float maxWaitTime = 8f;

    [Header("Burst Settings")]
    [Tooltip("Minimum number of toggles in a single burst.")]
    [SerializeField] private int minFlickersPerBurst = 3;
    [Tooltip("Maximum number of toggles in a single burst.")]
    [SerializeField] private int maxFlickersPerBurst = 8;

    [Tooltip("Fastest possible delay between toggles.")]
    [SerializeField] private float minFlickerSpeed = 0.05f;
    [Tooltip("Slowest possible delay between toggles.")]
    [SerializeField] private float maxFlickerSpeed = 0.15f;

    private void Start()
    {
        if (lightsToFlicker.Length > 0)
        {
            StartCoroutine(FlickerRoutine());
        }
    }

    private IEnumerator FlickerRoutine()
    {
        while (true)
        {
            float waitTime = Random.Range(minWaitTime, maxWaitTime);
            yield return new WaitForSeconds(waitTime);

            int flickersInBurst = Random.Range(minFlickersPerBurst, maxFlickersPerBurst + 1);

            for (int i = 0; i < flickersInBurst; i++)
            {
                foreach (Light light in lightsToFlicker)
                {
                    if (light != null)
                    {
                        light.enabled = !light.enabled;
                    }
                }

                float flickerDelay = Random.Range(minFlickerSpeed, maxFlickerSpeed);
                yield return new WaitForSeconds(flickerDelay);
            }

            foreach (Light light in lightsToFlicker)
            {
                if (light != null)
                {
                    light.enabled = true;
                }
            }
        }
    }
}