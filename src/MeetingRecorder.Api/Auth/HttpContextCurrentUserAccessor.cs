using MeetingRecorder.Application.Abstractions;

namespace MeetingRecorder.Api.Auth;

public class HttpContextCurrentUserAccessor(IHttpContextAccessor httpContextAccessor) : ICurrentUserAccessor
{
    public string UserId =>
        httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value
        ?? "dev-user";
}
