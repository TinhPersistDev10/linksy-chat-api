using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using linksy_backend_api.Core.Interfaces.Repositories;
using linksy_backend_api.Core.Interfaces.Services;
using linksy_backend_api.Domain.Interfaces.Repositories;
using linksy_backend_api.Domain.Interfaces.Services;
using linksy_backend_api.Hubs;
using linksy_backend_api.Domain.Options;
using linksy_backend_api.Infrastructure.Filters;
using linksy_backend_api.Infrastructure.Repositories;
using linksy_backend_api.Infrastructure.Services;
using linksy_backend_api.Models;
using linksy_backend_api.Repositories;
using linksy_backend_api.Repositories.IRepositories;
using linksy_backend_api.Services;
using linksy_backend_api.Services.IServices;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
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

//  Database 
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

//  JWT 
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
            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("Jwt");
            logger.LogWarning(
                context.Exception,
                "JWT authentication failed. Path={Path}, TraceId={TraceId}",
                context.Request.Path,
                context.HttpContext.TraceIdentifier);

            if (context.Exception is SecurityTokenExpiredException)
                context.Response.Headers["Token-Expired"] = "true";
            return Task.CompletedTask;
        },

        OnTokenValidated = async context =>
        {
            var tokenClaims = context.SecurityToken switch
            {
                JwtSecurityToken jwtToken => jwtToken.Claims,
                Microsoft.IdentityModel.JsonWebTokens.JsonWebToken jsonWebToken => jsonWebToken.Claims,
                _ => Enumerable.Empty<Claim>()
            };

            var jti = tokenClaims.FirstOrDefault(claim =>
                    claim.Type == JwtRegisteredClaimNames.Jti)?.Value
                ?? context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Jti)
                ?? context.Principal?.FindFirstValue(ClaimTypes.SerialNumber);
            if (string.IsNullOrWhiteSpace(jti))
            {
                Console.WriteLine("[JWT] Token rejected: missing jti claim.");
                context.Fail("JWT is missing jti claim.");
                return;
            }

            var rawToken = context.SecurityToken switch
            {
                JwtSecurityToken jwtToken => jwtToken.RawData,
                Microsoft.IdentityModel.JsonWebTokens.JsonWebToken jsonWebToken => jsonWebToken.EncodedToken,
                _ => null
            };
            if (string.IsNullOrWhiteSpace(rawToken))
            {
                Console.WriteLine(
                    $"[JWT] Token rejected: raw token unavailable. SecurityTokenType={context.SecurityToken?.GetType().FullName ?? "null"}");
                context.Fail("JWT token payload is unavailable.");
                return;
            }

            var tokenRepository = context.HttpContext.RequestServices
                .GetRequiredService<ITokenRepository>();
            var activeToken = await tokenRepository.GetActiveByTokenAsync(rawToken);

            if (activeToken is null)
            {
                Console.WriteLine("[JWT] Token rejected: token is revoked or not active.");
                context.Fail("JWT token has been revoked or is no longer active.");
                return;
            }

            Console.WriteLine("[JWT] Token validated successfully.");
        },

        OnChallenge = context =>
        {
            Console.WriteLine($"[JWT] OnChallenge: {context.Error} — {context.ErrorDescription}");
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(option =>
{
    option.AddPolicy("Frontend", policy =>
    {
        policy
            .SetIsOriginAllowed(origin =>
            {
                var allowed = allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase);
                return allowed;
            })
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
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
builder.Services.AddScoped<IUserReportService, UserReportService>();
builder.Services.AddScoped<IUserModerationService, UserModerationService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IGroupInvitationService, GroupInvitationService>();
builder.Services.AddScoped<IChatroomAccessService, ChatroomAccessService>();
builder.Services.AddScoped<IDirectContactPrivacyService, DirectContactPrivacyService>();
builder.Services.AddScoped<IMemberPermissionService, MemberPermissionService>();
builder.Services.AddScoped<IReactionService, ReactionService>();
builder.Services.AddScoped<IPollService, PollService>();
builder.Services.AddSingleton<IContentModerationService, ContentModerationService>();
builder.Services.Configure<linksy_backend_api.Domain.Options.ContentModerationOptions>(
    builder.Configuration.GetSection(
        linksy_backend_api.Domain.Options.ContentModerationOptions.SectionName));
builder.Services.AddScoped<IUserSettingsService, UserSettingsService>();
builder.Services.AddScoped<ICallService, CallService>();
builder.Services.AddScoped<ITokenRepository, TokenRepository>();
builder.Services.AddHttpClient();
builder.Services.AddDirectoryBrowser();

// ── Cache ─────────────────────────────────────────────────────────────────────
builder.Services.AddMemoryCache();

var redisEnabled = builder.Configuration.GetValue<bool>("Redis:Enabled", true);
var redisConn = builder.Configuration.GetValue<string>("Redis:ConnectionString");
using var loggerFactory = LoggerFactory.Create(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});
var startupLogger = loggerFactory.CreateLogger("Startup");

if (redisEnabled && !string.IsNullOrEmpty(redisConn))
{
    try
    {
        var redisOptions = ConfigurationOptions.Parse(redisConn);
        redisOptions.AbortOnConnectFail = false;

        using var redis = ConnectionMultiplexer.Connect(redisOptions);
        if (!redis.IsConnected)
        {
            startupLogger.LogWarning(
                "Redis connection could not be established. Failing back to in-memory distributed cach. ConnectionString: {RedisConnection}",
                redisConn
            );
            builder.Services.AddDistributedMemoryCache();
        }
        else
        {
            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConn;
                options.InstanceName = builder.Configuration.GetValue<string>("Redis:InstanceName");
            });
            startupLogger.LogInformation("Redis cache enabled and connected");
        }
    }
    catch (System.Exception ex)
    {
        startupLogger.LogError(ex, "Redis connection failed during starup. Failing back to in-memory distributed  cache. ConnectionString: {RedisConnection}",
        redisConn);
        builder.Services.AddDistributedMemoryCache();
    }
}
else
{
    builder.Services.AddDistributedMemoryCache();
    startupLogger.LogWarning("Redis disabled or missing connection string. Using in-memory distributed cache ");
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
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Only trust arbitrary proxies outside Development (configure KnownProxies in production when possible).
    if (!builder.Environment.IsDevelopment())
    {
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    }
});
var rateLimits = builder.Configuration
    .GetSection(RateLimitingOptions.SectionName)
    .Get<RateLimitingOptions>() ?? new RateLimitingOptions();

