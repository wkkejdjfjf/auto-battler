using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// Inspector helper script to preview the wave system
[ExecuteInEditMode]
public class WaveSystemPreview : MonoBehaviour
{
    [SerializeField] private WaveSystem waveSystem;
    [SerializeField] private int previewWave = 1;

    [ContextMenu("Preview Wave")]
    private void PreviewWave()
    {
        if (waveSystem != null)
        {
            waveSystem.SetWave(previewWave);
        }
    }
}
