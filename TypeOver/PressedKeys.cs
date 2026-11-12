using System.Windows.Input;

namespace TypeOver;

sealed class PressedKeys
{
    byte _modifiers;
    readonly List<byte> _keys = [];

    public void Press(Key key)
    {
        _modifiers |= Modifier(key);

        // A keyboard report has room for six non-modifier keys.
        var usage = Usage(key);
        if (usage != 0 && _keys.Count < 6)
            _keys.Add(usage);
    }

    public void Release(Key key)
    {
        _modifiers &= (byte)~Modifier(key);
        _keys.Remove(Usage(key));
    }

    public void Clear()
    {
        _modifiers = 0;
        _keys.Clear();
    }

    public byte[] ToBytes()
    {
        var bytes = new byte[8];
        bytes[0] = _modifiers;
        _keys.CopyTo(bytes, 2);
        return bytes;
    }

    static byte Modifier(Key key) => key switch
    {
        Key.LeftCtrl => 1,
        Key.LeftShift => 2,
        Key.LeftAlt => 4,
        Key.LWin => 8,
        Key.RightCtrl => 16,
        Key.RightShift => 32,
        Key.RightAlt => 64,
        Key.RWin => 128,
        _ => 0
    };

    static byte Usage(Key key) => key switch
    {
        >= Key.A and <= Key.Z => (byte)(4 + key - Key.A),
        >= Key.D1 and <= Key.D9 => (byte)(0x1e + key - Key.D1),
        >= Key.F1 and <= Key.F12 => (byte)(0x3a + key - Key.F1),
        >= Key.NumPad1 and <= Key.NumPad9 => (byte)(0x59 + key - Key.NumPad1),
        Key.D0 => 0x27,
        Key.Enter => 0x28,
        Key.Back => 0x2a,
        Key.Tab => 0x2b,
        Key.Space => 0x2c,
        Key.OemMinus => 0x2d,
        Key.OemPlus => 0x2e,
        Key.OemOpenBrackets => 0x2f,
        Key.OemCloseBrackets => 0x30,
        Key.OemPipe => 0x31,
        Key.OemSemicolon => 0x33,
        Key.OemQuotes => 0x34,
        Key.OemTilde => 0x35,
        Key.OemComma => 0x36,
        Key.OemPeriod => 0x37,
        Key.OemQuestion => 0x38,
        Key.CapsLock => 0x39,
        Key.PrintScreen => 0x46,
        Key.Scroll => 0x47,
        Key.Pause => 0x48,
        Key.Insert => 0x49,
        Key.Home => 0x4a,
        Key.PageUp => 0x4b,
        Key.Delete => 0x4c,
        Key.End => 0x4d,
        Key.PageDown => 0x4e,
        Key.Right => 0x4f,
        Key.Left => 0x50,
        Key.Down => 0x51,
        Key.Up => 0x52,
        Key.NumLock => 0x53,
        Key.Divide => 0x54,
        Key.Multiply => 0x55,
        Key.Subtract => 0x56,
        Key.Add => 0x57,
        Key.NumPad0 => 0x62,
        Key.Decimal => 0x63,
        Key.OemBackslash => 0x64,
        Key.Apps => 0x65,
        _ => 0
    };
}
