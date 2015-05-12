DROP FUNCTION [Sirius].[Send]
GO
DROP ASSEMBLY [Sirius]
GO
CREATE ASSEMBLY [Sirius] FROM 'C:\Программы\MasterBank\xp_MasterBank.dll' WITH PERMISSION_SET = UNSAFE 
GO
CREATE FUNCTION [Sirius].[Send](@Host nVarChar(4000), @Port Int, @Data Xml) 
  RETURNS TABLE (id NVarChar(4000), [value] NVarChar(4000))
AS EXTERNAL NAME Sirius.T.xp_MasterBank_SendCmd
go
