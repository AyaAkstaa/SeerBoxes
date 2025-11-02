using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class Chest : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler {
    public int index;
    public Image body;
    public Text label;
    public Action<int> OnClicked;

    public virtual void Init(int idx, Action<int> onClicked) {
        index = idx;
        OnClicked = onClicked;
        if (label) label.text = (idx+1).ToString();
        gameObject.SetActive(true);
    }

    public void OnPointerClick(PointerEventData eventData) {
        OnClicked?.Invoke(index);
    }

    public void OnPointerEnter(PointerEventData eventData) {
        if (label) label.color = Color.yellow;
    }
    public void OnPointerExit(PointerEventData eventData) {
        if (label) label.color = Color.white;
    }

    public void SetLabelText(string s) { if (label) label.text = s; }
    public void SetImage(Sprite sp) { /* for ImageChest override or set child image */ }
    public void SetColor(Color c) { if (body) body.color = c; }
    public void SetAlpha(float a) { var col = body.color; col.a = a; body.color = col; }
}
