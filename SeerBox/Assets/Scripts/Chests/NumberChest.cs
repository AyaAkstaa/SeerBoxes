using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NumberChest : Chest {
    public TextMeshProUGUI numberLabel;
    public void SetNumber(int v) {
        if (numberLabel) numberLabel.text = v.ToString();
        gameObject.name = "NumberChest_" + v;
    }
    public void SetNumber(string s) {
        if (numberLabel) numberLabel.text = s;
        gameObject.name = "NumberChest_" + s;
    }
}