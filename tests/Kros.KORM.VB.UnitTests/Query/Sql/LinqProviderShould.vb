Imports FluentAssertions
Imports Kros.KORM.UnitTests.Base
Imports Xunit

Namespace Kros.KORM.VB.UnitTests.Query.Sql
    Public Class LinqProviderShould
        Inherits DatabaseTestBase

#Region "SQL Scripts"

        Private Const Table_TestTable As String = "TestTable"

        Private CreateTable_TestTable As String =
$"CREATE TABLE[dbo].[{Table_TestTable}] (
    [Id] [int] NOT NULL,
    [Number] [int] NOT NULL,
    [Description] [nvarchar] (50) NULL
) ON[PRIMARY];

INSERT INTO [{Table_TestTable}] ([Id], [Number], [Description]) VALUES (1, 10, 'Lorem ipsum');
INSERT INTO [{Table_TestTable}] ([Id], [Number], [Description]) VALUES (2, 20, NULL);
INSERT INTO [{Table_TestTable}] ([Id], [Number], [Description]) VALUES (3, 20, 'Hello world');
INSERT INTO [{Table_TestTable}] ([Id], [Number], [Description]) VALUES (4, 40, 'Nothing special');
"

#End Region

        <Fact>
        Public Sub ExecuteFirstOrDefault()
            Using korm = CreateDatabase(CreateTable_TestTable)
                Dim desc = "Hello world"
                Dim actual = korm.Query(Of TestTable)().FirstOrDefault(Function(p) p.Description = desc)

                actual.Id.Should().Be(3)
            End Using
        End Sub

        Private Class TestTable
            Public Property Id As Integer

            Public Property Number As Integer

            Public Property Description As String
        End Class
    End Class
End Namespace

