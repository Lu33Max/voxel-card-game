using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private TextMeshProUGUI musicValue;
    [SerializeField] private TextMeshProUGUI sfxValue;
    
    private void Start()
    {
        musicSlider.value = PlayerPrefs.GetInt("musicVol", 10);
        sfxSlider.value = PlayerPrefs.GetInt("sfxInt", 10);
    }

    public void OnMusicSliderChanged()
    {
        float newValue = musicSlider.value;

        musicValue.text = ((int)newValue).ToString();
        AudioManager.Instance.SetVolume(newValue / 10f, AudioManager.SfxVolume);
        PlayerPrefs.SetInt("musicVol", (int) newValue);
    }
    
    public void OnSfxSliderChanged()
    {
        float newValue = sfxSlider.value;
        
        sfxValue.text = ((int)newValue).ToString();
        AudioManager.Instance.SetVolume(AudioManager.MusicVolume, newValue / 10f);
        PlayerPrefs.SetInt("sfxVol", (int) newValue);
    }
}
