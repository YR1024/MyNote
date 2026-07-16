using System;
using System.Runtime.InteropServices;

namespace SkipDrama_YuanShen
{
    internal static class Sdl3Native
    {
        internal const uint InitGamepad = 0x00002000;
        internal const string BackgroundEventsHint = "SDL_JOYSTICK_ALLOW_BACKGROUND_EVENTS";
        internal const uint EventJoystickButtonDown = 0x603;
        internal const uint EventJoystickButtonUp = 0x604;
        internal const uint EventGamepadButtonDown = 0x651;
        internal const uint EventGamepadButtonUp = 0x652;

        [DllImport("SDL3.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool SDL_Init(uint flags);

        [DllImport("SDL3.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SDL_Quit();

        [DllImport("SDL3.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool SDL_SetHint(
            [MarshalAs(UnmanagedType.LPStr)] string name,
            [MarshalAs(UnmanagedType.LPStr)] string value);

        [DllImport("SDL3.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr SDL_GetGamepads(out int count);

        [DllImport("SDL3.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SDL_free(IntPtr memory);

        [DllImport("SDL3.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SDL_PumpEvents();

        [DllImport("SDL3.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool SDL_PollEvent(out SdlEvent ev);

        [DllImport("SDL3.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr SDL_GetGamepadNameForID(uint instanceId);

        [DllImport("SDL3.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr SDL_GetGamepadPathForID(uint instanceId);

        [DllImport("SDL3.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern ushort SDL_GetGamepadVendorForID(uint instanceId);

        [DllImport("SDL3.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern ushort SDL_GetGamepadProductForID(uint instanceId);

        [DllImport("SDL3.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr SDL_OpenGamepad(uint instanceId);

        [DllImport("SDL3.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SDL_CloseGamepad(IntPtr gamepad);

        [DllImport("SDL3.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr SDL_GetGamepadJoystick(IntPtr gamepad);

        [DllImport("SDL3.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SDL_GetNumJoystickAxes(IntPtr joystick);

        [DllImport("SDL3.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern short SDL_GetJoystickAxis(IntPtr joystick, int axis);

        [DllImport("SDL3.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SDL_GetNumJoystickButtons(IntPtr joystick);

        [DllImport("SDL3.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool SDL_GetJoystickButton(IntPtr joystick, int button);

        [DllImport("SDL3.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool SDL_GamepadConnected(IntPtr gamepad);

        [DllImport("SDL3.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SDL_UpdateGamepads();

        [DllImport("SDL3.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool SDL_GetGamepadButton(IntPtr gamepad, SdlGamepadButton button);

        [DllImport("SDL3.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern short SDL_GetGamepadAxis(IntPtr gamepad, SdlGamepadAxis axis);

        [DllImport("SDL3.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr SDL_GetError();

        internal static string StringFromUtf8(IntPtr value)
        {
            return value == IntPtr.Zero ? string.Empty : Marshal.PtrToStringAnsi(value) ?? string.Empty;
        }

        internal static string GetError()
        {
            return StringFromUtf8(SDL_GetError());
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 128)]
    internal struct SdlEvent
    {
        internal uint Type;
        internal uint Reserved;
        internal ulong Timestamp;
        internal uint Which;
        internal byte Button;
        internal byte Down;
        internal byte Padding1;
        internal byte Padding2;
    }

    internal enum SdlGamepadButton
    {
        South = 0,
        East = 1,
        West = 2,
        North = 3,
        Back = 4,
        Guide = 5,
        Start = 6,
        LeftStick = 7,
        RightStick = 8,
        LeftShoulder = 9,
        RightShoulder = 10,
        DpadUp = 11,
        DpadDown = 12,
        DpadLeft = 13,
        DpadRight = 14,
        Misc1 = 15
    }

    internal enum SdlGamepadAxis
    {
        LeftX = 0,
        LeftY = 1,
        RightX = 2,
        RightY = 3,
        LeftTrigger = 4,
        RightTrigger = 5
    }
}
