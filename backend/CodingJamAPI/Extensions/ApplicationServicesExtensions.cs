using CodingJamAPI.Application.Abstractions;
using CodingJamAPI.Application.Services;
using CodingJamAPI.Infrastructure.Auth;
using CodingJamAPI.Infrastructure.Realtime;
using CodingJamAPI.Infrastructure.Storage;
using CodingJamAPI.Infrastructure.Xmpp;
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
        // Real mail when an SMTP host is configured (docker compose: Mailpit), otherwise the link is only logged.
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        if (!string.IsNullOrWhiteSpace(configuration[$"{EmailOptions.SectionName}:Host"]))
        {
            services.AddSingleton<IEmailSender, SmtpEmailSender>();
        }
        else
        {
            services.AddSingleton<IEmailSender, LoggingEmailSender>();
        }

        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));
        services.AddSingleton<IFileStorage, LocalFileStorage>();

        services.Configure<PresenceOptions>(configuration.GetSection(PresenceOptions.SectionName));
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IPresenceTracker, PresenceTracker>();
        services.AddHostedService<PresenceSweeperService>();

        services.AddSingleton<IUserIdProvider, SubUserIdProvider>();
        services.AddSingleton<IRealtimeNotifier, SignalRNotifier>();

        services.Configure<XmppOptions>(configuration.GetSection(XmppOptions.SectionName));
        services.Configure<AdminOptions>(configuration.GetSection(AdminOptions.SectionName));
        services.AddScoped<IXmppAuthService, XmppAuthService>();
        services.AddHttpClient<IXmppAdminService, EjabberdAdminService>(c => c.Timeout = TimeSpan.FromSeconds(6));

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
