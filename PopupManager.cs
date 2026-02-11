using UnityEngine;

public class PopupManager : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private GameObject newWavePopup;
    private WaveSystem wavesystem;
    [SerializeField] private Transform notifPopupPos;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        wavesystem = FindAnyObjectByType<WaveSystem>(); 
        wavesystem.OnMiniWaveStarted += OnMiniWaveStarted;
    }

    void OnMiniWaveStarted(int WaveNumber, int miniWaveCount)
    {
        GameObject popupObject = Instantiate(newWavePopup, notifPopupPos.position, Quaternion.identity);
        popupObject.GetComponent<Popup>().SetText($"Wave {WaveNumber} Miniwave {miniWaveCount + 1} has started");
    }
}
