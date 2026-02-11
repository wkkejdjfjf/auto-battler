using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Popup : MonoBehaviour
{
    public TextMeshProUGUI text;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Destroy(gameObject, 1.5f);
    }

    public void SetText(string value)
    {
        text.text = value;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
