using UnityEngine;

public class AudioList : MonoBehaviour
{
    //Add here music clips and SFX clips to be used in the game

    [Header("Locked Door Clip")]
    [SerializeField] public AudioClip lockedDoorSFX;

    [Header("Unlock Door Clip")]
    [SerializeField] public AudioClip unlockDoorSFX;

    [Header("Painting Restoration Music Clip")]
    [SerializeField] public AudioClip paintingRestorationMusic;

    [Header("Painting Restoration Complete SFX Clip")]
    [SerializeField] public AudioClip paintingRestorationCompleteSFX;

    [Header("Enemy Distracted SFX Clip")]
    [SerializeField] public AudioClip enemyDistractedSFX;

    [Header("Player Collectible SFX Clip")]
    [SerializeField] public AudioClip playerCollectibleSFX;

    [Header("Testing Clips")]
    [SerializeField] public AudioClip testMusic;
    [SerializeField] public AudioClip testSFX;

}
