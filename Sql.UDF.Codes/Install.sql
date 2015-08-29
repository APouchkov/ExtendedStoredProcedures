IF NOT EXISTS (SELECT * FROM sys.assemblies asms WHERE asms.name = N'UDF.Codes' and is_user_defined = 1) 
  CREATE ASSEMBLY [UDF.Codes] 
  FROM 'E:\Programms\Extended Stored Procedures\UDF.Codes.dll' 
  WITH PERMISSION_SET = SAFE 
ELSE
  ALTER ASSEMBLY [UDF.Codes] 
  FROM 'E:\Programms\Extended Stored Procedures\UDF.Codes.dll' 
  WITH PERMISSION_SET = SAFE, UNCHECKED DATA 