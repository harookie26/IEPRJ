using UnityEngine;
using Game.ObjectTypes;

[RequireComponent(typeof(Collider))]
public class PaintingInteractable : MonoBehaviour, IInteractable
{
    private AudioSource sfxAudioSource;
    private AudioList audioList;



    /* When Awake is called it will find the SFX object with the 
     SFX audio source component in the scene and store a reference to it.

     This audio source is what will be used to play the SFX clips with 
     the VolumeSettings script controlling the volume.
    
     Its important that each scene has a "Settings With Audio Manager Container"
     as this prefab houses everything needed for audio management including the audio mixer 
     and settings panel.

     this prefab already includes the SFX and Music audio source object with the correct tag 
     so as long this container is in every scene there should be no problem.
     */
    void Awake()
    {
        audioList = FindAnyObjectByType<AudioList>();
        //Find the SFX audio source object in the scene by its tag
        GameObject audioObject = GameObject.FindWithTag("SFXAudioSource");

        if (audioObject != null)
        {
            sfxAudioSource = audioObject.GetComponent<AudioSource>();
        }
        else
        {
            Debug.LogWarning("No GameObject with tag 'SFXAudioSource' found in scene.");
        }

    }

    public void Interact()
    {
        // Find the enemy state machine in the scene and tell it to distract at this painting.
        var enemyStateMachine = Object.FindFirstObjectByType<EnemyStateMachine>();
        if (enemyStateMachine == null)
        {
            Debug.LogWarning("PaintingInteractable.Interact: No EnemyStateMachine found in scene.");
            return;
        }

        sfxAudioSource.PlayOneShot(audioList.enemyDistractedSFX); //Play the SFX using the SFX audio source object reference
        enemyStateMachine.DistractAt(transform.position);
    }
}