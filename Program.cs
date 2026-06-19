using System.Security.Claims;
using System.Text;
using DotNetEnv;
using linksy_backend_api.Core.Interfaces.Repositories;
using linksy_backend_api.Core.Interfaces.Services;
using linksy_backend_api.Domain.Interfaces.Repositories;
using linksy_backend_api.Domain.Interfaces.Services;
using linksy_backend_api.Hubs;
using linksy_backend_api.Infrastructure.Filters;
using linksy_backend_api.Infrastructure.Repositories;
using linksy_backend_api.Infrastructure.Services;
using linksy_backend_api.Models;
using linksy_backend_api.Repositories;
using linksy_backend_api.Repositories.IRepositories;
using linksy_backend_api.Services;
using linksy_backend_api.Services.IServices;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
Env.Load();

var builder = WebApplication.CreateBuilder(args);

// ── Logging ───────────────────────────────────────────────────────────────────
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(
    builder.Environment.IsDevelopment() ? LogLevel.Debug : LogLevel.Information);
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.IncludeScopes = true;
    options.UseUtcTimestamp = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff 'UTC' ";
});

// ── Core services ─────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks();

// ── Swagger ───────────────────────────────────────────────────────────────────
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Linksy API",
        Version = "v1",
        Description = "Linksy Chat Application API with JWT Authentication"
    });
    c.OperationFilter<FileUploadOperationFilter>();
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ── Database ──────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<LinksyDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));

    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// ── JWT ───────────────────────────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

Console.WriteLine($"JWT Config — Key Length: {jwtKey?.Length}, Issuer: {jwtIssuer}, Audience: {jwtAudience}");

if (string.IsNullOrEmpty(jwtKey))
    throw new ArgumentNullException(nameof(jwtKey), "JWT Key is not configured");
if (string.IsNullOrEmpty(jwtIssuer))
    throw new ArgumentNullException(nameof(jwtIssuer), "JWT Issuer is not configured");
if (string.IsNullOrEmpty(jwtAudience))
    throw new ArgumentNullException(nameof(jwtAudience), "JWT Audience is not configured");

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = signingKey,
        ClockSkew = TimeSpan.Zero,
        NameClaimType = ClaimTypes.Name,
        RoleClaimType = ClaimTypes.Role
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // Ưu tiên 1: httpOnly cookie
            var cookieToken = context.Request.Cookies["accessToken"];
            if (!string.IsNullOrEmpty(cookieToken))
            {
                context.Token = cookieToken;
                return Task.CompletedTask;
            }

            // Ưu tiên 2: query string (dành cho SignalR)
            var queryToken = context.Request.Query["access_token"];
            if (!string.IsNullOrEmpty(queryToken) &&
                context.HttpContext.Request.Path.StartsWithSegments("/hubs/chat"))
            {
                context.Token = queryToken;
            }

            return Task.CompletedTask;
        },

        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"[JWT] Authentication failed: {context.Exception.Message}");
            if (context.Exception is SecurityTokenExpiredException)
                context.Response.Headers["Token-Expired"] = "true";
            return Task.CompletedTask;
        },

        OnTokenValidated = context =>
        {
            Console.WriteLine("[JWT] Token validated successfully.");
            return Task.CompletedTask;
        },

        OnChallenge = context =>
        {
            Console.WriteLine($"[JWT] OnChallenge: {context.Error} — {context.ErrorDescription}");
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// ── CORS ──────────────────────────────────────────────────────────────────────
var allowAnyOrigin = builder.Configuration.GetValue<bool>("Cors:AllowAnyOrigin");

// Danh sách origins tĩnh (production + local dev)
var staticOrigins = new[]
{
    "http://localhost:3000",
    "http://localhost:3001",
    "https://linksy-frontend-ashen.vercel.app"
};

// Origins bổ sung từ config / env (dùng cho staging hoặc custom domain)
var extraOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

var allStaticOrigins = staticOrigins.Concat(extraOrigins).ToArray();

// Vercel config
// VercelProjectName : tiền tố tên project,  vd: "linksy-frontend"
// VercelTeamSlug   : phần team trong preview URL, vd: "tinhsnguyeenx281-3273s-projects"
//                    để trống ("") = bỏ qua kiểm tra team (cho phép mọi team có cùng project name)
var vercelProjectName = builder.Configuration.GetValue<string>("Cors:VercelProjectName")
                        ?? "linksy-frontend";
var vercelTeamSlug = builder.Configuration.GetValue<string>("Cors:VercelTeamSlug")
                     ?? "";   // vd: "tinhsnguyeenx281-3273s-projects"

// Vercel preview URL patterns:
//   1. Team-scoped  : <project>-<git-hash>-<team-slug>.vercel.app
//   2. Personal     : <project>-<git-hash>-<username>.vercel.app
//   3. Alias tĩnh   : <project>-git-<branch>-<team-slug>.vercel.app
//
// Chiến lược: host phải bắt đầu bằng <projectName>- VÀ kết thúc bằng .vercel.app
// Nếu vercelTeamSlug được chỉ định → kiểm tra thêm phần team.
static bool IsVercelPreviewUrl(string origin, string projectName, string teamSlug)
{
    try
    {
        var uri = new Uri(origin);
        if (uri.Scheme != "https") return false;

        var host = uri.Host;
        if (!host.EndsWith(".vercel.app", StringComparison.OrdinalIgnoreCase)) return false;

        // Host phải bắt đầu bằng "<projectName>-"
        var prefix = projectName + "-";
        if (!host.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return false;

        // Nếu có team slug → host phải kết thúc bằng "-<teamSlug>.vercel.app"
        if (!string.IsNullOrEmpty(teamSlug))
        {
            var teamSuffix = "-" + teamSlug + ".vercel.app";
            return host.EndsWith(teamSuffix, StringComparison.OrdinalIgnoreCase);
        }

        return true;
    }
    catch
    {
        return false;
    }
}

Console.WriteLine($"[CORS] AllowAnyOrigin={allowAnyOrigin}, StaticOrigins={allStaticOrigins.Length}");
Console.WriteLine($"[CORS] VercelProject={vercelProjectName}, VercelTeam={vercelTeamSlug}");

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        if (allowAnyOrigin)
        {
            // Dev only — cho phép mọi origin
            policy.SetIsOriginAllowed(_ => true)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();

            Console.WriteLine("[CORS] Mode: AllowAnyOrigin (development)");
        }
        else
        {
            // Production — chỉ cho phép origins đã biết + Vercel preview URLs
            policy.SetIsOriginAllowed(origin =>
                  {
                      if (allStaticOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
                          return true;

                      if (IsVercelPreviewUrl(origin, vercelProjectName, vercelTeamSlug))
                      {
                          Console.WriteLine($"[CORS] Allowed Vercel preview: {origin}");
                          return true;
                      }

                      Console.WriteLine($"[CORS] Blocked origin: {origin}");
                      return false;
                  })
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();

            Console.WriteLine($"[CORS] Mode: Restricted — allowed static origins: {string.Join(", ", allStaticOrigins)}");
        }
    });
});

