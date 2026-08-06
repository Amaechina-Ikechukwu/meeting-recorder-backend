namespace MeetingRecorder.Application.Messaging;

public static class QueueNames
{
    public const string RecordingUploaded = "recording.uploaded";
    public const string TranscriptionCompleted = "transcription.completed";
    public const string DiarizationCompleted = "diarization.completed";
    public const string MeetingReady = "meeting.ready";
}

public record RecordingUploadedMessage(string MeetingId, string RecordingId, string StorageKey, string ContentType);

public record TranscriptionCompletedMessage(string MeetingId, string RecordingId);

public record DiarizationCompletedMessage(string MeetingId, string RecordingId);

public record MeetingReadyMessage(string MeetingId);

public record ProcessingFailedMessage(string MeetingId, string RecordingId, string Stage, string Reason);
