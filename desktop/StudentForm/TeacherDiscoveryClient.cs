using System.Net;
using System.Net.Sockets;
using System.Text;
using ExamGuard.Protocol;

namespace StudentForm;

internal static class TeacherDiscoveryClient
{
    public static async Task<DiscoveryAnnouncement?> DiscoverAsync(string? expectedSessionId, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);

        using UdpClient udpClient = CreateClient();
        udpClient.EnableBroadcast = true;

        while (!timeoutCts.Token.IsCancellationRequested)
        {
            try
            {
                UdpReceiveResult result = await udpClient.ReceiveAsync(timeoutCts.Token);
                string json = Encoding.UTF8.GetString(result.Buffer);
                DiscoveryAnnouncement? announcement = DiscoveryAnnouncement.FromJson(json);
                if (announcement is not null)
                {
                    if (!string.IsNullOrWhiteSpace(expectedSessionId) &&
                        !string.Equals(announcement.SessionId, expectedSessionId, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(announcement.Host))
                    {
                        announcement.Host = result.RemoteEndPoint.Address.ToString();
                    }

                    return announcement;
                }
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }

        return null;
    }

    private const int TeacherDiscoveryPort = 9091;

    private static UdpClient CreateClient()
    {
        UdpClient client = new(AddressFamily.InterNetwork);
        client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        client.Client.Bind(new IPEndPoint(IPAddress.Any, TeacherDiscoveryPort));
        return client;
    }
}
