using Game.ObjectTypes;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PaintingInteractable : MonoBehaviour, IInteractable
{
    private AudioSource sfxAudioSource;
    private AudioList audioList;

    /* When Awake is called it will find the SFX object with the 
     SFX audio source component in the scene and store a reference to it.

     Along with this is the AudioList component which has all 
     the SFX and Music clips that will be used in the game

     This audio source is what will be used to play the SFX clips with 
     the VolumeSettings script controlling the volume.
    
     Its important that each scene has a "Settings With Audio Manager Container"
     as this prefab houses everything needed for audio management including the audio mixer 
     and settings panel.

     this prefab already includes the SFX and Music audio source object with the correct tag
     and the AudioList (component of AudioManager).
     So as long this container is in every scene there should be no problem.
     */
    void Awake()
    {
        //Find the AudioList object in the scene
        audioList = FindAnyObjectByType<AudioList>();
        //Find the SFX audio source object in the scene by its tag
        GameObject audioObject = GameObject.FindWithTag("SFXAudioSource");

        if (audioObject != null)
            sfxAudioSource = audioObject.GetComponent<AudioSource>();
        else
            Debug.LogWarning("No GameObject with tag 'SFXAudioSource' found in scene.");

    }

    public void Interact()
    {
        // Find the _enemy state machine in the scene and tell it to distract at this painting.
        var enemyStateMachine = FindFirstObjectByType<EnemyStateMachine>();
        if (enemyStateMachine == null)
        {
            Debug.LogWarning("PaintingInteractable.Interact: No EnemyStateMachine found in scene.");
            return;
        }

        // Ensure audio references are available before trying to play
        if (audioList == null)
            audioList = FindAnyObjectByType<AudioList>();

        if (sfxAudioSource == null)
        {
            var audioObject = GameObject.FindWithTag("SFXAudioSource");
            if (audioObject != null)
                sfxAudioSource = audioObject.GetComponent<AudioSource>();
        }

        if (sfxAudioSource != null && audioList != null && audioList.enemyDistractedSFX != null)
        {
            // sfxAudioSource.PlayOneShot(audioList.enemyDistractedSFX);
        }
        else
        {
            Debug.LogWarning("PaintingInteractable.Interact: Missing audio source, AudioList, or clip. Skipping SFX playback.");
        }

        // If this interactable is associated with the main painting, check if it's fully revealed
        var mainPainting = GetComponent<MainPainting>() ?? GetComponentInParent<MainPainting>();

        if (mainPainting != null)
        {
            Debug.Log("PaintingInteractable.Interact: Found MainPainting component. Checking if fully revealed.");
        }
        var gameState = FindFirstObjectByType<GameStateManager>();

        bool revealedByCovers = mainPainting != null && mainPainting.IsFullyRevealed();
        //bool revealedByProgress = gameState != null && gameState.GetCurrentLevelProgress() >= 4;

        if (revealedByCovers) //  || revealedByProgress
        {
            if (gameState != null)
            {
                gameState.TriggerWinSequence();
                Debug.Log("PaintingInteractable.Interact: WIN SEQUENCE TRIGGERED");
                // No need to distract _enemy if win sequence will start
                return;
            }
            else
            {
                Debug.LogWarning("PaintingInteractable.Interact: No GameStateManager found to trigger win sequence.");
            }
        }

        // Hide the interact HUD after a successful interaction
        var ui = FindFirstObjectByType<UIManager>();
        if (ui != null)
        {
            ui.HideHUD(UIManager.Keys.Interact);
        }
    }
}