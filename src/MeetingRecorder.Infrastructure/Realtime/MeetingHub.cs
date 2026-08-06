using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MeetingRecorder.Infrastructure.Realtime;

[Authorize]
public class MeetingHub : Hub
{
    public async Task JoinMeetingGroup(string meetingId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(meetingId));

    public async Task LeaveMeetingGroup(string meetingId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(meetingId));

    public static string GroupName(string meetingId) => $"meeting:{meetingId}";
}
