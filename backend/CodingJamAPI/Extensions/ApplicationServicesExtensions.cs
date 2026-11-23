using CodingJamAPI.Application.Abstractions;
using CodingJamAPI.Application.Services;
using CodingJamAPI.Infrastructure.Auth;
using CodingJamAPI.Infrastructure.Realtime;
using CodingJamAPI.Infrastructure.Storage;
using CodingJamAPI.Options;
using Microsoft.AspNetCore.SignalR;

namespace CodingJamAPI.Extensions;

public static class ApplicationServicesExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AppOptions>(configuration.GetSection(AppOptions.SectionName));

        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<IAuthCookieService, AuthCookieService>();
        services.AddSingleton<IEmailSender, LoggingEmailSender>();

        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));
        services.AddSingleton<IFileStorage, LocalFileStorage>();

        services.Configure<PresenceOptions>(configuration.GetSection(PresenceOptions.SectionName));
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IPresenceTracker, PresenceTracker>();
        services.AddHostedService<PresenceSweeperService>();

        services.AddSingleton<IUserIdProvider, SubUserIdProvider>();
        services.AddSingleton<IRealtimeNotifier, SignalRNotifier>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IFriendService, FriendService>();
        services.AddScoped<IBanService, BanService>();
        services.AddScoped<IChatAccessService, ChatAccessService>();
        services.AddScoped<IPersonalChatService, PersonalChatService>();
        services.AddScoped<IMessageService, MessageService>();
        services.AddScoped<IAttachmentService, AttachmentService>();
        services.AddScoped<IRoomService, RoomService>();
        services.AddScoped<IPresenceAudienceService, PresenceAudienceService>();
        return services;
    }
}
