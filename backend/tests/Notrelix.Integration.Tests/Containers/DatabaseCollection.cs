namespace Notrelix.Integration.Tests.Containers;

[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<PostgresTestContainer>
{
}
