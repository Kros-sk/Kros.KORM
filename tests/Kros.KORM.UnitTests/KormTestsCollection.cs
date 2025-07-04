using Xunit;

namespace Kros.KORM.UnitTests;

[CollectionDefinition(Name)]
public class KormTestsCollection : ICollectionFixture<KormTestsFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
    public const string Name = "KormUnitTests";
}
