using MeetingRecorder.Application.Abstractions;
using MeetingRecorder.Domain.Entities;
using MeetingRecorder.Domain.Enums;

namespace MeetingRecorder.Application.UseCases.Processing;

/// <summary>Notification Worker: emails the owner and pushes the final status over SignalR.</summary>
public class ProcessMeetingReady(
    IMeetingRepository meetings,
    IUserDirectory userDirectory,
    INotificationEmailSender emailSender,
    IMeetingNotifier notifier)
{
    public async Task ExecuteAsync(string meetingId, CancellationToken ct = default)
    {
        var meeting = await meetings.GetByIdAsync(meetingId, ct)
            ?? throw new Exceptions.NotFoundException(nameof(Meeting), meetingId);

        var email = await userDirectory.GetEmailAsync(meeting.OwnerId, ct);
        if (!string.IsNullOrWhiteSpace(email))
            await emailSender.SendTranscriptReadyEmailAsync(email, meeting.Id, meeting.Title, ct);

        await notifier.NotifyStatusChangedAsync(meetingId, MeetingStatus.Ready, ct);
    }
}
