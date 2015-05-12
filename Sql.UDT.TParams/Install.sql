IF NOT EXISTS (SELECT * FROM sys.assemblies asms WHERE asms.name = N'UDT.TParams' and is_user_defined = 1) 
  CREATE ASSEMBLY [UDT.TParams] 
  FROM 'E:\Programms\Extended Stored Procedures\UDT.TParams.dll' 
  WITH PERMISSION_SET = SAFE 
ELSE
  ALTER ASSEMBLY [UDT.TParams] 
  FROM 'E:\Programms\Extended Stored Procedures\UDT.TParams.dll' 
  WITH PERMISSION_SET = SAFE, UNCHECKED DATA 