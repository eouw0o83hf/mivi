namespace Mivi.Core
{
    public class KeyboardEvents
    {
        // Temporary API until I nail down keyboard input better.
        // Based on the GLFW key codes
        public void PushKeyChange(int keyCode, KeyboardEventTypes type)
            => OnKeyChange?.Invoke(keyCode, type);

        public event KeyboardEvent? OnKeyChange;

        public delegate void KeyboardEvent(int keyCode, KeyboardEventTypes type);
    }

    public enum KeyboardEventTypes
    {
        Pressed,
        Repeated,
        Released
    }
}
