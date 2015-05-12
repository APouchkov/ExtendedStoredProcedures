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
    [SqlFunction(Name = "Period Begin", IsDeterministic = true, IsPrecise = true, DataAccess = DataAccessKind.None)]
    public static SqlDateTime PeriodBegin(SqlString datepart, SqlDateTime date)
    {
       if (datepart.IsNull || date.IsNull) return new SqlDateTime();
       switch (datepart.Value)
       {
           case "year": case "yy": case "yyyy":
               return new SqlDateTime(date.Value.Year, 1, 1);
           case "quarter": case "qq": case "q":
               return new SqlDateTime(date.Value.Year, ((date.Value.Month + 2) / 3) * 3 - 2, 1);
           case "month": case "mm": case "m":
               return new SqlDateTime(date.Value.Year, date.Value.Month, 1);
           case "dayofyear": case "dy": case "y":
               return new SqlDateTime(date.Value.Year, 1, 1);
           case "day": case "dd": case "d":
               return new SqlDateTime(date.Value.Date);
           case "week": case "wk": case "ww":
               while (date.Value.DayOfWeek != System.DayOfWeek.Monday)
               {
                   date = date.Value.AddDays(-1);
               }
               return new SqlDateTime(date.Value.Date);
           case "weekday": case "dw": case "w":
               while (date.Value.DayOfWeek != System.DayOfWeek.Monday)
               {
                   date = date.Value.AddDays(-1);
               }
               return new SqlDateTime(date.Value.Date);
           case "hour": case "hh":
               return new SqlDateTime(date.Value.Year, date.Value.Month, date.Value.Day, date.Value.Hour, 0, 0);
           case "minute": case "mi": case "n":
               return new SqlDateTime(date.Value.Year, date.Value.Month, date.Value.Day, date.Value.Hour, date.Value.Minute, 0);
           case "second": case "ss": case "s":
               return new SqlDateTime(date.Value.Year, date.Value.Month, date.Value.Day, date.Value.Hour, date.Value.Minute, date.Value.Second);
           case "millisecond": case "ms":
               return new SqlDateTime(date.Value.Year, date.Value.Month, date.Value.Day, date.Value.Hour, date.Value.Minute, date.Value.Second, date.Value.Millisecond * 1000);
           case "microsecond": case "mcs":
               return date;
           case "nanosecond": case "ns":
               return date;
           default: throw new Exception(String.Format("'{0}' не является известным параметром PeriodBegin.", datepart.Value));
       }
    }

    /// <summary>
    /// Возвращает окончание периода
    /// </summary>
    /// <param name="datepart">Часть аргумента date, которая задает тип периода. </param>
    /// <param name="date">Выражение, которое можно привести к значению типа date.</param>
    /// <returns></returns>
    [SqlFunction(Name = "Period End", IsDeterministic = true, IsPrecise = true, DataAccess = DataAccessKind.None)]
    public static SqlDateTime PeriodEnd(SqlString datepart, SqlDateTime date)
    {
        if (datepart.IsNull || date.IsNull) return new SqlDateTime();
        switch (datepart.Value)
        {
            case "year":
            case "yy":
            case "yyyy":
                return new SqlDateTime(date.Value.Year + 1, 1, 1);
            case "quarter":
            case "qq":
            case "q":
                return new SqlDateTime(date.Value.Year, ((date.Value.Month + 2) / 3) * 3 + 1, 1);
            case "month":
            case "mm":
            case "m":
                return new SqlDateTime(date.Value.Year, date.Value.Month + 1, 1);
            case "dayofyear":
            case "dy":
            case "y":
                return new SqlDateTime(date.Value.Year + 1, 1, 1);
            case "day":
            case "dd":
            case "d":
                return new SqlDateTime(date.Value.Date.AddDays(1));
            case "week":
            case "wk":
            case "ww":
                while (date.Value.DayOfWeek != System.DayOfWeek.Sunday)
                {
                    date = date.Value.AddDays(1);
                }
                return new SqlDateTime(date.Value.Date.AddDays(1));
            case "weekday":
            case "dw":
            case "w":
                while (date.Value.DayOfWeek != System.DayOfWeek.Sunday)
                {
                    date = date.Value.AddDays(1);
                }
                return new SqlDateTime(date.Value.Date.AddDays(1));
            case "hour":
            case "hh":
                return new SqlDateTime(date.Value.Year, date.Value.Month, date.Value.Day, date.Value.Hour + 1, 0, 0);
            case "minute":
            case "mi":
            case "n":
                return new SqlDateTime(date.Value.Year, date.Value.Month, date.Value.Day, date.Value.Hour, date.Value.Minute + 1, 0);
            case "second":
            case "ss":
            case "s":
                return new SqlDateTime(date.Value.Year, date.Value.Month, date.Value.Day, date.Value.Hour, date.Value.Minute, date.Value.Second + 1);
            case "millisecond":
            case "ms":
                return new SqlDateTime(date.Value.Year, date.Value.Month, date.Value.Day, date.Value.Hour, date.Value.Minute, date.Value.Second, date.Value.Millisecond * 1000 + 3000);
            case "microsecond":
            case "mcs":
                return new SqlDateTime(date.Value.Year, date.Value.Month, date.Value.Day, date.Value.Hour, date.Value.Minute, date.Value.Second, date.Value.Millisecond * 1000 + 3000);
            case "nanosecond":
            case "ns":
                return new SqlDateTime(date.Value.Year, date.Value.Month, date.Value.Day, date.Value.Hour, date.Value.Minute, date.Value.Second, date.Value.Millisecond * 1000 + 3000);
            default: throw new Exception(String.Format("'{0}' не является известным параметром PeriodBegin.", datepart.Value));
        }
    }
};