builder.Services.Configure<RateLimitingOptions>(
    builder.Configuration.GetSection(RateLimitingOptions.SectionName));

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        if (IsRateLimitExempt(httpContext))
            return RateLimitPartition.GetNoLimiter("exempt");

        return RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: $"global:{GetClientIp(httpContext)}",
            factory: _ => SlidingWindow(rateLimits.Global));
    });

    // Login brute-force — per IP, separate from OTP/refresh
    options.AddPolicy(RateLimitingOptions.AuthLoginPolicy, httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: $"login:{GetClientIp(httpContext)}",
            factory: _ => SlidingWindow(rateLimits.AuthLogin)));

    // Register / OTP / forgot-reset — per IP (email flooding)
    options.AddPolicy(RateLimitingOptions.AuthSensitivePolicy, httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: $"auth-sensitive:{GetClientIp(httpContext)}",
            factory: _ => SlidingWindow(rateLimits.AuthSensitive)));

    // Authenticated API — per user, fallback IP
    options.AddPolicy(RateLimitingOptions.ApiPolicy, httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: GetAuthenticatedKey(httpContext),
            factory: _ => SlidingWindow(rateLimits.Api)));

    // Uploads — per user
    options.AddPolicy(RateLimitingOptions.UploadPolicy, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"upload:{GetAuthenticatedKey(httpContext)}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = rateLimits.Upload.PermitLimit,
                Window = TimeSpan.FromSeconds(rateLimits.Upload.WindowSeconds),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    // Public/unauthenticated endpoints — per IP
    options.AddPolicy(RateLimitingOptions.PublicPolicy, httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: $"public:{GetClientIp(httpContext)}",
            factory: _ => SlidingWindow(rateLimits.Public)));

    options.OnRejected = async (context, token) =>
    {
        var http = context.HttpContext;
        var logger = http.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("RateLimiter");

        int? retryAfterSeconds = null;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
            http.Response.Headers.RetryAfter = retryAfterSeconds.Value.ToString();
        }

        logger.LogWarning(
            "Rate limit exceeded. Path={Path} Method={Method} Ip={Ip} RetryAfter={RetryAfter}",
            http.Request.Path,
            http.Request.Method,
            GetClientIp(http),
            retryAfterSeconds);

        http.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await http.Response.WriteAsJsonAsync(new
        {
            success = false,
            code = "TOO_MANY_REQUESTS",
            message = retryAfterSeconds is > 0
                ? $"Bạn gửi quá nhiều request. Vui lòng thử lại sau {retryAfterSeconds} giây."
                : "Bạn gửi quá nhiều request. Vui lòng thử lại sau.",
            retryAfterSeconds
        }, token);
    };
});

