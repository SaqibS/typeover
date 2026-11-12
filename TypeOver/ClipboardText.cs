using System.Text;
using System.Windows.Input;

namespace TypeOver;

static class ClipboardText
{
    const string Unshifted = "-=[]\\;'`,./";
    const string Shifted = "_+{}|:\"~<>?";
    static readonly Key[] Punctuation =
    [
        Key.OemMinus, Key.OemPlus, Key.OemOpenBrackets, Key.OemCloseBrackets,
        Key.OemPipe, Key.OemSemicolon, Key.OemQuotes, Key.OemTilde,
        Key.OemComma, Key.OemPeriod, Key.OemQuestion
    ];

    public static bool CanType(string text, out Rune unsupported)
    {
        foreach (var character in text.EnumerateRunes())
        {
            if (!TryGetKey(character.Value, out _, out _))
            {
                unsupported = character;
                return false;
            }
        }

        unsupported = default;
        return true;
    }

    public static async Task TypeAsync(
        string text, Func<byte[], Task> send, CancellationToken cancellationToken)
    {
        if (!CanType(text, out var unsupported))
            throw new ArgumentException($"Unsupported character U+{unsupported.Value:X4}.", nameof(text));

        var keys = new PressedKeys();
        try
        {
            for (var i = 0; i < text.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (text[i] == '\n' && i > 0 && text[i - 1] == '\r')
                    continue;

                TryGetKey(text[i], out var key, out var shift);
                if (shift)
                    keys.Press(Key.LeftShift);
                keys.Press(key);
                await send(keys.ToBytes());
                await Task.Delay(20, cancellationToken);
                keys.Clear();
                await send(keys.ToBytes());
                await Task.Delay(20, cancellationToken);
            }
        }
        finally
        {
            await send(new byte[8]);
        }
    }

    static bool TryGetKey(int character, out Key key, out bool shift)
    {
        shift = character is >= 'A' and <= 'Z';
        key = character switch
        {
            >= 'a' and <= 'z' => Key.A + (character - 'a'),
            >= 'A' and <= 'Z' => Key.A + (character - 'A'),
            >= '1' and <= '9' => Key.D1 + (character - '1'),
            '0' => Key.D0,
            ' ' => Key.Space,
            '\t' => Key.Tab,
            '\r' or '\n' => Key.Enter,
            _ => Key.None
        };
        if (key != Key.None)
            return true;

        if (character > char.MaxValue)
            return false;

        var index = Unshifted.IndexOf((char)character);
        if (index >= 0)
        {
            key = Punctuation[index];
            return true;
        }

        index = Shifted.IndexOf((char)character);
        if (index >= 0)
        {
            shift = true;
            key = Punctuation[index];
            return true;
        }

        index = "!@#$%^&*()".IndexOf((char)character);
        if (index < 0)
            return false;

        shift = true;
        key = index == 9 ? Key.D0 : Key.D1 + index;
        return true;
    }
}
