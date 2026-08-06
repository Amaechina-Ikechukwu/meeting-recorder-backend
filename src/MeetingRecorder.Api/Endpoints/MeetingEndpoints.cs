using MeetingRecorder.Application.Abstractions;
using MeetingRecorder.Application.Dtos;
using MeetingRecorder.Application.UseCases.Meetings;
using Microsoft.AspNetCore.Mvc;

namespace MeetingRecorder.Api.Endpoints;

public static class MeetingEndpoints
{
    public static IEndpointRouteBuilder MapMeetingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/meetings").WithTags("Meetings");

        group.MapPost("/", async (
            CreateMeetingRequest request, CreateMeeting useCase, ICurrentUserAccessor user, CancellationToken ct) =>
            {
                var result = await useCase.ExecuteAsync(user.UserId, request, ct);
                return Results.Created($"/meetings/{result.Id}", result);
            })
            .WithName("CreateMeeting");

        group.MapGet("/{meetingId}", async (
            string meetingId, GetMeeting useCase, ICurrentUserAccessor user, CancellationToken ct) =>
                Results.Ok(await useCase.ExecuteAsync(user.UserId, meetingId, ct)))
            .WithName("GetMeeting");

        group.MapGet("/{meetingId}/status", async (
            string meetingId, GetMeetingStatus useCase, ICurrentUserAccessor user, CancellationToken ct) =>
                Results.Ok(await useCase.ExecuteAsync(user.UserId, meetingId, ct)))
            .WithName("GetMeetingStatus");

        group.MapPost("/{meetingId}/retry", async (
            string meetingId, [FromQuery] string recordingId, RetryMeeting useCase, ICurrentUserAccessor user, CancellationToken ct) =>
            {
                await useCase.ExecuteAsync(user.UserId, meetingId, recordingId, ct);
                return Results.Accepted();
            })
            .WithName("RetryMeeting");

        return app;
    }
}
