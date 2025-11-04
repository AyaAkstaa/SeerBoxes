using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource audioSource;
    public AudioSource typewriterAudioSource;
    public AudioSource musicAudioSource; 

    [Header("Sound Effects")]
    public AudioClip chestHoverSound;
    public AudioClip correctChestSound;
    public AudioClip wrongChestSound;
    public AudioClip buttonClickSound;
    public AudioClip winSound;
    public AudioClip loseSound;
    public AudioClip typewriterSound;

    [Header("Background Music")]
    public AudioClip backgroundMusic;

    [Header("Volume Settings")]
    [Range(0, 1)] public float chestHoverVolume = 0.3f;
    [Range(0, 1)] public float correctChestVolume = 0.8f;
    [Range(0, 1)] public float wrongChestVolume = 0.6f;
    [Range(0, 1)] public float buttonClickVolume = 0.5f;
    [Range(0, 1)] public float winSoundVolume = 1f;
    [Range(0, 1)] public float loseSoundVolume = 1f;
    [Range(0, 1)] public float typewriterVolume = 0.7f;
    [Range(0, 1)] public float musicVolume = 0.5f; // Громкость музыки

    [Header("Typewriter Settings")]
    [Range(0.5f, 2f)] public float minPitch = 0.8f;
    [Range(0.5f, 2f)] public float maxPitch = 1.2f;
    public bool typewriterEnabled = true;

    [Header("Music Settings")]
    public bool musicEnabled = true;
    public bool loopMusic = true;

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

        // Create music audio source if it doesn't exist
        if (musicAudioSource == null)
        {
            musicAudioSource = gameObject.AddComponent<AudioSource>();
            musicAudioSource.playOnAwake = false;
            musicAudioSource.loop = loopMusic;
        }

        // Start background music if enabled
        if (musicEnabled && backgroundMusic != null)
        {
            PlayBackgroundMusic();
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

    // ====== МУЗЫКАЛЬНЫЕ МЕТОДЫ ======

    // Воспроизведение фоновой музыки
    public void PlayBackgroundMusic()
    {
        if (!musicEnabled || backgroundMusic == null || musicAudioSource == null)
            return;

        if (!musicAudioSource.isPlaying || musicAudioSource.clip != backgroundMusic)
        {
            musicAudioSource.clip = backgroundMusic;
            musicAudioSource.volume = musicVolume;
            musicAudioSource.Play();
        }
    }

    // Остановка музыки
    public void StopMusic()
    {
        if (musicAudioSource != null && musicAudioSource.isPlaying)
        {
            musicAudioSource.Stop();
        }
    }

    // Пауза музыки
    public void PauseMusic()
    {
        if (musicAudioSource != null && musicAudioSource.isPlaying)
        {
            musicAudioSource.Pause();
        }
    }

    // Продолжить музыку
    public void ResumeMusic()
    {
        if (musicAudioSource != null && !musicAudioSource.isPlaying && musicAudioSource.clip != null)
        {
            musicAudioSource.UnPause();
        }
    }

    // ====== НАСТРОЙКИ ГРОМКОСТИ ======

    // Установка общей громкости звуковых эффектов
    public void SetVolume(float volume)
    {
        if (audioSource != null)
        {
            audioSource.volume = volume;
        }
    }

    // Установка громкости музыки
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicAudioSource != null)
        {
            musicAudioSource.volume = musicVolume;
        }
    }

    // Установка громкости звуковых эффектов
    public void SetSFXVolume(float volume)
    {
        if (audioSource != null)
        {
            audioSource.volume = volume;
        }
        if (typewriterAudioSource != null)
        {
            typewriterAudioSource.volume = volume;
        }
    }

    // Включение/выключение музыки
    public void SetMusicEnabled(bool enabled)
    {
        musicEnabled = enabled;
        if (musicEnabled && !musicAudioSource.isPlaying)
        {
            PlayBackgroundMusic();
        }
        else if (!musicEnabled)
        {
            StopMusic();
        }
    }

    // Установка громкости кнопок
    public void SetButtonClickVolume(float volume)
    {
        buttonClickVolume = Mathf.Clamp01(volume);
    }
    
    // Установка громкости печатной машинки
    public void SetTypewriterVolume(float volume)
    {
        typewriterVolume = Mathf.Clamp01(volume);
    }
}