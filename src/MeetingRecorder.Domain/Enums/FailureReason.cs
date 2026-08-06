namespace MeetingRecorder.Domain.Enums;

public enum FailureReason
{
    None,
    TranscriptionFailed,
    DiarizationFailed,
    StorageError,
    MergeFailed
}
