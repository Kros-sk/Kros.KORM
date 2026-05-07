Imports Kros.KORM.UnitTests
Imports Xunit

Namespace Kros.KORM.VB.UnitTests

    <CollectionDefinition(KormTestsCollection.Name)>
    Public Class KormTestsCollection
        Implements ICollectionFixture(Of KormTestsFixture)

        ' This class has no code, And Is never created. Its purpose Is simply
        ' to be the place to apply [CollectionDefinition] And all the
        ' ICollectionFixture<> interfaces.
        Public Const Name As String = "KormUnitTests"
    End Class

End Namespace
