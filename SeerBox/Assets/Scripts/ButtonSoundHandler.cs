using UnityEngine;
using UnityEngine.UI;

public class ButtonSoundHandler : MonoBehaviour
{
    private Button button;

    void Start()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(PlayButtonClickSound);
        }
        else
        {
            Debug.LogWarning("ButtonSoundHandler: No Button component found on " + gameObject.name);
        }
    }

    void PlayButtonClickSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick();
        }
        else
        {
            Debug.LogWarning("ButtonSoundHandler: AudioManager instance is null");
        }
    }

    void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(PlayButtonClickSound);
        }
    }
}