using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource audioSource;
    public AudioSource typewriterAudioSource; // Новый AudioSource для звука печатной машинки

    [Header("Sound Effects")]
    public AudioClip chestHoverSound;
    public AudioClip correctChestSound;
    public AudioClip wrongChestSound;
    public AudioClip buttonClickSound;
    public AudioClip winSound;
    public AudioClip loseSound;
    public AudioClip typewriterSound; // Звук печатной машинки

    [Header("Volume Settings")]
    [Range(0, 1)] public float chestHoverVolume = 0.3f;
    [Range(0, 1)] public float correctChestVolume = 0.8f;
    [Range(0, 1)] public float wrongChestVolume = 0.6f;
    [Range(0, 1)] public float buttonClickVolume = 0.5f;
    [Range(0, 1)] public float winSoundVolume = 1f;
    [Range(0, 1)] public float loseSoundVolume = 1f;
    [Range(0, 1)] public float typewriterVolume = 0.7f;

    [Header("Typewriter Settings")]
    [Range(0.5f, 2f)] public float minPitch = 0.8f;
    [Range(0.5f, 2f)] public float maxPitch = 1.2f;
    public bool typewriterEnabled = true;

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

        // Ensure audio sources exist
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
            
        if (typewriterAudioSource == null)
        {
            typewriterAudioSource = gameObject.AddComponent<AudioSource>();
            typewriterAudioSource.playOnAwake = false;
            typewriterAudioSource.loop = false;
        }
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

    // Новый метод для звука печатной машинки
    public void PlayTypewriterSound()
    {
        if (!typewriterEnabled || typewriterSound == null || typewriterAudioSource == null)
            return;

        // Случайным образом меняем тон
        typewriterAudioSource.pitch = Random.Range(minPitch, maxPitch);
        typewriterAudioSource.PlayOneShot(typewriterSound, typewriterVolume);
    }

    // Метод для включения/выключения звука печатной машинки
    public void SetTypewriterEnabled(bool enabled)
    {
        typewriterEnabled = enabled;
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
    
    public void SetTypewriterVolume(float volume)
    {
        typewriterVolume = Mathf.Clamp01(volume);
    }
}