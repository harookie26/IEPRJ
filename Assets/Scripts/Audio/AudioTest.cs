using UnityEngine;

public class AudioTest : MonoBehaviour
{
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [SerializeField] private AudioClip musicClip;
    [SerializeField] private AudioClip sfxClip;

    private void Awake()
    {
        if (musicSource != null && musicClip != null)
        {
            musicSource.clip = musicClip;
            musicSource.Play();
        }
        if (sfxSource != null && sfxClip != null)
        {
            sfxSource.clip = sfxClip;
            sfxSource.Play();
        }
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
