using CodingJamAPI.Contracts.Requests;
using CodingJamAPI.Contracts.Responses;

namespace CodingJamAPI.Application.Abstractions;

public interface IFriendService
{
    Task<IReadOnlyList<FriendResponse>> GetFriendsAsync(Guid userId, CancellationToken ct);
    Task RemoveFriendAsync(Guid userId, Guid friendId, CancellationToken ct);

    Task<FriendRequestsResponse> GetRequestsAsync(Guid userId, CancellationToken ct);
    Task SendRequestAsync(Guid userId, SendFriendRequestRequest request, CancellationToken ct);
    Task AcceptRequestAsync(Guid userId, Guid requestId, CancellationToken ct);
    Task DeclineRequestAsync(Guid userId, Guid requestId, CancellationToken ct);
    Task CancelRequestAsync(Guid userId, Guid requestId, CancellationToken ct);
}
