using MeetingRecorder.Application.Abstractions;
using MeetingRecorder.Application.Dtos;
using MeetingRecorder.Application.UseCases.Recordings;

namespace MeetingRecorder.Api.Endpoints;

public static class RecordingEndpoints
{
    public static IEndpointRouteBuilder MapRecordingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/meetings/{meetingId}/recordings").WithTags("Recordings");

        group.MapPost("/upload-url", async (
            string meetingId, GetUploadUrlRequest request, GetUploadUrl useCase, ICurrentUserAccessor user, CancellationToken ct) =>
                Results.Ok(await useCase.ExecuteAsync(user.UserId, meetingId, request, ct)))
            .WithName("GetRecordingUploadUrl");

        group.MapPost("/{recordingId}/complete", async (
            string meetingId, string recordingId, CompleteUploadRequest request,
            CompleteUpload useCase, ICurrentUserAccessor user, CancellationToken ct) =>
            {
                await useCase.ExecuteAsync(user.UserId, meetingId, recordingId, request, ct);
                return Results.NoContent();
            })
            .WithName("CompleteRecordingUpload");

        group.MapGet("/{recordingId}/audio-url", async (
            string meetingId, string recordingId, GetAudioUrl useCase, ICurrentUserAccessor user, CancellationToken ct) =>
                Results.Ok(await useCase.ExecuteAsync(user.UserId, meetingId, recordingId, ct)))
            .WithName("GetAudioUrl");

        return app;
    }
}
