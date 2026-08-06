using MeetingRecorder.Domain.Entities;
using MeetingRecorder.Domain.Enums;

namespace MeetingRecorder.Application.Abstractions;

/// <summary>Pushes real-time updates to clients subscribed to a meeting's SignalR group.</summary>
public interface IMeetingNotifier
{
    Task NotifyStatusChangedAsync(string meetingId, MeetingStatus status, CancellationToken ct = default);
    Task NotifyTranscriptSegmentReadyAsync(string meetingId, TranscriptSegment segment, CancellationToken ct = default);
}
