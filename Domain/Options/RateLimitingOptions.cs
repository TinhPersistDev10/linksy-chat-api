namespace linksy_backend_api.Domain.Options
{
    public class RateLimitingOptions
    {
        public const string SectionName = "RateLimiting";

        public const string AuthLoginPolicy = "auth";
        public const string AuthSensitivePolicy = "auth-sensitive";
        public const string ApiPolicy = "api";
        public const string UploadPolicy = "upload";
        public const string PublicPolicy = "public";

        /// <summary>Account lockout after failed password attempts (AuthService).</summary>
        public int LoginAttemptsLimit { get; set; } = 5;
        public int LockoutDurationMinutes { get; set; } = 30;
        public int OtpMaxAttempts { get; set; } = 5;
        public int OtpExpirationMinutes { get; set; } = 15;

        public RateLimitPolicyOptions Global { get; set; } = new()
        {
            PermitLimit = 300,
            WindowSeconds = 60,
            SegmentsPerWindow = 6
        };

        public RateLimitPolicyOptions AuthLogin { get; set; } = new()
        {
            PermitLimit = 10,
            WindowSeconds = 60,
            SegmentsPerWindow = 6
        };

        public RateLimitPolicyOptions AuthSensitive { get; set; } = new()
        {
            PermitLimit = 8,
            WindowSeconds = 60,
            SegmentsPerWindow = 6
        };

        public RateLimitPolicyOptions Api { get; set; } = new()
        {
            PermitLimit = 120,
            WindowSeconds = 60,
            SegmentsPerWindow = 6
        };

        public RateLimitPolicyOptions Upload { get; set; } = new()
        {
            PermitLimit = 20,
            WindowSeconds = 600
        };

        public RateLimitPolicyOptions Public { get; set; } = new()
        {
            PermitLimit = 60,
            WindowSeconds = 60,
            SegmentsPerWindow = 6
        };
    }

    public class RateLimitPolicyOptions
    {
        public int PermitLimit { get; set; }
        public int WindowSeconds { get; set; } = 60;
        public int SegmentsPerWindow { get; set; } = 6;
    }
}
