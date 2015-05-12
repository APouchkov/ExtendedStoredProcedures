IF NOT EXISTS (SELECT * FROM sys.assemblies asms WHERE asms.name = N'UDT.TWho' and is_user_defined = 1) 
  CREATE ASSEMBLY [UDT.TWho] 
  FROM 'D:\Programms\Extended Stored Procedures\SQL.UDT.TWho.dll' 
  WITH PERMISSION_SET = SAFE 
ELSE
  ALTER ASSEMBLY [UDT.TWho] 
  FROM 'D:\Programms\Extended Stored Procedures\SQL.UDT.TWho.dll' 
  WITH PERMISSION_SET = SAFE, UNCHECKED DATA 