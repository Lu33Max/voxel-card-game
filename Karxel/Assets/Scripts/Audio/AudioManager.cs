using UnityEngine;
using UnityEngine.Serialization;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    
    [Header("Global Sounds")]
    [SerializeField] private AudioClip buttonHover;
    [SerializeField] private AudioClip buttonPressed;
    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioClip combatMusic;
    
    [Header("Unit Sounds")]
    [SerializeField] private AudioClip unitMove;
    [SerializeField] private AudioClip unitAttack;
    [SerializeField] private AudioClip unitHurt;

    public AudioClip ButtonHover => buttonHover;
    public AudioClip ButtonPressed => buttonPressed;
    public AudioClip MenuMusic => menuMusic;
    public AudioClip CombatMusic => combatMusic;
    public AudioClip UnitMove => unitMove;
    public AudioClip UnitAttack => unitAttack;
    public AudioClip UnitHurt => unitHurt;

    public static float SfxVolume { get; private set; } = 1f;
    public static float MusicVolume { get; private set; } = 1f;
    
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
            
        SetVolume(PlayerPrefs.GetInt("musicVol", 10) / 10f, PlayerPrefs.GetInt("sfxVol", 10) / 10f);
        PlayMusic(menuMusic);
    }

    public void PlaySfx(AudioClip clip, float minPitch = 0.90f, float maxPitch = 1.1f)
    {
        PlaySfx(sfxSource, clip, minPitch, maxPitch);
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
        sfxSource.volume = SfxVolume;
    }
    
    public static void PlaySfx(AudioSource source, AudioClip clip, float minPitch = 0.90f, float maxPitch = 1.1f)
    {
        source.volume = SfxVolume;
        source.pitch = Random.Range(minPitch, maxPitch);
        source.PlayOneShot(clip);
        source.pitch = 1.0f;
    }
}