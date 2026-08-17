using System.IO;
using System.IO.Pipes;
using System.Text;

namespace MoniHop.Desktop.Lifecycle;

public sealed class SingleInstanceCoordinator : IDisposable
{
    private static readonly byte[] ShowCommand = Encoding.UTF8.GetBytes("show");
    private readonly Mutex _mutex;
    private readonly string _pipeName;
    private readonly CancellationTokenSource _cancellation = new();
    private readonly Task? _listenerTask;
    private bool _disposed;

    public SingleInstanceCoordinator(string name, TimeSpan? acquisitionTimeout = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        _pipeName = name.Replace('\\', '.');
        _mutex = new Mutex(initiallyOwned: true, $"Local\\{name}", out var createdNew);
        IsPrimary = createdNew || TryAcquireExisting(acquisitionTimeout);
        if (IsPrimary)
        {
            _listenerTask = Task.Run(ListenAsync);
        }
    }

    private bool TryAcquireExisting(TimeSpan? acquisitionTimeout)
    {
        if (acquisitionTimeout is null || acquisitionTimeout <= TimeSpan.Zero)
        {
            return false;
        }

        try
        {
            return _mutex.WaitOne(acquisitionTimeout.Value);
        }
        catch (AbandonedMutexException)
        {
            return true;
        }
    }

    public event EventHandler? ShowRequested;

    public bool IsPrimary { get; }

    public async Task SignalPrimaryAsync(CancellationToken cancellationToken = default)
    {
        if (IsPrimary)
        {
            return;
        }

        using var client = new NamedPipeClientStream(
            ".",
            _pipeName,
            PipeDirection.Out,
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(2));
        await client.ConnectAsync(timeout.Token).ConfigureAwait(false);
        await client.WriteAsync(ShowCommand, timeout.Token).ConfigureAwait(false);
        await client.FlushAsync(timeout.Token).ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _cancellation.Cancel();
        if (IsPrimary)
        {
            try
            {
                _mutex.ReleaseMutex();
            }
            catch (ApplicationException)
            {
            }
        }

        _mutex.Dispose();
        _cancellation.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task ListenAsync()
    {
        while (!_cancellation.IsCancellationRequested)
        {
            try
            {
                await using var server = new NamedPipeServerStream(
                    _pipeName,
                    PipeDirection.In,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                await server.WaitForConnectionAsync(_cancellation.Token).ConfigureAwait(false);
                var buffer = new byte[ShowCommand.Length];
                var read = await server.ReadAsync(buffer, _cancellation.Token).ConfigureAwait(false);
                if (read == ShowCommand.Length && buffer.AsSpan().SequenceEqual(ShowCommand))
                {
                    ShowRequested?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (OperationCanceledException) when (_cancellation.IsCancellationRequested)
            {
                return;
            }
            catch (IOException) when (!_cancellation.IsCancellationRequested)
            {
            }
        }
    }
}
