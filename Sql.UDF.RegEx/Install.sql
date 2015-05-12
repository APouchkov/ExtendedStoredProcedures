IF NOT EXISTS (SELECT * FROM sys.assemblies asms WHERE asms.name = N'UDF.RegExp' and is_user_defined = 1) 
  CREATE ASSEMBLY [UDF.RegExp] 
  FROM 'D:\Programms\Extended Stored Procedures\UDF.RegEx.dll' 
  WITH PERMISSION_SET = UNSAFE 
ELSE
  ALTER ASSEMBLY [UDF.RegExp] 
  FROM 'D:\Programms\Extended Stored Procedures\UDF.RegEx.dll' 
  WITH PERMISSION_SET = UNSAFE, UNCHECKED DATA 