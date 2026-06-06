using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class EnemyManager : MonoBehaviour
{
    [Header("Player Tracking")]
    [SerializeField] private GameObject targetPlayer;

    [Header("All Floor Enemies")]
    [Tooltip("Assign all the ghost AI state machines currently placed on your map floors here.")]
    [SerializeField] private List<EnemyStateMachine> managedEnemies = new List<EnemyStateMachine>();

    [Header("Timer Setup")]
    [Tooltip("The time interval sequences in seconds. After each check, it moves to the next index.")]
    [SerializeField] private float[] intervals = new float[] { 60f, 50f, 40f, 30f };

    [Header("Progression Constraints")]
    [SerializeField] private int triggersRequiredToActivate = 3;
    private int currentTriggerCount = 0;
    private bool systemIsActivated = false;

    [Header("Spawning Safety")]
    [SerializeField] private float minimumSpawnDistance = 15f;

    [Header("Post Processing Settings")]
    [SerializeField] private Volume postProcessVolume;
    [SerializeField] private float dynamicEffectRadius = 20f;
    [SerializeField] private float minVignetteIntensity = 0.2f;
    [SerializeField] private float maxVignetteIntensity = 0.65f;
    [Range(-100f, 0f)]
    [SerializeField] private float maxDesaturationTarget = -75f;
    [Range(0f, 1f)]
    [SerializeField] private float maxChromaticIntensity = 0.8f;
    private Vignette vignetteComponent;
    private ColorAdjustments colorAdjustments;
    private ChromaticAberration chromaticAberration;

    private float defaultFieldOfView = 60f;

    [Header("Camera Zoom Settings")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float zoomedFieldOfView = 30f;
    [SerializeField] private float zoomReturnSpeed = 3f;

    private int currentIntervalIndex = 0;
    private float checkTimer = 0f;
    private EnemyStateMachine activeGhost = null;

    private void Start()
    {
        if (targetPlayer == null)
        {
            targetPlayer = GameObject.FindWithTag("Player");
            if (targetPlayer == null) Debug.LogError("[EnemyManager] Target Player is missing!");
        }

        if (playerCamera != null)
        {
            defaultFieldOfView = playerCamera.fieldOfView;
        }
        else
        {
            playerCamera = Camera.main;
            if (playerCamera != null) defaultFieldOfView = playerCamera.fieldOfView;
            else Debug.LogWarning("[EnemyManager] Player Camera is unassigned and Camera.main was not found!");
        }

        if (postProcessVolume != null)
        {
            postProcessVolume.profile.TryGet(out vignetteComponent);
            postProcessVolume.profile.TryGet(out colorAdjustments);
            postProcessVolume.profile.TryGet(out chromaticAberration);
        }
        else
        {
            Debug.LogWarning("[EnemyManager] PostProcess Volume component is missing from inspector!");
        }

        foreach (var ghost in managedEnemies)
        {
            if (ghost != null)
            {
                ghost.SetActiveGhost(false);
            }
        }


        ResetManager();
    }

    private void Update()
    {
        HandleProximityEffects();

        if (!systemIsActivated || managedEnemies.Count == 0 || targetPlayer == null) return;

        checkTimer += Time.deltaTime;
        float currentTargetDuration = intervals[Mathf.Clamp(currentIntervalIndex, 0, intervals.Length - 1)];

        if (checkTimer >= currentTargetDuration)
        {
            EvaluateAndSwitchFloors();

            checkTimer = 0f;
            if (currentIntervalIndex < intervals.Length - 1)
            {
                currentIntervalIndex++;
                Debug.Log($"[EnemyManager] Timer accelerated. Next check occurs in: {intervals[currentIntervalIndex]}s");
            }
        }
    }
    private void HandleProximityEffects()
    {
        if (!systemIsActivated || activeGhost == null || targetPlayer == null)
        {
            RestoreDefaultPostProcessing();
            return;
        }

        float distance = Vector3.Distance(targetPlayer.transform.position, activeGhost.transform.position);

        if (distance <= dynamicEffectRadius)
        {
            float proximityFactor = 1f - (distance / dynamicEffectRadius);

            if (vignetteComponent != null)
            {
                float targetVignette = Mathf.Lerp(minVignetteIntensity, maxVignetteIntensity, proximityFactor);
                vignetteComponent.intensity.value = Mathf.MoveTowards(vignetteComponent.intensity.value, targetVignette, Time.deltaTime * 0.5f);
            }

            if (colorAdjustments != null)
            {
                float targetSaturation = Mathf.Lerp(0f, maxDesaturationTarget, proximityFactor);
                colorAdjustments.saturation.value = Mathf.MoveTowards(colorAdjustments.saturation.value, targetSaturation, Time.deltaTime * 40f);
            }

            if (chromaticAberration != null)
            {
                float jitterAmount = (distance <= 8f) ? Mathf.Sin(Time.time * 35f) * 0.15f : 0f;
                float targetChromatic = Mathf.Lerp(0f, maxChromaticIntensity, proximityFactor) + jitterAmount;
                chromaticAberration.intensity.value = Mathf.MoveTowards(chromaticAberration.intensity.value, Mathf.Clamp01(targetChromatic), Time.deltaTime * 2f);
            }

            if (playerCamera != null)
            {
                float targetFOV = Mathf.Lerp(defaultFieldOfView, zoomedFieldOfView, proximityFactor);
                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * zoomReturnSpeed);
            }
        }
        else
        {
            RestoreDefaultPostProcessing();
        }
    }

    private void RestoreDefaultPostProcessing()
    {
        if (vignetteComponent != null)
            vignetteComponent.intensity.value = Mathf.MoveTowards(vignetteComponent.intensity.value, minVignetteIntensity, Time.deltaTime * 0.5f);

        if (colorAdjustments != null)
            colorAdjustments.saturation.value = Mathf.MoveTowards(colorAdjustments.saturation.value, 0f, Time.deltaTime * 40f);

        if (chromaticAberration != null)
            chromaticAberration.intensity.value = Mathf.MoveTowards(chromaticAberration.intensity.value, 0f, Time.deltaTime * 2f);

        if (playerCamera != null && !Mathf.Approximately(playerCamera.fieldOfView, defaultFieldOfView))
        {
            playerCamera.fieldOfView = Mathf.MoveTowards(playerCamera.fieldOfView, defaultFieldOfView, Time.deltaTime * (zoomReturnSpeed * 10f));
        }
    }

    public void ReportTriggerActivated()
    {
        if (systemIsActivated) return; 

        currentTriggerCount++;
        Debug.Log($"[EnemyManager] Trigger recorded! Progress: ({currentTriggerCount}/{triggersRequiredToActivate})");

        if (currentTriggerCount >= triggersRequiredToActivate)
        {
            systemIsActivated = true;
            Debug.Log("[EnemyManager] 3 Triggers hit.");

            EvaluateAndSwitchFloors();
        }
    }

    private void EvaluateAndSwitchFloors()
    {
        if (!systemIsActivated) return;

        float playerY = targetPlayer.transform.position.y;
        EnemyStateMachine closestGhostOnY = null;
        float absoluteClosestDistanceY = float.MaxValue;

        foreach (var ghost in managedEnemies)
        {
            if (ghost == null) continue;

            float diffY = Mathf.Abs(ghost.transform.position.y - playerY);
            if (diffY < absoluteClosestDistanceY)
            {
                absoluteClosestDistanceY = diffY;
                closestGhostOnY = ghost;
            }
        }

        if (closestGhostOnY != null && closestGhostOnY != activeGhost)
        {
            Debug.Log($"[EnemyManager] Handover initiated! Player floor height: {playerY}. Target: {closestGhostOnY.gameObject.name}");

            foreach (var ghost in managedEnemies)
            {
                if (ghost == null) continue;

                if (ghost == closestGhostOnY)
                {
                    float currentDistance = Vector3.Distance(targetPlayer.transform.position, ghost.Enemy.transform.position);

                    if (currentDistance < minimumSpawnDistance)
                    {
                        Debug.LogWarning($"[EnemyManager] {ghost.gameObject.name} is too close ({currentDistance}m). Repositioning to a safe spot on the same floor...");
                        RepositionGhostToSafePoint(ghost);
                    }

                    ghost.SetActiveGhost(true);
                }
                else
                {
                    ghost.SetActiveGhost(false);
                }
            }

            activeGhost = closestGhostOnY;
        }
    }

    private void RepositionGhostToSafePoint(EnemyStateMachine ghost)
    {
        if (ghost.teleportPoints == null || ghost.teleportPoints.Count == 0) return;

        Vector3 playerPos = targetPlayer.transform.position;
        List<Transform> safePointsOnFloor = new List<Transform>();

        foreach (Transform point in ghost.teleportPoints)
        {
            if (point == null) continue;

            float floorHeightDiff = Mathf.Abs(point.position.y - ghost.Enemy.transform.position.y);
            if (floorHeightDiff > 2.0f) continue; // Skip points on other floors

            Vector2 player2D = new Vector2(playerPos.x, playerPos.z);
            Vector2 point2D = new Vector2(point.position.x, point.position.z);

            if (Vector2.Distance(point2D, player2D) >= minimumSpawnDistance)
            {
                safePointsOnFloor.Add(point);
            }
        }

        if (safePointsOnFloor.Count > 0)
        {
            Transform chosenPoint = safePointsOnFloor[Random.Range(0, safePointsOnFloor.Count)];
            ghost.Enemy.transform.position = chosenPoint.position;

            Debug.Log($"[EnemyManager] Safe relocation successful. {ghost.gameObject.name} moved to: {chosenPoint.name} ({Vector3.Distance(playerPos, chosenPoint.position)}m away)");
        }
        else
        {
            Debug.LogWarning($"[EnemyManager] Could not find any teleport nodes outside the {minimumSpawnDistance}m radius on this floor! Ghost will stay at its default starting location.");
        }
    }

    public void ResetManager()
    {
        checkTimer = 0f;
        currentIntervalIndex = 0;
        currentTriggerCount = 0;
        systemIsActivated = false;
        activeGhost = null;

        foreach (var ghost in managedEnemies)
        {
            if (ghost != null) ghost.SetActiveGhost(false);
        }
    }
}