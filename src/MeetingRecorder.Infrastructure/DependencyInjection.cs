using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Google.Cloud.Storage.V1;
using MeetingRecorder.Application.Abstractions;
using MeetingRecorder.Infrastructure.Auth;
using MeetingRecorder.Infrastructure.Export;
using MeetingRecorder.Infrastructure.Messaging;
using MeetingRecorder.Infrastructure.Notifications;
using MeetingRecorder.Infrastructure.Options;
using MeetingRecorder.Infrastructure.Persistence;
using MeetingRecorder.Infrastructure.Realtime;
using MeetingRecorder.Infrastructure.SpeechToText;
using MeetingRecorder.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace MeetingRecorder.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FirestoreOptions>(configuration.GetSection(FirestoreOptions.SectionName));
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.Configure<DeepgramOptions>(configuration.GetSection(DeepgramOptions.SectionName));
        services.Configure<ZeptoMailOptions>(configuration.GetSection(ZeptoMailOptions.SectionName));

        var credential = GoogleCredential.GetApplicationDefault();
        services.AddSingleton(credential);

        var firestoreProjectId = configuration[$"{FirestoreOptions.SectionName}:ProjectId"]
            ?? throw new InvalidOperationException("Firestore:ProjectId is not configured.");
        services.AddSingleton(new FirestoreDbBuilder { ProjectId = firestoreProjectId, Credential = credential }.Build());
        services.AddSingleton(StorageClient.Create(credential));

        if (FirebaseApp.DefaultInstance is null)
        {
            FirebaseApp.Create(new AppOptions { Credential = credential });
        }
        services.AddSingleton(FirebaseAuth.DefaultInstance);

        services.AddSingleton<IConnection>(sp =>
            RabbitMqConnectionFactory.CreateAsync(sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<RabbitMqOptions>>())
                .GetAwaiter().GetResult());

        services.AddHttpClient<DeepgramClient>((sp, client) =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<DeepgramOptions>>().Value;
            client.BaseAddress = new Uri(opts.BaseUrl);
        });
        services.AddHttpClient<ZeptoMailEmailSender>((sp, client) =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ZeptoMailOptions>>().Value;
            client.BaseAddress = new Uri(opts.BaseUrl);
        });

        services.AddScoped<IMeetingRepository, FirestoreMeetingRepository>();
        services.AddScoped<IRecordingRepository, FirestoreRecordingRepository>();
        services.AddScoped<ITranscriptRepository, FirestoreTranscriptRepository>();
        services.AddScoped<ISpeakerRepository, FirestoreSpeakerRepository>();

        services.AddScoped<IRecordingStorage, GcsRecordingStorage>();
        services.AddScoped<IMessagePublisher, RabbitMqMessagePublisher>();
        services.AddScoped<IUserDirectory, FirebaseUserDirectory>();
        services.AddScoped<INotificationEmailSender, ZeptoMailEmailSender>();
        services.AddScoped<IDocxExporter, OpenXmlDocxExporter>();

        services.AddScoped<ITranscriptionEngine, DeepgramTranscriptionEngine>();
        services.AddScoped<IMeetingNotifier, MeetingNotifier>();

        return services;
    }

    /// <summary>
    /// Registers SignalR plus IMeetingNotifier. Call from both the Api host (which also
    /// maps MeetingHub) and the Workers host (which only ever pushes, never accepts
    /// connections).
    /// </summary>
    /// <remarks>
    /// There is no backplane. Each host gets its own in-memory SignalR, so a push raised
    /// in the Workers host does not reach browsers connected to the Api host; the client
    /// polls meeting status as its fallback for that.
    /// </remarks>
    public static IServiceCollection AddSignalRRealtime(this IServiceCollection services)
    {
        services.AddSignalR();
        return services;
    }
}
