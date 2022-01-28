using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

using Random = UnityEngine.Random;

public class Menu : MonoBehaviour
{
    public Transform menuPanel;
    public Transform settingsPanel;
    public Transform gameplaySettingsPanel;
    public Transform videoSettingsPanel;
    public Transform audioSettingsPanel;
    public Transform controlsSettingsPanel;

    public Button continueButton;

    public TMP_Dropdown resolutionsDropdown;
    public TMP_Dropdown fullScreenModesDropdown;
    public TMP_Dropdown qualitiesDropdown;

    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider SFXVolumeSlider;   

    private float currentMasterVolume;
    private float currentMusicVolume;
    private float currentSFXVolume;

    private float newMasterVolume;
    private float newMusicVolume;
    private float newSFXVolume;

    [SerializeField] private InputActionAsset inputActions;

    private void Start()
    {
        resolutionsDropdown.options.Clear();
        Resolution[] resolutions = Screen.resolutions;
        foreach (var resolution in resolutions)
        {
            resolutionsDropdown.options.Add(new TMP_Dropdown.OptionData() 
                { text = resolution.width + "x" + resolution.height + " : " + resolution.refreshRate });
        }

        fullScreenModesDropdown.options.Clear();
        foreach (var fullScreenMode in Enum.GetNames(typeof(FullScreenMode)))
        {
            fullScreenModesDropdown.options.Add(new TMP_Dropdown.OptionData() { text = fullScreenMode });
        }

        qualitiesDropdown.options.Clear();
        string[] names = QualitySettings.names;
        for (int i = 0; i < names.Length; i++)
        {
            qualitiesDropdown.options.Add(new TMP_Dropdown.OptionData() { text = names[i] });
        }

        if(GameUI.Instance == null && !SaveLoadProgress.SaveFileExists())
        {
            continueButton.interactable = false;
        }
    }

    public void NewGame()
    {
        GameManager.Instance.seed = Random.Range(0, 10000);
        GameManager.paused = false;
        GameManager.Instance.isNewGame = true;
        GameManager.Instance.isLoading = true;
        SceneLoader.Instance.LoadTravelMapScene();
    }

    public void ContitnueGame()
    {
        SaveLoadProgress.Load();
        GameManager.paused = false;
        GameManager.Instance.isNewGame = false;
        GameManager.Instance.isLoading = true;
        SceneLoader.Instance.LoadTravelMapScene();
    }

    public bool CloseMenu()
    {
        if (settingsPanel.gameObject.activeSelf)
        {
            CloseSettings();
            return false;
        }
        else
        {
            gameObject.SetActive(false);
            return true;
        }
    }

    public void OpenSettings()
    {
        Resolution[] resolutions = Screen.resolutions;

        resolutionsDropdown.value = Array.FindIndex(resolutions, res => res.width == Screen.currentResolution.width &&
            res.height == Screen.currentResolution.height && (res.refreshRate == Screen.currentResolution.refreshRate || res.refreshRate + 1 == Screen.currentResolution.refreshRate));

        fullScreenModesDropdown.value = (int)Screen.fullScreenMode;

        qualitiesDropdown.value = QualitySettings.GetQualityLevel();

        masterVolumeSlider.value = newMasterVolume = currentMasterVolume = AudioManager.Instance.MasterVolume;
        musicVolumeSlider.value = newMusicVolume = currentMusicVolume = AudioManager.Instance.MusicVolume;
        SFXVolumeSlider.value = newSFXVolume = currentSFXVolume = AudioManager.Instance.SFXVolume;

        menuPanel.gameObject.SetActive(false);
        settingsPanel.gameObject.SetActive(true);
    }

    public void ResetAllBindings()
    {
        foreach(InputActionMap map in inputActions.actionMaps)
        {
            map.RemoveAllBindingOverrides();
        }

        PlayerPrefs.DeleteKey("rebinds");
    }

    public void CloseSettings()
    {
        AudioManager.Instance.MasterVolume = currentMasterVolume;
        AudioManager.Instance.MusicVolume = currentMusicVolume;
        AudioManager.Instance.SFXVolume = currentSFXVolume;

        newMasterVolume = currentMasterVolume;
        newMusicVolume = currentMusicVolume;
        newSFXVolume = currentSFXVolume;

        menuPanel.gameObject.SetActive(true);
        settingsPanel.gameObject.SetActive(false);
    }

