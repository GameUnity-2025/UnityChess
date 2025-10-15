
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    // ===== BGM (nhạc nền menu) =====
    [Header("Menu BGM")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip menuBgm;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.6f;
    [SerializeField, Range(0.05f, 3f)] private float fadeTime = 0.5f;

    private void Awake()
    {
        EnsureMusicSource();
        PlayMenuBgm();
    }

    private void EnsureMusicSource()
    {
        if (musicSource == null)
        {
            musicSource = gameObject.GetComponent<AudioSource>();
            if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
        }
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f; // 2D
        musicSource.volume = musicVolume;
    }

    private void PlayMenuBgm()
    {
        if (menuBgm == null) return;
        if (musicSource.clip != menuBgm) musicSource.clip = menuBgm;
        if (!musicSource.isPlaying) musicSource.Play();
    }

    private System.Collections.IEnumerator FadeOutAndStop()
    {
        float start = musicSource.volume;
        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(start, 0f, t / fadeTime);
            yield return null;
        }
        musicSource.Stop();
        musicSource.volume = musicVolume; // reset cho lần sau
    }

    private System.Collections.IEnumerator FadeThenLoad(string sceneName)
    {
        if (musicSource != null && musicSource.isPlaying)
            yield return StartCoroutine(FadeOutAndStop());
        SceneManager.LoadScene(sceneName);
    }

    private System.Collections.IEnumerator FadeThenQuit()
    {
        if (musicSource != null && musicSource.isPlaying)
            yield return StartCoroutine(FadeOutAndStop());
        Application.Quit();
    }
    // ===== /BGM =====

    // Scene name of the main game board
    private string gameSceneName = "Board";

    public void PlayPlayerVsPlayer()
    {
        // Here we will eventually set a static flag or use a ScriptableObject to tell GameManager what mode to start
        Debug.Log("Starting Player vs Player mode...");
        // For now, we just load the scene. The GameManager's default is PvP.
        StartCoroutine(FadeThenLoad(gameSceneName));

    }

    public void PlayPlayerVsAI()
    {
        // This will require more logic to select side (White/Black)
        // For now, let's just log it.
        Debug.Log("Player vs AI mode selected - (Not fully implemented yet)");
        // Example of how it could work:
        // GameManager.StartAsPlayerVsAI(Side.White);
        // SceneManager.LoadScene(gameSceneName);
    }

    public void OpenSettings()
    {
        // Logic to open a settings panel/scene
        Debug.Log("Opening Settings... (Not implemented yet)");
    }

    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        StartCoroutine(FadeThenQuit());

    }
}
