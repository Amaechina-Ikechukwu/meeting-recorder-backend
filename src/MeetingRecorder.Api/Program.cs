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

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddSignalRRealtime(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserAccessor, HttpContextCurrentUserAccessor>();

builder.Services.AddExceptionHandler<AppExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
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
