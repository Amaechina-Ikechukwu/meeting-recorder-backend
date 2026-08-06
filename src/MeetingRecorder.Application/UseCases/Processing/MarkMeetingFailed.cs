using MeetingRecorder.Application.Abstractions;
using MeetingRecorder.Domain.Entities;
using MeetingRecorder.Domain.Enums;

namespace MeetingRecorder.Application.UseCases.Processing;

/// <summary>Invoked when a worker exhausts its retries and dead-letters a job — surfaces
/// the failure on the meeting so the client can show an actionable message and retry.</summary>
public class MarkMeetingFailed(IMeetingRepository meetings, IMeetingNotifier notifier)
{
    public async Task ExecuteAsync(string meetingId, FailureReason reason, string message, CancellationToken ct = default)
    {
        var meeting = await meetings.GetByIdAsync(meetingId, ct)
            ?? throw new Exceptions.NotFoundException(nameof(Meeting), meetingId);

        meeting.Status = MeetingStatus.Failed;
        meeting.FailureReason = reason;
        meeting.FailureMessage = message;
        await meetings.UpdateAsync(meeting, ct);

        await notifier.NotifyStatusChangedAsync(meetingId, MeetingStatus.Failed, ct);
    }
}
