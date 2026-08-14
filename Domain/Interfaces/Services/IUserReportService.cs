using linksy_backend_api.DTOs;
using linksy_backend_api.DTOs.Report;

namespace linksy_backend_api.Core.Interfaces.Services;

public interface IUserReportService
{
    Task<ApiResponseDto> CreateReportAsync(Guid reporterUserId, CreateUserReportRequest request);
    Task<ApiResponseDto> GetMyReportsAsync(Guid reporterUserId, int page, int pageSize);
    Task<ApiResponseDto> GetReportsForAdminAsync(string? status, int page, int pageSize);
    Task<ApiResponseDto> GetReportByIdAsync(Guid reportId);
    Task<ApiResponseDto> UpdateReportStatusAsync(Guid adminUserId, Guid reportId, UpdateReportStatusRequest request);
    IReadOnlyList<object> GetReportReasons();
}
