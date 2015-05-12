DROP FUNCTION [File].[Read]
GO
DROP PROCEDURE [File].[Write]
GO
DROP FUNCTION [File].[Info]
GO
DROP FUNCTION [File].[Find]
GO
DROP FUNCTION [File].[Exists]
GO
DROP PROCEDURE [File].[Create Directory]
GO
DROP ASSEMBLY FileIO
GO
CREATE ASSEMBLY FileIO FROM 'C:\Программы\Assemblies\FileIO.dll' WITH PERMISSION_SET = EXTERNAL_ACCESS 
GO
CREATE FUNCTION [File].[Read](@FileName NVARCHAR(4000)) RETURNS VarBinary(max)
AS EXTERNAL NAME FileIO.T.FileRead
GO
CREATE PROCEDURE [File].[Write](@FileName NVARCHAR(4000), @Text VarBinary(max), @CheckPath BIT)
AS EXTERNAL NAME FileIO.T.FileWrite
GO
CREATE PROCEDURE [File].[Create Directory](@DirName NVARCHAR(4000))
AS EXTERNAL NAME FileIO.T.CreateDirectory
GO
CREATE FUNCTION [File].[Exists](@FileName NVARCHAR(4000)) RETURNS Bit
AS EXTERNAL NAME FileIO.T.FileExists
GO

CREATE FUNCTION [File].[Find](@DirName NVARCHAR(4000), @Pattern NVARCHAR(4000)='*.*')
RETURNS TABLE (FileName NVarChar(4000), [Length] BigInt, IsDirectory Bit, CreateTime DateTime,  LastAccessTime DateTime, LastWriteTime DateTime, Attributes NVarChar(100))
AS EXTERNAL NAME FileIO.T.GetFileList
GO

CREATE FUNCTION [File].[Info](@DirName NVARCHAR(4000))
RETURNS TABLE ([Length] BigInt, CreateTime DateTime,  LastAccessTime DateTime, LastWriteTime DateTime, Attributes NVarChar(100))
AS EXTERNAL NAME FileIO.T.FileInfo
GO


GO
DECLARE @bin VarBinary(max)
SET @bin = 0x12124333333252
EXEC [File].[Write] '1.txt', @bin, 0
SELECT [File].[Read]('1.txt')
SELECT * FROM [FILE].[Info]('C:\boot.ini')
SELECT * FROM [File].[Find]('C:\')

