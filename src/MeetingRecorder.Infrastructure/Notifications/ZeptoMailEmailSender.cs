using System.Net.Http.Headers;
using System.Net.Http.Json;
using MeetingRecorder.Application.Abstractions;
using MeetingRecorder.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace MeetingRecorder.Infrastructure.Notifications;

public class ZeptoMailEmailSender(HttpClient httpClient, IOptions<ZeptoMailOptions> options) : INotificationEmailSender
{
    private readonly ZeptoMailOptions _options = options.Value;

    public async Task SendTranscriptReadyEmailAsync(string toEmail, string meetingId, string meetingTitle, CancellationToken ct = default)
    {
        var payload = new
        {
            from = new { address = _options.FromAddress, name = _options.FromName },
            to = new[] { new { email_address = new { address = toEmail } } },
            subject = $"Your transcript for \"{meetingTitle}\" is ready",
            htmlbody = $"<p>Your meeting recording <strong>{meetingTitle}</strong> has finished processing. " +
                       $"Sign in to view the transcript.</p><p>Meeting ID: {meetingId}</p>"
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "v1.1/email") { Content = JsonContent.Create(payload) };
        request.Headers.Authorization = AuthenticationHeaderValue.Parse(_options.ApiKey);

        var response = await httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
    }
}
