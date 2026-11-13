using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The game's HUD, built at runtime:
///   top-left  kills / score           top-centre  wave + zombies left
///   centre    dynamic crosshair, hit markers, score popups, wave banner, hints
///   bottom-left  health bar            bottom-right  ammo + reload bar
/// The scene's original HUD objects are kept (scripts still write to them) but hidden,
/// and the pause / death / mission-complete screens are restyled to match.
/// </summary>
public class GameHUD : MonoBehaviour
{
    private static readonly Color Bone = AtmosphereDirector.Palette.Bone;
    private static readonly Color Blood = AtmosphereDirector.Palette.Blood;
    private static readonly Color Toxic = AtmosphereDirector.Palette.Toxic;
    private static readonly Color Amber = new Color(0.95f, 0.65f, 0.15f);
    private static readonly Color Panel = new Color(0.03f, 0.04f, 0.04f, 0.62f);
    private static readonly Color Dim = new Color(0.62f, 0.66f, 0.64f, 1f);

    private PlayerController player;
    private Shoot gun;
    private ZombieWaveSpawner spawner;
    private WaveUIManager waveUI;

    private RectTransform root;
    private CanvasGroup rootGroup;

    // crosshair + hit marker
    private readonly RectTransform[] crossBars = new RectTransform[4];
    private readonly Image[] crossImages = new Image[4];
    private Image crossDot;
    private RectTransform hitMarker;
    private readonly Image[] hitImages = new Image[4];
    private float hitTimer, hitScale = 1f, spreadKick;
    private bool hitWasKill;

    // panels
    private TextMeshProUGUI killsText, scoreText, waveText, zombiesText, bannerText, hintText;
    private TextMeshProUGUI hpText, ammoMagText, ammoReserveText, weaponText;
    private Image hpFill, hpGhost, reloadFill, weaponIcon;
    private RectTransform reloadBar;
    private float ghostHealth = 1f;

    private readonly List<Popup> popups = new List<Popup>();
    private readonly List<DamageIndicator> damageIndicators = new List<DamageIndicator>();

    private class Popup { public TextMeshProUGUI text; public float t; public Vector2 start; }
    private class DamageIndicator { public RectTransform rt; public Image img; public Vector3 source; public float t; }

    private void Start()
    {
        player = FindAnyObjectByType<PlayerController>();
        gun = FindAnyObjectByType<Shoot>();
        spawner = FindAnyObjectByType<ZombieWaveSpawner>();
        waveUI = FindAnyObjectByType<WaveUIManager>();

        BuildCanvas();
        BuildCrosshair();
        BuildHitMarker();
        BuildTopLeft();
        BuildTopCentre();
        BuildCentreTexts();
        BuildHealth();
        BuildAmmo();

        try { HideOldHud(); } catch (System.Exception e) { Debug.LogWarning("GameHUD: hiding old HUD failed: " + e.Message); }
        try { RestyleMenus(); } catch (System.Exception e) { Debug.LogWarning("GameHUD: menu restyle failed: " + e.Message); }

        GameState.OnHit += HandleHit;
        GameState.OnKill += HandleKill;
        if (gun != null) gun.Fired += () => spreadKick = Mathf.Min(spreadKick + 9f, 24f);
        if (player != null) player.Damaged += (dmg, src) => SpawnDamageIndicator(src);
    }

    private void OnDestroy()
    {
        GameState.OnHit -= HandleHit;
        GameState.OnKill -= HandleKill;
    }

