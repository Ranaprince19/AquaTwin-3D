using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace LeakWatch
{
    public enum LWKey { Space, B, A, L, R, F9 }

    public static class InputHelper
    {
        public static bool KeyDown(LWKey key)
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return false;
            switch (key)
            {
                case LWKey.Space: return kb.spaceKey.wasPressedThisFrame;
                case LWKey.B: return kb.bKey.wasPressedThisFrame;
                case LWKey.A: return kb.aKey.wasPressedThisFrame;
                case LWKey.L: return kb.lKey.wasPressedThisFrame;
                case LWKey.R: return kb.rKey.wasPressedThisFrame;
                case LWKey.F9: return kb.f9Key.wasPressedThisFrame;
            }
            return false;
#else
            switch (key)
            {
                case LWKey.Space: return Input.GetKeyDown(KeyCode.Space);
                case LWKey.B: return Input.GetKeyDown(KeyCode.B);
                case LWKey.A: return Input.GetKeyDown(KeyCode.A);
                case LWKey.L: return Input.GetKeyDown(KeyCode.L);
                case LWKey.R: return Input.GetKeyDown(KeyCode.R);
                case LWKey.F9: return Input.GetKeyDown(KeyCode.F9);
            }
            return false;
#endif
        }
    }
}
