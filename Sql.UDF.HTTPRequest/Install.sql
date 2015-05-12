IF NOT EXISTS (SELECT * FROM sys.assemblies asms WHERE asms.name = N'UDF.HTTPRequest' and is_user_defined = 1) 
  CREATE ASSEMBLY [UDF.HTTPRequest] 
  FROM 'D:\Programms\Extended Stored Procedures\UDF.HTTPRequest.dll' 
  WITH PERMISSION_SET = UNSAFE 
ELSE
  ALTER ASSEMBLY [UDF.HTTPRequest] 
  FROM 'D:\Programms\Extended Stored Procedures\UDF.HTTPRequest.dll' 
  WITH PERMISSION_SET = UNSAFE, UNCHECKED DATA 