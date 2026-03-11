using System.IO.Pipes;
using System.Text.Json;

namespace MarkdownPad;

internal sealed class ExternalActivationRequestEventArgs(IReadOnlyList<string> filePaths) : EventArgs
{
    public IReadOnlyList<string> FilePaths { get; } = filePaths;
}

internal sealed class SingleInstanceChannel : IDisposable
{
    private const string PipeName = "MarkdownPad.OpenDocuments.v1";

    private readonly CancellationTokenSource _shutdown = new();
    private Task? _listenTask;

    public event EventHandler<ExternalActivationRequestEventArgs>? RequestReceived;

    public void Start()
    {
        _listenTask ??= Task.Run(() => ListenLoopAsync(_shutdown.Token));
    }

    public static bool TrySendRequest(IEnumerable<string> filePaths)
    {
        string payload = JsonSerializer.Serialize(new ExternalActivationRequest
        {
            FilePaths = [.. NormalizeFilePaths(filePaths)]
        });

        for (int attempt = 0; attempt < 20; attempt++)
        {
            try
            {
                using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
                client.Connect(200);

                using var writer = new StreamWriter(client);
                writer.Write(payload);
                writer.Flush();
                return true;
            }
            catch (IOException)
            {
                Thread.Sleep(150);
            }
            catch (TimeoutException)
            {
                Thread.Sleep(150);
            }
        }

        return false;
    }

    public void Dispose()
    {
        _shutdown.Cancel();

        try
        {
            _listenTask?.Wait(1000);
        }
        catch
        {
        }

        _shutdown.Dispose();
    }

    private async Task ListenLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var server = new NamedPipeServerStream(
                    PipeName,
                    PipeDirection.In,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                await server.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);

                using var reader = new StreamReader(server);
                string payload = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
                IReadOnlyList<string> filePaths = DeserializeRequest(payload);
                RequestReceived?.Invoke(this, new ExternalActivationRequestEventArgs(filePaths));
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (IOException)
            {
            }
            catch (JsonException)
            {
            }
        }
    }

    private static IReadOnlyList<string> DeserializeRequest(string payload)
    {
        ExternalActivationRequest? request = JsonSerializer.Deserialize<ExternalActivationRequest>(payload);
        return [.. NormalizeFilePaths(request?.FilePaths)];
    }

    private static IEnumerable<string> NormalizeFilePaths(IEnumerable<string>? filePaths)
    {
        if (filePaths is null)
            yield break;

        foreach (string path in filePaths)
        {
            if (string.IsNullOrWhiteSpace(path))
                continue;

            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(path);
            }
            catch
            {
                continue;
            }

            yield return fullPath;
        }
    }

    private sealed class ExternalActivationRequest
    {
        public List<string> FilePaths { get; set; } = [];
    }
}
