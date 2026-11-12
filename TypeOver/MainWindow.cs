using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TypeOver;

sealed class MainWindow : Window
{
    const string ReadyText = "Click or press Enter to type to phone. Press Ctrl+V to paste.";
    const string TypingText = "Typing to phone. Press Ctrl+V to paste. Press Esc to stop.";

    readonly BleKeyboard _keyboard = new();
    readonly PressedKeys _pressedKeys = new();
    readonly TextBlock _message = new() { TextWrapping = TextWrapping.Wrap, Margin = new(24) };
    readonly Button _button = new() { Margin = new(24) };
    bool _capturing;
    CancellationTokenSource? _pasteCancellation;

    public MainWindow()
    {
        Title = "TypeOver";
        Width = 560;
        Height = 350;
        FontSize = 20;
        ShowMessage("Starting Bluetooth...");

        Loaded += async (_, _) =>
        {
            try
            {
                await _keyboard.StartAsync();
            }
            catch (Exception ex)
            {
                ShowMessage($"Bluetooth could not start: {ex.Message}");
            }
        };
        Deactivated += (_, _) => StopCapture();
        Closed += (_, _) =>
        {
            StopCapture();
            _keyboard.Dispose();
        };
        _keyboard.ConnectionChanged += () => Dispatcher.BeginInvoke(() =>
        {
            if (_keyboard.HasSubscriber)
            {
                if (!_capturing && _pasteCancellation is null)
                    ShowButton(ReadyText);
            }
            else
            {
                StopCapture();
                ShowMessage("Phone disconnected.");
            }
        });
        _keyboard.StatusChanged += message => Dispatcher.BeginInvoke(() =>
        {
            if (!_keyboard.HasSubscriber)
                ShowMessage(message);
            else
                _button.Content = message;
        });

        _button.Click += (_, _) =>
        {
            if (_capturing || _pasteCancellation is not null)
                StopCapture();
            else
                StartCapture();
        };
        CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste,
            async (_, e) =>
            {
                e.Handled = true;
                await PasteClipboardAsync();
            },
            (_, e) =>
            {
                e.CanExecute = _keyboard.HasSubscriber && _pasteCancellation is null;
                e.Handled = true;
            }));
        _button.PreviewKeyDown += OnKeyDown;
        _button.PreviewKeyUp += OnKeyUp;
    }

    void ShowMessage(string text)
    {
        _message.Text = text;
        Content = _message;
    }

    void ShowButton(string text)
    {
        _button.Content = text;
        Content = _button;
        // Focus only succeeds once the newly shown button has been laid out.
        UpdateLayout();
        _button.Focus();
    }

    void StartCapture()
    {
        if (_pasteCancellation is not null || !_keyboard.HasSubscriber)
            return;

        _capturing = true;
        ShowButton(TypingText);
    }

    void StopCapture()
    {
        _pasteCancellation?.Cancel();
        if (_capturing)
        {
            _capturing = false;
            _pressedKeys.Clear();
            if (_keyboard.HasSubscriber)
                _keyboard.Send(_pressedKeys.ToBytes());
        }
        _button.Content = _pasteCancellation is null
            ? ReadyText
            : "Cancelling paste. Please wait.";
    }

    void OnKeyDown(object sender, KeyEventArgs e)
    {
        var key = RealKey(e);
        if (key == Key.Escape && (_capturing || _pasteCancellation is not null))
        {
            e.Handled = true;
            StopCapture();
            return;
        }

        if (_pasteCancellation is not null)
        {
            e.Handled = true;
            return;
        }

        if (!_capturing)
            return;

        foreach (InputGesture gesture in ApplicationCommands.Paste.InputGestures)
            if (gesture.Matches(_button, e))
                return;

        e.Handled = true;
        if (e.IsRepeat)
            return;

        _pressedKeys.Press(key);
        _keyboard.Send(_pressedKeys.ToBytes());
    }

    void OnKeyUp(object sender, KeyEventArgs e)
    {
        if (_pasteCancellation is not null)
        {
            e.Handled = true;
            return;
        }
        if (!_capturing)
            return;

        e.Handled = true;
        _pressedKeys.Release(RealKey(e));
        _keyboard.Send(_pressedKeys.ToBytes());
    }

    async Task PasteClipboardAsync()
    {
        if (_pasteCancellation is not null || !_keyboard.HasSubscriber)
            return;

        using var cancellation = new CancellationTokenSource();
        _pasteCancellation = cancellation;
        _pressedKeys.Clear();
        CommandManager.InvalidateRequerySuggested();
        string? error = null;
        try
        {
            var text = Clipboard.GetText();
            if (text.Length == 0)
            {
                error = "Clipboard has no text.";
            }
            else if (!ClipboardText.CanType(text, out var unsupported))
            {
                error = $"Cannot paste: unsupported character U+{unsupported.Value:X4}. No clipboard text was sent.";
            }
            else
            {
                _button.Content = "Pasting to phone. Click or press Esc to cancel.";
                await _keyboard.SendAsync(_pressedKeys.ToBytes());
                cancellation.Token.ThrowIfCancellationRequested();
                await ClipboardText.TypeAsync(text, _keyboard.SendAsync, cancellation.Token);
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            error = $"Paste failed: {ex.Message}";
        }
        finally
        {
            _pasteCancellation = null;
            CommandManager.InvalidateRequerySuggested();
            if (_keyboard.HasSubscriber)
            {
                var instructions = _capturing ? TypingText : ReadyText;
                _button.Content = error is null || cancellation.IsCancellationRequested
                    ? instructions
                    : $"{error} {instructions}";
            }
        }
    }

    // WPF reports Alt and Alt-combinations as Key.System, hiding the actual key.
    static Key RealKey(KeyEventArgs e) => e.Key == Key.System ? e.SystemKey : e.Key;
}
