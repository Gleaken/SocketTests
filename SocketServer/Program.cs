// See https://aka.ms/new-console-template for more information

using System.Net.Sockets;
using System.Text;

var socketPath = "/tmp/myapp.sock";
if (File.Exists(socketPath))
{
    File.Delete(socketPath);
}
var endpoint = new UnixDomainSocketEndPoint(socketPath);
using var server = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
server.Bind(endpoint);
server.Listen(10);
Console.WriteLine("Listening...");
while (true) // <-- loop to accept multiple clients
{
    var client = await server.AcceptAsync();
    Console.WriteLine("Client connected!");

    // Handle each client in a separate task (so server can accept others)
    _ = Task.Run(async () =>
    {
        try
        {
            using var stream = new NetworkStream(client);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

            string? message;
            while ((message = await reader.ReadLineAsync()) != null)
            {
                Console.WriteLine($"Received: {message}");

                // Send response
                await writer.WriteLineAsync($"Echo: {message}");
            }

            Console.WriteLine("Client disconnected.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Client error: {ex.Message}");
        }
        finally
        {
            client.Close();
        }
    });
}
