namespace MeetingRecorder.Application.Exceptions;

public class NotFoundException(string entity, string id) : Exception($"{entity} '{id}' was not found.");

public class ForbiddenAccessException() : Exception("You do not have access to this resource.");

public class ValidationFailedException(IReadOnlyList<string> errors)
    : Exception(string.Join("; ", errors))
{
    public IReadOnlyList<string> Errors { get; } = errors;
}
