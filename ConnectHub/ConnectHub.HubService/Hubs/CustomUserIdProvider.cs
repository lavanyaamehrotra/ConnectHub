using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace ConnectHub.HubService.Hubs
{
    // ============================================================
    // WHAT IS THIS FILE?
    // Tells SignalR how to extract the userId from JWT claims.
    //
    // WHY NEEDED?
    // Clients.User(userId) needs to know which connections belong
    // to that userId. This class provides that mapping.
    //
    // HOW IT WORKS:
    // Every time a client connects, SignalR calls GetUserId() once.
    // We return the userId string from the JWT NameIdentifier claim.
    // SignalR stores: connectionId → userId
    // Clients.User(userId) then delivers to ALL connections for that user.
    //
    // REGISTERED AS: AddSingleton<IUserIdProvider, CustomUserIdProvider>
    // ============================================================
    public class CustomUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            // NameIdentifier claim was set in AuthService JwtHelper.GenerateToken():
            // new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString())
            return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}
