namespace Mivi.Core
{
    public class KeyboardEvents
    {
        // Temporary API until I nail down keyboard input better.
        // Based on the GLFW key codes
        public void PushKeyChange(int keyCode, bool pressed)
            => OnKeyChange?.Invoke(keyCode, pressed);

        public event KeyboardEvent? OnKeyChange;

        public delegate void KeyboardEvent(int keyCode, bool pressed);
    }
}
