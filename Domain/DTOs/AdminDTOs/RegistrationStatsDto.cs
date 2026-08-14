using System;
using System.Collections.Generic;

namespace linksy_backend_api.Core.DTOs.AdminDTOs;

public class RegistrationStatsDto
{
    /// <summary>day | month | year</summary>
    public string Period { get; set; } = "day";

    public DateTime From { get; set; }

    public DateTime To { get; set; }

    public int TotalRegistrations { get; set; }

    public List<RegistrationBucketDto> Buckets { get; set; } = new();
}

public class RegistrationBucketDto
{
    /// <summary>Label for the bucket (yyyy-MM-dd | yyyy-MM | yyyy).</summary>
    public string Label { get; set; } = string.Empty;

    public DateTime PeriodStart { get; set; }

    public int Count { get; set; }
}
