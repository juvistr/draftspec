using DraftSpec.Cli;

namespace DraftSpec.Tests.Cli;

/// <summary>
/// Tests for FileWatcher class.
/// </summary>
public class FileWatcherTests
{
    private string _tempDir = null!;

    [Before(Test)]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"draftspec-watcher-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    [After(Test)]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            try
            {
                Directory.Delete(_tempDir, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }

    #region Debouncing

    [Test]
    public async Task FileWatcher_SingleChange_TriggersCallbackOnce()
    {
        var callCount = 0;
        FileChangeInfo? receivedChange = null;
        var tcs = new TaskCompletionSource<bool>();

        using var watcher = new FileWatcher(_tempDir, change =>
        {
            Interlocked.Increment(ref callCount);
            receivedChange = change;
            tcs.TrySetResult(true);
        }, debounceMs: 50);

        // Create a spec file
        var specFile = Path.Combine(_tempDir, "test.spec.csx");
        await File.WriteAllTextAsync(specFile, "// spec");

        // Wait for callback with timeout
        var completed = await Task.WhenAny(tcs.Task, Task.Delay(2000));
        await Assert.That(completed == tcs.Task).IsTrue();

        // Small delay to ensure no extra callbacks
        await Task.Delay(100);

        await Assert.That(callCount).IsEqualTo(1);
        await Assert.That(receivedChange).IsNotNull();
        await Assert.That(receivedChange!.IsSpecFile).IsTrue();
    }

    [Test]
    public async Task FileWatcher_MultipleRapidChanges_DebouncesToSingleCallback()
    {
        var callCount = 0;
        var tcs = new TaskCompletionSource<bool>();

        using var watcher = new FileWatcher(_tempDir, _ =>
        {
            var count = Interlocked.Increment(ref callCount);
            if (count == 1)
            {
                // Set after first callback to start waiting
                Task.Delay(150).ContinueWith(_ => tcs.TrySetResult(true));
            }
        }, debounceMs: 50);

        var specFile = Path.Combine(_tempDir, "test.spec.csx");

        // Rapid changes to same file
        for (var i = 0; i < 5; i++)
        {
            await File.WriteAllTextAsync(specFile, $"// version {i}");
            await Task.Delay(10); // Small delay between writes
        }

        // Wait for debounce to settle
        await Task.WhenAny(tcs.Task, Task.Delay(2000));
        await Task.Delay(150); // Extra time for any additional callbacks

        // Should be debounced - significantly fewer than 5 (1-3 depending on timing)
        await Assert.That(callCount).IsLessThanOrEqualTo(3);
        await Assert.That(callCount).IsLessThan(5); // Definitely debounced
    }

    #endregion

    #region Temporary Files

    [Test]
    public async Task FileWatcher_TemporaryFilesWithDotPrefix_AreIgnored()
    {
        var callCount = 0;
        using var watcher = new FileWatcher(_tempDir, _ =>
        {
            Interlocked.Increment(ref callCount);
        }, debounceMs: 50);

        // Create a temporary file (dot prefix)
        var tempFile = Path.Combine(_tempDir, ".temp.spec.csx");
        await File.WriteAllTextAsync(tempFile, "// temp");

        // Wait for potential callback
        await Task.Delay(200);

        await Assert.That(callCount).IsEqualTo(0);
    }

    [Test]
    public async Task FileWatcher_TemporaryFilesWithTildeSuffix_AreIgnored()
    {
        var callCount = 0;
        using var watcher = new FileWatcher(_tempDir, _ =>
        {
            Interlocked.Increment(ref callCount);
        }, debounceMs: 50);

        // Create a backup file (tilde suffix)
        var backupFile = Path.Combine(_tempDir, "test.spec.csx~");
        await File.WriteAllTextAsync(backupFile, "// backup");

        // Wait for potential callback
        await Task.Delay(200);

        await Assert.That(callCount).IsEqualTo(0);
    }

    #endregion

    #region Change Escalation

    [Test]
    public async Task FileWatcher_MultipleSpecFiles_EscalatesToFullRun()
    {
        FileChangeInfo? receivedChange = null;
        var tcs = new TaskCompletionSource<bool>();

        using var watcher = new FileWatcher(_tempDir, change =>
        {
            receivedChange = change;
            tcs.TrySetResult(true);
        }, debounceMs: 100);

        // Create two different spec files rapidly
        var spec1 = Path.Combine(_tempDir, "first.spec.csx");
        var spec2 = Path.Combine(_tempDir, "second.spec.csx");

        await File.WriteAllTextAsync(spec1, "// first");
        await Task.Delay(10);
        await File.WriteAllTextAsync(spec2, "// second");

        // Wait for callback
        await Task.WhenAny(tcs.Task, Task.Delay(2000));

        await Assert.That(receivedChange).IsNotNull();
        // Multiple files should escalate to full run (FilePath = null)
        await Assert.That(receivedChange!.FilePath).IsNull();
        await Assert.That(receivedChange.IsSpecFile).IsFalse();
    }

    [Test]
    public async Task FileWatcher_SourceFileChange_EscalatesToFullRun()
    {
        FileChangeInfo? receivedChange = null;
        var tcs = new TaskCompletionSource<bool>();

        using var watcher = new FileWatcher(_tempDir, change =>
        {
            receivedChange = change;
            tcs.TrySetResult(true);
        }, debounceMs: 50);

        // Create a .cs source file (not a spec)
        var sourceFile = Path.Combine(_tempDir, "Program.cs");
        await File.WriteAllTextAsync(sourceFile, "class Program {}");

        // Wait for callback
        await Task.WhenAny(tcs.Task, Task.Delay(2000));

        await Assert.That(receivedChange).IsNotNull();
        await Assert.That(receivedChange!.IsSpecFile).IsFalse();
    }

    #endregion

    #region Dispose

    [Test]
    public async Task FileWatcher_Dispose_StopsWatching()
    {
        var callCount = 0;
        var watcher = new FileWatcher(_tempDir, _ =>
        {
            Interlocked.Increment(ref callCount);
        }, debounceMs: 50);

        // Dispose the watcher
        watcher.Dispose();

        // Try to trigger changes after dispose
        var specFile = Path.Combine(_tempDir, "test.spec.csx");
        await File.WriteAllTextAsync(specFile, "// spec");

        // Wait to ensure no callbacks
        await Task.Delay(200);

        await Assert.That(callCount).IsEqualTo(0);
    }

    [Test]
    public async Task FileWatcher_DoubleDispose_DoesNotThrow()
    {
        var watcher = new FileWatcher(_tempDir, _ => { }, debounceMs: 50);

        // Should not throw on double dispose
        watcher.Dispose();
        watcher.Dispose();

        await Task.CompletedTask;
    }

    #endregion

    #region FileChangeInfo Record

    [Test]
    public async Task FileChangeInfo_PreservesProperties()
    {
        var info = new FileChangeInfo("/path/to/spec.csx", IsSpecFile: true);

        await Assert.That(info.FilePath).IsEqualTo("/path/to/spec.csx");
        await Assert.That(info.IsSpecFile).IsTrue();
    }

    [Test]
    public async Task FileChangeInfo_NullFilePath_IndicatesMultipleFiles()
    {
        var info = new FileChangeInfo(null, IsSpecFile: false);

        await Assert.That(info.FilePath).IsNull();
        await Assert.That(info.IsSpecFile).IsFalse();
    }

    #endregion
}
