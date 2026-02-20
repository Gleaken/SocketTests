using System.Net.Sockets;
using System.Text;

var endpoint = new UnixDomainSocketEndPoint("/tmp/myapp.sock");
using var client = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
await client.ConnectAsync(endpoint);

using var stream = new NetworkStream(client);
using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
using var reader = new StreamReader(stream, Encoding.UTF8);

var message = "Hello, server!";
await writer.WriteLineAsync(message);
var response = await reader.ReadLineAsync();
Console.WriteLine($"Server said: {response}");

client.Close();