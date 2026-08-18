using MeetingRecorder.Application;
using MeetingRecorder.Infrastructure;
using MeetingRecorder.Workers.Consumers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddSignalRRealtime();

builder.Services.AddHostedService<TranscriptionConsumer>();
builder.Services.AddHostedService<DiarizationConsumer>();
builder.Services.AddHostedService<MergeOnTranscriptionCompletedConsumer>();
builder.Services.AddHostedService<MergeOnDiarizationCompletedConsumer>();
builder.Services.AddHostedService<NotificationConsumer>();

var host = builder.Build();
host.Run();
