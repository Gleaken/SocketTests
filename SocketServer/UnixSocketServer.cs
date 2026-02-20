using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SocketServer;

public interface ISocketServer
{
    Task StartAsync(CancellationToken cancellationToken);
}

public class UnixSocketServer : ISocketServer, IHostedService
{
    private readonly string _socketPath;
    private readonly ILogger<UnixSocketServer> _logger;
    private Socket? _server;

    public UnixSocketServer(ILogger<UnixSocketServer> logger, IConfiguration configuration)
    {
        _logger = logger;
        _socketPath = configuration["SocketPath"] ?? "/tmp/myapp.sock";
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (File.Exists(_socketPath))
        {
            File.Delete(_socketPath);
        }

        var endpoint = new UnixDomainSocketEndPoint(_socketPath);
        _server = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        _server.Bind(endpoint);
        _server.Listen(10);

        _logger.LogInformation("Listening on {SocketPath}...", _socketPath);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var client = await _server.AcceptAsync(cancellationToken);
                _logger.LogInformation("Client connected!");

                _ = HandleClientAsync(client, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting client");
            }
        }
    }

    private async Task HandleClientAsync(Socket client, CancellationToken cancellationToken)
    {
        try
        {
            using var stream = new NetworkStream(client);
            using var reader = new StreamReader(stream, Encoding.UTF8);

            string? message;
            while (!cancellationToken.IsCancellationRequested && (message = await reader.ReadLineAsync(cancellationToken)) != null)
            {
                _logger.LogInformation("Received: {Message}", message);
                using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
                await writer.WriteLineAsync($"Echo: {message}");
            }
            _logger.LogInformation("Client disconnected.");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Client handler cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Client error - most likely client disconnected.");
        }
        finally
        {
            client.Close();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping server...");
        _server?.Close();
        if (File.Exists(_socketPath))
        {
            File.Delete(_socketPath);
        }
        return Task.CompletedTask;
    }
}
