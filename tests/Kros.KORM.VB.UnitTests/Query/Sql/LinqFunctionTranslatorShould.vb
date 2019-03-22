Imports Kros.KORM.Metadata.Attribute
Imports Kros.KORM.UnitTests.Query.Sql
Imports Xunit

Namespace Kros.KORM.VB.UnitTests.Query.Sql
    Public Class LinqFunctionTranslatorShould
        Inherits LinqTranslatorTestBase

        <Fact>
        Public Sub TranslateWhereMethod()
            Dim query = MyBase.Query(Of Person)().Where(Function(p) p.Id = 5)

            AreSame(query, "SELECT Id, FirstName, LastName, PostAddress FROM People" +
                           " WHERE ((Id = @1))", 5)
        End Sub


        <Fact>
        Public Sub TranslateWhereMethodCompareString()
            Dim query = MyBase.Query(Of Person)().Where(Function(p) p.FirstName = "Janko")

            AreSame(query, "SELECT Id, FirstName, LastName, PostAddress FROM People" &
                           " WHERE ((FirstName = @1))", "Janko")
        End Sub

        <Fact>
        Public Sub TranslateFirstOrDefaultMethod()
            Dim query = MyBase.Query(Of Person)
            Dim item = query.FirstOrDefault

            WasGeneratedSameSql(query, "SELECT TOP 1 Id, FirstName, LastName, PostAddress FROM People")
        End Sub

        <Fact>
        Public Sub TranslateFirstOrDefaultWithConditionMethod()
            Dim query = MyBase.Query(Of Person)
            Dim item = query.FirstOrDefault(Function(p) p.Id = 5)

            WasGeneratedSameSql(query, "SELECT TOP 1 Id, FirstName, LastName, PostAddress FROM People" &
                                       " WHERE ((Id = @1))", 5)
        End Sub

        <Fact>
        Public Sub TranslateFirstOrDefaultWithConditionMethodCompareString()
            Dim query = MyBase.Query(Of Person)
            Dim item = query.FirstOrDefault(Function(p) p.FirstName = "Janko")

            WasGeneratedSameSql(query, "SELECT TOP 1 Id, FirstName, LastName, PostAddress FROM People" &
                                       " WHERE ((FirstName = @1))", "Janko")
        End Sub

        <Fact>
        Public Sub TranslateFirstOrDefaultWithConditionMethodLessThan()
            Dim query = MyBase.Query(Of Person)
            Dim item = query.FirstOrDefault(Function(p) p.Id < 5)

            WasGeneratedSameSql(query, "SELECT TOP 1 Id, FirstName, LastName, PostAddress FROM People" &
                                       " WHERE ((Id < @1))", 5)
        End Sub

        <Fact>
        Public Sub TranslateFirstOrDefaultWithWhereMethod()
            Dim query = MyBase.Query(Of Person)
            Dim item = query.Where(Function(p) p.FirstName = "Janko").FirstOrDefault()

            WasGeneratedSameSql(query, "SELECT TOP 1 Id, FirstName, LastName, PostAddress FROM People" &
                                       " WHERE ((FirstName = @1))", "Janko")
        End Sub


        <[Alias]("People")>
        Private Shadows Class Person
            Public Property Id As Integer

            Public Property FirstName As String

            Public Property LastName As String

            <[Alias]("PostAddress")>
            Public Property Address As String
        End Class

    End Class
End Namespace
