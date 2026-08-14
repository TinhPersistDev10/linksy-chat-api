namespace linksy_backend_api.Domain.Results
{
    public class MuteResult
    {
        public string Message { get; init; } = string.Empty;
        public object? Data { get; init; }

        public static MuteResult Success(bool isMuted, DateTime? mutedUntil)
        {
            var untilText = mutedUntil.HasValue
                ? $" đến {mutedUntil:dd/MM/yyyy HH:mm} UTC"
                : " vô thời hạn";

            return new MuteResult
            {
                Message = $"Đã mute thành viên{untilText}",
                Data = new { IsMuted = isMuted, MutedUntil = mutedUntil }
            };
        }
    }
}
