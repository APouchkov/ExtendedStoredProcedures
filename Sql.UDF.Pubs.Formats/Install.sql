IF NOT EXISTS (SELECT * FROM sys.assemblies asms WHERE asms.name = N'UDF.Pubs.Formats' and is_user_defined = 1) 
  CREATE ASSEMBLY [UDF.Pubs.Formats] 
  FROM 'E:\Programms\Extended Stored Procedures\UDF.Pubs.Formats.dll' 
  WITH PERMISSION_SET = SAFE 
ELSE
  ALTER ASSEMBLY [UDF.Pubs.Formats] 
  FROM 'E:\Programms\Extended Stored Procedures\UDF.Pubs.Formats.dll' 
  WITH PERMISSION_SET = SAFE, UNCHECKED DATA 