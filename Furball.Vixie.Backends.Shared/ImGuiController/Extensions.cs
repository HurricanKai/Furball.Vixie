﻿using System;
using ImGuiNET;
using Silk.NET.Input;

namespace Furball.Vixie.Backends.Shared.ImGuiController; 

public static class Extensions {
    public static int ToImGuiButton(this MouseButton b) {
        return b switch {
            MouseButton.Left     => 0,
            MouseButton.Right    => 1,
            MouseButton.Middle   => 2,
            _                    => -1 
        };
    }
    
    public static ImGuiKey ToImGuiKey(this Key k) {
            return k switch {
                Key.Space          => ImGuiKey.Space,
                Key.Apostrophe     => ImGuiKey.Apostrophe,
                Key.Comma          => ImGuiKey.Comma,
                Key.Minus          => ImGuiKey.Minus,
                Key.Period         => ImGuiKey.Period,
                Key.Slash          => ImGuiKey.Slash,
                Key.Number0        => ImGuiKey._0,
                Key.Number1        => ImGuiKey._1,
                Key.Number2        => ImGuiKey._2,
                Key.Number3        => ImGuiKey._3,
                Key.Number4        => ImGuiKey._4,
                Key.Number5        => ImGuiKey._5,
                Key.Number6        => ImGuiKey._6,
                Key.Number7        => ImGuiKey._7,
                Key.Number8        => ImGuiKey._8,
                Key.Number9        => ImGuiKey._9,
                Key.Semicolon      => ImGuiKey.Semicolon,
                Key.Equal          => ImGuiKey.Equal,
                Key.A              => ImGuiKey.A,
                Key.B              => ImGuiKey.B,
                Key.C              => ImGuiKey.C,
                Key.D              => ImGuiKey.D,
                Key.E              => ImGuiKey.E,
                Key.F              => ImGuiKey.F,
                Key.G              => ImGuiKey.G,
                Key.H              => ImGuiKey.H,
                Key.I              => ImGuiKey.I,
                Key.J              => ImGuiKey.J,
                Key.K              => ImGuiKey.K,
                Key.L              => ImGuiKey.L,
                Key.M              => ImGuiKey.M,
                Key.N              => ImGuiKey.N,
                Key.O              => ImGuiKey.O,
                Key.P              => ImGuiKey.P,
                Key.Q              => ImGuiKey.Q,
                Key.R              => ImGuiKey.R,
                Key.S              => ImGuiKey.S,
                Key.T              => ImGuiKey.T,
                Key.U              => ImGuiKey.U,
                Key.V              => ImGuiKey.V,
                Key.W              => ImGuiKey.W,
                Key.X              => ImGuiKey.X,
                Key.Y              => ImGuiKey.Y,
                Key.Z              => ImGuiKey.Z,
                Key.LeftBracket    => ImGuiKey.LeftBracket,
                Key.BackSlash      => ImGuiKey.Backslash,
                Key.RightBracket   => ImGuiKey.RightBracket,
                Key.GraveAccent    => ImGuiKey.GraveAccent,
                Key.Escape         => ImGuiKey.Escape,
                Key.Enter          => ImGuiKey.Enter,
                Key.Tab            => ImGuiKey.Tab,
                Key.Backspace      => ImGuiKey.Backspace,
                Key.Insert         => ImGuiKey.Insert,
                Key.Delete         => ImGuiKey.Delete,
                Key.Right          => ImGuiKey.RightArrow,
                Key.Left           => ImGuiKey.LeftArrow,
                Key.Down           => ImGuiKey.DownArrow,
                Key.Up             => ImGuiKey.UpArrow,
                Key.PageUp         => ImGuiKey.PageUp,
                Key.PageDown       => ImGuiKey.PageDown,
                Key.Home           => ImGuiKey.Home,
                Key.End            => ImGuiKey.End,
                Key.CapsLock       => ImGuiKey.CapsLock,
                Key.ScrollLock     => ImGuiKey.ScrollLock,
                Key.NumLock        => ImGuiKey.NumLock,
                Key.PrintScreen    => ImGuiKey.PrintScreen,
                Key.Pause          => ImGuiKey.Pause,
                Key.F1             => ImGuiKey.F1,
                Key.F2             => ImGuiKey.F2,
                Key.F3             => ImGuiKey.F3,
                Key.F4             => ImGuiKey.F4,
                Key.F5             => ImGuiKey.F5,
                Key.F6             => ImGuiKey.F6,
                Key.F7             => ImGuiKey.F7,
                Key.F8             => ImGuiKey.F8,
                Key.F9             => ImGuiKey.F9,
                Key.F10            => ImGuiKey.F10,
                Key.F11            => ImGuiKey.F11,
                Key.F12            => ImGuiKey.F12,
                Key.Keypad0        => ImGuiKey.Keypad0,
                Key.Keypad1        => ImGuiKey.Keypad1,
                Key.Keypad2        => ImGuiKey.Keypad2,
                Key.Keypad3        => ImGuiKey.Keypad3,
                Key.Keypad4        => ImGuiKey.Keypad4,
                Key.Keypad5        => ImGuiKey.Keypad5,
                Key.Keypad6        => ImGuiKey.Keypad6,
                Key.Keypad7        => ImGuiKey.Keypad7,
                Key.Keypad8        => ImGuiKey.Keypad8,
                Key.Keypad9        => ImGuiKey.Keypad9,
                Key.KeypadDecimal  => ImGuiKey.KeypadDecimal,
                Key.KeypadDivide   => ImGuiKey.KeypadDivide,
                Key.KeypadMultiply => ImGuiKey.KeypadMultiply,
                Key.KeypadSubtract => ImGuiKey.KeypadSubtract,
                Key.KeypadAdd      => ImGuiKey.KeypadAdd,
                Key.KeypadEnter    => ImGuiKey.KeypadEnter,
                Key.KeypadEqual    => ImGuiKey.KeypadEqual,
                Key.ShiftLeft      => ImGuiKey.LeftShift,
                Key.ControlLeft    => ImGuiKey.LeftCtrl,
                Key.AltLeft        => ImGuiKey.LeftAlt,
                Key.SuperLeft      => ImGuiKey.LeftSuper,
                Key.ShiftRight     => ImGuiKey.RightShift,
                Key.ControlRight   => ImGuiKey.RightCtrl,
                Key.AltRight       => ImGuiKey.RightAlt,
                Key.SuperRight     => ImGuiKey.RightSuper,
                Key.Menu           => ImGuiKey.Menu,
                Key.World1         => ImGuiKey.None,
                Key.World2         => ImGuiKey.None,
                Key.F13            => ImGuiKey.None,
                Key.F14            => ImGuiKey.None,
                Key.F15            => ImGuiKey.None,
                Key.F16            => ImGuiKey.None,
                Key.F17            => ImGuiKey.None,
                Key.F18            => ImGuiKey.None,
                Key.F19            => ImGuiKey.None,
                Key.F20            => ImGuiKey.None,
                Key.F21            => ImGuiKey.None,
                Key.F22            => ImGuiKey.None,
                Key.F23            => ImGuiKey.None,
                Key.F24            => ImGuiKey.None,
                Key.F25            => ImGuiKey.None,
                Key.Unknown        => ImGuiKey.None,
                _                  => ImGuiKey.None
            };
        }

}