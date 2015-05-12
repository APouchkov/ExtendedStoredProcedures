  DROP TYPE [TParams_EMPTY]
GO

IF NOT EXISTS (SELECT * FROM sys.assemblies asms WHERE asms.name = N'UDT.TParams_EMPTY' and is_user_defined = 1) 
  CREATE ASSEMBLY [UDT.TParams_EMPTY] 
  FROM 'E:\Programms\Extended Stored Procedures\UDT.TParams_EMPTY.dll' 
  WITH PERMISSION_SET = SAFE 
ELSE
  ALTER ASSEMBLY [UDT.TParams_EMPTY] 
  FROM 'E:\Programms\Extended Stored Procedures\UDT.TParams_EMPTY.dll' 
  WITH PERMISSION_SET = SAFE, UNCHECKED DATA
GO

CREATE TYPE [dbo].[TParams_EMPTY]
EXTERNAL NAME [UDT.TParams_EMPTY].[UDT_EMPTY.TParams_EMPTY]
GO
GRANT EXECUTE ON TYPE::[dbo].[TParams_EMPTY] TO [public]
GRANT REFERENCES ON TYPE::[dbo].[TParams_EMPTY] TO [public]
GO