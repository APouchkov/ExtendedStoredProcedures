IF NOT EXISTS (SELECT * FROM sys.assemblies asms WHERE asms.name = N'UDT.TLink' and is_user_defined = 1) 
  CREATE ASSEMBLY [UDT.TLink] 
  FROM 'E:\Programms\Extended Stored Procedures\UDT.TLink.dll' 
  WITH PERMISSION_SET = SAFE 
ELSE
  ALTER ASSEMBLY [UDT.TLink] 
  FROM 'E:\Programms\Extended Stored Procedures\UDT.TLink.dll' 
  WITH PERMISSION_SET = SAFE, UNCHECKED DATA 