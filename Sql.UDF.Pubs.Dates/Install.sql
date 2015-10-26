IF NOT EXISTS (SELECT * FROM sys.assemblies asms WHERE asms.name = N'UDF.Pubs.Dates' and is_user_defined = 1) 
  CREATE ASSEMBLY [UDF.Pubs.Dates] 
  FROM 'E:\Programms\Extended Stored Procedures\UDF.Pubs.Dates.dll' 
  WITH PERMISSION_SET = SAFE 
ELSE
  ALTER ASSEMBLY [UDF.Pubs.Dates] 
  FROM 'E:\Programms\Extended Stored Procedures\UDF.Pubs.Dates.dll' 
  WITH PERMISSION_SET = SAFE, UNCHECKED DATA 