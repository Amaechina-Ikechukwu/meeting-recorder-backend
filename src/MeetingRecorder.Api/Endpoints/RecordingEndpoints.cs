using MeetingRecorder.Application.Abstractions;
using MeetingRecorder.Application.Dtos;
using MeetingRecorder.Application.UseCases.Recordings;
using Microsoft.AspNetCore.Mvc;

namespace MeetingRecorder.Api.Endpoints;

public static class RecordingEndpoints
{
    public static IEndpointRouteBuilder MapRecordingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/meetings/{meetingId}/recordings").WithTags("Recordings");

        // Upload straight through the API: the client POSTs the audio bytes as the request
        // body with its Content-Type, and the API writes them to storage. Avoids the browser
        // having to PUT cross-origin to GCS, which needs a CORS policy on the bucket.
        group.MapPost("/", async (
            string meetingId, HttpRequest http, [FromQuery] long durationMs, [FromQuery] string? fileExtension,
            UploadRecording useCase, ICurrentUserAccessor user, CancellationToken ct) =>
            {
                var result = await useCase.ExecuteAsync(
                    user.UserId, meetingId, http.Body, http.ContentType ?? "", fileExtension, durationMs, ct);
                return Results.Created($"/meetings/{meetingId}/recordings/{result.RecordingId}", result);
            })
            .WithName("UploadRecording");

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
