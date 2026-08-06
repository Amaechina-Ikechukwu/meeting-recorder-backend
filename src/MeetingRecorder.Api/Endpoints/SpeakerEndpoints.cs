using MeetingRecorder.Application.Abstractions;
using MeetingRecorder.Application.Dtos;
using MeetingRecorder.Application.UseCases.Speakers;

namespace MeetingRecorder.Api.Endpoints;

public static class SpeakerEndpoints
{
    public static IEndpointRouteBuilder MapSpeakerEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPatch("/meetings/{meetingId}/speakers/{speakerId}", async (
            string meetingId, string speakerId, RenameSpeakerRequest request,
            RenameSpeaker useCase, ICurrentUserAccessor user, CancellationToken ct) =>
                Results.Ok(await useCase.ExecuteAsync(user.UserId, meetingId, speakerId, request, ct)))
            .RequireAuthorization()
            .WithTags("Speakers")
            .WithName("RenameSpeaker");

        return app;
    }
}
