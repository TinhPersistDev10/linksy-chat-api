using System.Text.RegularExpressions;

namespace linksy_backend_api.Infrastructure.Helpers
{
    /// <summary>
    /// Shared profile field rules used by register/update flows.
    /// Keep in sync with frontend validators (username/fullname/bio/dob).
    /// </summary>
    public static class ProfileValidationHelper
    {
        public static readonly Regex UsernamePattern = new(
            @"^[a-zA-Z0-9_]+$",
            RegexOptions.Compiled);

        public const int UsernameMinLength = 3;
        public const int UsernameMaxLength = 50;
        public const int FullnameMinLength = 2;
        public const int FullnameMaxLength = 100;
        public const int BioMaxLength = 500;
        public const int MinAgeYears = 13;
        public const int MaxAgeYears = 120;

        public static void EnsureUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new InvalidOperationException("Username là bắt buộc");

            var value = username.Trim();
            if (value.Length < UsernameMinLength || value.Length > UsernameMaxLength)
                throw new InvalidOperationException("Username phải từ 3-50 ký tự");

            if (!UsernamePattern.IsMatch(value))
                throw new InvalidOperationException(
                    "Username chỉ được chứa chữ, số và dấu gạch dưới");
        }

        public static string NormalizeFullname(string? fullname)
        {
            if (fullname is null) return string.Empty;

            var value = fullname.Trim();
            if (value.Length == 0) return value;

            if (value.Length < FullnameMinLength || value.Length > FullnameMaxLength)
                throw new InvalidOperationException("Họ và tên phải từ 2-100 ký tự");

            return value;
        }

        public static string NormalizeBio(string? bio)
        {
            if (bio is null) return string.Empty;

            if (bio.Length > BioMaxLength)
                throw new InvalidOperationException("Giới thiệu không được quá 500 ký tự");

            return bio.Trim();
        }

        public static void EnsureDateOfBirth(DateOnly? dateOfBirth)
        {
            if (!dateOfBirth.HasValue) return;

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (dateOfBirth.Value > today)
                throw new InvalidOperationException("Ngày sinh không được ở tương lai");

            var minAgeBirthDate = today.AddYears(-MinAgeYears);
            if (dateOfBirth.Value > minAgeBirthDate)
                throw new InvalidOperationException("Bạn phải từ 13 tuổi trở lên");

            var maxAgeBirthDate = today.AddYears(-MaxAgeYears);
            if (dateOfBirth.Value < maxAgeBirthDate)
                throw new InvalidOperationException("Ngày sinh không hợp lệ");
        }
    }
}