    // ================================================================ building blocks
    private void BuildCanvas()
    {
        var go = new GameObject("Game HUD", typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
        go.transform.SetParent(transform, false);
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10; // under the pause / death / mission screens (they're re-sorted above in RestyleMenus)
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        rootGroup = go.GetComponent<CanvasGroup>();
        rootGroup.blocksRaycasts = false;
        rootGroup.interactable = false;
        root = (RectTransform)go.transform;
    }

    private RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    private Image Img(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, Color c)
    {
        var img = Rect(name, parent, anchor, pivot, pos, size).gameObject.AddComponent<Image>();
        img.color = c;
        img.raycastTarget = false;
        return img;
    }

    private TextMeshProUGUI Txt(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size,
                                float fontSize, TextAlignmentOptions align, Color color, bool bold = true, float spacing = 3f)
    {
        var t = Rect(name, parent, anchor, pivot, pos, size).gameObject.AddComponent<TextMeshProUGUI>();
        var skin = UISkin.Get();
        if (skin != null && skin.bodyFont != null) t.font = skin.bodyFont; // same font as the menus
        t.fontSize = fontSize;
        t.alignment = align;
        t.color = color;
        t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        t.characterSpacing = spacing;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Overflow;
        if (t.font != null && t.fontSharedMaterial != null)
        {
            t.outlineWidth = 0.15f;
            t.outlineColor = new Color32(0, 0, 0, 190);
        }
        return t;
    }

    /// <summary>Dark translucent panel with a thin accent line along one edge.</summary>
    private RectTransform PanelBox(string name, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, Color accent, bool accentTop = false)
    {
        var bg = Img(name, root, anchor, pivot, pos, size, Panel);
        var line = Img("Accent", bg.transform, accentTop ? new Vector2(0.5f, 1f) : new Vector2(0f, 0.5f),
                       accentTop ? new Vector2(0.5f, 1f) : new Vector2(0f, 0.5f), Vector2.zero,
                       accentTop ? new Vector2(size.x, 3f) : new Vector2(4f, size.y), accent);
        return bg.rectTransform;
    }

    // ================================================================ sections
    private void BuildCrosshair()
    {
        var c = new Vector2(0.5f, 0.5f);
        for (int i = 0; i < 4; i++)
        {
            bool vertical = i % 2 == 0;
            var img = Img("Cross" + i, root, c, c, Vector2.zero, vertical ? new Vector2(3, 13) : new Vector2(13, 3), Bone);
            var sh = img.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0, 0, 0, 0.85f);
            sh.effectDistance = new Vector2(1, -1);
            crossImages[i] = img;
            crossBars[i] = img.rectTransform;
        }
        crossDot = Img("CrossDot", root, c, c, Vector2.zero, new Vector2(3, 3), Bone);
    }

    private void BuildHitMarker()
    {
        var c = new Vector2(0.5f, 0.5f);
        hitMarker = Rect("HitMarker", root, c, c, Vector2.zero, new Vector2(60, 60));
        for (int i = 0; i < 4; i++)
        {
            float ang = 45f + 90f * i;
            Vector2 dir = Quaternion.Euler(0, 0, ang) * Vector2.up;
            var img = Img("Hit" + i, hitMarker, c, c, dir * 15f, new Vector2(3, 12), Bone);
            img.rectTransform.localRotation = Quaternion.Euler(0, 0, ang);
            hitImages[i] = img;
        }
        hitMarker.gameObject.SetActive(false);
    }

