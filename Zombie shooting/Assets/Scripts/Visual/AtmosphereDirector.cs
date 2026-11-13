using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Horror atmosphere for the game scene: filmic URP post-processing, fog, a darker sky,
/// warm glowing lamps and a red "danger" vignette that pulses when the player is hurt.
/// Built at runtime so it works regardless of the scene's (old, Built-in) post-process setup.
///
/// Art bible: "fog-drowned harbour at dusk" — cold teal-grey fog, desaturated filmic grade,
/// warm lamp highlights, blood-red danger accents, bone-white UI text.
/// </summary>
public class AtmosphereDirector : MonoBehaviour
{
    public static class Palette
    {
        public static readonly Color Fog = new Color(0.27f, 0.31f, 0.33f);
        public static readonly Color SkyTint = new Color(0.36f, 0.40f, 0.43f);
        public static readonly Color Ground = new Color(0.18f, 0.19f, 0.19f);
        public static readonly Color SunLight = new Color(0.80f, 0.86f, 0.95f);
        public static readonly Color LampGlow = new Color(1.00f, 0.62f, 0.28f);
        public static readonly Color Blood = new Color(0.78f, 0.06f, 0.06f);
        public static readonly Color Bone = new Color(0.93f, 0.91f, 0.86f);
        public static readonly Color Toxic = new Color(0.55f, 0.85f, 0.35f);
    }

    [Header("Fog")]
    public float fogDensity = 0.028f;

    [Header("Grade")]
    public float postExposure = 0.4f;
    public float contrast = 18f;
    public float saturation = -28f;

    private Volume volume;
    private Vignette vignette;
    private ColorAdjustments colorAdjust;
    private ChromaticAberration chroma;
    private float baseVignette = 0.32f;
    private float hurtPulse;
    private PlayerController player;

    private void Start()
    {
        player = FindAnyObjectByType<PlayerController>();
        if (player != null) player.Damaged += (_, __) => hurtPulse = 1f;

        SetupCamera();
        SetupVolume();
        SetupFogAndSky();
        SetupLights();
        DisableBuiltInPostProcessing();
    }

    private void SetupCamera()
    {
        Camera cam = player != null ? player.playerCamera : null;
        if (cam == null) cam = Camera.main;
        if (cam == null) cam = FindAnyObjectByType<Camera>();
        if (cam == null) return;

        var data = cam.GetUniversalAdditionalCameraData();
        data.renderPostProcessing = true;
        data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        data.antialiasingQuality = AntialiasingQuality.High;
        cam.nearClipPlane = 0.03f;           // view-model gun doesn't clip
        cam.farClipPlane = Mathf.Max(cam.farClipPlane, 250f);

        // Soft warm fill light that rides with the camera so the gun (and close zombies) read in the dark.
        var fill = new GameObject("Viewmodel Fill Light").AddComponent<Light>();
        fill.transform.SetParent(cam.transform, false);
        fill.transform.localPosition = new Vector3(0.25f, 0.35f, 0.1f);
        fill.type = LightType.Point;
        fill.range = 3.5f;
        fill.intensity = 1.6f;
        fill.color = new Color(1f, 0.85f, 0.7f);
        fill.shadows = LightShadows.None;
    }

    private void SetupVolume()
    {
        var go = new GameObject("Horror Post FX");
        go.transform.SetParent(transform, false);
        volume = go.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 100f;
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        volume.profile = profile;

        var tone = profile.Add<Tonemapping>(true);
        tone.mode.Override(TonemappingMode.ACES);

        colorAdjust = profile.Add<ColorAdjustments>(true);
        colorAdjust.postExposure.Override(postExposure);
        colorAdjust.contrast.Override(contrast);
        colorAdjust.saturation.Override(saturation);
        colorAdjust.colorFilter.Override(new Color(0.94f, 0.98f, 1.0f));

        var wb = profile.Add<WhiteBalance>(true);
        wb.temperature.Override(-12f);
        wb.tint.Override(4f);

        var smh = profile.Add<ShadowsMidtonesHighlights>(true);
        smh.shadows.Override(new Vector4(0.86f, 1.0f, 1.02f, -0.05f));     // teal shadows
        smh.highlights.Override(new Vector4(1.04f, 1.0f, 0.92f, 0.0f));    // warm highlights

        var bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(1.1f);
        bloom.intensity.Override(0.7f);
        bloom.scatter.Override(0.72f);
        bloom.tint.Override(new Color(1f, 0.85f, 0.7f));

        vignette = profile.Add<Vignette>(true);
        vignette.intensity.Override(baseVignette);
        vignette.smoothness.Override(0.45f);
        vignette.color.Override(Color.black);

        var grain = profile.Add<FilmGrain>(true);
        grain.type.Override(FilmGrainLookup.Medium3);
        grain.intensity.Override(0.32f);
        grain.response.Override(0.75f);

        chroma = profile.Add<ChromaticAberration>(true);
        chroma.intensity.Override(0.06f);
    }

