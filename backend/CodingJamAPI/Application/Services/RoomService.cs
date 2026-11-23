using CodingJamAPI.Application.Abstractions;
using CodingJamAPI.Contracts.Requests;
using CodingJamAPI.Contracts.Responses;
using CodingJamAPI.Domain.Entities;
using CodingJamAPI.Domain.Enums;
using CodingJamAPI.Domain.Exceptions;
using CodingJamAPI.Infrastructure.Persistence;
using CodingJamAPI.Infrastructure.Realtime;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CodingJamAPI.Application.Services;

public sealed class RoomService(
    AppDbContext db,
    IRealtimeNotifier notifier,
    IFileStorage storage,
    IBanService userBans) : IRoomService
{
    /// <summary>TASK.md 3.1: a single room may contain up to 1000 participants.</summary>
    private const int MaxMembers = 1000;
    private const int CatalogLimit = 100;

    // ---------- create / read ----------

    public async Task<RoomDetailsResponse> CreateAsync(Guid userId, CreateRoomRequest request, CancellationToken ct)
    {
        var name = request.Name.Trim();
        await EnsureNameFreeAsync(name, exceptRoomId: null, ct);

        var now = DateTime.UtcNow;
        var roomId = Guid.NewGuid();
        db.Rooms.Add(new Room
        {
            Id = roomId,
            Name = name,
            Description = request.Description?.Trim() ?? string.Empty,
            Visibility = request.Visibility,
            OwnerId = userId,
            CreatedAt = now,
            Chat = new Chat { Id = roomId, Type = ChatType.Room, CreatedAt = now },
            Members = { new RoomMember { UserId = userId, Role = RoomRole.Owner, JoinedAt = now } }
        });

        await SaveRoomAsync(ct);
        await notifier.NotifyAsync([userId], RealtimeEvents.RoomsChanged, roomId, ct);
        return await GetAsync(userId, roomId, ct);
    }

    public async Task<IReadOnlyList<RoomSummaryResponse>> GetMyRoomsAsync(Guid userId, CancellationToken ct)
    {
        var rooms = await (
            from m in db.RoomMembers.AsNoTracking()
            where m.UserId == userId
            join r in db.Rooms.AsNoTracking() on m.RoomId equals r.Id
            select new { r.Id, r.Name, r.Description, r.Visibility, m.Role }).ToListAsync(ct);
        if (rooms.Count == 0)
        {
            return [];
        }

        var ids = rooms.Select(r => r.Id).ToList();
        var counts = await db.RoomMembers.AsNoTracking()
            .Where(m => ids.Contains(m.RoomId))
            .GroupBy(m => m.RoomId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

        // Unread = other people's messages newer than the user's read marker for that room.
        var unread = await db.Messages.AsNoTracking()
            .Where(m => ids.Contains(m.ChatId) && m.AuthorId != userId)
            .Where(m => !db.ChatReadStates.Any(rs => rs.ChatId == m.ChatId && rs.UserId == userId && rs.LastReadAt >= m.CreatedAt))
            .GroupBy(m => m.ChatId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

        return rooms
            .OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .Select(r => new RoomSummaryResponse(
                r.Id, r.Name, r.Description, r.Visibility,
                counts.GetValueOrDefault(r.Id), r.Role, unread.GetValueOrDefault(r.Id)))
            .ToList();
    }

    public async Task<IReadOnlyList<RoomCatalogItemResponse>> GetCatalogAsync(Guid userId, string? search, CancellationToken ct)
    {
        var query = db.Rooms.AsNoTracking().Where(r => r.Visibility == RoomVisibility.Public);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = "%" + search.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            query = query.Where(r => EF.Functions.ILike(r.Name, pattern) || EF.Functions.ILike(r.Description, pattern));
        }

        var rooms = await query.OrderBy(r => r.Name).Take(CatalogLimit)
            .Select(r => new { r.Id, r.Name, r.Description })
            .ToListAsync(ct);
        var ids = rooms.Select(r => r.Id).ToList();

        var counts = await db.RoomMembers.AsNoTracking()
            .Where(m => ids.Contains(m.RoomId))
            .GroupBy(m => m.RoomId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);
        var mine = (await db.RoomMembers.AsNoTracking()
            .Where(m => m.UserId == userId && ids.Contains(m.RoomId))
            .Select(m => m.RoomId)
            .ToListAsync(ct)).ToHashSet();

        return rooms
            .Select(r => new RoomCatalogItemResponse(r.Id, r.Name, r.Description, counts.GetValueOrDefault(r.Id), mine.Contains(r.Id)))
            .ToList();
    }

    public async Task<RoomDetailsResponse> GetAsync(Guid userId, Guid roomId, CancellationToken ct)
    {
        var myRole = (await FindMemberAsync(roomId, userId, ct))?.Role
                     ?? throw new NotFoundException("Room not found.");
        var room = await db.Rooms.AsNoTracking().FirstOrDefaultAsync(r => r.Id == roomId, ct)
                   ?? throw new NotFoundException("Room not found.");

        var members = await (
            from m in db.RoomMembers.AsNoTracking()
            where m.RoomId == roomId
            join u in db.Users.AsNoTracking() on m.UserId equals u.Id
            select new RoomMemberResponse(u.Id, u.Username, m.Role, m.JoinedAt)).ToListAsync(ct);

        var owner = members.FirstOrDefault(m => m.UserId == room.OwnerId);
        var ordered = members
            .OrderByDescending(m => m.Role)
            .ThenBy(m => m.Username, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return new RoomDetailsResponse(room.Id, room.Name, room.Description, room.Visibility,
            room.OwnerId, owner?.Username ?? "unknown", myRole, ordered);
    }

    // ---------- settings / deletion ----------

    public async Task<RoomDetailsResponse> UpdateAsync(Guid userId, Guid roomId, UpdateRoomRequest request, CancellationToken ct)
    {
        var room = await RequireOwnerAsync(userId, roomId, ct);
        var name = request.Name.Trim();
        await EnsureNameFreeAsync(name, roomId, ct);

        room.Name = name;
        room.Description = request.Description?.Trim() ?? string.Empty;
        room.Visibility = request.Visibility;
        await SaveRoomAsync(ct);

        await notifier.NotifyAsync(await MemberIdsAsync(roomId, ct), RealtimeEvents.RoomsChanged, roomId, ct);
        return await GetAsync(userId, roomId, ct);
    }

    public async Task DeleteAsync(Guid userId, Guid roomId, CancellationToken ct)
    {
        await RequireOwnerAsync(userId, roomId, ct);
        var memberIds = await MemberIdsAsync(roomId, ct);

        // TASK.md 2.4.6: everything in the room is deleted permanently. Deleting the Chat row cascades to
        // the room, members, bans, invitations, messages (including soft deleted ones) and attachments.
        await db.Chats.Where(c => c.Id == roomId).ExecuteDeleteAsync(ct);
        storage.DeleteFolder(roomId.ToString("N"));

        await notifier.NotifyAsync(memberIds, RealtimeEvents.RoomsChanged, roomId, ct);
    }

    // ---------- joining / leaving ----------

    public async Task JoinAsync(Guid userId, Guid roomId, CancellationToken ct)
    {
        // Private rooms are only joinable by invitation and are not revealed here.
        var room = await db.Rooms.AsNoTracking().FirstOrDefaultAsync(r => r.Id == roomId && r.Visibility == RoomVisibility.Public, ct)
                   ?? throw new NotFoundException("Room not found.");

        if (await FindMemberAsync(roomId, userId, ct) is not null)
        {
            return;
        }
        await AddMemberAsync(room.Id, userId, ct);
    }

    public async Task LeaveAsync(Guid userId, Guid roomId, CancellationToken ct)
    {
        var member = await FindMemberAsync(roomId, userId, ct) ?? throw new NotFoundException("Room not found.");
        if (member.Role == RoomRole.Owner)
        {
            throw new ValidationFailedException("room", "The owner cannot leave the room. Delete the room instead.");
        }

        var others = await MemberIdsAsync(roomId, ct);
        db.RoomMembers.Remove(member); // soft delete
        await db.SaveChangesAsync(ct);
        await notifier.NotifyAsync(others, RealtimeEvents.RoomsChanged, roomId, ct);
    }

    // ---------- roles / bans ----------

    public async Task SetAdminAsync(Guid userId, Guid roomId, Guid targetUserId, bool isAdmin, CancellationToken ct)
    {
        // Owner and admins may both promote and demote, but never the owner.
        await RequireAdminAsync(userId, roomId, ct);
        var target = await FindMemberAsync(roomId, targetUserId, ct) ?? throw new NotFoundException("Member not found.");

        if (target.Role == RoomRole.Owner)
        {
            throw new ForbiddenException("The owner is always an admin and cannot lose admin privileges.");
        }

        target.Role = isAdmin ? RoomRole.Admin : RoomRole.Member;
        await db.SaveChangesAsync(ct);
        await notifier.NotifyAsync(await MemberIdsAsync(roomId, ct), RealtimeEvents.RoomsChanged, roomId, ct);
    }

    /// <summary>Removing a member from a room is a ban (TASK.md 2.4.8): they cannot rejoin until unbanned.</summary>
    public async Task BanMemberAsync(Guid userId, Guid roomId, Guid targetUserId, CancellationToken ct)
    {
        var actor = await RequireAdminAsync(userId, roomId, ct);
        if (targetUserId == userId)
        {
            throw new ValidationFailedException("userId", "You cannot ban yourself.");
        }

        var target = await FindMemberAsync(roomId, targetUserId, ct) ?? throw new NotFoundException("Member not found.");
        if (target.Role == RoomRole.Owner)
        {
            throw new ForbiddenException("The owner cannot be removed from the room.");
        }
        if (target.Role == RoomRole.Admin && actor.Role != RoomRole.Owner)
        {
            throw new ForbiddenException("Only the owner can remove an admin.");
        }

        var audience = await MemberIdsAsync(roomId, ct);
        db.RoomMembers.Remove(target); // soft delete
        db.RoomBans.Add(new RoomBan { RoomId = roomId, UserId = targetUserId, BannedById = userId, CreatedAt = DateTime.UtcNow });
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ValidationFailedException("userId", "This user is already banned.");
        }

        await notifier.NotifyAsync(audience, RealtimeEvents.RoomsChanged, roomId, ct);
    }

    public async Task<IReadOnlyList<RoomBanResponse>> GetBansAsync(Guid userId, Guid roomId, CancellationToken ct)
    {
        await RequireAdminAsync(userId, roomId, ct);
        var bans = await db.RoomBans.AsNoTracking().Where(b => b.RoomId == roomId).ToListAsync(ct);

        var ids = bans.Select(b => b.UserId).Concat(bans.Select(b => b.BannedById)).Distinct().ToList();
        var names = await db.Users.IgnoreQueryFilters().AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Username, ct);

        return bans
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new RoomBanResponse(
                b.UserId, names.GetValueOrDefault(b.UserId, "unknown"),
                b.BannedById, names.GetValueOrDefault(b.BannedById, "unknown"), b.CreatedAt))
            .ToList();
    }

    public async Task UnbanAsync(Guid userId, Guid roomId, Guid targetUserId, CancellationToken ct)
    {
        await RequireAdminAsync(userId, roomId, ct);
        var ban = await db.RoomBans.FirstOrDefaultAsync(b => b.RoomId == roomId && b.UserId == targetUserId, ct)
                  ?? throw new NotFoundException("Ban not found.");

        db.RoomBans.Remove(ban); // soft delete
        await db.SaveChangesAsync(ct);
        await notifier.NotifyAsync(await MemberIdsAsync(roomId, ct), RealtimeEvents.RoomsChanged, roomId, ct);
    }

    // ---------- invitations ----------

    public async Task InviteAsync(Guid userId, Guid roomId, InviteToRoomRequest request, CancellationToken ct)
    {
        if (await FindMemberAsync(roomId, userId, ct) is null)
        {
            throw new NotFoundException("Room not found.");
        }

        var username = request.Username.Trim().ToLowerInvariant();
        var target = await db.Users.AsNoTracking()
            .Where(u => u.Username.ToLower() == username)
            .Select(u => new { u.Id })
            .FirstOrDefaultAsync(ct)
            ?? throw new ValidationFailedException("username", "No user with this username.");

        if (target.Id == userId)
        {
            throw new ValidationFailedException("username", "You are already in this room.");
        }
        if (await FindMemberAsync(roomId, target.Id, ct) is not null)
        {
            throw new ValidationFailedException("username", "This user is already a member.");
        }
        if (await db.RoomBans.AnyAsync(b => b.RoomId == roomId && b.UserId == target.Id, ct))
        {
            throw new ValidationFailedException("username", "This user is banned from the room. Unban them first.");
        }
        if (await userBans.IsBlockedAsync(userId, target.Id, ct))
        {
            throw new ValidationFailedException("username", "You cannot invite this user.");
        }
        if (await db.RoomInvitations.AnyAsync(i => i.RoomId == roomId && i.InvitedUserId == target.Id && i.Status == RequestStatus.Pending, ct))
        {
            throw new ValidationFailedException("username", "This user has already been invited.");
        }

        db.RoomInvitations.Add(new RoomInvitation
        {
            RoomId = roomId,
            InvitedUserId = target.Id,
            InvitedById = userId,
            Status = RequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ValidationFailedException("username", "This user has already been invited.");
        }

        await notifier.NotifyAsync([target.Id], RealtimeEvents.RoomsChanged, roomId, ct);
        await notifier.NotifyAsync(await AdminIdsAsync(roomId, ct), RealtimeEvents.RoomsChanged, roomId, ct);
    }

    public async Task<IReadOnlyList<RoomInvitationResponse>> GetRoomInvitationsAsync(Guid userId, Guid roomId, CancellationToken ct)
    {
        await RequireAdminAsync(userId, roomId, ct);
        var invitations = await db.RoomInvitations.AsNoTracking()
            .Where(i => i.RoomId == roomId && i.Status == RequestStatus.Pending)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(ct);

        var names = await LoadNamesAsync(invitations.SelectMany(i => new[] { i.InvitedUserId, i.InvitedById }), ct);
        return invitations
            .Select(i => new RoomInvitationResponse(
                i.Id, i.InvitedUserId, names.GetValueOrDefault(i.InvitedUserId, "unknown"),
                names.GetValueOrDefault(i.InvitedById, "unknown"), i.CreatedAt))
            .ToList();
    }

    public async Task CancelInvitationAsync(Guid userId, Guid roomId, Guid invitationId, CancellationToken ct)
    {
        await RequireAdminAsync(userId, roomId, ct);
        var invitation = await db.RoomInvitations
            .FirstOrDefaultAsync(i => i.Id == invitationId && i.RoomId == roomId && i.Status == RequestStatus.Pending, ct)
            ?? throw new NotFoundException("Invitation not found.");

        db.RoomInvitations.Remove(invitation); // soft delete
        await db.SaveChangesAsync(ct);
        await notifier.NotifyAsync([invitation.InvitedUserId], RealtimeEvents.RoomsChanged, roomId, ct);
        await notifier.NotifyAsync(await AdminIdsAsync(roomId, ct), RealtimeEvents.RoomsChanged, roomId, ct);
    }

    public async Task<IReadOnlyList<IncomingInvitationResponse>> GetMyInvitationsAsync(Guid userId, CancellationToken ct)
    {
        var invitations = await (
            from i in db.RoomInvitations.AsNoTracking()
            where i.InvitedUserId == userId && i.Status == RequestStatus.Pending
            join r in db.Rooms.AsNoTracking() on i.RoomId equals r.Id
            orderby i.CreatedAt descending
            select new { i.Id, i.RoomId, RoomName = r.Name, i.InvitedById, i.CreatedAt }).ToListAsync(ct);

        var names = await LoadNamesAsync(invitations.Select(i => i.InvitedById), ct);
        return invitations
            .Select(i => new IncomingInvitationResponse(
                i.Id, i.RoomId, i.RoomName, names.GetValueOrDefault(i.InvitedById, "unknown"), i.CreatedAt))
            .ToList();
    }

    public async Task AcceptInvitationAsync(Guid userId, Guid invitationId, CancellationToken ct)
    {
        var invitation = await FindMyInvitationAsync(userId, invitationId, ct);
        if (await db.RoomBans.AnyAsync(b => b.RoomId == invitation.RoomId && b.UserId == userId, ct))
        {
            throw new ForbiddenException("You are banned from this room.");
        }

        invitation.Status = RequestStatus.Accepted;
        invitation.RespondedAt = DateTime.UtcNow;

        if (await FindMemberAsync(invitation.RoomId, userId, ct) is null)
        {
            await AddMemberAsync(invitation.RoomId, userId, ct);
        }
        else
        {
            await db.SaveChangesAsync(ct);
        }
        await notifier.NotifyAsync(await AdminIdsAsync(invitation.RoomId, ct), RealtimeEvents.RoomsChanged, invitation.RoomId, ct);
    }

    public async Task DeclineInvitationAsync(Guid userId, Guid invitationId, CancellationToken ct)
    {
        var invitation = await FindMyInvitationAsync(userId, invitationId, ct);
        invitation.Status = RequestStatus.Declined;
        invitation.RespondedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        await notifier.NotifyAsync([userId], RealtimeEvents.RoomsChanged, invitation.RoomId, ct);
        await notifier.NotifyAsync(await AdminIdsAsync(invitation.RoomId, ct), RealtimeEvents.RoomsChanged, invitation.RoomId, ct);
    }

    // ---------- helpers ----------

    private Task<RoomMember?> FindMemberAsync(Guid roomId, Guid userId, CancellationToken ct) =>
        db.RoomMembers.FirstOrDefaultAsync(m => m.RoomId == roomId && m.UserId == userId, ct);

    /// <summary>Members that are admins or the owner see invitation lists; they are told when those change.</summary>
    private Task<List<Guid>> AdminIdsAsync(Guid roomId, CancellationToken ct) =>
        db.RoomMembers.AsNoTracking()
            .Where(m => m.RoomId == roomId && m.Role >= RoomRole.Admin)
            .Select(m => m.UserId)
            .ToListAsync(ct);

    private Task<List<Guid>> MemberIdsAsync(Guid roomId, CancellationToken ct) =>
        db.RoomMembers.AsNoTracking().Where(m => m.RoomId == roomId).Select(m => m.UserId).ToListAsync(ct);

    /// <summary>Member with admin rights (or the owner); other members get 403, non-members 404.</summary>
    private async Task<RoomMember> RequireAdminAsync(Guid userId, Guid roomId, CancellationToken ct)
    {
        var member = await FindMemberAsync(roomId, userId, ct) ?? throw new NotFoundException("Room not found.");
        return member.Role >= RoomRole.Admin ? member : throw new ForbiddenException("Only room admins can do this.");
    }

    private async Task<Room> RequireOwnerAsync(Guid userId, Guid roomId, CancellationToken ct)
    {
        var member = await FindMemberAsync(roomId, userId, ct) ?? throw new NotFoundException("Room not found.");
        if (member.Role != RoomRole.Owner)
        {
            throw new ForbiddenException("Only the room owner can do this.");
        }
        return await db.Rooms.FirstAsync(r => r.Id == roomId, ct);
    }

    private async Task<RoomInvitation> FindMyInvitationAsync(Guid userId, Guid invitationId, CancellationToken ct) =>
        await db.RoomInvitations.FirstOrDefaultAsync(
            i => i.Id == invitationId && i.InvitedUserId == userId && i.Status == RequestStatus.Pending, ct)
        ?? throw new NotFoundException("Invitation not found.");

    private async Task EnsureNameFreeAsync(string name, Guid? exceptRoomId, CancellationToken ct)
    {
        var lower = name.ToLowerInvariant();
        if (await db.Rooms.AnyAsync(r => r.Name.ToLower() == lower && r.Id != exceptRoomId, ct))
        {
            throw new ValidationFailedException("name", "A room with this name already exists.");
        }
    }

    private async Task SaveRoomAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ValidationFailedException("name", "A room with this name already exists.");
        }
    }

    /// <summary>Adds a member (checking the ban list and the size limit) and tells everyone involved.</summary>
    private async Task AddMemberAsync(Guid roomId, Guid userId, CancellationToken ct)
    {
        if (await db.RoomBans.AnyAsync(b => b.RoomId == roomId && b.UserId == userId, ct))
        {
            throw new ForbiddenException("You are banned from this room.");
        }
        if (await db.RoomMembers.CountAsync(m => m.RoomId == roomId, ct) >= MaxMembers)
        {
            throw new ValidationFailedException("room", "This room is full.");
        }

        db.RoomMembers.Add(new RoomMember { RoomId = roomId, UserId = userId, Role = RoomRole.Member, JoinedAt = DateTime.UtcNow });
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return; // joined concurrently: already a member
        }

        await notifier.NotifyAsync(await MemberIdsAsync(roomId, ct), RealtimeEvents.RoomsChanged, roomId, ct);
    }

    private async Task<Dictionary<Guid, string>> LoadNamesAsync(IEnumerable<Guid> ids, CancellationToken ct)
    {
        var distinct = ids.Distinct().ToList();
        return await db.Users.IgnoreQueryFilters().AsNoTracking()
            .Where(u => distinct.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Username, ct);
    }
}
