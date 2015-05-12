using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Text;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.SqlServer.Server;
using PublicDomain.ZoneInfo;

public partial class WindowsTimeZones
{
  struct TimeZoneRow
  {
    public String   Id;
    public Int16    OffsetMinutes;
    public String   Name;
    public Boolean  Daylight;
  }

  public static void FillTimeZoneRow(object row, out SqlString Id, out SqlInt16 OffsetMinutes, out SqlString Name, out SqlBoolean Daylight)
  {
    Id	          = ((TimeZoneRow)row).Id;
    OffsetMinutes = ((TimeZoneRow)row).OffsetMinutes;
    Name          = ((TimeZoneRow)row).Name;
    Daylight      = ((TimeZoneRow)row).Daylight;
  }

  [
    SqlFunction
    (
      FillRowMethodName = "FillTimeZoneRow",
      DataAccess        = DataAccessKind.None,
      TableDefinition   = "[Id] nvarchar(256), [OffsetMinutes] smallint, [Name] nvarchar(256), [Daylight] bit", 
      IsDeterministic   = true
    )
  ]
  public static IEnumerable List()
  {
    ReadOnlyCollection<TimeZoneInfo> timeZones;
    timeZones = TimeZoneInfo.GetSystemTimeZones();

    TimeZoneRow row;
    List<TimeZoneRow> rows = new List<TimeZoneRow>();

    foreach (TimeZoneInfo timeZone in timeZones)
    {
      row.Id            = timeZone.Id;
      row.Name          = timeZone.DisplayName;
      row.OffsetMinutes = (Int16)timeZone.BaseUtcOffset.TotalMinutes;
      row.Daylight      = timeZone.SupportsDaylightSavingTime;
      rows.Add(row);
    }
    return rows;
  }

