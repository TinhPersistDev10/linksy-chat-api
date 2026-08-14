using linksy_backend_api.Core.Interfaces.Services;
using linksy_backend_api.DTOs;
using linksy_backend_api.DTOs.Report;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace linksy_backend_api.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/reports")]
public class ReportsController : BaseApiController
{
    private readonly IUserReportService _reportService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IUserReportService reportService, ILogger<ReportsController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    /// <summary>GET /api/v1/reports/reasons</summary>
    [HttpGet("reasons")]
    public IActionResult GetReasons()
    {
        return Ok(new ApiResponseDto
        {
            Success = true,
            Message = "OK",
            Data = _reportService.GetReportReasons()
        });
    }

    /// <summary>POST /api/v1/reports</summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponseDto>> CreateReport([FromBody] CreateUserReportRequest request)
    {
        var result = await _reportService.CreateReportAsync(CurrentUserId, request);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    /// <summary>GET /api/v1/reports/mine</summary>
    [HttpGet("mine")]
    public async Task<ActionResult<ApiResponseDto>> GetMyReports(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _reportService.GetMyReportsAsync(CurrentUserId, page, pageSize);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }
}
