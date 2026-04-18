using ConnectHub.MessageService.DTOs;

namespace ConnectHub.MessageService.Interfaces
{
    /// <summary>
    /// 📋 MESSAGE SERVICE INTERFACE - Contract for message operations
    /// 
    /// WHAT is an Interface?
    /// - A contract that says "any class implementing me MUST have these methods"
    /// - Like a promise - "I guarantee these methods exist"
    /// 
    /// WHY use Interfaces?
    /// 1. Testability - Can create fake versions for testing
    /// 2. Flexibility - Can swap implementations
    /// 3. Clean Architecture - Separates WHAT from HOW
    /// </summary>
    public interface IMessageService
    {
        /// <summary>Send a message from one user to another</summary>
        Task<MessageResponse> SendMessageAsync(Guid senderId, SendMessageRequest request);

        /// <summary>Get conversation history between two users</summary>
        Task<ConversationResponse> GetConversationAsync(Guid userId, Guid otherUserId);

        /// <summary>Edit an existing message (only sender can edit)</summary>
        Task<MessageResponse> EditMessageAsync(Guid userId, Guid messageId, EditMessageRequest request);

        /// <summary>Soft delete a message (both users can delete)</summary>
        Task<bool> DeleteMessageAsync(Guid userId, Guid messageId);

        /// <summary>Mark a message as read (only receiver can mark)</summary>
        Task<MessageResponse> MarkAsReadAsync(Guid userId, Guid messageId);

        /// <summary>Search messages by content</summary>
        Task<SearchMessagesResponse> SearchMessagesAsync(Guid userId, string searchTerm);
    }
}