using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    WaveSystem waveSystem;

    // Start is called before the first frame update
    void Start()
    {
        waveSystem = FindFirstObjectByType<WaveSystem>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnDestroy()
    {
        waveSystem.NotifyPlayerDeath();
    }
}
