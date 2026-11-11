using System.Net.Sockets;
using System.Security.Cryptography;

// User-configurable settings (press Enter for default)
string serverIp = Prompt.Ask("Server IP", "127.0.0.1");
int port = Prompt.Port();
byte[] key = Crypto.DeriveKey(Prompt.Ask("Encryption key", "NerveSecret"));

// Send as many inputs as the user wants; one connection per request
while (true)
{
    string input = Prompt.Ask("\nInput (e.g. SetA-Two, blank to quit)");
    if (input == "") break;

    try
    {
        await SendAsync(input);
    }
    catch (SocketException ex)
    {
        Console.WriteLine($"Could not reach server at {serverIp}:{port}: {ex.Message}");
    }
}

async Task SendAsync(string input)
{
    using TcpClient client = new();
    await client.ConnectAsync(serverIp, port);

    using NetworkStream stream = client.GetStream();
    using StreamWriter writer = new(stream) { AutoFlush = true };
    using StreamReader reader = new(stream);

    string cipher = Crypto.Encrypt(input, key);
    await writer.WriteLineAsync(cipher);
    Console.WriteLine($"Sent (encrypted): {cipher}");

    // Decrypt and display every message until the server closes the connection
    int received = 0;
    try
    {
        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            Console.WriteLine(Crypto.Decrypt(line, key));
            received++;
        }
    }
    catch (Exception ex) when (ex is CryptographicException or FormatException)
    {
        Console.WriteLine("Could not decrypt server response (key mismatch?).");
        return;
    }
    catch (IOException ex)
    {
        Console.WriteLine($"Connection lost: {ex.Message}");
        return;
    }

    if (received == 0)
        Console.WriteLine("No response from server (key mismatch?).");
}
