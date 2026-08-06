namespace MeetingRecorder.Application.Abstractions;

/// <summary>Resolves the authenticated Firebase Auth UID for the current request.</summary>
public interface ICurrentUserAccessor
{
    string UserId { get; }
}
