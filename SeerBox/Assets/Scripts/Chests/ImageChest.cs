using UnityEngine;
using UnityEngine.UI;

public class ImageChest : Chest
{
    public Image content;
    public void SetContent(Sprite s)
    {
        if (content) content.sprite = s;
    }
}