// using UnityEngine;

// public class ColorChest : Chest {
//     public void SetColor(Color c) { base.SetColor(c); }
// }

using UnityEngine;
using UnityEngine.UI;

public class ColorChest : Chest
{
    public Image chestImage; // Assign in prefab, the Image component to color

    public void SetColor(Color c)
    {
        if (chestImage != null)
        {
            chestImage.color = c;
        }
        else
        {
            // fallback: ищем Image на этом объекте
            var img = GetComponent<Image>();
            if (img != null) img.color = c;
        }
    }

    // Если хочешь, можно инициализировать при старте
    void Awake()
    {
        if (chestImage == null) chestImage = GetComponent<Image>();
    }
}
