using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TypeOver;

sealed class MainWindow : Window
{
    const string ReadyText = "Click or press Enter to type to phone.";
    const string TypingText = "Typing to phone. Press Esc to stop.";

    readonly BleKeyboard _keyboard = new();
    readonly PressedKeys _pressedKeys = new();
    readonly TextBlock _message = new() { TextWrapping = TextWrapping.Wrap, Margin = new(24) };
    readonly Button _button = new() { Margin = new(24) };
    bool _capturing;

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
        Closed += (_, _) => _keyboard.Dispose();
        _keyboard.ConnectionChanged += () => Dispatcher.BeginInvoke(() =>
        {
            if (_keyboard.HasSubscriber)
            {
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
        });

        _button.Click += (_, _) =>
        {
            if (_capturing)
                StopCapture();
            else
                StartCapture();
        };
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
        _capturing = true;
        ShowButton(TypingText);
    }

    void StopCapture()
    {
        if (!_capturing)
            return;

        _capturing = false;
        _pressedKeys.Clear();
        _keyboard.Send(_pressedKeys.ToBytes());
        ShowButton(ReadyText);
    }

    void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (!_capturing)
            return;

        e.Handled = true;
        var key = RealKey(e);
        if (key == Key.Escape)
        {
            StopCapture();
            return;
        }

        if (e.IsRepeat)
            return;

        _pressedKeys.Press(key);
        _keyboard.Send(_pressedKeys.ToBytes());
    }

    void OnKeyUp(object sender, KeyEventArgs e)
    {
        if (!_capturing)
            return;

        e.Handled = true;
        _pressedKeys.Release(RealKey(e));
        _keyboard.Send(_pressedKeys.ToBytes());
    }

    // WPF reports Alt and Alt-combinations as Key.System, hiding the actual key.
    static Key RealKey(KeyEventArgs e) => e.Key == Key.System ? e.SystemKey : e.Key;
}
