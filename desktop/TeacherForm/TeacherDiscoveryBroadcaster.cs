using System.Net;
using System.Net.Sockets;
using System.Text;
using ExamGuard.Protocol;

namespace TeacherForm;

internal sealed class TeacherDiscoveryBroadcaster : IAsyncDisposable
{
    public const int DiscoveryPort = 9091;

    private readonly Action<string> _log;
    private CancellationTokenSource? _cts;
    private UdpClient? _udpClient;

    public TeacherDiscoveryBroadcaster(Action<string> log)
    {
        _log = log;
    }

    public void Start(string sessionId, int teacherPort)
    {
        Stop();
        _cts = new CancellationTokenSource();
        _udpClient = new UdpClient { EnableBroadcast = true };
        _ = Task.Run(() => BroadcastLoopAsync(sessionId, teacherPort, _cts.Token));
        _log($"LAN discovery broadcasting on UDP {DiscoveryPort}.");
    }

    public void Stop()
    {
        _cts?.Cancel();
        _udpClient?.Dispose();
        _cts?.Dispose();
        _cts = null;
        _udpClient = null;
    }

    public ValueTask DisposeAsync()
    {
        Stop();
        return ValueTask.CompletedTask;
    }

    private async Task BroadcastLoopAsync(string sessionId, int teacherPort, CancellationToken cancellationToken)
    {
        IPEndPoint endpoint = new(IPAddress.Broadcast, DiscoveryPort);
        while (!cancellationToken.IsCancellationRequested && _udpClient is not null)
        {
            try
            {
                DiscoveryAnnouncement announcement = new()
                {
                    SessionId = sessionId,
                    Host = GetLikelyLocalIp(),
                    Port = teacherPort,
                    TeacherMachine = Environment.MachineName
                };
                byte[] bytes = Encoding.UTF8.GetBytes(announcement.ToJson());
                await _udpClient.SendAsync(bytes, bytes.Length, endpoint);
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                _log($"Discovery broadcast failed: {ex.Message}");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken).ContinueWith(_ => { }, CancellationToken.None);
        }
    }

    public static string GetLikelyLocalIp()
    {
        try
        {
            return Dns.GetHostAddresses(Dns.GetHostName())
                .Where(x => x.AddressFamily == AddressFamily.InterNetwork)
                .Select(x => x.ToString())
                .FirstOrDefault(x => !x.StartsWith("127.", StringComparison.Ordinal)) ?? "127.0.0.1";
        }
        catch
        {
            return "127.0.0.1";
        }
    }
}
