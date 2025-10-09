using UnityEngine;

public class AudioTest : MonoBehaviour
{
    private AudioSource musicAudioSource;
    private AudioSource sfxAudioSource;
    private AudioList audioList;

    void Awake()
    {
        audioList = FindAnyObjectByType<AudioList>();
        //Find the SFX audio source object in the scene by its tag
        GameObject audioObject1 = GameObject.FindWithTag("SFXAudioSource");
        GameObject audioObject2 = GameObject.FindWithTag("MusicAudioSource");

        if (audioObject1 != null)
        {
            
            sfxAudioSource = audioObject1.GetComponent<AudioSource>();
            AudioClip sfxClip = audioList.testSFX;
            sfxAudioSource.clip = sfxClip;
        }
        else
        {
            //.
            Debug.LogWarning("No GameObject with tag 'SFXAudioSource' found in scene.");
        }

        if (audioObject2 != null)
        {
            musicAudioSource = audioObject2.GetComponent<AudioSource>();
            AudioClip musicClip = audioList.testMusic;
            musicAudioSource.clip = musicClip;
        }
        else
        {
            Debug.LogWarning("No GameObject with tag 'MusicAudioSource' found in scene.");
        }

        PlayTest();
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    void PlayTest()
    {
        sfxAudioSource.Play();
        Debug.Log("Playing test SFX");
        musicAudioSource.Play();
        Debug.Log("Playing test Music");
    }
}
