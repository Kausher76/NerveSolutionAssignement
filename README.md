# Nerve Socket Assignment: Encrypted TCP Client/Server (.NET 10)

A TCP server and client written in C#. The client sends a string such as `SetA-Two`. The server looks up the Set and the key in its collection. If both exist, it sends the current time *n* times, one second apart, where *n* is the stored value. Otherwise it sends `EMPTY`. Every message, in both directions, is encrypted.

## Project layout

| Path | Purpose |
|---|---|
| `TCPServer/Program.cs` | Server: loads the collection, accepts clients concurrently, decrypts requests and sends encrypted replies |
| `TCPServer/data.json` | Server collection, in the same format as the assignment |
| `TCPClient/Program.cs` | Client: encrypts input, sends it, then decrypts and displays every reply; send as many inputs as you like |
| `Shared/Crypto.cs` | AES-256-GCM encryption, with the key derived from a passphrase using PBKDF2-SHA256 |
| `Shared/Prompt.cs` | Console prompts with defaults and port validation |

## How each requirement is met

| Requirement | Implementation |
|---|---|
| Server listens passively; client connects to a given IP and port | `TcpListener` / `TcpClient` |
| Look up the Set, then the key, and send the time *n* times at 1-second intervals | `HandleClientAsync` in the server |
| Otherwise send `EMPTY` | Any unknown Set, unknown key or malformed input |
| Client displays each received string | Read loop in the client |
| **Part B:** encrypt and decrypt every message, on both sides | AES-256-GCM, Base64, one line per message. A wrong key or tampered message is rejected. |
| Handle multiple clients without breaking existing connections | Each client runs on its own task, and one failing client doesn't affect the others |
| All inputs are user configurable | Prompted at startup (press Enter for the default): see below |

### Configurable inputs

| Server | Default | Client | Default |
|---|---|---|---|
| Listen IP | `0.0.0.0` (all interfaces) | Server IP | `127.0.0.1` |
| Port | `5000` | Port | `5000` |
| Encryption key | `NerveSecret` | Encryption key | `NerveSecret` |
| Collection file | `data.json` | Input | e.g. `SetA-Two` (leave blank to quit) |

The server and client must use the **same port and encryption key**.

## Run

Requires the .NET 10 SDK. Run each command from the repository root, in its own terminal.

```
dotnet run --project TCPServer
dotnet run --project TCPClient
```

To connect from another machine, enter the server machine's IP at the client's `Server IP` prompt, and allow the port through the server's firewall.

## Test cases

| Input / setup | Expected result |
|---|---|
| `SetA-Two` | Two timestamps one second apart, e.g. `09-05-2020 15:30:30`, `09-05-2020 15:30:31` |
| `SetE-Ten` | Ten timestamps |
| `SetZ-One`, `SetA-Nine`, `hello` | `EMPTY` |
| Client key differs from the server key | Server logs "Rejected message… authentication tag"; client shows "No response from server (key mismatch?)" |
| `SetE-Ten` in one client, then `SetA-One` in a second client | The second client answers immediately while the first is still receiving |
| Close a client in the middle of `SetE-Ten` | Server logs "Client disconnected" and keeps serving |
| Port `abc` or `99999` | Re-prompts: "Invalid port" |
| Client started with no server running | "Could not reach server…" and the client stays usable |
| Collection file that doesn't exist | Server prints "Could not load collection…" and exits |

The server log shows each message as it arrived on the wire (encrypted) and after decryption, which shows that the network traffic is encrypted.
