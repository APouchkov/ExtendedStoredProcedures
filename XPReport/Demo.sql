-- sp_addextendedproc 'xp_Report_Help', 'xpReport.dll'

-- sp_dropextendedproc xp_Report_Help
-- DBCC xpReport(FREE)

exec xp_Report_Help

-- sp_addextendedproc 'xp_Report_System', 'xpReport.dll'

-- sp_dropextendedproc xp_Report_System
-- DBCC xpReport(FREE)

exec xp_Report_System @Action='Info'

-- sp_addextendedproc 'xp_Report_Show', 'xpReport.dll'

-- sp_dropextendedproc xp_Report_Show
-- DBCC xpReport(FREE)

declare 
  @DateBegin datetime,
  @DateEnd   datetime

select
  @DateBegin = '20070701',
  @DateEnd = '20070701'

exec xp_Report_Show
  @object_id = 35,
  @Proc_Name = 'bo_Report_Client',
  @nnFirm = 1, 
  @DateBegin = @DateBegin,
  @DateEnd = @DateEnd,
  @nnIAccount = 1782

-- sp_addextendedproc 'xp_Report_Save', 'xpReport.dll'

-- sp_dropextendedproc xp_Report_Save
-- DBCC xpReport(FREE)

declare 
  @DateBegin datetime,
  @DateEnd   datetime

select
  @DateBegin = '20070701',
  @DateEnd = '20070701'

exec master..xp_Report_Save
  @object_id = 35,
  @FileName = 'rtdfg\',
  @Proc_Name = 'bo_Report_Client',
  @nnFirm = 1,
  @DateBegin = @DateBegin,
  @DateEnd = @DateEnd,
  @nnIAccount = 1782

-- sp_addextendedproc 'xp_Report_Mail', 'xpReport.dll'

-- sp_dropextendedproc xp_Report_Mail
-- DBCC xpReport(FREE)

declare 
  @DateBegin datetime,
  @DateEnd   datetime

select
  @DateBegin = '20070701',
  @DateEnd = '20070701'

exec xp_Report_Mail
  @object_id = 35,
  @EMail = 'turyansky@finam.ru',
  @From = 'FINAM',
  @Proc_Name = 'bo_Report_Client',
  @nnFirm = 1,
  @DateBegin = @DateBegin,
  @DateEnd = @DateEnd,
  @nnIAccount = 1782 
