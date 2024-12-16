DECLARE @viewName NVARCHAR(128)
DECLARE viewCursor CURSOR FOR
SELECT name FROM sys.views

OPEN viewCursor
FETCH NEXT FROM viewCursor INTO @viewName

WHILE @@FETCH_STATUS = 0
BEGIN
    EXEC sp_refreshview @viewName
    FETCH NEXT FROM viewCursor INTO @viewName
END

CLOSE viewCursor
DEALLOCATE viewCursor
