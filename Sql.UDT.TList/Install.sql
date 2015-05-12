IF NOT EXISTS (SELECT * FROM sys.assemblies asms WHERE asms.name = N'UDT.TList' and is_user_defined = 1) 
  CREATE ASSEMBLY [UDT.TList] 
  FROM 'E:\Programms\Extended Stored Procedures\UDT.TList.dll' 
  WITH PERMISSION_SET = SAFE 
ELSE
  ALTER ASSEMBLY [UDT.TList] 
  FROM 'E:\Programms\Extended Stored Procedures\UDT.TList.dll' 
  WITH PERMISSION_SET = SAFE, UNCHECKED DATA 