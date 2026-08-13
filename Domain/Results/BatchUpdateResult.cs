namespace linksy_backend_api.Domain.Results
{
    public class BatchUpdateResult
    {
        public string Message { get; init; } = string.Empty;
        public int UpdatedCount { get; init; }
        public List<string> Errors { get; init; } = new();

        public static BatchUpdateResult Success(int count, List<string> errors) => new()
        {
            Message = $"Đã cập nhật {count} permission(s)" +
                           (errors.Count > 0 ? $", bỏ qua {errors.Count} lỗi" : string.Empty),
            UpdatedCount = count,
            Errors = errors
        };

        public static BatchUpdateResult Empty() => new()
        {
            Message = "Không có gì để cập nhật",
            UpdatedCount = 0
        };
    }
}
