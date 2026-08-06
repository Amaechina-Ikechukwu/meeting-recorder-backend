namespace MeetingRecorder.Application.Abstractions;

/// <summary>Abstracts the transactional email provider (ZeptoMail).</summary>
public interface INotificationEmailSender
{
    Task SendTranscriptReadyEmailAsync(string toEmail, string meetingId, string meetingTitle, CancellationToken ct = default);
}
