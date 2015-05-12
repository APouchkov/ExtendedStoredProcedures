IF NOT EXISTS (SELECT * FROM sys.assemblies asms WHERE asms.name = N'UDF.XmlParams' and is_user_defined = 1) 
  CREATE ASSEMBLY [UDF.XmlParams] 
  FROM 'D:\Programms\Extended Stored Procedures\UDF.XmlParams.dll' 
  WITH PERMISSION_SET = SAFE 
ELSE
  ALTER ASSEMBLY [UDF.XmlParams] 
  FROM 'D:\Programms\Extended Stored Procedures\UDF.XmlParams.dll' 
  WITH PERMISSION_SET = SAFE, UNCHECKED DATA 