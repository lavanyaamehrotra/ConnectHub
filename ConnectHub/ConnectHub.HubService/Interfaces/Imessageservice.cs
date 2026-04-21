namespace ConnectHub.HubService.Interfaces
{
    // ============================================================
    // FROM CLASS DIAGRAM (ChatHub dependencies):
    // _messageService → IMessageService
    //
    // HubService does NOT own the DB. It delegates persistence to
    // MessageService via HTTP. This interface abstracts that call
    // so ChatHub does not depend on raw HttpClient.
    // ============================================================
    public interface IMessageService
    {
        /// <summary>
        /// Persist a direct message via MessageService REST API.
        /// Returns the saved message payload (with MessageId, SentAt, etc.)
        /// </summary>
        Task<object> SendMessageAsync(Guid senderId, Guid receiverId, string content, string? token = null);

        /// <summary>
        /// Mark a message as read (updates IsRead=true, ReadAt=now).
        /// </summary>
        Task MarkAsReadAsync(Guid messageId, string? token = null);
    }
}