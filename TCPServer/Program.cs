using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json;

// User-configurable settings (press Enter for default)
// Re-prompt until the listener actually binds (catches IPs not on this machine and ports already in use)
IPAddress ipAddress;
int port;
TcpListener server;
while (true)
{
    while (!IPAddress.TryParse(Prompt.Ask("Listen IP (0.0.0.0 = all network interfaces)", "0.0.0.0"), out ipAddress!))
        Console.WriteLine("Invalid IP address.");
    port = Prompt.Port();
    try
    {
        server = new TcpListener(ipAddress, port);
        server.Start();
        break;
    }
    catch (SocketException ex)
    {
        Console.WriteLine($"Cannot listen on {ipAddress}:{port}: {ex.Message}");
        Console.WriteLine("Use 0.0.0.0, 127.0.0.1 or one of this machine's own IPs (see ipconfig), and a port not already in use.");
    }
}

byte[] key = Crypto.DeriveKey(Prompt.Ask("Encryption key", "NerveSecret"));
string dataPath = Path.Combine(AppContext.BaseDirectory, Prompt.Ask("Collection file", "data.json"));

// Server collection, same format as the spec: {"SetA":[{"One":1,"Two":2}], ...}
Dictionary<string, Dictionary<string, int>> data;
try
{
    var raw = JsonSerializer.Deserialize<Dictionary<string, List<Dictionary<string, int>>>>(File.ReadAllText(dataPath))!;
    data = raw.ToDictionary(set => set.Key, set => set.Value.SelectMany(d => d).ToDictionary(p => p.Key, p => p.Value));
}
catch (Exception ex) when (ex is IOException or JsonException or ArgumentException or UnauthorizedAccessException)
{
    Console.WriteLine($"Could not load collection from {dataPath}: {ex.Message}");
    return;
}
Console.WriteLine($"Loaded {data.Count} sets from {dataPath}");

Console.WriteLine($"Server listening on {ipAddress}:{port}");

while (true)
{
    TcpClient client = await server.AcceptTcpClientAsync();
    // Each client runs independently, so one connection never blocks or breaks another
    _ = HandleClientAsync(client);
}

async Task HandleClientAsync(TcpClient client)
{
    string who = client.Client.RemoteEndPoint?.ToString() ?? "?";
    try
    {
        using (client)
        using (NetworkStream stream = client.GetStream())
        using (StreamReader reader = new(stream))
        using (StreamWriter writer = new(stream) { AutoFlush = true })
        {
            string? cipher = await reader.ReadLineAsync();
            if (cipher is null) return;
            string input = Crypto.Decrypt(cipher, key);
            Console.WriteLine($"[{who}] Received (encrypted): {cipher}");
            Console.WriteLine($"[{who}] Decrypted: {input}");

            int count = 0;
            string[] parts = input.Split('-');
            if (parts.Length == 2
                && data.TryGetValue(parts[0].Trim(), out var subset)
                && subset.TryGetValue(parts[1].Trim(), out int value))
            {
                count = value;
            }

            if (count <= 0)
            {
                await writer.WriteLineAsync(Crypto.Encrypt("EMPTY", key));
                return;
            }

            for (int i = 0; i < count; i++)
            {
                if (i > 0) await Task.Delay(1000);
                await writer.WriteLineAsync(Crypto.Encrypt(DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"), key));
            }
        }
    }
    catch (Exception ex) when (ex is IOException or SocketException)
    {
        Console.WriteLine($"[{who}] Client disconnected: {ex.Message}");
    }
    catch (Exception ex) when (ex is CryptographicException or FormatException)
    {
        Console.WriteLine($"[{who}] Rejected message, could not decrypt (wrong key or tampered): {ex.Message}");
    }
}