  [SqlFunction(DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public static SqlDateTime Convert(SqlDateTime SourceDateTime, SqlString SourceTimeZone, SqlString DestinationTimeZone)
  {
    string        LSourceTimeZone = (SourceTimeZone.IsNull ? "" : SourceTimeZone.Value);
    TimeZoneInfo  tzSource        = (LSourceTimeZone == "" ? TimeZoneInfo.Local : TimeZoneInfo.FindSystemTimeZoneById(LSourceTimeZone));
    DateTime      LSourceDateTime = (SourceDateTime.IsNull ? System.DateTime.Now : SourceDateTime.Value);
    TimeZoneInfo  tzDestination   = (DestinationTimeZone.IsNull ? TimeZoneInfo.Local : TimeZoneInfo.FindSystemTimeZoneById(DestinationTimeZone.Value));

    return TimeZoneInfo.ConvertTime(LSourceDateTime, tzSource, tzDestination);
  }

  [SqlFunction(DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public static SqlDateTime ConvertToUTC(SqlDateTime SourceDateTime, SqlString SourceTimeZone)
  {
    string        LSourceTimeZone = (SourceTimeZone.IsNull ? "" : SourceTimeZone.Value);
    TimeZoneInfo  tzSource        = (LSourceTimeZone == "" ? TimeZoneInfo.Local : TimeZoneInfo.FindSystemTimeZoneById(LSourceTimeZone));
    DateTime      LSourceDateTime = (SourceDateTime.IsNull ? System.DateTime.Now : SourceDateTime.Value);

    return TimeZoneInfo.ConvertTimeToUtc(LSourceDateTime, tzSource);
  }

  [SqlFunction(DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public static SqlDateTime ConvertFromUTC(SqlDateTime SourceDateTime, SqlString DestinationTimeZone)
  {
    string        LDestinationTimeZone  = (DestinationTimeZone.IsNull ? "" : DestinationTimeZone.Value);
    TimeZoneInfo  tzDestination              = (LDestinationTimeZone == "" ? TimeZoneInfo.Local : TimeZoneInfo.FindSystemTimeZoneById(LDestinationTimeZone));
    DateTime      LSourceDateTime       = (SourceDateTime.IsNull ? System.DateTime.Now : SourceDateTime.Value);

    return TimeZoneInfo.ConvertTimeFromUtc(LSourceDateTime, tzDestination);
  }

  [SqlFunction(DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public static SqlBoolean IsDaylightSavingTime(SqlDateTime SourceDateTime, SqlString SourceTimeZone)
  {
    string        LSourceTimeZone = (SourceTimeZone.IsNull ? "" : SourceTimeZone.Value);
    TimeZoneInfo  tzSource        = (LSourceTimeZone == "" ? TimeZoneInfo.Local : TimeZoneInfo.FindSystemTimeZoneById(LSourceTimeZone));
    DateTime      LSourceDateTime = (SourceDateTime.IsNull ? System.DateTime.Now : SourceDateTime.Value);

    return tzSource.IsDaylightSavingTime(LSourceDateTime);
  }

  [SqlFunction(DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public static SqlBoolean IsAmbiguousTime(SqlDateTime SourceDateTime, SqlString SourceTimeZone)
  {
    string        LSourceTimeZone = (SourceTimeZone.IsNull ? "" : SourceTimeZone.Value);
    TimeZoneInfo  tzSource        = (LSourceTimeZone == "" ? TimeZoneInfo.Local : TimeZoneInfo.FindSystemTimeZoneById(LSourceTimeZone));
    DateTime      LSourceDateTime = (SourceDateTime.IsNull ? System.DateTime.Now : SourceDateTime.Value);

    return tzSource.IsAmbiguousTime(LSourceDateTime);
  }
}

public partial class IANATimeZones
{
  static IANATimeZones()
  {
//    Database.LoadFiles(@"D:\Programms\Extended Stored Procedures\TimeZones");
    Database.OffsetInMinutes = true;
    using (SqlConnection LocalConnection = new SqlConnection("Context Connection=True"))
    {
      LocalConnection.Open();
      using (SqlCommand DataQuery = new SqlCommand("SELECT [FileName], [Lines] FROM [IANA].[TimeZones:Files]", LocalConnection))
      using (SqlDataReader DataFiles = DataQuery.ExecuteReader())
      {
        while (DataFiles.Read())
        {
          StringReader sr = new System.IO.StringReader(DataFiles.GetSqlString(1).Value);
          try
          {            
            Database.LoadFromStream(sr);
          }
          catch (Exception E)
          {
            throw new Exception(String.Format("Ошибка чтения файла '{0}': {1}", DataFiles.GetSqlString(0).Value, E.Message));
          }
        }
      }
    }
  }

  public static void ListRow(object row, out SqlString Id)
  {
    Id = (String)row;
  }

  [
    SqlFunction
    (
      FillRowMethodName = "ListRow",
      DataAccess        = DataAccessKind.Read,
      TableDefinition   = "[Id] nvarchar(256)", 
      IsDeterministic   = true
    )
  ]
  public static IEnumerable List()
  {
    List<String> rows = new List<String>();

    foreach (string TimeZoneName in Database.GetZoneNames())
    {
      rows.Add(TimeZoneName);
    }
    return rows;
  }

  struct CutoverRow
  {
    public DateTime DateTime;
    //public DateTime DateTimeUTC;
    public Int16    OffsetMinutes;
  }

  public static void FillCutoverRow(object row, out SqlDateTime DateTime/*, out SqlDateTime DateTimeUTC*/, out SqlInt16 OffsetMinutes)
  {
    DateTime      = ((CutoverRow)row).DateTime;
    //DateTimeUTC   = ((CutoverRow)row).DateTimeUTC;
    OffsetMinutes = ((CutoverRow)row).OffsetMinutes;
  }

  [
    SqlFunction
    (
      FillRowMethodName = "FillCutoverRow",
      DataAccess        = DataAccessKind.Read,
      TableDefinition   = "[DateTime] DateTime, [OffsetMinutes] SmallInt", //, [DateTimeUTC] DateTime
      IsDeterministic   = true
    )
  ]
  public static IEnumerable CutoverWindows(SqlString TimeZone, SqlDateTime DateTimeFrom, SqlDateTime DateTimeTo)
  {
    Zone  LTimeZone  = Database.GetZone(TimeZone.Value);

    CutoverRow row;
    List<CutoverRow> rows = new List<CutoverRow>();

    foreach(Zone.CutoverWindow Window in LTimeZone.GetCutoverWindows(DateTimeFrom.Value, DateTimeTo.Value))
    {
      row.DateTime      = Window.DateTime;
//      row.DateTimeUTC   = LTimeZone.ConvertToUtc(Window.DateTime);
      row.OffsetMinutes = (Int16)Window.GmtOffset.TotalMinutes;
      rows.Add(row);
    }

    return rows;
  }

  [SqlFunction(DataAccess = DataAccessKind.Read, IsDeterministic = true)]
  public static SqlDateTime Convert(SqlDateTime SourceDateTime, SqlString SourceTimeZone, SqlString DestinationTimeZone)
  {
    if (SourceDateTime.IsNull)
      return SqlDateTime.Null;

    Zone  tzSource      = Database.GetZone(SourceTimeZone.Value);
    Zone  tzDestination = Database.GetZone(DestinationTimeZone.Value);

    return tzDestination.ConvertToLocal(tzSource.ConvertToUtc(SourceDateTime.Value));
  }

  [SqlFunction(DataAccess = DataAccessKind.Read, IsDeterministic = true)]
  public static SqlDateTime ConvertToUTC(SqlDateTime SourceDateTime, SqlString SourceTimeZone)
  {
    if (SourceDateTime.IsNull)
      return SqlDateTime.Null;

    Zone  tzSource      = Database.GetZone(SourceTimeZone.Value);

    return tzSource.ConvertToUtc(SourceDateTime.Value);
  }

  [SqlFunction(DataAccess = DataAccessKind.Read, IsDeterministic = true)]
  public static SqlDateTime ConvertFromUTC(SqlDateTime SourceDateTime, SqlString DestinationTimeZone)
  {
    if (SourceDateTime.IsNull)
      return SqlDateTime.Null;

    Zone  tzDestination = Database.GetZone(DestinationTimeZone.Value);

    return tzDestination.ConvertToLocal(SourceDateTime.Value);
  }
}
