(******************************************************************************)
(*  xpReport - Help                                                           *)
(*  �����: ��������� ���������                                                *)
(*  ������: 3.5 �� 27.03.2008                                                 *)
(******************************************************************************)
unit HelpStrs;

interface

resourcestring

  // Help
  SHelpTitle = '������ ������������ ������� FoReport v %s Build: %d (%s)';

  SHelpMain = '��������� xp_Report_Help;' +
    '  ������� ������� �� ��������� �� ���������e.;' +
    '���������:;' +
    '  xp_Report_Help [ , [ @ProcName = ] ''procedure'' ] [ , [ @Action = ] ''action''];' +
    '���������:;' +
    '[ @procname = ] procedure;' +
    '  ��� ���������, �� ������� ���������� �������� �������. �������� procedure ����� ��� varchar � �� ����� �������� �� ���������;' +
    '[ @Action = ] action;' +
    '  ����������� ��������, �� ������� ���������� �������� �������, ���� �� ������ � �������������� ����������, �� ���������� ������ ���� ��������� ��������. �������� action ����� ��� varchar � �� ����� �������� �� ���������. ;' +
    '�������������� ���������:;' +
    '  xp_Report_Help;' +
    '  xp_Report_System;' +
    '  xp_Report_Show;' +
    '  xp_Report_Save;' +
    '  xp_Report_Print;' +
    '  xp_Report_Mail;';

  SHelpReportShow = '��������� xp_Report_Show;' +
    '  ��������� ����� � ���������� ��� � ������ ������.;' +
    '���������:;' +
    '  xp_Report_Show [ , @ParamName = ''param_value'' [ ,...n] ];' +
    '���������:;' +
    '[ @ParamName = ] param_value;' +
    '  ��������� ������.;';

  SHelpReportSave = '��������� xp_Report_Save;' +
    '  ��������� ����� � ���������� ��� � ������ ������.;' +
    '���������:;' +
    '  xp_Report_Save [ @FileName = ''file_name'' ] [ , @ParamName = ''param_value'' [ ,...n] ];' +
    '���������:;' +
    '@FileName = ''file_name'';' +
    '  ������ ��� ����� (����������) ��� ���������� ����� ������.  �������� file_name ����� ��� varchar � �� ����� �������� �� ���������.;' +
    '@ParamName = param_value;' +
    '  ��������� ������.;';

  SHelpReportPrint = '��������� xp_Report_Print;' +
    '  ��������� ����� � ������� ��� �� ������.;' +
    '���������:;' +
    '  xp_Report_Print [ @PrinerName = ''printer_name'' ] [ , @ParamName = ''param_value'' [ ,...n] ];' +
    '���������:;' +
    '@PrinerName = ''printer_name'';' +
    '  ������ ��� �������� ��� ������ �� ������ ������. ;' +
    '@ParamName = param_value;' +
    '  ��������� ������.;';

  SHelpReportMail = '��������� xp_Report_Mail;' +
    '  ��������� ����� � ���������� ��� �� ����������� ����� ��������.;' +
    '���������:;' +
    '  xp_Report_Mail @EMail = ''email'' [ , @EMailFrom = ''email_from'' ] [ , @Subject = ''subject'' ] [ , @TextBody = ''text_body'' ] [ , @HTMLBody = ''html_body'' ] [ , @ParamName = ''param_value'' [ ,...n] ];' +
    '���������:;' +
    '@EMail = ''email'';' +
    '  ����������� ����� ����������. �������� email ����� ��� varchar � �� ����� �������� �� ���������.;' +
    '@EMailFrom = ''email_from'';' +
    '  ����������� ����� �����������. �������� email_from ����� ��� varchar � �� ����� �������� �� ���������.;' +
    '@Subject = ''subject'';' +
    '  ���� ������. �������� subject ����� ��� varchar � �� ����� �������� �� ���������.;' +
    '@TextBody = ''text_body'';' +
    '  ����� ������. �������� text_body ����� ��� varchar � �� ����� �������� �� ���������.;' +
    '@HTMLBody = ''html_body'';' +
    '  ����� ������ � ������� HTML. �������� html_body ����� ��� varchar � �� ����� �������� �� ���������.;' +
    '@ParamName = param_value;' +
    '  ��������� ������.;';

  SHelpSystem = '��������� xp_Report_System;' +
    '  ��������� ����� ��������� �������.;' +
    '���������:;' +
    '  xp_Report_System @Action = ''action'' [ , [ @ParamName = ] ''param_value'' [ ,...n] ];' +
    '���������:;' +
    '[ @Action = ] action;' +
    '  ����������� ��������. �������� action ����� ��� varchar � �������� �� ��������� ''Info''. ;' +
    '[ @ParamName = ] param_value;' +
    '  ��������� ��������.;' +
    '�������������� ��������:;' +
    '  Info;' +
    '  SetFileTime;' +
    '  SetSystemTime;';

  SHelpSytstemInfo = '��������� xp_Report_System [ @Action = ] Info;' +
    '  ���������� ���������� � ������� ��������� ������ ������������ �������.;' +
    '���������:;' +
    '  xp_Report_System [ @Action = ] Info;';

  SHelpSystemSetFileTime = '��������� xp_Report_System [ @Action = ] SetFileTime;' +
    '  ������������� ���� � ����� ��������, ���������� ������� � ���������� ��������� �����.;' +
    '���������:;' +
    '  xp_Report_System [ @Action = ] SetFileTime, [ @FileName = ] FileName [ , [ @CreationTime = ] ''creation_time'' ] [ , [ @LastAccessTime = ] ''last_access_time'' ] [ , [ @LastWriteTime = ] ''last_write_time'' ];' +
    '���������:;' +
    '[ @FileName = ] ''FileName'';' +
    '  ������ ��� ����� � ������������ ������� (���������� ���). �������� file_name ����� ��� varchar � �� ����� �������� �� ���������.;' +
    '[ @CreationTime = ] ''creation_time'';' +
    '  ���� �������� �����. �������� creation_time ����� ��� datetime � �������� �� ��������� ����� ������� ���� �������� �����.;' +
    '[ @LastAccessTime = ] ''last_access_time'';' +
    '  ���� ���������� ������� � �����. �������� last_access_time ����� ��� datetime � �������� �� ��������� ����� ������� ���� ���������� ������� � �����.;' +
    '[ @LastWriteTime = ] ''last_write_time'';' +
    '  ���� ���������� ��������� �����. �������� last_write_time ����� ��� datetime � �������� �� ��������� ����� ������� ���� ���������� ��������� �����.';

  SHelpSystemSetSystemTime = '��������� xp_Report_System [ @Action = ] SetSystemTime;' +
    '  ������������� ��������� ���� � �����.;' +
    '���������:;' +
    '  xp_Report_System [ @Action = ] SetSystemTime, [ @SystemTime = ] system_time;' +
    '���������:;' +
    '[ @SystemTime = ] ''system_time'';' +
    '  ����� ��������� �����. �������� system_time ����� ��� datetime � �� ����� �������� �� ���������.;';

implementation

end.
