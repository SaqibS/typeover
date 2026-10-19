using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using static Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristicProperties;

namespace TypeOver;

sealed class BleKeyboard : IDisposable
{
    const ushort HidServiceId = 0x1812;
    const ushort HidInformationId = 0x2a4a;
    const ushort ReportMapId = 0x2a4b;
    const ushort ReportId = 0x2a4d;
    const ushort ControlPointId = 0x2a4c;
    const ushort ReportReferenceId = 0x2908;
    const byte InputReport = 1;
    const byte OutputReport = 2;

    // HID spec version 1.11, no country code, normally connectable.
    static readonly byte[] HidInformation = [0x11, 0x01, 0x00, 0x02];

    // Standard keyboard: input is modifiers, a reserved byte and six key codes;
    // output is five LED bits.
    static readonly byte[] ReportMap =
    [
        0x05, 0x01, 0x09, 0x06, 0xa1, 0x01,
        0x05, 0x07, 0x19, 0xe0, 0x29, 0xe7,
        0x15, 0x00, 0x25, 0x01, 0x75, 0x01,
        0x95, 0x08, 0x81, 0x02,
        0x95, 0x01, 0x75, 0x08, 0x81, 0x01,
        0x95, 0x05, 0x75, 0x01, 0x05, 0x08,
        0x19, 0x01, 0x29, 0x05, 0x91, 0x02,
        0x95, 0x01, 0x75, 0x03, 0x91, 0x01,
        0x95, 0x06, 0x75, 0x08, 0x15, 0x00,
        0x25, 0x65, 0x05, 0x07, 0x19, 0x00,
        0x29, 0x65, 0x81, 0x00, 0xc0
    ];

    GattServiceProvider? _provider;
    GattLocalCharacteristic? _input;

    public event Action? ConnectionChanged;
    public event Action<string>? StatusChanged;
    public bool HasSubscriber => _input?.SubscribedClients.Count > 0;

    public async Task StartAsync()
    {
        var result = await GattServiceProvider.CreateAsync(Uuid(HidServiceId));
        Check(result.Error, "Create HID service");
        _provider = result.ServiceProvider;

        await CharacteristicAsync(HidInformationId, Read, HidInformation);
        await CharacteristicAsync(ReportMapId, Read, ReportMap);

        _input = await CharacteristicAsync(ReportId, Read | Notify);
        AnswerReadsWithZeros(_input, 8);
        await ReportReferenceAsync(_input, InputReport);
        _input.SubscribedClientsChanged += (_, _) => ConnectionChanged?.Invoke();

        // Phones write LED state here; the app doesn't use it but must accept it.
        var output = await CharacteristicAsync(ReportId, Read | Write | WriteWithoutResponse);
        AnswerReadsWithZeros(output, 1);
        output.WriteRequested += AcceptWrite;
        await ReportReferenceAsync(output, OutputReport);

        var control = await CharacteristicAsync(ControlPointId, WriteWithoutResponse);
        control.WriteRequested += AcceptWrite;

        _provider.AdvertisementStatusChanged += (_, args) =>
            StatusChanged?.Invoke(args.Status == GattServiceProviderAdvertisementStatus.Started
                ? $"Advertising as {Environment.MachineName}. Pair from your phone."
                : $"Bluetooth advertising: {args.Status}.");
        _provider.StartAdvertising(new GattServiceProviderAdvertisingParameters
        {
            IsConnectable = true,
            IsDiscoverable = true
        });
    }

    public void Send(byte[] report) => _ = _input?.NotifyValueAsync(report.AsBuffer());

    async Task<GattLocalCharacteristic> CharacteristicAsync(
        ushort uuid, GattCharacteristicProperties properties, byte[]? staticValue = null)
    {
        var result = await _provider!.Service.CreateCharacteristicAsync(Uuid(uuid),
            new GattLocalCharacteristicParameters
            {
                CharacteristicProperties = properties,
                ReadProtectionLevel = GattProtectionLevel.EncryptionRequired,
                WriteProtectionLevel = GattProtectionLevel.EncryptionRequired,
                StaticValue = staticValue?.AsBuffer()
            });
        Check(result.Error, $"Create characteristic {uuid:X4}");
        return result.Characteristic;
    }

    static async Task ReportReferenceAsync(GattLocalCharacteristic characteristic, byte type)
    {
        var result = await characteristic.CreateDescriptorAsync(Uuid(ReportReferenceId),
            new GattLocalDescriptorParameters
            {
                ReadProtectionLevel = GattProtectionLevel.EncryptionRequired,
                StaticValue = new byte[] { 0, type }.AsBuffer()
            });
        Check(result.Error, "Create report reference");
    }

    static void AnswerReadsWithZeros(GattLocalCharacteristic characteristic, int length) =>
        characteristic.ReadRequested += async (_, args) =>
        {
            using var deferral = args.GetDeferral();
            var request = await args.GetRequestAsync();
            request.RespondWithValue(new byte[length].AsBuffer());
        };

    static async void AcceptWrite(GattLocalCharacteristic sender, GattWriteRequestedEventArgs args)
    {
        using var deferral = args.GetDeferral();
        var request = await args.GetRequestAsync();
        if (request.Option == GattWriteOption.WriteWithResponse)
            request.Respond();
    }

    static Guid Uuid(ushort value) => new($"{value:x8}-0000-1000-8000-00805f9b34fb");

    static void Check(BluetoothError error, string operation)
    {
        if (error != BluetoothError.Success)
            throw new InvalidOperationException($"{operation}: {error}");
    }

    public void Dispose() => _provider?.StopAdvertising();
}
