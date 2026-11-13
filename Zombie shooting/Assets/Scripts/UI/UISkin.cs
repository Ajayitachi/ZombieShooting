using TMPro;
using UnityEngine;

/// <summary>
/// Art used by every menu/HUD element (fonts, button frames, sounds, menu backdrop).
/// Lives at Assets/Resources/UISkin.asset so runtime-built UI can load it.
/// Built from the SlimUI "Modern Menu" pack that's already in the project.
/// </summary>
[CreateAssetMenu(menuName = "Zombie Shooting/UI Skin", fileName = "UISkin")]
public class UISkin : ScriptableObject
{
    [Header("Fonts")]
    public TMP_FontAsset titleFont;
    public TMP_FontAsset bodyFont;

    [Header("Sprites")]
    public Sprite buttonFrame;
    public Sprite buttonFrameHover;
    public Sprite panelFrame;
    public Sprite divider;
    public Sprite cornerDetail;

    [Header("Backdrop")]
    public Texture2D menuBackground;

    [Header("Audio")]
    public AudioClip hoverSound;
    public AudioClip clickSound;
    public AudioClip menuMusic;

    private static UISkin cached;
    public static UISkin Get()
    {
        if (cached == null) cached = Resources.Load<UISkin>("UISkin");
        return cached;
    }
}
