using System.Text.Json;
using System.Text.Json.Serialization;

namespace AlmostNewOutpost.Tests.Api.Infrastructure;

/// <summary>
/// JSON settings matching the API's: camelCase names and enums as text.
/// </summary>
public static class TestJson
{
    public static readonly JsonSerializerOptions Options = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        JsonSerializerOptions options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
