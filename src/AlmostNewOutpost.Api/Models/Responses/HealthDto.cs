namespace AlmostNewOutpost.Api.Models.Responses;

/// <summary>
/// Result of GET /api/health: { "status": "ok", "database": "ok" | "unreachable" }.
/// </summary>
public class HealthDto
{
    public string Status { get; set; } = "ok";

    public string Database { get; set; } = "ok";
}
