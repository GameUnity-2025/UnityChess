using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject settingsPanel;       // Panel chứa menu setting
    public Button openButton;              // Nút mở panel
    public Button closeButton;             // Nút đóng panel
    public Slider volumeSlider;            // (Tuỳ chọn) thanh chỉnh âm thanh

    void Start()
    {
        // Đảm bảo panel tắt ban đầu
        settingsPanel.SetActive(false);

        // Gắn sự kiện nút
        openButton.onClick.AddListener(OpenSettings);
        closeButton.onClick.AddListener(CloseSettings);

        // Gắn sự kiện slider (nếu có)
        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.AddListener(SetVolume);
            volumeSlider.value = AudioListener.volume;
        }
    }

    void OpenSettings()
    {
        settingsPanel.SetActive(true);
    }

    void CloseSettings()
    {
        settingsPanel.SetActive(false);
    }

    void SetVolume(float value)
    {
        AudioListener.volume = value;
    }
}
