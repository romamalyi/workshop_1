namespace LeoHR.Server.IntegrationTests.Infrastructure;

// Every test class joins this collection, so they share one database and run one at a time:
// promotion criteria, role mappings and responsible positions are global settings that tests set.
[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<LeoHrApiFactory>
{
    public const string Name = "LeoHR API";
}
