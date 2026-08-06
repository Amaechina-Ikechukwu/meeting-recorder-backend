namespace MeetingRecorder.Application.Abstractions;

/// <summary>Resolves profile info (e.g. email) for a Firebase Auth UID.</summary>
public interface IUserDirectory
{
    Task<string?> GetEmailAsync(string uid, CancellationToken ct = default);
}
