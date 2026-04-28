namespace ConnectHub.HubService.Interfaces
{
    public interface IAuthServiceClient
    {
        Task UpdatePresenceAsync(Guid userId, bool isOnline, string token);
    }
}
