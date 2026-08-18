using MeetingRecorder.Api.Auth;
using MeetingRecorder.Api.Endpoints;
using MeetingRecorder.Api.Middleware;
using MeetingRecorder.Application;
using MeetingRecorder.Application.Abstractions;
using MeetingRecorder.Infrastructure;
using MeetingRecorder.Infrastructure.Realtime;
using Microsoft.Extensions.FileProviders;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Recordings are POSTed as a single request body. Cloud Run caps an HTTP/1 request at
// 32 MiB, so match that instead of Kestrel's lower default and fail at the same point
// the platform would.
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 32L * 1024 * 1024);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddSignalRRealtime();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserAccessor, HttpContextCurrentUserAccessor>();

builder.Services.AddExceptionHandler<AppExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddOpenApi();

// Open to any origin: the client is served from whatever host it is deployed to, so
// pinning an allow-list here only ever blocked it. AllowCredentials is deliberately
// absent -- it cannot be combined with AllowAnyOrigin, and callers authenticate with a
// bearer token rather than a cookie, so nothing depends on it.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseExceptionHandler();

app.UseCors();

var docsPath = Path.Combine(app.Environment.ContentRootPath, "docs");
if (Directory.Exists(docsPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(docsPath),
        RequestPath = ""
    });
}

app.MapOpenApi();
app.MapScalarApiReference();

app.MapMeetingEndpoints();
app.MapRecordingEndpoints();
app.MapTranscriptEndpoints();
app.MapSpeakerEndpoints();

app.MapHub<MeetingHub>("/hubs/meeting");

app.Run();
