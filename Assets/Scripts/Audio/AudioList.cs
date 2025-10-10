using UnityEngine;

public class AudioList : MonoBehaviour
{
    //Add here music clips and SFX clips to be used in the game

    [Header("Painting Restoration Music Clip")]
    [SerializeField] public AudioClip paintingRestorationMusic;

    [Header("Painting Restoration Complete SFX Clip")]
    [SerializeField] public AudioClip paintingRestorationCompleteSFX;

    [Header("Enemy Distracted SFX Clip")]
    [SerializeField] public AudioClip enemyDistractedSFX;

    [Header("Testing Clips")]
    [SerializeField] public AudioClip testMusic;
    [SerializeField] public AudioClip testSFX;
}
