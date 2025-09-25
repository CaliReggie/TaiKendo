using UnityEngine;

public class AudioManager : Singleton<AudioManager>
{
    [Header("References")]

    [SerializeField] private AudioSource musicSource;

    [SerializeField] private AudioSource sfxSource;
    
    protected override void Awake()
    {
        base.Awake();
        
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
        }
        
        if ( sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }
    }
    
    public void PlayMusic(AudioClip music, bool looping = false)
    {
        musicSource.clip = music;

        musicSource.loop = looping;
        
        musicSource.Play();
    }
    
    public void StopMusic()
    {
        if (musicSource.isPlaying) musicSource.Stop();
    }
    
    public void PlaySfx(AudioClip sound, Vector3 pos, float vol = 1)
    {
        sfxSource.transform.position = pos;

        PlaySfx(sound, vol);
    }
    
    public void PlaySfx(AudioClip sound, Vector2 pos, float vol = 1)
    {
        sfxSource.transform.position = new Vector3(pos.x, pos.y, 0);

        PlaySfx(sound, vol);
    }
    
    public void StopSfx()
    {
        if (sfxSource.isPlaying) sfxSource.Stop();
    }
    
    private void PlaySfx(AudioClip sound, float vol = 1)
    {
        sfxSource.PlayOneShot(sound, vol);
    }
}