    private void SetupFogAndSky()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = fogDensity;
        RenderSettings.fogColor = Palette.Fog;

        var skyShader = Shader.Find("Skybox/Procedural");
        if (skyShader != null)
        {
            var sky = new Material(skyShader);
            sky.SetFloat("_SunSize", 0.02f);
            sky.SetFloat("_AtmosphereThickness", 1.9f);
            sky.SetColor("_SkyTint", Palette.SkyTint);
            sky.SetColor("_GroundColor", Palette.Ground);
            sky.SetFloat("_Exposure", 0.42f);
            sky.SetFloat("_SunSizeConvergence", 10f);
            RenderSettings.skybox = sky;
        }
        RenderSettings.ambientIntensity = 0.85f;
    }

    private void SetupLights()
    {
        foreach (var l in FindObjectsByType<Light>(FindObjectsInactive.Exclude))
        {
            if (l.type == LightType.Directional)
            {
                l.color = Palette.SunLight;
                l.intensity = Mathf.Min(l.intensity, 0.8f);
                l.shadows = LightShadows.Soft;
                l.shadowStrength = 0.85f;
            }
            else if (l.type == LightType.Point)
            {
                l.color = Palette.LampGlow;      // warm lantern pools in the fog
                l.intensity = Mathf.Max(l.intensity, 1.6f);
                l.range = Mathf.Max(l.range, 7f);
            }
        }

        // Make lantern glass actually glow so bloom picks it up.
        foreach (var r in FindObjectsByType<Renderer>(FindObjectsInactive.Exclude))
        {
            var m = r.sharedMaterial;
            if (m == null || !m.name.StartsWith("Lamp_Glow")) continue;
            var inst = r.material; // instance, don't touch the asset
            inst.EnableKeyword("_EMISSION");
            inst.SetColor("_EmissionColor", Palette.LampGlow * 0.9f); // glows in the fog without blinding up close
            inst.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
    }

    // The scene still has Post Processing Stack v2 components, which do nothing in URP.
    private void DisableBuiltInPostProcessing()
    {
        foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude))
        {
            if (mb == null) continue;
            string n = mb.GetType().FullName;
            if (n == "UnityEngine.Rendering.PostProcessing.PostProcessVolume" || n == "UnityEngine.Rendering.PostProcessing.PostProcessLayer")
                mb.enabled = false;
        }
    }

    private void Update()
    {
        if (vignette == null || player == null) return;

        // Danger vignette: red and pulsing as health drops, plus a flash when hit.
        float danger = Mathf.Clamp01(1f - player.HealthPercent / 0.6f);
        hurtPulse = Mathf.MoveTowards(hurtPulse, 0f, Time.unscaledDeltaTime * 2.5f);
        float pulse = danger > 0f ? (Mathf.Sin(Time.unscaledTime * (4f + danger * 4f)) * 0.5f + 0.5f) * 0.08f * danger : 0f;

        vignette.intensity.value = baseVignette + danger * 0.22f + pulse + hurtPulse * 0.15f;
        vignette.color.value = Color.Lerp(Color.black, Palette.Blood, Mathf.Max(danger * 0.85f, hurtPulse));
        chroma.intensity.value = 0.06f + hurtPulse * 0.5f + danger * 0.15f;

        // On death the world drains to grey.
        if (player.IsDead) deathFade = Mathf.MoveTowards(deathFade, 1f, Time.unscaledDeltaTime * 0.6f);
        colorAdjust.saturation.value = Mathf.Lerp(saturation - danger * 35f, -100f, deathFade);
    }

    private float deathFade;
}
