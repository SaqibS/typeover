using TypeOver;
using Xunit;

namespace TypeOver.Tests;

public sealed class ClipboardTextTests
{
    [Theory]
    [InlineData('a', 0x04, false)]
    [InlineData('Z', 0x1d, true)]
    [InlineData('0', 0x27, false)]
    [InlineData('9', 0x26, false)]
    [InlineData('!', 0x1e, true)]
    [InlineData('?', 0x38, true)]
    [InlineData('\\', 0x31, false)]
    [InlineData('~', 0x35, true)]
    public async Task TypesCharacterAndReleasesKeys(char character, int usage, bool shift)
    {
        List<byte[]> reports = [];
        await ClipboardText.TypeAsync(character.ToString(), report =>
        {
            reports.Add(report);
            return Task.CompletedTask;
        }, CancellationToken.None);

        Assert.Equal(3, reports.Count);
        Assert.Equal(new byte[] { shift ? (byte)2 : (byte)0, 0, (byte)usage, 0, 0, 0, 0, 0 }, reports[0]);
        Assert.Equal(new byte[8], reports[1]);
        Assert.Equal(new byte[8], reports[2]);
    }

    [Fact]
    public async Task NormalizesCrLfAndPreservesStandaloneLineBreaksAndTabs()
    {
        List<byte> usages = [];
        await ClipboardText.TypeAsync("\r\n\n\r\t", report =>
        {
            if (report[2] != 0)
                usages.Add(report[2]);
            return Task.CompletedTask;
        }, CancellationToken.None);

        Assert.Equal(new byte[] { 0x28, 0x28, 0x28, 0x2b }, usages);
    }

    [Theory]
    [InlineData("valid\u00e9", 0xe9)]
    [InlineData("valid\U0001f600", 0x1f600)]
    [InlineData("valid\0", 0)]
    public async Task RejectsUnsupportedTextBeforeSendingAnyReports(string text, int codePoint)
    {
        Assert.False(ClipboardText.CanType(text, out var unsupported));
        Assert.Equal(codePoint, unsupported.Value);
        List<byte[]> reports = [];

        await Assert.ThrowsAsync<ArgumentException>(() => ClipboardText.TypeAsync(text, report =>
        {
            reports.Add(report);
            return Task.CompletedTask;
        }, CancellationToken.None));

        Assert.Empty(reports);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancellationReleasesKeysAndStopsTyping(bool cancelBeforeTyping)
    {
        using var cancellation = new CancellationTokenSource();
        if (cancelBeforeTyping)
            cancellation.Cancel();
        List<byte[]> reports = [];

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            ClipboardText.TypeAsync("AB", report =>
            {
                reports.Add(report);
                cancellation.Cancel();
                return Task.CompletedTask;
            }, cancellation.Token));

        Assert.Equal(cancelBeforeTyping ? 1 : 2, reports.Count);
        if (!cancelBeforeTyping)
            Assert.Equal(new byte[] { 2, 0, 4, 0, 0, 0, 0, 0 }, reports[0]);
        Assert.Equal(new byte[8], reports[^1]);
    }

    [Fact]
    public async Task SendFailureStopsTypingAndAttemptsToReleaseKeys()
    {
        List<byte[]> reports = [];
        var failure = new InvalidOperationException("Disconnected");

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ClipboardText.TypeAsync("ab", report =>
            {
                reports.Add(report);
                if (reports.Count == 1)
                    throw failure;
                return Task.CompletedTask;
            }, CancellationToken.None));

        Assert.Same(failure, actual);
        Assert.Equal(2, reports.Count);
        Assert.Equal(new byte[8], reports[^1]);
    }
}
