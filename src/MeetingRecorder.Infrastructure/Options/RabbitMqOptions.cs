namespace MeetingRecorder.Infrastructure.Options;

public class RabbitMqOptions
{
    public const string SectionName = "RabbitMQ";

    public required string ConnectionString { get; set; }
    public string ExchangeName { get; set; } = "meeting-recorder";
    public int MaxRetryAttempts { get; set; } = 5;
}
