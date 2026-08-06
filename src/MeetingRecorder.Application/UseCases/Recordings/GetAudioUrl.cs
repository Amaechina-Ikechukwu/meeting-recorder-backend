using MeetingRecorder.Application.Abstractions;
using MeetingRecorder.Application.Dtos;
using MeetingRecorder.Application.Exceptions;
using MeetingRecorder.Domain.Entities;

namespace MeetingRecorder.Application.UseCases.Recordings;

public class GetAudioUrl(IMeetingRepository meetings, IRecordingRepository recordings, IRecordingStorage storage)
{
    private static readonly TimeSpan LinkLifetime = TimeSpan.FromMinutes(15);

    public async Task<AudioUrlResponse> ExecuteAsync(
        string requesterId, string meetingId, string recordingId, CancellationToken ct = default)
    {
        var meeting = await meetings.GetByIdAsync(meetingId, ct)
            ?? throw new NotFoundException(nameof(Meeting), meetingId);

        if (meeting.OwnerId != requesterId)
            throw new ForbiddenAccessException();

        var recording = await recordings.GetByIdAsync(meetingId, recordingId, ct)
            ?? throw new NotFoundException(nameof(Recording), recordingId);

        var url = await storage.CreatePlaybackUrlAsync(recording.StorageKey, LinkLifetime, ct);
        return new AudioUrlResponse(url, DateTimeOffset.UtcNow.Add(LinkLifetime));
    }
}
