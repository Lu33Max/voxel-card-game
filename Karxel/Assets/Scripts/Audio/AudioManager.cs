using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [SerializeField, Range(0f, 1f)] private float musicVol = 1f;
    [SerializeField, Range(0f, 1f)] private float sfxVol = 1f;
    
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    
    [Header("Global Sounds")]
    [SerializeField] private AudioClip buttonHover;
    [SerializeField] private AudioClip buttonPressed;
    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioClip combatMusic;

    public AudioClip ButtonHover => buttonHover;
    public AudioClip ButtonPressed => buttonPressed;
    public AudioClip MenuMusic => menuMusic;
    public AudioClip CombatMusic => combatMusic;

    public static float SfxVolume { get; private set; } = 1f;
    public static float MusicVolume { get; private set; } = 1f;
    
    private void Awake()
    {
        // TODO: Remove upon settings implementation
        SetVolume(musicVol, sfxVol);
        
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            return;
        }

        Destroy(gameObject);
    }

    public void PlaySFX(AudioClip clip, float minPitch = 0.90f, float maxPitch = 1.1f)
    {
        PlaySFX(sfxSource, clip, minPitch, maxPitch);
    }

    public void PlayMusic(AudioClip clip)
    {
        if (musicSource.clip == clip) 
            return;
        
        musicSource.clip = clip;
        musicSource.Play();
    }

    public void SetVolume(float musicVol, float sfxVol)
    {
        MusicVolume = musicVol;
        SfxVolume = sfxVol;

        musicSource.volume = MusicVolume;
    }
    
    public static void PlaySFX(AudioSource source, AudioClip clip, float minPitch = 0.90f, float maxPitch = 1.1f)
    {
        source.volume = SfxVolume;
        source.pitch = Random.Range(minPitch, maxPitch);
        source.PlayOneShot(clip);
        source.pitch = 1.0f;
    }
}