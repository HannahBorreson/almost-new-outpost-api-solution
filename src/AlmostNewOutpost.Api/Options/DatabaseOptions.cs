using System.ComponentModel.DataAnnotations;

namespace AlmostNewOutpost.Api.Options;

/// <summary>
/// The database connection string, bound from the "ConnectionStrings" section.
/// It lives in user-secrets, never in appsettings.json.
/// </summary>
public class DatabaseOptions
{
    public const string SectionName = "ConnectionStrings";

    [Required(ErrorMessage = "ConnectionStrings:Outpost is missing. Set it with: dotnet user-secrets set \"ConnectionStrings:Outpost\" \"Server=localhost;Port=3306;Database=almost_new_outpost;User ID=outpost_app;Password=...;\" (see README).")]
    public string Outpost { get; set; } = string.Empty;
}
