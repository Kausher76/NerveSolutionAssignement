// Console prompts with defaults, shared by client and server
static class Prompt
{
    public static string Ask(string label, string defaultValue = "")
    {
        Console.Write(defaultValue == "" ? $"{label}: " : $"{label} [{defaultValue}]: ");
        string? value = Console.ReadLine();
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value.Trim();
    }

    public static int Port(string defaultValue = "5000")
    {
        while (true)
        {
            if (int.TryParse(Ask("Port", defaultValue), out int port) && port is >= 1 and <= 65535)
                return port;
            Console.WriteLine("Invalid port. Enter a number between 1 and 65535.");
        }
    }
}
