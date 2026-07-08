using System.Text.Json;

namespace ExamGuard.Protocol;

public sealed class DiscoveryAnnouncement
{
    public string Product { get; set; } = "ExamGuard";
    public string MessageType { get; set; } = "TEACHER_DISCOVERY";
    public string TeacherMachine { get; set; } = Environment.MachineName;
    public string SessionId { get; set; } = "";
    public string Host { get; set; } = "";
    public int Port { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    public string ToJson()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    public static DiscoveryAnnouncement? FromJson(string json)
    {
        try
        {
            DiscoveryAnnouncement? announcement = JsonSerializer.Deserialize<DiscoveryAnnouncement>(
                json,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return announcement?.Product == "ExamGuard" && announcement.MessageType == "TEACHER_DISCOVERY"
                ? announcement
                : null;
        }
        catch
        {
            return null;
        }
    }
}
