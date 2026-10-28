namespace AlmostNewOutpost.Tests.Api.Infrastructure;

/// <summary>
/// Puts every API test class in one collection, so they share one ApiFixture and run one
/// at a time (they all use the same test database).
/// </summary>
[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<ApiFixture>
{
    public const string Name = "API tests";
}
