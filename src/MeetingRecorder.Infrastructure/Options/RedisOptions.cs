namespace MeetingRecorder.Infrastructure.Options;

public class RedisOptions
{
    public const string SectionName = "Redis";

    /// <summary>SignalR backplane connection string. Leave empty for single-instance/local dev.</summary>
    public string? ConnectionString { get; set; }
}
