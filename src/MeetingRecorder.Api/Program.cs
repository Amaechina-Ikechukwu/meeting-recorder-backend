using MeetingRecorder.Api.Auth;
using MeetingRecorder.Api.Endpoints;
using MeetingRecorder.Api.Middleware;
using MeetingRecorder.Application;
using MeetingRecorder.Application.Abstractions;
using MeetingRecorder.Infrastructure;
using MeetingRecorder.Infrastructure.Realtime;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var firebaseProjectId = builder.Configuration["FirebaseAuth:ProjectId"]
    ?? throw new InvalidOperationException("FirebaseAuth:ProjectId is not configured.");

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddSignalRRealtime(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserAccessor, HttpContextCurrentUserAccessor>();

builder.Services.AddExceptionHandler<AppExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://securetoken.google.com/{firebaseProjectId}";
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = $"https://securetoken.google.com/{firebaseProjectId}",
            ValidAudience = firebaseProjectId,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();
app.MapScalarApiReference();

app.MapMeetingEndpoints();
app.MapRecordingEndpoints();
app.MapTranscriptEndpoints();
app.MapSpeakerEndpoints();

app.MapHub<MeetingHub>("/hubs/meeting").RequireAuthorization();

app.Run();
