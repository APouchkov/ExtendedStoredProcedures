using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;

public partial class Pub
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
    //if (String.IsNullOrEmpty(ADatePart) || date.IsNull) return new SqlDateTime();

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

      case "DAYOFYEAR": 
      case "DY": 
      case "YD":
        return new SqlDateTime(ADate.Year, 1, 1);

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

      case "WEEKDAY": 
      case "DW": 
      case "WD":
        while (ADate.DayOfWeek != System.DayOfWeek.Monday)
        {
          ADate = ADate.AddDays(-1);
        }
        return new SqlDateTime(ADate.Date);

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
      // if (datepart.IsNull || date.IsNull) return new SqlDateTime();

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

      case "DAYOFYEAR":
      case "DY":
      case "YD":
        return new SqlDateTime(ADate.Year + 1, 1, 1);

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

      case "WEEKDAY": 
      case "DW": 
      case "WD":
        while (ADate.DayOfWeek != System.DayOfWeek.Sunday)
        {
          ADate = ADate.AddDays(1);
        }
        return new SqlDateTime(ADate.Date.AddDays(1));

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
};