CREATE TABLE [dbo].[__KormMigrationsHistory](
    [MigrationId] [bigint] NOT NULL,
    [MigrationName] [nvarchar](255) NOT NULL,
    [ProductInfo] [nvarchar](255) NOT NULL,
    [Updated] [datetime2] NULL,
    CONSTRAINT [PK___KormMigrationsHistory] PRIMARY KEY CLUSTERED
    (
        [MigrationId] ASC
    )
) ON [PRIMARY]
