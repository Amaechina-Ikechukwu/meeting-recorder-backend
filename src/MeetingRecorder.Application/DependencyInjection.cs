using MeetingRecorder.Application.UseCases.Meetings;
using MeetingRecorder.Application.UseCases.Processing;
using MeetingRecorder.Application.UseCases.Recordings;
using MeetingRecorder.Application.UseCases.Speakers;
using MeetingRecorder.Application.UseCases.Transcripts;
using Microsoft.Extensions.DependencyInjection;

namespace MeetingRecorder.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateMeeting>();
        services.AddScoped<GetMeeting>();
        services.AddScoped<GetMeetingStatus>();
        services.AddScoped<RetryMeeting>();

        services.AddScoped<UploadRecording>();
        services.AddScoped<GetUploadUrl>();
        services.AddScoped<CompleteUpload>();
        services.AddScoped<GetAudioUrl>();

        services.AddScoped<GetMeetingTranscript>();
        services.AddScoped<SearchTranscript>();
        services.AddScoped<ExportTranscript>();

        services.AddScoped<RenameSpeaker>();

        services.AddScoped<ProcessRecordingUploaded>();
        services.AddScoped<ProcessDiarization>();
        services.AddScoped<MergeTranscriptAndDiarization>();
        services.AddScoped<ProcessMeetingReady>();
        services.AddScoped<MarkMeetingFailed>();

        return services;
    }
}
