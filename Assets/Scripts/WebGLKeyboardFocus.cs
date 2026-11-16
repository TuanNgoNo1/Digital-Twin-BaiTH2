using UnityEngine;

// Let the focused browser element own the keyboard when Unity shares a page
// with Guacamole or other HTML inputs. Applies to every entry scene.
internal static class WebGLKeyboardFocus
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLInput.captureAllKeyboardInput = false;
#endif
    }
}
