using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Source")]
    public AudioSource audioSource;

    [Header("Sound Effects")]
    public AudioClip chestHoverSound;
    public AudioClip correctChestSound;
    public AudioClip wrongChestSound;
    public AudioClip buttonClickSound;
    public AudioClip winSound;
    public AudioClip loseSound;

    [Header("Volume Settings")]
    [Range(0, 1)] public float chestHoverVolume = 0.3f;
    [Range(0, 1)] public float correctChestVolume = 0.8f;
    [Range(0, 1)] public float wrongChestVolume = 0.6f;
    [Range(0, 1)] public float buttonClickVolume = 0.5f;
    [Range(0, 1)] public float winSoundVolume = 1f;
    [Range(0, 1)] public float loseSoundVolume = 1f;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Ensure audio source exists
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    public void PlayChestHover()
    {
        if (chestHoverSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(chestHoverSound, chestHoverVolume);
        }
    }

    public void PlayCorrectChest()
    {
        if (correctChestSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(correctChestSound, correctChestVolume);
        }
    }

    public void PlayWrongChest()
    {
        if (wrongChestSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(wrongChestSound, wrongChestVolume);
        }
    }

    public void PlayButtonClick()
    {
        if (buttonClickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(buttonClickSound, buttonClickVolume);
        }
    }

    public void PlayWinSound()
    {
        if (winSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(winSound, winSoundVolume);
        }
    }

    public void PlayLoseSound()
    {
        if (loseSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(loseSound, loseSoundVolume);
        }
    }

    // Optional: Control volume
    public void SetVolume(float volume)
    {
        if (audioSource != null)
        {
            audioSource.volume = volume;
        }
    }

    // Optional: Control individual volumes
    public void SetButtonClickVolume(float volume)
    {
        buttonClickVolume = Mathf.Clamp01(volume);
    }
}