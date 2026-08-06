using MeetingRecorder.Api.Auth;
using MeetingRecorder.Api.Endpoints;
using MeetingRecorder.Api.Middleware;
using MeetingRecorder.Application;
using MeetingRecorder.Application.Abstractions;
using MeetingRecorder.Infrastructure;
using MeetingRecorder.Infrastructure.Realtime;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddSignalRRealtime(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserAccessor, HttpContextCurrentUserAccessor>();

builder.Services.AddExceptionHandler<AppExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

app.MapOpenApi();
app.MapScalarApiReference();

app.MapMeetingEndpoints();
app.MapRecordingEndpoints();
app.MapTranscriptEndpoints();
app.MapSpeakerEndpoints();

app.MapHub<MeetingHub>("/hubs/meeting");

app.Run();
