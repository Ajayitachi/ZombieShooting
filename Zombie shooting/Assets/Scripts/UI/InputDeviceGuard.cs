using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Keeps the real mouse working for the UI.
/// When a Device Simulator window exists in the Editor, the Input System disables the Mouse device
/// (it feeds a simulated touchscreen instead). If you then play in the normal Game view, the UI's
/// Input System module never receives clicks — buttons look dead. This re-enables the mouse whenever
/// the Simulator isn't the view you're playing in. In builds there is no simulator, so it does nothing.
/// </summary>
public class InputDeviceGuard : MonoBehaviour
{
    private static InputDeviceGuard instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (instance != null) return;
        var go = new GameObject("Input Device Guard");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<InputDeviceGuard>();
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM && UNITY_EDITOR
        var mouse = Mouse.current;
        if (mouse == null || mouse.enabled) return;
        if (SimulatorIsActiveView()) return;   // playing inside the Simulator: touch input is correct there
        InputSystem.EnableDevice(mouse);
#endif
    }

#if UNITY_EDITOR
    // The Simulator is "active" only when it's the focused/visible play view.
    private static bool SimulatorIsActiveView()
    {
        var focused = UnityEditor.EditorWindow.focusedWindow;
        if (focused != null) return focused.GetType().Name == "SimulatorWindow";
        foreach (var w in Resources.FindObjectsOfTypeAll<UnityEditor.EditorWindow>())
            if (w.GetType().Name == "SimulatorWindow" && w.hasFocus) return true;
        return false;
    }
#endif
}
