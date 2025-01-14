DECLARE @sql NVARCHAR(MAX)

SET @sql = (
    SELECT STRING_AGG('EXEC sp_refreshview ' + QUOTENAME(name), '; ') 
    FROM sys.views
)

EXEC sp_executesql @sql