static SlidingWindowRateLimiterOptions SlidingWindow(RateLimitPolicyOptions policy) => new()
{
    PermitLimit = Math.Max(1, policy.PermitLimit),
    Window = TimeSpan.FromSeconds(Math.Max(1, policy.WindowSeconds)),
    SegmentsPerWindow = Math.Max(1, policy.SegmentsPerWindow),
    QueueLimit = 0,
    AutoReplenishment = true
};

static bool IsRateLimitExempt(HttpContext ctx)
{
    if (HttpMethods.IsOptions(ctx.Request.Method))
        return true;

    var path = ctx.Request.Path;
    return path.StartsWithSegments("/health")
        || path.StartsWithSegments("/swagger")
        || path.StartsWithSegments("/hubs");
}

// Prefer RemoteIpAddress (rewritten by UseForwardedHeaders when behind a trusted proxy).
static string GetClientIp(HttpContext ctx) =>
    ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";

static string GetAuthenticatedKey(HttpContext ctx)
{
    var userId = ctx.User.FindFirst("user_id")?.Value;
    if (!string.IsNullOrEmpty(userId))
        return $"user:{userId}";
    return $"ip:{GetClientIp(ctx)}";
}

var app = builder.Build();
app.UseForwardedHeaders();
if (!app.Environment.IsProduction())
{
    app.Use(async (context, next) =>
    {
        var origin = context.Request.Headers.Origin.ToString();

        Console.WriteLine(
            $"[HTTP] Origin={origin} Method={context.Request.Method} Path={context.Request.Path}");

        await next();

        Console.WriteLine(
            $"[HTTP] Status={context.Response.StatusCode} Path={context.Request.Path}");
    });
}
// ── Swagger ───────────────────────────────────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Linksy API V1");
    c.RoutePrefix = string.Empty;
});
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var logger = context.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("GlobalException");

        var exceptionFeature = context.Features
            .Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();

        var exception = exceptionFeature?.Error;
        var traceId = context.TraceIdentifier;

        if (exception != null)
        {
            logger.LogError(
                exception,
                "Unhandled exception. Path={Path}, Method={Method}, TraceId={TraceId}",
                context.Request.Path,
                context.Request.Method,
                traceId);
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(new
        {
            success = false,
            code = "INTERNAL_SERVER_ERROR",
            message = "Có lỗi xảy ra trong hệ thống.",
            traceId
        });
    });
});
// ── Middleware pipeline (thứ tự quan trọng) ───────────────────────────────────
app.UseResponseCompression();

app.UseRouting();

app.Use(async (context, next) =>
{
    var logger = context.RequestServices
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("HttpRequest");
    var startedAt = DateTime.Now;
    var origin = context.Request.Headers.Origin.ToString();
    var method = context.Request.Method;
    var path = context.Request.Path.ToString();
    var traceId = context.TraceIdentifier;
    var userId = context.User.FindFirst("user_id")?.Value;
    try
    {
        await next();
    }
    finally
    {
        var durationMs = (DateTime.Now - startedAt).TotalMilliseconds;
        logger.LogInformation(
            "HTTP {Method} {Path} responded {StatusCode} in {DurationMs}ms. Origin={Origin}, UserId={UserId}, TraceId={TraceId}",
            method,
            path,
            context.Response.StatusCode,
            durationMs,
            string.IsNullOrWhiteSpace(origin) ? "none" : origin,
            string.IsNullOrWhiteSpace(userId) ? "anonymous" : userId,
            traceId);
    }
});

app.UseCors("Frontend");

app.UseAuthentication();
app.UseRateLimiter();

app.UseAuthorization();

// ── Endpoints ─────────────────────────────────────────────────────────────────
app.MapHealthChecks("/health");

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

// Dev-only: unlock accounts locked by failed login attempts
if (args.Contains("--unlock-locked-users") && app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<LinksyDbContext>();
    var now = DateTime.UtcNow;
    var locked = await db.Users
        .Where(u => u.AccountLockedUntil != null && u.AccountLockedUntil > now)
        .ToListAsync();

    foreach (var user in locked)
    {
        user.FailedLoginAttempts = 0;
        user.AccountLockedUntil = null;
    }

    await db.SaveChangesAsync();
    Console.WriteLine($"Unlocked {locked.Count} user(s).");
    foreach (var user in locked)
        Console.WriteLine($"  - {user.Username} ({user.Email})");
    return;
}

app.Run();
