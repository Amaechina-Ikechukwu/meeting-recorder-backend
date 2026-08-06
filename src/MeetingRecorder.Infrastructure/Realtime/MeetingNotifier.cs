using MeetingRecorder.Application.Abstractions;
using MeetingRecorder.Domain.Entities;
using MeetingRecorder.Domain.Enums;
using Microsoft.AspNetCore.SignalR;

namespace MeetingRecorder.Infrastructure.Realtime;

/// <summary>
/// Pushes updates to the "meeting:{id}" SignalR group. Registered in both the Api and
/// Workers hosts; with a shared Redis backplane a Worker-raised event reaches clients
/// connected to a different Api instance (see DependencyInjection.AddSignalRRealtime).
/// </summary>
public class MeetingNotifier(IHubContext<MeetingHub> hubContext) : IMeetingNotifier
{
    public Task NotifyStatusChangedAsync(string meetingId, MeetingStatus status, CancellationToken ct = default) =>
        hubContext.Clients.Group(MeetingHub.GroupName(meetingId))
            .SendAsync("meetingStatusChanged", new { meetingId, status = status.ToString() }, ct);

    public Task NotifyTranscriptSegmentReadyAsync(string meetingId, TranscriptSegment segment, CancellationToken ct = default) =>
        hubContext.Clients.Group(MeetingHub.GroupName(meetingId))
            .SendAsync("transcriptSegmentReady", segment, ct);
}
