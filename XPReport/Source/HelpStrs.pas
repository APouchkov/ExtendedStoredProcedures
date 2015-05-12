(******************************************************************************)
(*  xpReport - Help                                                           *)
(*  Автор: Турянский Александр                                                *)
(*  Версия: 3.5 от 27.03.2008                                                 *)
(******************************************************************************)
unit HelpStrs;

interface

resourcestring

  // Help
  SHelpTitle = 'Модуль формирования отчетов FoReport v %s Build: %d (%s)';

  SHelpMain = 'Процедура xp_Report_Help;' +
    '  Выводит справку по процедуре из библиотекe.;' +
    'Синтаксис:;' +
    '  xp_Report_Help [ , [ @ProcName = ] ''procedure'' ] [ , [ @Action = ] ''action''];' +
    'Аргументы:;' +
    '[ @procname = ] procedure;' +
    '  Имя процедуры, по которой необходимо получить справку. Аргумент procedure имеет тип varchar и не имеет значения по умолчанию;' +
    '[ @Action = ] action;' +
    '  Наменование операции, по которой необходимо получить справку, если не задано и поддерживается процедурой, то выводиться список всех возможных операций. Аргумент action имеет тип varchar и не имеет значения по умолчанию. ;' +
    'Поддерживаемые процедуры:;' +
    '  xp_Report_Help;' +
    '  xp_Report_System;' +
    '  xp_Report_Show;' +
    '  xp_Report_Save;' +
    '  xp_Report_Print;' +
    '  xp_Report_Mail;';

  SHelpReportShow = 'Процедура xp_Report_Show;' +
    '  Формирует отчет и возвращает его в наборе данных.;' +
    'Синтаксис:;' +
    '  xp_Report_Show [ , @ParamName = ''param_value'' [ ,...n] ];' +
    'Аргументы:;' +
    '[ @ParamName = ] param_value;' +
    '  Параметры отчета.;';

  SHelpReportSave = 'Процедура xp_Report_Save;' +
    '  Формирует отчет и возвращает его в наборе данных.;' +
    'Синтаксис:;' +
    '  xp_Report_Save [ @FileName = ''file_name'' ] [ , @ParamName = ''param_value'' [ ,...n] ];' +
    'Аргументы:;' +
    '@FileName = ''file_name'';' +
    '  Задает имя файла (директории) для сохранения файла отчета.  Аргумент file_name имеет тип varchar и не имеет значения по умолчанию.;' +
    '@ParamName = param_value;' +
    '  Параметры отчета.;';

  SHelpReportPrint = 'Процедура xp_Report_Print;' +
    '  Формирует отчет и выводит его на печать.;' +
    'Синтаксис:;' +
    '  xp_Report_Print [ @PrinerName = ''printer_name'' ] [ , @ParamName = ''param_value'' [ ,...n] ];' +
    'Аргументы:;' +
    '@PrinerName = ''printer_name'';' +
    '  Задает имя принтерв для вывода на печать отчета. ;' +
    '@ParamName = param_value;' +
    '  Параметры отчета.;';

  SHelpReportMail = 'Процедура xp_Report_Mail;' +
    '  Формирует отчет и отпровляет его по электронной почте адресату.;' +
    'Синтаксис:;' +
    '  xp_Report_Mail @EMail = ''email'' [ , @EMailFrom = ''email_from'' ] [ , @Subject = ''subject'' ] [ , @TextBody = ''text_body'' ] [ , @HTMLBody = ''html_body'' ] [ , @ParamName = ''param_value'' [ ,...n] ];' +
    'Аргументы:;' +
    '@EMail = ''email'';' +
    '  Электронный адрес получателя. Аргумент email имеет тип varchar и не имеет значения по умолчанию.;' +
    '@EMailFrom = ''email_from'';' +
    '  Электронный адрес отправителя. Аргумент email_from имеет тип varchar и не имеет значения по умолчанию.;' +
    '@Subject = ''subject'';' +
    '  Тема письма. Аргумент subject имеет тип varchar и не имеет значения по умолчанию.;' +
    '@TextBody = ''text_body'';' +
    '  Текст письма. Аргумент text_body имеет тип varchar и не имеет значения по умолчанию.;' +
    '@HTMLBody = ''html_body'';' +
    '  Текст письма в формате HTML. Аргумент html_body имеет тип varchar и не имеет значения по умолчанию.;' +
    '@ParamName = param_value;' +
    '  Параметры отчета.;';

  SHelpSystem = 'Процедура xp_Report_System;' +
    '  Реализует набор системных функций.;' +
    'Синтаксис:;' +
    '  xp_Report_System @Action = ''action'' [ , [ @ParamName = ] ''param_value'' [ ,...n] ];' +
    'Аргументы:;' +
    '[ @Action = ] action;' +
    '  Наменование операции. Аргумент action имеет тип varchar и значение по умолчанию ''Info''. ;' +
    '[ @ParamName = ] param_value;' +
    '  Параметры операции.;' +
    'Поддерживаемые операции:;' +
    '  Info;' +
    '  SetFileTime;' +
    '  SetSystemTime;';

  SHelpSytstemInfo = 'Процедура xp_Report_System [ @Action = ] Info;' +
    '  Показывает информацию о текущем состоянии модуля формирования отчетов.;' +
    'Синтаксис:;' +
    '  xp_Report_System [ @Action = ] Info;';

  SHelpSystemSetFileTime = 'Процедура xp_Report_System [ @Action = ] SetFileTime;' +
    '  Устанавливает дату и время создания, последнего доступа и последнего изменения файла.;' +
    'Синтаксис:;' +
    '  xp_Report_System [ @Action = ] SetFileTime, [ @FileName = ] FileName [ , [ @CreationTime = ] ''creation_time'' ] [ , [ @LastAccessTime = ] ''last_access_time'' ] [ , [ @LastWriteTime = ] ''last_write_time'' ];' +
    'Аргументы:;' +
    '[ @FileName = ] ''FileName'';' +
    '  Задает имя файла в операционной системе (физическое имя). Аргумент file_name имеет тип varchar и не имеет значения по умолчанию.;' +
    '[ @CreationTime = ] ''creation_time'';' +
    '  Дата создания файла. Аргумент creation_time имеет тип datetime и значение по умолчанию равно текущей дате создания файла.;' +
    '[ @LastAccessTime = ] ''last_access_time'';' +
    '  Дата последнего доступа к файлу. Аргумент last_access_time имеет тип datetime и значение по умолчанию равно текущей дате последнего доступа к файлу.;' +
    '[ @LastWriteTime = ] ''last_write_time'';' +
    '  Дата последнего изменения файла. Аргумент last_write_time имеет тип datetime и значение по умолчанию равно текущей дате последнего изменения файла.';

  SHelpSystemSetSystemTime = 'Процедура xp_Report_System [ @Action = ] SetSystemTime;' +
    '  Устанавливает системную дату и время.;' +
    'Синтаксис:;' +
    '  xp_Report_System [ @Action = ] SetSystemTime, [ @SystemTime = ] system_time;' +
    'Аргументы:;' +
    '[ @SystemTime = ] ''system_time'';' +
    '  Новое системное время. Аргумент system_time имеет тип datetime и не имеет значения по умолчанию.;';

implementation

end.
