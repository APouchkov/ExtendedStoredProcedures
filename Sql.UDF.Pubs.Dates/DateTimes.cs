using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;

public class Date
{
  /// <summary>
  /// Возвращает начало периода
  /// </summary>
  /// <param name="datepart">Часть аргумента date, которая задает тип периода. </param>
  /// <param name="date">Выражение, которое можно привести к значению типа date.</param>
  /// <returns></returns>

  [SqlFunction(Name = "Period Begin", IsPrecise = true, DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  // RETURNS NULL ON NULL INPUT
  public static SqlDateTime PeriodBegin(String ADatePart, DateTime ADate)
  {
    switch (ADatePart.ToUpper())
    {
      case "YEAR":
      case "Y":
        return new SqlDateTime(ADate.Year, 1, 1);

      case "QUARTER":
      case "Q":
        return new SqlDateTime(ADate.Year, ((ADate.Month + 2) / 3) * 3 - 2, 1);

      case "MONTH": 
      case "M":
        return new SqlDateTime(ADate.Year, ADate.Month, 1);

      //case "DAYOFYEAR": 
      //case "DY": 
      //case "YD":
      //  return new SqlDateTime(ADate.Year, 1, 1);

      case "DAY": 
      case "D":
        return new SqlDateTime(ADate.Date);

      case "WEEK": 
      case "W":
        while (ADate.DayOfWeek != System.DayOfWeek.Monday)
        {
          ADate = ADate.AddDays(-1);
        }
        return new SqlDateTime(ADate.Date);

      //case "WEEKDAY": 
      //case "DW": 
      //case "WD":
      //  while (ADate.DayOfWeek != System.DayOfWeek.Monday)
      //  {
      //    ADate = ADate.AddDays(-1);
      //  }
      //  return new SqlDateTime(ADate.Date);

      case "HOUR": 
      case "H":
        return new SqlDateTime(ADate.Year, ADate.Month, ADate.Day, ADate.Hour, 0, 0);

      case "MINUTE": 
      case "N":
        return new SqlDateTime(ADate.Year, ADate.Month, ADate.Day, ADate.Hour, ADate.Minute, 0);

      case "SECOND": 
      case "S":
        return new SqlDateTime(ADate.Year, ADate.Month, ADate.Day, ADate.Hour, ADate.Minute, ADate.Second);

      case "MILLISECOND": 
      case "MLS":
        return new SqlDateTime(ADate.Year, ADate.Month, ADate.Day, ADate.Hour, ADate.Minute, ADate.Second, ADate.Millisecond * 1000);

      case "MICROSECOND": 
      case "MCS":
          return ADate;

      case "NANOSECOND": 
      case "NNS":
          return ADate;

      default: 
        throw new Exception(String.Format("'{0}' не является известным параметром PeriodBegin.", ADatePart));
    }
  }

  /// <summary>
  /// Возвращает окончание периода
  /// </summary>
  /// <param name="datepart">Часть аргумента date, которая задает тип периода. </param>
  /// <param name="date">Выражение, которое можно привести к значению типа date.</param>
  /// <returns></returns>

  [SqlFunction(Name = "Period End", IsPrecise = true, DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  // RETURNS NULL ON NULL INPUT
  public static SqlDateTime PeriodEnd(String ADatePart, DateTime ADate)
  {
    switch (ADatePart.ToUpper())
    {
      case "YEAR":
      case "Y":
        return new SqlDateTime(ADate.Year + 1, 1, 1);

      case "QUARTER":
      case "Q":
        return new SqlDateTime(ADate.Year, ((ADate.Month + 2) / 3) * 3 + 1, 1);

      case "MONTH":
      case "M":
        return new SqlDateTime(ADate.Year, ADate.Month + 1, 1);

      //case "DAYOFYEAR":
      //case "DY":
      //case "YD":
      //  return new SqlDateTime(ADate.Year + 1, 1, 1);

      case "DAY":
      case "D":
        return new SqlDateTime(ADate.Date.AddDays(1));

      case "WEEK":
      case "W":
        while (ADate.DayOfWeek != System.DayOfWeek.Sunday)
        {
          ADate = ADate.AddDays(1);
        }
        return new SqlDateTime(ADate.Date.AddDays(1));

      //case "WEEKDAY": 
      //case "DW": 
      //case "WD":
      //  while (ADate.DayOfWeek != System.DayOfWeek.Sunday)
      //  {
      //    ADate = ADate.AddDays(1);
      //  }
      //  return new SqlDateTime(ADate.Date.AddDays(1));

      case "HOUR": 
      case "H":
        return new SqlDateTime(ADate.Year, ADate.Month, ADate.Day, ADate.Hour + 1, 0, 0);

      case "MINUTE":
      case "N":
        return new SqlDateTime(ADate.Year, ADate.Month, ADate.Day, ADate.Hour, ADate.Minute + 1, 0);

      case "SECOND":
      case "S":
        return new SqlDateTime(ADate.Year, ADate.Month, ADate.Day, ADate.Hour, ADate.Minute, ADate.Second + 1);

      case "MILLISECOND": 
      case "MLS":
        return new SqlDateTime(ADate.Year, ADate.Month, ADate.Day, ADate.Hour, ADate.Minute, ADate.Second, ADate.Millisecond * 1000 + 3000);

      case "MICROSECOND":
      case "MCS":
        return new SqlDateTime(ADate.Year, ADate.Month, ADate.Day, ADate.Hour, ADate.Minute, ADate.Second, ADate.Millisecond * 1000 + 3000);

      case "NANOSECOND":
      case "NNS":
        return new SqlDateTime(ADate.Year, ADate.Month, ADate.Day, ADate.Hour, ADate.Minute, ADate.Second, ADate.Millisecond * 1000 + 3000);

      default: 
        throw new Exception(String.Format("'{0}' не является известным параметром PeriodBegin.", ADatePart));
    }
  }

  /// <summary>
  /// Возвращает окончание периода
  /// </summary>
  /// <param name="datepart">Часть аргумента date, которая задает тип периода. </param>
  /// <param name="date">Выражение, которое можно привести к значению типа date.</param>
  /// <returns></returns>

  [SqlFunction(Name = "DateAdd", IsPrecise = true, DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  // RETURNS NULL ON NULL INPUT
  public static DateTime DateAdd(String ADatePart, Int32 ACount, DateTime ADate)
  {
    switch (ADatePart.ToUpper())
    {
      case "YEAR":
      case "Y":
        return ADate.AddYears(ACount);

      case "MONTH":
      case "M":
        return ADate.AddMonths(ACount);

      case "DAY":
      case "D":
        return ADate.AddDays(ACount);

      case "WEEK":
      case "W":
        return ADate.AddDays(ACount * 7);

      case "HOUR":
      case "H":
        return ADate.AddHours(ACount);

      case "MINUTE":
      case "N":
        return ADate.AddMinutes(ACount);

      case "SECOND":
      case "S":
        return ADate.AddSeconds(ACount);

      case "MILLISECOND": 
      case "MLS":
        return ADate.AddMilliseconds(ACount);

      default: 
        throw new Exception(String.Format("'{0}' не является известным параметром DateAdd.", ADatePart));
    }
  }

  [SqlFunction(Name = "DateAdd(Text)", IsPrecise = true, DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  // RETURNS NULL ON NULL INPUT
  public static DateTime DateAddText(String ADatePart, DateTime ADate)
  {
    // int i = ADatePart.LastIndexOfAny(new Char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9'});
    for(int i = 0; i < ADatePart.Length; i++)
    {
      if (ADatePart[i] < '0' || ADatePart[i] > '9')
      {
        if (i == 0)
          break;
        else
          return DateAdd(ADatePart.Substring(i), Int32.Parse(ADatePart.Substring(0, i)), ADate);
      }
    }
    throw new Exception(String.Format("'{0}' не является попустимым параметром DateAddText.", ADatePart));
    //return 0;
  }
};