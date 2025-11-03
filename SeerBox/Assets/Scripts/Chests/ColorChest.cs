using UnityEngine;
using UnityEngine.UI;

public class ColorChest : Chest
{
    public Image chestImage; // Assign in prefab, the Image component to color

    // Переопределяем Init для ColorChest
    public override void Init(int idx, System.Action<int> onClicked)
    {
        // Сначала вызываем базовую инициализацию
        base.Init(idx, onClicked);
        
        // Убеждаемся, что chestImage установлен
        if (chestImage == null)
        {
            chestImage = GetComponent<Image>();
            if (chestImage == null)
            {
                // Если все еще null, ищем в дочерних объектах
                chestImage = GetComponentInChildren<Image>();
            }
        }
        
        // Гарантируем, что изображение видимо
        if (chestImage != null)
        {
            chestImage.color = new Color(chestImage.color.r, chestImage.color.g, chestImage.color.b, 1f);
        }
    }

    public void SetColor(Color c)
    {
        if (chestImage != null)
        {
            chestImage.color = c;
            // Гарантируем полную непрозрачность
            chestImage.color = new Color(c.r, c.g, c.b, 1f);
        }
        else
        {
            // fallback: ищем Image на этом объекте
            var img = GetComponent<Image>();
            if (img != null) 
            {
                img.color = c;
                img.color = new Color(c.r, c.g, c.b, 1f);
            }
        }
    }

    void Awake()
    {
        // Инициализируем chestImage
        if (chestImage == null) 
        {
            chestImage = GetComponent<Image>();
            if (chestImage == null)
            {
                chestImage = GetComponentInChildren<Image>();
            }
        }
    }
}