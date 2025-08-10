using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private TextMeshProUGUI musicValue;
    [SerializeField] private TextMeshProUGUI sfxValue;

    [CanBeNull] private CameraController _cameraController;
    
    private void Start()
    {
        if (Camera.main != null && Camera.main.TryGetComponent(typeof(CameraController), out var controller))
        {
            _cameraController = (CameraController)controller;
            // Start only executed on the first SetActive, but doesn't separately call onEnable
            _cameraController!.DisableMovement(true);
        }
        
        musicSlider.value = PlayerPrefs.GetInt("musicVol", 10);
        sfxSlider.value = PlayerPrefs.GetInt("sfxInt", 10);
    }

    private void OnEnable()
    {
        if (_cameraController != null) 
            _cameraController.DisableMovement(true);
    }
    
    private void OnDisable()
    {
        if (_cameraController != null) 
            _cameraController.DisableMovement(false);
    }

    public void OnMusicSliderChanged()
    {
        var newValue = musicSlider.value;

        musicValue.text = ((int)newValue).ToString();
        AudioManager.Instance.SetVolume(newValue / 10f, AudioManager.SfxVolume);
        PlayerPrefs.SetInt("musicVol", (int) newValue);
    }
    
    public void OnSfxSliderChanged()
    {
        var newValue = sfxSlider.value;
        
        sfxValue.text = ((int)newValue).ToString();
        AudioManager.Instance.SetVolume(AudioManager.MusicVolume, newValue / 10f);
        PlayerPrefs.SetInt("sfxVol", (int) newValue);
    }
}
