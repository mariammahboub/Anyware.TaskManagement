using System.Text.Json;
using System.Text.Json.Serialization;

namespace Anyware.TaskManagement.API.Tests.Helpers;

public static class TestJsonOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull,
        Converters                  = { new JsonStringEnumConverter() }
    };
}