// ── SignalR ───────────────────────────────────────────────────────────────────
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});

// ── Repository Pattern ────────────────────────────────────────────────────────
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<UnitOfWork>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IChatroomRepository, ChatroomRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IFriendshipRepository, FriendshipRepository>();
builder.Services.AddScoped<IFriendRequestRepository, FriendRequestRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IBlockedUserRepository, BlockedUserRepository>();
builder.Services.AddScoped<IGroupInvitationRepository, GroupInvitationRepository>();
builder.Services.AddScoped<IMemberPermissionRepository, MemberPermissionRepository>();
builder.Services.AddScoped<IMessageReactionRepository, MessageReactionRepository>();
builder.Services.AddScoped<IMessageDeliveryRepository, MessageDeliveryRepository>();
builder.Services.AddScoped<IMessageAttachmentRepository, MessageAttachmentRepository>();
builder.Services.AddScoped<IUserSettingsRepository, UserSettingsRepository>();
builder.Services.AddScoped<INotificationSettingsRepository, NotificationSettingsRepository>();
builder.Services.AddScoped<IPrivacySettingsRepository, PrivacySettingsRepository>();
builder.Services.AddScoped<IUserStatusRepository, UserStatusRepository>();
builder.Services.AddScoped<ITokenRepository, TokenRepository>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddSingleton<IConnectionManager, ConnectionManager>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IChatroomService, ChatroomService>();
builder.Services.AddScoped<IFriendService, FriendService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IBlockedService, BlockedService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IGroupInvitationService, GroupInvitationService>();
builder.Services.AddScoped<IChatroomAccessService, ChatroomAccessService>();
builder.Services.AddScoped<IMemberPermissionService, MemberPermissionService>();
builder.Services.AddScoped<IReactionService, ReactionService>();
builder.Services.AddScoped<IUserSettingsService, UserSettingsService>();
builder.Services.AddHttpClient();
builder.Services.AddDirectoryBrowser();

// ── Cache ─────────────────────────────────────────────────────────────────────
builder.Services.AddMemoryCache();

var redisEnabled = builder.Configuration.GetValue<bool>("Redis:Enabled", true);
var redisConn = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING");

if (redisEnabled && !string.IsNullOrEmpty(redisConn))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConn;
        options.InstanceName = Environment.GetEnvironmentVariable("REDIS_INSTANCE_NAME") ?? "linksy:";
    });
    Console.WriteLine("✅ Redis cache enabled");
}
else
{
    builder.Services.AddDistributedMemoryCache();
    Console.WriteLine("⚠️  Redis disabled — using in-memory cache");
}

builder.Services.AddScoped<ICacheService, CacheService>();

// ── Response compression ──────────────────────────────────────────────────────
builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" });
});

// ── Route options ─────────────────────────────────────────────────────────────
builder.Services.Configure<RouteOptions>(options =>
{
    options.LowercaseUrls = true;
});

// ═════════════════════════════════════════════════════════════════════════════
var app = builder.Build();
// ═════════════════════════════════════════════════════════════════════════════

// ── CORS debug middleware (chỉ chạy khi không phải Production) ────────────────
if (!app.Environment.IsProduction())
{
    app.Use(async (context, next) =>
    {
        var origin = context.Request.Headers["Origin"].ToString();
        if (!string.IsNullOrEmpty(origin))
            Console.WriteLine($"[CORS-DEBUG] Incoming request from origin: {origin} → {context.Request.Method} {context.Request.Path}");
        await next();
    });
}

// ── Swagger ───────────────────────────────────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Linksy API V1");
    c.RoutePrefix = string.Empty;
});

// ── Middleware pipeline (thứ tự quan trọng) ───────────────────────────────────
app.UseResponseCompression();
app.UseRouting();
app.UseCors("Frontend");          // ← phải sau UseRouting, trước UseAuthentication
app.UseAuthentication();
app.UseAuthorization();

// ── Endpoints ─────────────────────────────────────────────────────────────────
app.MapHealthChecks("/health");
app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

app.Run();