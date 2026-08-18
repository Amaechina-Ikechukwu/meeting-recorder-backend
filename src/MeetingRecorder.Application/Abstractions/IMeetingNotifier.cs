using MeetingRecorder.Domain.Entities;
using MeetingRecorder.Domain.Enums;

namespace MeetingRecorder.Application.Abstractions;

/// <summary>Pushes real-time updates to clients subscribed to a meeting's SignalR group.</summary>
public interface IMeetingNotifier
{
    Task NotifyStatusChangedAsync(string meetingId, MeetingStatus status, CancellationToken ct = default);

    /// <summary>
    /// Pushes a whole transcript at once. Segments only ever become available together —
    /// batch transcription returns the entire meeting in one response — so sending them one
    /// per hub message added a round trip per segment between a finished transcript and a
    /// meeting the user can read.
    /// </summary>
    Task NotifyTranscriptSegmentsReadyAsync(string meetingId, IReadOnlyList<TranscriptSegment> segments, CancellationToken ct = default);
}
