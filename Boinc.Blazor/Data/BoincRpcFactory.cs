using System.ComponentModel;
using BoincRpc;

namespace Boinc.Blazor.Data;

public record BoincHost
{
    public int Id { get; set; }
    public string Uri { get; set; } = "localhost";
    public int Port { get; set; } = 31416;
    public string AuthKey { get; set; } = "";
}

public record BoincHostViewModel : BoincHost, INotifyPropertyChanged
{
    public BoincHostStatus Status { get; private set; } = BoincHostStatus.None;
    public HostInfo? HostInfo { get; private set; }
    public CoreClientState? ClientState { get; private set; }

    public BoincHostViewModel(BoincHost host)
    {
        Id = host.Id;
        Uri = host.Uri;
        Port = host.Port;
        AuthKey = host.AuthKey;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public async Task<BoincHostViewModel> LoadDataAsync()
    {
        (Status, var client) = await GetRpcClientAsync();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Status)));

        if (Status.Status == ConnectionStatus.Ready)
        {
            using (client)
            {
                HostInfo = await client.GetHostInfoAsync();
                ClientState = await client.GetStateAsync();

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HostInfo)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ClientState)));
            }
        }

        return this;
    }

    private async Task<(BoincHostStatus, RpcClient)> GetRpcClientAsync()
    {
        var client = new RpcClient();

        try
        {
            await client.ConnectAsync(Uri, Port);
            var authorized = await client.AuthorizeAsync(AuthKey);

            return authorized switch
            {
                true => (BoincHostStatus.Ready, client),
                false => (BoincHostStatus.Unauthorized, client)
            };

        }
        catch (Exception ex)
        {
            return (BoincHostStatus.Unreachable(ex.Message), client);
        }
    }
}

public record BoincHostStatus
{
    public ConnectionStatus Status { get; init; }
    public string Message { get; init; } = "";

    public static BoincHostStatus None => new() { Status = ConnectionStatus.None };
    public static BoincHostStatus Ready => new() { Status = ConnectionStatus.Ready };
    public static BoincHostStatus Unauthorized => new() { Status = ConnectionStatus.Unauthorized };
    public static BoincHostStatus Unreachable(string message) => new() { Status = ConnectionStatus.Unreachable, Message = message };
}

public enum ConnectionStatus
{
    None,
    Ready,
    Unauthorized,
    Unreachable
}
