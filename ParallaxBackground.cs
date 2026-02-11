using UnityEngine;
using System;

public class DynamicParallaxBackground : MonoBehaviour
{
    [Serializable]
    public class ParallaxLayer
    {
        [Header("Layer Setup")]
        public Transform[] layerSegments;

        [Header("Layer Dimensions")]
        public float segmentWidth = 19.2f;  // Width of background segment
        public float segmentHeight = 10.8f; // Height of background segment

        [Header("Normal Scrolling")]
        [Range(0f, 1f)] public float normalScrollSpeed = 0.1f;

        [Header("Wave Completion Boost")]
        [Range(0f, 10f)] public float waveCompletionScrollSpeed = 1f;
        public float waveCompletionDuration = 2f;

        [HideInInspector] public float currentScrollSpeed;
        [HideInInspector] public float waveCompletionTimer;
    }

    [Header("Parallax Layers")]
    public ParallaxLayer[] parallaxLayers;

    [Header("Scrolling Configuration")]
    public Vector2 scrollDirection = Vector2.right;
    public bool followPlayerMovement = false;
    public Transform playerTransform;
    public Camera mainCamera;

    [Header("Wave Completion Boost")]
    [Range(0f, 1f)] public float globalBoostIntensity = 1f;

    public WaveSystem waveSystem;

    private void Start()
    {
        // Find main camera if not assigned
        if (mainCamera == null)
            mainCamera = Camera.main;

        // Find player if not assigned
        if (playerTransform == null)
            playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;

        // Initialize layer scroll speeds and initial positioning
        foreach (var layer in parallaxLayers)
        {
            layer.currentScrollSpeed = layer.normalScrollSpeed;
            PositionLayerSegments(layer);
        }

        waveSystem.OnWaveCompleted += OnWaveCompleted;
    }

    private void PositionLayerSegments(ParallaxLayer layer)
    {
        if (layer.layerSegments == null || layer.layerSegments.Length == 0) return;

        // Position segments side by side with no gaps
        for (int i = 0; i < layer.layerSegments.Length; i++)
        {
            layer.layerSegments[i].position = new Vector3(
                i * layer.segmentWidth,  // Multiply by segment width to pack segments tightly
                layer.layerSegments[i].position.y,
                layer.layerSegments[i].position.z
            );
        }
    }


    private void Update()
    {
        foreach (var layer in parallaxLayers)
        {
            // Wave completion speed boost logic
            if (layer.waveCompletionTimer > 0)
            {
                layer.waveCompletionTimer -= Time.deltaTime;
                float t = layer.waveCompletionTimer / layer.waveCompletionDuration;
                layer.currentScrollSpeed = Mathf.Lerp(
                    layer.normalScrollSpeed,
                    layer.waveCompletionScrollSpeed * globalBoostIntensity,
                    1f - t
                );

                if (layer.waveCompletionTimer <= 0)
                {
                    layer.currentScrollSpeed = layer.normalScrollSpeed;
                }
            }

            // Apply scrolling and wrapping
            if (layer.layerSegments != null && layer.layerSegments.Length > 0)
            {
                Vector3 scrollMovement = new Vector3(
                    scrollDirection.x * layer.currentScrollSpeed * Time.deltaTime,
                    scrollDirection.y * layer.currentScrollSpeed * Time.deltaTime,
                    0
                );

                // Move all segments
                foreach (var segment in layer.layerSegments)
                {
                    segment.position -= scrollMovement;
                }

                // Wrap segments
                WrapLayerSegments(layer);
            }
        }
    }

    private void WrapLayerSegments(ParallaxLayer layer)
    {
        if (mainCamera == null) return;

        // Camera view boundaries
        float cameraLeftEdge = mainCamera.transform.position.x - mainCamera.orthographicSize * mainCamera.aspect;
        float cameraRightEdge = mainCamera.transform.position.x + mainCamera.orthographicSize * mainCamera.aspect;

        // Find leftmost and rightmost segments
        Transform leftmostSegment = null;
        Transform rightmostSegment = null;
        float leftmostX = float.MaxValue;
        float rightmostX = float.MinValue;

        foreach (var segment in layer.layerSegments)
        {
            if (segment.position.x < leftmostX)
            {
                leftmostSegment = segment;
                leftmostX = segment.position.x;
            }

            if (segment.position.x > rightmostX)
            {
                rightmostSegment = segment;
                rightmostX = segment.position.x;
            }
        }

        // Wrap left side
        if (leftmostSegment != null && leftmostSegment.position.x + layer.segmentWidth < cameraLeftEdge)
        {
            Vector3 newPosition = rightmostSegment.position + new Vector3(layer.segmentWidth, 0, 0);
            leftmostSegment.position = newPosition;
        }

        // Wrap right side
        if (rightmostSegment != null && rightmostSegment.position.x - layer.segmentWidth > cameraRightEdge)
        {
            Vector3 newPosition = leftmostSegment.position - new Vector3(layer.segmentWidth, 0, 0);
            rightmostSegment.position = newPosition;
        }
    }

    // Existing methods remain the same...
    public void OnWaveCompleted(int waveNumber)
    {
        foreach (var layer in parallaxLayers)
        {
            layer.waveCompletionTimer = layer.waveCompletionDuration;
        }
    }

    // Additional utility methods for dynamic configuration
    public void SetScrollDirection(Vector2 direction)
    {
        scrollDirection = direction.normalized;
    }

    public void SetGlobalBoostIntensity(float intensity)
    {
        globalBoostIntensity = Mathf.Clamp01(intensity);
    }
}