    private void BuildTopLeft()
    {
        var box = PanelBox("Score", new Vector2(0, 1), new Vector2(0, 1), new Vector2(28, -24), new Vector2(300, 74), Bone);
        var kl = Txt("KillsLabel", box, new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -8), new Vector2(120, 22), 16, TextAlignmentOptions.Left, Dim, true, 6);
        kl.text = "KILLS";
        killsText = Txt("Kills", box, new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -28), new Vector2(120, 40), 34, TextAlignmentOptions.Left, Bone);
        var sl = Txt("ScoreLabel", box, new Vector2(0, 1), new Vector2(0, 1), new Vector2(140, -8), new Vector2(150, 22), 16, TextAlignmentOptions.Left, Dim, true, 6);
        sl.text = "SCORE";
        scoreText = Txt("ScoreValue", box, new Vector2(0, 1), new Vector2(0, 1), new Vector2(140, -28), new Vector2(150, 40), 34, TextAlignmentOptions.Left, Bone);
    }

    private void BuildTopCentre()
    {
        var box = PanelBox("Wave", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -24), new Vector2(320, 74), Blood, true);
        waveText = Txt("WaveText", box, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(300, 38), 32, TextAlignmentOptions.Center, Bone, true, 8);
        zombiesText = Txt("ZombiesLeft", box, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(300, 24), 18, TextAlignmentOptions.Center, Toxic, true, 4);
    }

    private void BuildCentreTexts()
    {
        var c = new Vector2(0.5f, 0.5f);
        bannerText = Txt("Banner", root, c, c, new Vector2(0, 230), new Vector2(1400, 80), 54, TextAlignmentOptions.Center, Bone, true, 14);
        hintText = Txt("Hint", root, c, c, new Vector2(0, -110), new Vector2(800, 36), 22, TextAlignmentOptions.Center, Amber, true, 6);
        reloadBar = Rect("ReloadBar", root, c, c, new Vector2(0, -70), new Vector2(150, 5));
        var bg = reloadBar.gameObject.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.6f);
        bg.raycastTarget = false;
        reloadFill = Img("Fill", reloadBar, new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(0, 5), Bone);
        reloadBar.gameObject.SetActive(false);
    }

    private void BuildHealth()
    {
        var box = PanelBox("Health", new Vector2(0, 0), new Vector2(0, 0), new Vector2(28, 28), new Vector2(430, 84), Blood);
        var label = Txt("HPLabel", box, new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -10), new Vector2(120, 22), 16, TextAlignmentOptions.Left, Dim, true, 6);
        label.text = "HEALTH";
        hpText = Txt("HPValue", box, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-16, -4), new Vector2(160, 36), 30, TextAlignmentOptions.Right, Bone);
        var barBg = Img("BarBg", box, new Vector2(0, 0), new Vector2(0, 0), new Vector2(20, 16), new Vector2(394, 16), new Color(0, 0, 0, 0.55f));
        hpGhost = Img("Ghost", barBg.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(394, 16), new Color(1f, 1f, 1f, 0.35f));
        hpFill = Img("Fill", barBg.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(394, 16), Toxic);
    }

    private void BuildAmmo()
    {
        var box = PanelBox("Ammo", new Vector2(1, 0), new Vector2(1, 0), new Vector2(-28, 28), new Vector2(330, 84), Bone);
        weaponText = Txt("Weapon", box, new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -10), new Vector2(160, 22), 16, TextAlignmentOptions.Left, Dim, true, 6);
        weaponText.text = "PISTOL";
        ammoMagText = Txt("Mag", box, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-110, 6), new Vector2(150, 64), 56, TextAlignmentOptions.Right, Bone, true, 0);
        ammoReserveText = Txt("Reserve", box, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-16, 14), new Vector2(92, 36), 26, TextAlignmentOptions.Left, Dim, true, 0);
        weaponIcon = Img("Icon", box, new Vector2(0, 0), new Vector2(0, 0), new Vector2(20, 12), new Vector2(80, 44), new Color(Bone.r, Bone.g, Bone.b, 0.85f));
        weaponIcon.preserveAspect = true;
        weaponIcon.enabled = false; // turned on once we've borrowed the scene's pistol sprite
    }

    // ================================================================ old HUD
    private void HideOldHud()
    {
        // Borrow the pistol silhouette from the old ammo panel before hiding it.
        foreach (var img in FindObjectsByType<Image>(FindObjectsInactive.Include))
            if (!img.transform.IsChildOf(transform) && img.sprite != null && img.sprite.name.ToLower().Contains("pistol"))
            { weaponIcon.sprite = img.sprite; weaponIcon.enabled = true; break; }

        var hide = new List<GameObject>();
        if (player != null && player.healthSlider != null) hide.Add(player.healthSlider.gameObject);
        if (gun != null)
        {
            var ammo = typeof(Shoot).GetField("ammoText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(gun) as TMP_Text;
            if (ammo != null) hide.Add(ammo.transform.parent != null && ammo.transform.parent.GetComponent<Canvas>() == null ? ammo.transform.parent.gameObject : ammo.gameObject);
        }
        if (waveUI != null)
        {
            if (waveUI.waveText != null) hide.Add(waveUI.waveText.gameObject);
            if (waveUI.statusText != null) hide.Add(waveUI.statusText.gameObject);
        }
        foreach (var img in FindObjectsByType<Image>(FindObjectsInactive.Include))
            if (!img.transform.IsChildOf(transform) && img.sprite != null && img.sprite.name.ToLower().Contains("cross"))
                hide.Add(img.gameObject);

        // Hide visually but keep them active so the old scripts keep updating them.
        foreach (var go in hide)
        {
            if (go == null) continue;
            // Note: no "??" here — Unity's fake-null from GetComponent isn't C# null in the Editor.
            var cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.blocksRaycasts = false;
            cg.interactable = false;
        }
    }

    // Pause / death / mission-complete: dark panels, bone text, blood-red buttons.
    private void RestyleMenus()
    {
        var screens = new List<GameObject>();
        var pause = FindAnyObjectByType<PauseManager>();
        if (pause != null && pause.pauseMenu != null) screens.Add(pause.pauseMenu);
        var mission = FindAnyObjectByType<MissionCompleteUI>();
        if (mission != null && mission.missionCompletePanel != null) screens.Add(mission.missionCompletePanel);
        var death = FindAnyObjectByType<DeathScreen>(FindObjectsInactive.Include);

        // Pause + mission complete: these panels are only active while shown, so they can be restyled fully.
        foreach (var s in screens)
        {
            var bg = s.GetComponent<Image>();
            if (bg != null) bg.color = new Color(0.02f, 0.02f, 0.03f, 0.82f);
            StyleButtons(s);
            foreach (var t in s.GetComponentsInChildren<TMP_Text>(true))
                if (t.GetComponentInParent<Button>() == null) { t.color = Bone; t.characterSpacing = 10f; }
            foreach (var img in s.GetComponentsInChildren<Image>(true))
                if (img.gameObject != s && img.GetComponentInParent<Button>() == null)
                    img.color = new Color(0.03f, 0.03f, 0.04f, Mathf.Max(img.color.a, 0.85f));
        }

        // Death screen is always active and fades its own overlay in, so only change colours, never alpha.
        if (death != null)
        {
            StyleButtons(death.gameObject);
            if (death.targetImage != null) { var c = death.targetImage.color; death.targetImage.color = new Color(0.03f, 0.0f, 0.0f, c.a); }
            if (death.targetText != null) { var c = death.targetText.color; death.targetText.color = new Color(Blood.r, Blood.g, Blood.b, c.a); }
            screens.Add(death.gameObject);
        }

        // Keep the menus above the HUD.
        foreach (var s in screens)
        {
            var c = s.GetComponentInParent<Canvas>(true);
            if (c != null && c.rootCanvas.sortingOrder <= 10) c.rootCanvas.sortingOrder = 20;
        }
    }

    private void StyleButtons(GameObject screen)
    {
            foreach (var b in screen.GetComponentsInChildren<Button>(true))
            {
                var bi = b.GetComponent<Image>();
                if (bi != null) bi.color = Color.white; // tint comes from the ColorBlock below
                var cb = b.colors;
                cb.normalColor = new Color(0.10f, 0.10f, 0.11f, 0.95f);
                cb.highlightedColor = new Color(Blood.r, Blood.g, Blood.b, 1f);
                cb.selectedColor = cb.highlightedColor;
                cb.pressedColor = new Color(0.45f, 0.03f, 0.03f, 1f);
                cb.colorMultiplier = 1f;
                b.colors = cb;
                var rt = (RectTransform)b.transform;
                rt.sizeDelta = new Vector2(Mathf.Max(rt.sizeDelta.x, 280f), Mathf.Max(rt.sizeDelta.y, 56f));
                foreach (var t in b.GetComponentsInChildren<TMP_Text>(true)) { t.color = Bone; t.fontStyle = FontStyles.Bold; t.characterSpacing = 6f; t.text = t.text.Trim().ToUpperInvariant(); t.fontSize = Mathf.Max(t.fontSize, 26f); }
                foreach (var t in b.GetComponentsInChildren<Text>(true)) { t.color = Bone; t.fontStyle = FontStyle.Bold; t.text = t.text.Trim().ToUpperInvariant(); }
            }
    }

    // ================================================================ events
    private void HandleHit(bool kill)
    {
        hitTimer = kill ? 0.3f : 0.15f;
        hitWasKill = kill;
        hitScale = kill ? 1.6f : 1.25f;
    }

    private void HandleKill(int points, bool headshot)
    {
        var c = new Vector2(0.5f, 0.5f);
        var t = Txt("Popup", root, c, new Vector2(0, 0.5f), new Vector2(40, 40), new Vector2(400, 40), headshot ? 28 : 22, TextAlignmentOptions.Left, headshot ? Blood : Bone);
        t.text = headshot ? $"HEADSHOT  +{points}" : $"+{points}";
        popups.Add(new Popup { text = t, t = 0f, start = t.rectTransform.anchoredPosition });
    }

    private void SpawnDamageIndicator(Vector3 source)
    {
        var c = new Vector2(0.5f, 0.5f);
        var img = Img("DamageDir", root, c, new Vector2(0.5f, -9f), Vector2.zero, new Vector2(140, 14), Blood);
        damageIndicators.Add(new DamageIndicator { rt = img.rectTransform, img = img, source = source, t = 1.2f });
    }

    // ================================================================ update
    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        rootGroup.alpha = Mathf.MoveTowards(rootGroup.alpha, GameState.IsGameOver || GameState.IsPaused ? 0f : 1f, dt * 6f);

        UpdateCrosshair(dt);
        UpdatePanels(dt);
        UpdatePopups(dt);
    }

    private void UpdateCrosshair(float dt)
    {
        float move = player != null ? player.MoveAmount : 0f;
        spreadKick = Mathf.MoveTowards(spreadKick, 0f, dt * 60f);
        float spread = 9f + move * 10f + spreadKick;
        crossBars[0].anchoredPosition = new Vector2(0, spread + 6);
        crossBars[2].anchoredPosition = new Vector2(0, -spread - 6);
        crossBars[1].anchoredPosition = new Vector2(spread + 6, 0);
        crossBars[3].anchoredPosition = new Vector2(-spread - 6, 0);
        bool reloading = gun != null && gun.IsReloading;
        var cc = new Color(Bone.r, Bone.g, Bone.b, reloading ? 0.3f : 0.95f);
        foreach (var img in crossImages) img.color = cc;
        crossDot.color = cc;

        if (hitTimer > 0f)
        {
            hitTimer -= dt;
            hitMarker.gameObject.SetActive(true);
            hitScale = Mathf.Lerp(hitScale, 1f, dt * 18f);
            hitMarker.localScale = Vector3.one * hitScale;
            foreach (var img in hitImages) img.color = hitWasKill ? Blood : Bone;
        }
        else hitMarker.gameObject.SetActive(false);
    }

    private void UpdatePanels(float dt)
    {
        killsText.text = GameState.Kills.ToString();
        scoreText.text = GameState.Score.ToString("N0");

        if (spawner != null)
        {
            waveText.text = spawner.CurrentWave > 0 && spawner.TotalWaves > 0 ? $"WAVE {Mathf.Min(spawner.CurrentWave, spawner.TotalWaves)} / {spawner.TotalWaves}" : "GET READY";
            zombiesText.text = spawner.ZombiesRemainingInWave > 0 ? $"{spawner.ZombiesRemainingInWave} ZOMBIES LEFT" : "AREA CLEAR";
        }

        // Mirror the old wave status text ("WAVE 2", "WAVE 1 CLEARED ...") as a big centre banner.
        if (waveUI != null && waveUI.statusText != null)
        {
            bannerText.text = waveUI.statusText.text;
            bannerText.alpha = waveUI.statusText.alpha;
        }

        // Health
        if (player != null)
        {
            float h = Mathf.Clamp01(player.HealthPercent);
            ghostHealth = Mathf.MoveTowards(ghostHealth, h, dt * 0.5f);
            if (ghostHealth < h) ghostHealth = h;
            hpFill.rectTransform.sizeDelta = new Vector2(394f * h, 16f);
            hpGhost.rectTransform.sizeDelta = new Vector2(394f * ghostHealth, 16f);
            hpFill.color = h > 0.5f ? Color.Lerp(Amber, Toxic, (h - 0.5f) * 2f) : Color.Lerp(Blood, Amber, h * 2f);
            hpText.text = Mathf.CeilToInt(player.currentHealth).ToString();
            hpText.color = h < 0.3f ? Color.Lerp(Bone, Blood, Mathf.PingPong(Time.unscaledTime * 3f, 1f)) : Bone;
        }

        // Ammo + reload
        if (gun != null)
        {
            bool reloading = gun.IsReloading;
            int low = Mathf.CeilToInt(gun.MagazineSize * 0.25f);
            ammoMagText.text = gun.CurrentAmmo.ToString();
            ammoMagText.color = gun.CurrentAmmo == 0 ? Blood : gun.CurrentAmmo <= low ? Amber : Bone;
            ammoReserveText.text = "/ " + gun.ReserveAmmo;
            weaponText.text = reloading ? "RELOADING" : "PISTOL";
            weaponText.color = reloading ? Amber : Dim;

            reloadBar.gameObject.SetActive(reloading);
            if (reloading) reloadFill.rectTransform.sizeDelta = new Vector2(150f * gun.ReloadProgress, 5f);

            if (!reloading && gun.CurrentAmmo == 0) hintText.text = gun.ReserveAmmo > 0 ? "PRESS  R  TO RELOAD" : "OUT OF AMMO — SURVIVE THE WAVE";
            else if (!reloading && gun.CurrentAmmo <= low && gun.ReserveAmmo > 0) hintText.text = "LOW AMMO";
            else hintText.text = "";
            hintText.color = gun.CurrentAmmo == 0 ? Blood : Amber;
        }
    }

    private void UpdatePopups(float dt)
    {
        for (int i = popups.Count - 1; i >= 0; i--)
        {
            var p = popups[i];
            p.t += dt;
            p.text.rectTransform.anchoredPosition = p.start + new Vector2(0, p.t * 60f);
            p.text.alpha = Mathf.Clamp01(1.4f - p.t);
            if (p.t > 1.4f) { Destroy(p.text.gameObject); popups.RemoveAt(i); }
        }

        Transform cam = player != null && player.playerCamera != null ? player.playerCamera.transform : Camera.main != null ? Camera.main.transform : null;
        for (int i = damageIndicators.Count - 1; i >= 0; i--)
        {
            var d = damageIndicators[i];
            d.t -= dt;
            if (cam != null)
            {
                Vector3 to = d.source - cam.position; to.y = 0f;
                Vector3 fwd = cam.forward; fwd.y = 0f;
                d.rt.localRotation = Quaternion.Euler(0, 0, -Vector3.SignedAngle(fwd, to, Vector3.up));
            }
            d.img.color = new Color(Blood.r, Blood.g, Blood.b, Mathf.Clamp01(d.t) * 0.85f);
            if (d.t <= 0f) { Destroy(d.rt.gameObject); damageIndicators.RemoveAt(i); }
        }
    }
}
