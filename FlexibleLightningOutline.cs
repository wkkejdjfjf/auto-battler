using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class FlexibleLightningOutline : MonoBehaviour
{
    [Header("Banner Shape (Sci-Fi Panel)")]
    [Tooltip("How much to cut off the corners (0 = rectangle, higher = more angled)")]
    public float cornerCutoff = 15f;
    [Tooltip("Slight rounding for the cut corners")]
    public float cornerRounding = 5f;

    [Header("Lightning Settings")]
    public int numberOfLightningBolts = 16;
    public float lightningDistance = 20f;
    public float lightningWidth = 2.5f;
    public Color lightningColor = new Color(0, 1, 1, 1); // Cyan like your image
    public Gradient lightningGradient;

    [Header("Animation")]
    public float pulseSpeed = 3f;
    public float flickerSpeed = 0.08f;
    public float intensityMultiplier = 1.5f;
    public bool randomFlicker = true;

    [Header("Glow Effect")]
    public bool addGlowEffect = true;
    public float glowRadius = 10f;
    public Color glowColor = new Color(0, 0.8f, 1f, 0.3f);

    private RectTransform rectTransform;
    private List<LineRenderer> lightningBolts = new List<LineRenderer>();
    private List<LineRenderer> glowLines = new List<LineRenderer>();
    private Vector3[] bannerOutline;
    private Canvas canvas;
    private bool isActive = false;
    private Coroutine lightningAnimation;
    private Material lightningMaterial;
    private Shader lightningShader;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();

        // Create the custom shader and material
        CreateLightningShader();
        CreateLightningMaterial();

        GenerateSciFiBannerOutline();
        SetupLightningSystem();
        SetupDefaultGradient();

        // Auto-start the lightning effect
        StartLightningEffect();
    }

    void CreateLightningShader()
    {
        // Create a custom additive shader for lightning effect
        string shaderCode = @"
Shader ""Custom/LightningAdditive""
{
    Properties
    {
        _MainTex (""Texture"", 2D) = ""white"" {}
        _Color (""Color"", Color) = (1,1,1,1)
        _Intensity (""Intensity"", Range(0, 5)) = 1
    }
    
    SubShader
    {
        Tags 
        { 
            ""Queue""=""Transparent"" 
            ""RenderType""=""Transparent""
            ""IgnoreProjector""=""True""
        }
        
        Blend SrcAlpha One
        ZWrite Off
        Cull Off
        Lighting Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include ""UnityCG.cginc""
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _Intensity;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);
                fixed4 col = tex * _Color * i.color * _Intensity;
                
                // Add some glow effect based on distance from center
                float2 center = float2(0.5, 0.5);
                float dist = distance(i.uv, center);
                float glow = 1.0 - saturate(dist * 2.0);
                col.rgb *= (1.0 + glow * 0.5);
                
                return col;
            }
            ENDCG
        }
    }
    
    Fallback ""Sprites/Default""
}";

        // Create the shader at runtime
        lightningShader = Shader.Find("Custom/LightningAdditive");

        if (lightningShader == null)
        {
            // If the custom shader doesn't exist, create it
            try
            {
                lightningShader = ShaderUtil.CreateShaderAsset(shaderCode);
                if (lightningShader == null)
                {
                    // Fallback to built-in shader if shader creation fails
                    Debug.LogWarning("Failed to create custom lightning shader, using fallback");
                    CreateFallbackShader();
                }
                else
                {
                    Debug.Log("Custom lightning shader created successfully!");
                }
            }
            catch
            {
                Debug.LogWarning("Shader creation not supported in this Unity version, using fallback");
                CreateFallbackShader();
            }
        }
    }

    void CreateFallbackShader()
    {
        // Try to find the best available built-in shader
        string[] shaderOptions = {
            "Legacy Shaders/Particles/Additive",
            "Sprites/Default",
            "UI/Default",
            "Mobile/Particles/Additive",
            "Unlit/Transparent"
        };

        foreach (string shaderName in shaderOptions)
        {
            lightningShader = Shader.Find(shaderName);
            if (lightningShader != null)
            {
                Debug.Log($"Using fallback shader: {shaderName}");
                break;
            }
        }

        if (lightningShader == null)
        {
            lightningShader = Shader.Find("Sprites/Default");
        }
    }

    void CreateLightningMaterial()
    {
        // Create the material with our custom shader
        lightningMaterial = new Material(lightningShader);
        lightningMaterial.name = "Lightning Material (Runtime)";

        // Create a simple white texture for the shader
        Texture2D lightningTexture = CreateLightningTexture();

        // Configure material properties
        lightningMaterial.SetTexture("_MainTex", lightningTexture);
        lightningMaterial.SetColor("_Color", Color.white);

        if (lightningMaterial.HasProperty("_Intensity"))
        {
            lightningMaterial.SetFloat("_Intensity", 1.5f);
        }

        if (lightningMaterial.HasProperty("_TintColor"))
        {
            lightningMaterial.SetColor("_TintColor", Color.white);
        }
    }

    Texture2D CreateLightningTexture()
    {
        int width = 64;
        int height = 8;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

        Color[] pixels = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;

                // Create a lightning bolt pattern
                float centerY = height * 0.5f;
                float distFromCenter = Mathf.Abs(y - centerY) / (height * 0.5f);

                // Add some jagged edges
                float jag = Mathf.Sin(x * 0.5f) * 0.3f + Mathf.Sin(x * 1.2f) * 0.2f;
                distFromCenter += Mathf.Abs(jag) * 0.3f;

                // Create falloff from center
                float alpha = 1.0f - Mathf.Clamp01(distFromCenter);
                alpha = Mathf.Pow(alpha, 2.0f); // Sharp falloff

                // Add some intensity variation along length
                float lengthIntensity = 1.0f - Mathf.Pow((x / (float)width), 2.0f);
                alpha *= lengthIntensity;

                pixels[index] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        return texture;
    }

    void GenerateSciFiBannerOutline()
    {
        Rect rect = rectTransform.rect;
        List<Vector3> points = new List<Vector3>();

        // Create the sci-fi panel shape with cut corners
        float cutoff = Mathf.Min(cornerCutoff, rect.width * 0.3f, rect.height * 0.3f);

        // Top edge (left to right)
        points.Add(new Vector3(rect.xMin + cutoff, rect.yMax, 0));
        points.Add(new Vector3(rect.xMax - cutoff, rect.yMax, 0));

        // Top-right corner (angled cut)
        if (cornerRounding > 0)
        {
            // Add subtle rounding to the cut
            int roundingSegments = 3;
            for (int i = 1; i <= roundingSegments; i++)
            {
                float t = i / (float)(roundingSegments + 1);
                Vector3 start = new Vector3(rect.xMax - cutoff, rect.yMax, 0);
                Vector3 end = new Vector3(rect.xMax, rect.yMax - cutoff, 0);
                Vector3 roundedPoint = Vector3.Lerp(start, end, t);

                // Apply slight curve
                Vector3 offset = (end - start).normalized * cornerRounding * Mathf.Sin(t * Mathf.PI);
                roundedPoint += new Vector3(-offset.y, offset.x, 0);
                points.Add(roundedPoint);
            }
        }
        points.Add(new Vector3(rect.xMax, rect.yMax - cutoff, 0));

        // Right edge
        points.Add(new Vector3(rect.xMax, rect.yMin + cutoff, 0));

        // Bottom-right corner
        if (cornerRounding > 0)
        {
            int roundingSegments = 3;
            for (int i = 1; i <= roundingSegments; i++)
            {
                float t = i / (float)(roundingSegments + 1);
                Vector3 start = new Vector3(rect.xMax, rect.yMin + cutoff, 0);
                Vector3 end = new Vector3(rect.xMax - cutoff, rect.yMin, 0);
                Vector3 roundedPoint = Vector3.Lerp(start, end, t);

                Vector3 offset = (end - start).normalized * cornerRounding * Mathf.Sin(t * Mathf.PI);
                roundedPoint += new Vector3(-offset.y, offset.x, 0);
                points.Add(roundedPoint);
            }
        }
        points.Add(new Vector3(rect.xMax - cutoff, rect.yMin, 0));

        // Bottom edge
        points.Add(new Vector3(rect.xMin + cutoff, rect.yMin, 0));

        // Bottom-left corner
        if (cornerRounding > 0)
        {
            int roundingSegments = 3;
            for (int i = 1; i <= roundingSegments; i++)
            {
                float t = i / (float)(roundingSegments + 1);
                Vector3 start = new Vector3(rect.xMin + cutoff, rect.yMin, 0);
                Vector3 end = new Vector3(rect.xMin, rect.yMin + cutoff, 0);
                Vector3 roundedPoint = Vector3.Lerp(start, end, t);

                Vector3 offset = (end - start).normalized * cornerRounding * Mathf.Sin(t * Mathf.PI);
                roundedPoint += new Vector3(-offset.y, offset.x, 0);
                points.Add(roundedPoint);
            }
        }
        points.Add(new Vector3(rect.xMin, rect.yMin + cutoff, 0));

        // Left edge
        points.Add(new Vector3(rect.xMin, rect.yMax - cutoff, 0));

        // Top-left corner
        if (cornerRounding > 0)
        {
            int roundingSegments = 3;
            for (int i = 1; i <= roundingSegments; i++)
            {
                float t = i / (float)(roundingSegments + 1);
                Vector3 start = new Vector3(rect.xMin, rect.yMax - cutoff, 0);
                Vector3 end = new Vector3(rect.xMin + cutoff, rect.yMax, 0);
                Vector3 roundedPoint = Vector3.Lerp(start, end, t);

                Vector3 offset = (end - start).normalized * cornerRounding * Mathf.Sin(t * Mathf.PI);
                roundedPoint += new Vector3(-offset.y, offset.x, 0);
                points.Add(roundedPoint);
            }
        }

        bannerOutline = points.ToArray();

        // Convert to world space
        for (int i = 0; i < bannerOutline.Length; i++)
        {
            bannerOutline[i] = rectTransform.TransformPoint(bannerOutline[i]);
        }
    }

    void SetupLightningSystem()
    {
        // Main lightning bolts
        for (int i = 0; i < numberOfLightningBolts; i++)
        {
            LineRenderer lr = CreateLightningBolt($"Lightning_{i}", lightningWidth, lightningColor);
            lightningBolts.Add(lr);
        }

        // Glow effect (thicker, more transparent lines behind main lightning)
        if (addGlowEffect)
        {
            for (int i = 0; i < numberOfLightningBolts; i++)
            {
                LineRenderer glowLr = CreateLightningBolt($"Glow_{i}", lightningWidth * 3f, glowColor);
                glowLr.sortingOrder = 99; // Behind main lightning
                glowLines.Add(glowLr);
            }
        }
    }

    LineRenderer CreateLightningBolt(string name, float width, Color color)
    {
        GameObject lightningObj = new GameObject(name);
        lightningObj.transform.SetParent(transform);

        LineRenderer lr = lightningObj.AddComponent<LineRenderer>();
        lr.material = lightningMaterial; // Use our runtime-created material
        lr.startColor = color;
        lr.endColor = color;
        lr.startWidth = width;
        lr.endWidth = width * 0.3f;
        lr.positionCount = 0;
        lr.useWorldSpace = true;
        lr.sortingLayerName = "UI";
        lr.sortingOrder = 100;

        // Use gradient for better visual effect
        if (lightningGradient != null && lightningGradient.colorKeys.Length > 0)
        {
            lr.colorGradient = lightningGradient;
        }

        return lr;
    }

    void SetupDefaultGradient()
    {
        if (lightningGradient == null)
        {
            lightningGradient = new Gradient();

            // Create a gradient that fades from bright to transparent
            GradientColorKey[] colorKeys = new GradientColorKey[2];
            colorKeys[0] = new GradientColorKey(lightningColor, 0f);
            colorKeys[1] = new GradientColorKey(lightningColor, 1f);

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[3];
            alphaKeys[0] = new GradientAlphaKey(0f, 0f);
            alphaKeys[1] = new GradientAlphaKey(1f, 0.5f);
            alphaKeys[2] = new GradientAlphaKey(0f, 1f);

            lightningGradient.SetKeys(colorKeys, alphaKeys);
        }
    }

    public void StartLightningEffect()
    {
        if (isActive) return;

        isActive = true;
        if (lightningAnimation != null)
            StopCoroutine(lightningAnimation);

        lightningAnimation = StartCoroutine(AnimateLightning());
    }

    public void StopLightningEffect()
    {
        isActive = false;

        if (lightningAnimation != null)
        {
            StopCoroutine(lightningAnimation);
            lightningAnimation = null;
        }

        // Clear all lightning
        foreach (var bolt in lightningBolts)
            bolt.positionCount = 0;

        foreach (var glow in glowLines)
            glow.positionCount = 0;
    }

    IEnumerator AnimateLightning()
    {
        while (isActive)
        {
            GenerateLightningAroundBanner();
            yield return new WaitForSeconds(flickerSpeed);
        }
    }

    void GenerateLightningAroundBanner()
    {
        if (bannerOutline == null || bannerOutline.Length == 0) return;

        float totalPerimeter = CalculatePerimeter();

        for (int i = 0; i < numberOfLightningBolts; i++)
        {
            // Distribute lightning evenly around perimeter
            float position = (i / (float)numberOfLightningBolts) * totalPerimeter;

            // Add some randomness to prevent too uniform look
            if (randomFlicker)
                position += Random.Range(-totalPerimeter * 0.05f, totalPerimeter * 0.05f);

            Vector3 startPoint, normal;
            GetPointOnPerimeter(position, out startPoint, out normal);

            // Generate main lightning bolt
            Vector3[] lightningPath = CreateLightningPath(startPoint, normal);

            // Apply to main lightning
            LineRenderer lr = lightningBolts[i];
            lr.positionCount = lightningPath.Length;
            lr.SetPositions(lightningPath);

            // Apply pulse effect
            float pulse = Mathf.Sin(Time.time * pulseSpeed + i * 0.3f) * 0.5f + 0.5f;
            float intensity = pulse * intensityMultiplier;

            // Random flicker
            if (randomFlicker && Random.value < 0.1f)
                intensity *= Random.Range(0.3f, 1.5f);

            lr.startColor = lightningColor * intensity;
            lr.endColor = lightningColor * intensity;

            // Apply to glow effect
            if (addGlowEffect && i < glowLines.Count)
            {
                LineRenderer glowLr = glowLines[i];
                glowLr.positionCount = lightningPath.Length;
                glowLr.SetPositions(lightningPath);
                glowLr.startColor = glowColor * intensity * 0.7f;
                glowLr.endColor = glowColor * intensity * 0.7f;
            }
        }
    }

    float CalculatePerimeter()
    {
        float perimeter = 0f;
        for (int i = 0; i < bannerOutline.Length; i++)
        {
            int nextIndex = (i + 1) % bannerOutline.Length;
            perimeter += Vector3.Distance(bannerOutline[i], bannerOutline[nextIndex]);
        }
        return perimeter;
    }

    void GetPointOnPerimeter(float distance, out Vector3 point, out Vector3 normal)
    {
        float currentDistance = 0f;

        for (int i = 0; i < bannerOutline.Length; i++)
        {
            int nextIndex = (i + 1) % bannerOutline.Length;
            float segmentLength = Vector3.Distance(bannerOutline[i], bannerOutline[nextIndex]);

            if (currentDistance + segmentLength >= distance)
            {
                float t = (distance - currentDistance) / segmentLength;
                point = Vector3.Lerp(bannerOutline[i], bannerOutline[nextIndex], t);

                // Calculate outward normal
                Vector3 segmentDir = (bannerOutline[nextIndex] - bannerOutline[i]).normalized;
                normal = new Vector3(-segmentDir.y, segmentDir.x, 0);
                return;
            }

            currentDistance += segmentLength;
        }

        // Fallback
        point = bannerOutline[0];
        normal = Vector3.up;
    }

    Vector3[] CreateLightningPath(Vector3 startPoint, Vector3 direction)
    {
        List<Vector3> path = new List<Vector3>();
        path.Add(startPoint);

        Vector3 endPoint = startPoint + direction * lightningDistance;
        int segments = Random.Range(2, 5);

        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector3 basePos = Vector3.Lerp(startPoint, endPoint, t);

            // Add jagged lightning effect
            float jitterAmount = lightningDistance * 0.25f * (1f - t); // Less jitter towards the end
            Vector3 jitter = new Vector3(
                Random.Range(-jitterAmount, jitterAmount),
                Random.Range(-jitterAmount, jitterAmount),
                0
            );

            path.Add(basePos + jitter);
        }

        return path.ToArray();
    }

    // Public methods for integration
    public void OnFusionBannerShow()
    {
        StartLightningEffect();
    }

    public void OnFusionBannerHide()
    {
        StopLightningEffect();
    }

    // Clean up when destroyed
    void OnDestroy()
    {
        if (lightningMaterial != null)
        {
            DestroyImmediate(lightningMaterial);
        }
    }

    // Gizmos for debugging in editor
    void OnDrawGizmosSelected()
    {
        if (bannerOutline != null && bannerOutline.Length > 0)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < bannerOutline.Length; i++)
            {
                int nextIndex = (i + 1) % bannerOutline.Length;
                Gizmos.DrawLine(bannerOutline[i], bannerOutline[nextIndex]);
            }
        }
    }
}

// Helper class for shader creation (Editor only)
#if UNITY_EDITOR
public static class ShaderUtil
{
    public static Shader CreateShaderAsset(string shaderCode)
    {
        // This is a placeholder - Unity doesn't support runtime shader compilation
        // in builds, only in editor. The fallback system will handle this.
        return null;
    }
}
#endif