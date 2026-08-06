using FirebaseAdmin.Auth;
using MeetingRecorder.Application.Abstractions;

namespace MeetingRecorder.Infrastructure.Auth;

public class FirebaseUserDirectory(FirebaseAuth firebaseAuth) : IUserDirectory
{
    public async Task<string?> GetEmailAsync(string uid, CancellationToken ct = default)
    {
        try
        {
            var user = await firebaseAuth.GetUserAsync(uid);
            return user.Email;
        }
        catch (FirebaseAuthException)
        {
            return null;
        }
    }
}
