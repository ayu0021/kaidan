using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GlobalSettingsUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject settingPanel;
    public Button settingButton;
    public Button closeButton;
    public Button backToMainMenuButton;
    public Slider musicVolumeSlider;

    [Header("Audio")]
    public AudioSource musicAudioSource;

    [Header("Scene")]
    public string mainMenuSceneName = "MainMenu";

    private const string MUSIC_VOLUME_KEY = "MusicVolume";

    void Start()
    {
        if (settingPanel != null)
            settingPanel.SetActive(false);

        float savedVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 0.5f);

        if (musicAudioSource != null)
            musicAudioSource.volume = savedVolume;

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.value = savedVolume;
            musicVolumeSlider.onValueChanged.RemoveAllListeners();
            musicVolumeSlider.onValueChanged.AddListener(ChangeMusicVolume);
        }

        if (settingButton != null)
        {
            settingButton.onClick.RemoveAllListeners();
            settingButton.onClick.AddListener(OpenSettingPanel);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseSettingPanel);
        }

        if (backToMainMenuButton != null)
        {
            backToMainMenuButton.onClick.RemoveAllListeners();
            backToMainMenuButton.onClick.AddListener(BackToMainMenu);
        }
    }

    public void OpenSettingPanel()
    {
        if (settingPanel != null)
            settingPanel.SetActive(true);
    }

    public void CloseSettingPanel()
    {
        if (settingPanel != null)
            settingPanel.SetActive(false);
    }

    public void ChangeMusicVolume(float volume)
    {
        if (musicAudioSource != null)
            musicAudioSource.volume = volume;

        PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, volume);
        PlayerPrefs.Save();
    }

    public void BackToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}