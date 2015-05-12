IF NOT EXISTS (SELECT * FROM sys.assemblies asms WHERE asms.name = N'UDP.DynamicSQL' and is_user_defined = 1) 
  CREATE ASSEMBLY [UDP.DynamicSQL]
  FROM 'E:\Programms\Extended Stored Procedures\UDP.DynamicSQL.dll' 
  WITH PERMISSION_SET = SAFE 
ELSE
  ALTER ASSEMBLY [UDP.DynamicSQL]
  FROM 'E:\Programms\Extended Stored Procedures\UDP.DynamicSQL.dll' 
  WITH PERMISSION_SET = SAFE, UNCHECKED DATA