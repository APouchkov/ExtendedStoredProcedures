IF NOT EXISTS (SELECT * FROM sys.assemblies asms WHERE asms.name = N'UDT.TPrivileges' and is_user_defined = 1) 
  CREATE ASSEMBLY [UDT.TPrivileges] 
  FROM 'E:\Programms\Extended Stored Procedures\UDT.TPrivileges.dll' 
  WITH PERMISSION_SET = SAFE 
ELSE
  ALTER ASSEMBLY [UDT.TPrivileges] 
  FROM 'E:\Programms\Extended Stored Procedures\UDT.TPrivileges.dll' 
  WITH PERMISSION_SET = SAFE, UNCHECKED DATA 