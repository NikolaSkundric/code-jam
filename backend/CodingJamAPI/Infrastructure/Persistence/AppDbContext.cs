using System.Linq.Expressions;
using CodingJamAPI.Domain.Common;
using CodingJamAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CodingJamAPI.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    public DbSet<FriendRequest> FriendRequests => Set<FriendRequest>();
    public DbSet<Friendship> Friendships => Set<Friendship>();
    public DbSet<UserBan> UserBans => Set<UserBan>();

    public DbSet<Chat> Chats => Set<Chat>();
    public DbSet<PersonalChat> PersonalChats => Set<PersonalChat>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<RoomMember> RoomMembers => Set<RoomMember>();
    public DbSet<RoomBan> RoomBans => Set<RoomBan>();
    public DbSet<RoomInvitation> RoomInvitations => Set<RoomInvitation>();

    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<ChatReadState> ChatReadStates => Set<ChatReadState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Global soft-delete filter for every SoftDeletableEntity.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(SoftDeletableEntity).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var isDeleted = Expression.Property(parameter, nameof(SoftDeletableEntity.IsDeleted));
            var filter = Expression.Lambda(Expression.Not(isDeleted), parameter);
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
        }
    }
}
