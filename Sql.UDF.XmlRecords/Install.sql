IF NOT EXISTS (SELECT * FROM sys.assemblies asms WHERE asms.name = N'UDF.XmlRecords' and is_user_defined = 1) 
  CREATE ASSEMBLY [UDF.XmlRecords] 
  FROM 'D:\Programms\Extended Stored Procedures\UDF.XmlRecords.dll' 
  WITH PERMISSION_SET = SAFE 
ELSE
  ALTER ASSEMBLY [UDF.XmlRecords] 
  FROM 'D:\Programms\Extended Stored Procedures\UDF.XmlRecords.dll' 
  WITH PERMISSION_SET = SAFE, UNCHECKED DATA 