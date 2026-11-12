# TypeOver

Use your PC's keyboard to type on your phone over Bluetooth. TypeOver runs on Windows 11, and your phone connects to it like a Bluetooth keyboard.

![Illustration of a laptop and a phone, with keycaps spelling "Hello" flowing from the laptop to the phone's notes app.](typeover.png)

## Why I Built This

I type much faster on a physical keyboard than on my phone. I rarely carry a separate keyboard, but I often have my laptop. TypeOver turns that laptop into a Bluetooth keyboard, without installing anything on the phone.

For example, when someone sends me a form on my phone, I can fill it in with my laptop's keyboard without moving the form to my laptop. I can also paste information I already have on my PC.

## Get Started

1. On your Windows PC, download `TypeOver.exe` from the [latest release](https://github.com/SaqibS/typeover/releases/latest).
2. Enable Bluetooth on your PC and phone, then start TypeOver.
3. Wait for the message `Advertising as <PC name>. Pair from your phone.`
4. Open your phone's Bluetooth settings and pair with your PC. Complete any pairing prompts shown by Windows or the phone.
5. Once the phone is ready to receive keystrokes, TypeOver displays `Click or press Enter to type to phone. Press Ctrl+V to paste.`
6. Open a text field on your phone. In TypeOver, click the button or press Enter to begin typing, or press Ctrl+V to paste the Windows clipboard without starting typing mode.
7. Press Esc to stop. Switching away from the TypeOver window also stops capture so you can use your keyboard on your PC again.

Keep TypeOver focused while typing. Closing the app stops Bluetooth advertising.

## Pasting from the Clipboard

With your phone connected and TypeOver focused, press Ctrl+V to paste text from the Windows clipboard. You do not need to start typing mode first. TypeOver sends the text as keystrokes, so no clipboard app is needed on your phone.

The same button shows the paste status; no separate screen opens. When the paste finishes, TypeOver returns to its previous mode: ready or typing.

Keep TypeOver focused until the paste finishes. Press Esc or click the button to cancel and return to ready mode. Switching away or disconnecting the phone also stops the paste and typing. Other keystrokes are ignored during a paste.

Pasting text containing tabs or line breaks may move between fields or submit a form, depending on the phone app.

## Supported Keys and Limitations

* You can use letters, numbers, common punctuation, arrow and navigation keys, F1-F12, and the numeric keypad.
* Ctrl, Shift, Alt, and Windows keys are supported. What shortcuts do depends on your phone and the app you're using.
* Ctrl+V is reserved for pasting the Windows clipboard and is not sent to the phone.
* Esc is reserved for stopping capture and is not sent to the phone.
* Windows may intercept system shortcuts before TypeOver can handle them.

## Feedback

Found a bug or have an idea? [Open an issue](https://github.com/SaqibS/typeover/issues). For bugs, please provide as much detail as possible, including your Windows version, phone model and OS version.

Using TypeOver? I'd love to hear how it's working for you. Email [me@saqibshaikh.com](mailto:me@saqibshaikh.com).

## Development

TypeOver is built in C# with .NET and WPF. It uses the Windows GATT APIs to advertise a Bluetooth Low Energy HID keyboard and send key presses and releases as HID reports. The phone uses its built-in Bluetooth keyboard support, so no companion app is needed.

To build and run from source, install the .NET 10 SDK on Windows, clone this repository, and run:

```powershell
cd TypeOver
dotnet build
dotnet run
```

Run the unit tests from the `Tests` directory:

```powershell
dotnet test
```

The tests use an in-memory sender. They do not access Bluetooth or send keys to a phone.