    public void AcceptSettings()
    {
        currentMasterVolume = newMasterVolume;
        currentMusicVolume = newMusicVolume;
        currentSFXVolume = newSFXVolume;

        PlayerPrefs.SetFloat(GameManager.masterVolumePlayerPrefKey, newMasterVolume);
        PlayerPrefs.SetFloat(GameManager.musicVolumePlayerPrefKey, newMusicVolume);
        PlayerPrefs.SetFloat(GameManager.SFXVolumePlayerPrefKey, newSFXVolume);

        if (Screen.currentResolution.height != Screen.resolutions[resolutionsDropdown.value].height ||
            Screen.currentResolution.width != Screen.resolutions[resolutionsDropdown.value].width ||
            (Screen.currentResolution.refreshRate != Screen.resolutions[resolutionsDropdown.value].refreshRate && Screen.currentResolution.refreshRate != Screen.resolutions[resolutionsDropdown.value].refreshRate + 1) ||
            Screen.fullScreenMode != (FullScreenMode)fullScreenModesDropdown.value)
        {
            Screen.SetResolution(Screen.resolutions[resolutionsDropdown.value].width, Screen.resolutions[resolutionsDropdown.value].height,
                (FullScreenMode)fullScreenModesDropdown.value, Screen.resolutions[resolutionsDropdown.value].refreshRate);

            PlayerPrefs.SetInt(GameManager.resolutionWidthPlayerPrefKey, Screen.currentResolution.width);
            PlayerPrefs.SetInt(GameManager.resolutionHeightPlayerPrefKey, Screen.currentResolution.height);
            PlayerPrefs.SetInt(GameManager.resolutionRefreshRatePlayerPrefKey, Screen.currentResolution.refreshRate);
            PlayerPrefs.SetInt(GameManager.fullScreenPlayerPrefKey, (int)Screen.fullScreenMode);
        }

        if (QualitySettings.GetQualityLevel() != qualitiesDropdown.value)
        {
            QualitySettings.SetQualityLevel(qualitiesDropdown.value);

            PlayerPrefs.SetInt(GameManager.qualityPlayerPrefKey, QualitySettings.GetQualityLevel());
        }
    }

    public void OpenGameplayPanel()
    {
        gameplaySettingsPanel.gameObject.SetActive(true);
        videoSettingsPanel.gameObject.SetActive(false);
        audioSettingsPanel.gameObject.SetActive(false);
        controlsSettingsPanel.gameObject.SetActive(false);
    }

    public void OpenVideoPanel()
    {
        gameplaySettingsPanel.gameObject.SetActive(false);
        videoSettingsPanel.gameObject.SetActive(true);
        audioSettingsPanel.gameObject.SetActive(false);
        controlsSettingsPanel.gameObject.SetActive(false);
    }

    public void OpenAudioPanel()
    {
        gameplaySettingsPanel.gameObject.SetActive(false);
        videoSettingsPanel.gameObject.SetActive(false);
        audioSettingsPanel.gameObject.SetActive(true);
        controlsSettingsPanel.gameObject.SetActive(false);
    }

    public void OpenControlsPanel()
    {
        gameplaySettingsPanel.gameObject.SetActive(false);
        videoSettingsPanel.gameObject.SetActive(false);
        audioSettingsPanel.gameObject.SetActive(false);
        controlsSettingsPanel.gameObject.SetActive(true);
    }

    public void SetMasterVolume(float masterVolume)
    {
        newMasterVolume = masterVolume;
        AudioManager.Instance.MasterVolume = masterVolume;
    }

    public void SetMusicVolume(float musicVolume)
    {
        newMusicVolume = musicVolume;
        AudioManager.Instance.MusicVolume = musicVolume;
    }

    public void SetSFXVolume(float SFXVolume)
    {
        newSFXVolume = SFXVolume;
        AudioManager.Instance.SFXVolume = SFXVolume;
    }

    public void QuitToMainMenu()
    {
        GameUI.Instance.CloseMenu();
        SceneLoader.Instance.LoadMenuScene();
    }

    public void Quit()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
