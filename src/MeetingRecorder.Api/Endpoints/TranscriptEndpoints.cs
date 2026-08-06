using MeetingRecorder.Application.Abstractions;
using MeetingRecorder.Application.Dtos;
using MeetingRecorder.Application.UseCases.Transcripts;
using Microsoft.AspNetCore.Mvc;

namespace MeetingRecorder.Api.Endpoints;

public static class TranscriptEndpoints
{
    public static IEndpointRouteBuilder MapTranscriptEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/meetings/{meetingId}/transcript").RequireAuthorization().WithTags("Transcripts");

        group.MapGet("/", async (
            string meetingId, GetMeetingTranscript useCase, ICurrentUserAccessor user, CancellationToken ct) =>
                Results.Ok(await useCase.ExecuteAsync(user.UserId, meetingId, ct)))
            .WithName("GetMeetingTranscript");

        group.MapGet("/search", async (
            string meetingId, [FromQuery] string q, SearchTranscript useCase, ICurrentUserAccessor user, CancellationToken ct) =>
                Results.Ok(await useCase.ExecuteAsync(user.UserId, meetingId, q, ct)))
            .WithName("SearchTranscript");

        app.MapGet("/meetings/{meetingId}/export", async (
            string meetingId, [FromQuery] ExportFormat format,
            ExportTranscript useCase, ICurrentUserAccessor user, CancellationToken ct) =>
            {
                var result = await useCase.ExecuteAsync(user.UserId, meetingId, format, ct);
                return Results.File(result.Content, result.ContentType, result.FileName);
            })
            .RequireAuthorization()
            .WithTags("Transcripts")
            .WithName("ExportTranscript");

        return app;
    }
}
