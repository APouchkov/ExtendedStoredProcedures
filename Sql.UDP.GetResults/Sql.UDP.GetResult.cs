using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using System.Xml;
using System.IO;
using System.Text;

public static class DynamicSQL
{
  private const String ContextConnection = "context connection=true";

  /// <summary>
  /// Обёртка OPENQUERY вокруг SQL-запроса
  /// </summary>
  private static String OpenQueryString(String ALinkedServer, String AQuery)
  {
    if (String.IsNullOrEmpty(ALinkedServer) || ALinkedServer.Equals("LOCAL", StringComparison.InvariantCultureIgnoreCase))
      return AQuery;

    if(String.IsNullOrEmpty(AQuery)) return AQuery;

    return "EXEC(" + Strings.Quote(AQuery, '\'') + ") AT " + Strings.Quote(ALinkedServer, '[');
  }

  [SqlFunction(Name = "Execute Scalar", DataAccess = DataAccessKind.Read, SystemDataAccess = SystemDataAccessKind.Read, IsDeterministic = false)]
  public static Object ExecuteScalar(String AQuery)
  {
    if (String.IsNullOrEmpty(AQuery)) return null;

    using (SqlConnection connection = new SqlConnection(ContextConnection))
    {
      connection.Open();
      SqlCommand cmd = new SqlCommand(AQuery, connection);
      return cmd.ExecuteScalar();
    } 
  }

  [SqlFunction(Name = "Remote Execute Scalar", DataAccess = DataAccessKind.Read, SystemDataAccess = SystemDataAccessKind.Read, IsDeterministic = false)]
  public static Object ExecuteRemoteScalar(String ALinkedServer, String AQuery)
  {
    return ExecuteScalar(OpenQueryString(ALinkedServer, AQuery));
  }

  //private const String PARAM_DATASET_NUMBER   = "dataset_number";
  private const String PARAM_RESULTSETS = "ResultSets";
  private const String PARAM_QUERY      = "Query";

  private const String DATASET_NAME_XML       = "Result";
  private const String DATASET_NUMBER_INVALID = "Набор данных не найден";

  private const String ATTRIBUTE_INDEX = "INDEX";
  private const String ATTRIBUTE_NAME  = "NAME";
  private const String ATTRIBUTE_TYPE  = "TYPE";

  public struct TRowSetMap
  {
    public Int16  Index;
    public String Name;
    public String Fields;
  }

  // <ROWSET1|NAME1>:<ALIAS1>=<NAME1>;
  public static List<TRowSetMap> ExtractRowSetMaps(String AResultSets)
  {
    List<TRowSetMap> AMaps = new List<TRowSetMap>();
    TRowSetMap LMap;

    try
    {
      foreach (String LRowSet in AResultSets.Split(new char[] {';'}))
      {
        int Idx = LRowSet.IndexOf(':');
        String LNameOrIndex;
        if(Idx >= 0)
        {
          LNameOrIndex = LRowSet.Substring(0, Idx);
          LMap.Fields = LRowSet.Substring(Idx + 1);
        }
        else
        {
          LNameOrIndex  = LRowSet;
          LMap.Fields   = "";
        }

        if(Int16.TryParse(LNameOrIndex, out LMap.Index))
          LMap.Name = null;
        else
          LMap.Name = LNameOrIndex;

        AMaps.Add(LMap);
      }
    }
    catch
    {
      throw new ArgumentException(PARAM_RESULTSETS);
    }

    return AMaps;
  }

  [SqlProcedure(Name = "Execute=>XML")]
  public static void ExecuteToXML
  (
    String AQuery,
    String AResultSets,
out SqlXml AXml,
    String ARowsetNamePrefix  = null,
    String ARootTag           = null,
    String ARowsetTag         = null,
    String ARowTag            = null,
    String ARowIndexColumn    = null
  )
  {
    const String ROWSET_TAG = "ROWSET";

    if (String.IsNullOrEmpty(AQuery))
      throw new ArgumentNullException(PARAM_QUERY);

    if (String.IsNullOrEmpty(ARowsetTag))
      ARowsetTag = ROWSET_TAG;

    List<TRowSetMap> LMaps;
    TRowSetMap LMap;

    if (AResultSets == null)
      LMaps = new List<TRowSetMap>();
    else
      LMaps = ExtractRowSetMaps(AResultSets);

    LMap.Index  = -1;
    LMap.Name   = null;
    LMap.Fields = "";

    using (SqlConnection LSqlConnection = new SqlConnection(ContextConnection))
    {
      LSqlConnection.Open();

      using (SqlCommand LSqlCommand = new SqlCommand(AQuery, LSqlConnection))
      using (SqlDataReader LReader = LSqlCommand.ExecuteReader())
      {
        int LRowsetNo = 0;
        int i = 0;

        // !!! Нельзя делать ms.Dispose, потому что данные тянет из ms уже в T-SQL-коде ("извне" этой процедуры )
        MemoryStream  LMemoryStream = new MemoryStream();
        XmlTextWriter LXml          = new XmlTextWriter(LMemoryStream, Encoding.Unicode);

        if(!String.IsNullOrEmpty(ARootTag))
          LXml.WriteStartElement(ARootTag);
        else
          AXml = SqlXml.Null;

        do
        {
          if(LReader.IsClosed) break;

          String LRowsetName = null;
          //DataTable LDataTable = LReader.GetSchemaTable();
          if (!String.IsNullOrEmpty(ARowsetNamePrefix))
          {
            String LColumn0Name = LReader.GetName(0);
            //SqlContext.Pipe.Send("LColumn0Name = " + LColumn0Name);

            if (!String.IsNullOrWhiteSpace(LColumn0Name) && LColumn0Name.StartsWith(ARowsetNamePrefix, StringComparison.InvariantCultureIgnoreCase))
              LRowsetName = LColumn0Name.Substring(ARowsetNamePrefix.Length);
          }

          if (AResultSets == null)
            i = ++LRowsetNo;
          else
          {
            LRowsetNo++;
            i = LMaps.FindIndex
                (
                  (
                    delegate(TRowSetMap AMap)
                    {
                      return (AMap.Index > 0 && AMap.Index == LRowsetNo) || (AMap.Index == 0 && LRowsetName != null && AMap.Name == LRowsetName);
                    }
                  ) 
                );
            
            if (i < 0) continue;
            LMap = LMaps[i++];
          }

          LXml.WriteStartElement(LRowsetName ?? ARowsetTag);

          LXml.WriteStartAttribute(ATTRIBUTE_INDEX);
          LXml.WriteValue(i.ToString());

          //if(LRowsetName != null)
          //{
          //  LXml.WriteStartAttribute(ATTRIBUTE_NAME);
          //  LXml.WriteValue(LRowsetName);
          //}

          SqlDataReaderToXml(LReader, LXml, (LRowsetName != null), ARowTag, ARowIndexColumn, LMap);
          LXml.WriteEndElement();

        } while (LReader.NextResult());

        if(!String.IsNullOrEmpty(ARootTag))
          LXml.WriteEndElement();

        LXml.Flush();
        AXml = new SqlXml(LMemoryStream);
      }
    }
  }

  [SqlProcedure(Name = "Execute")]
  public static void Execute(String AQuery, String AResultSets)
  {
    if (String.IsNullOrEmpty(AQuery))
      throw new ArgumentNullException(PARAM_QUERY);

    List<TRowSetMap> Maps;
    TRowSetMap Map;

    if (AResultSets == null)
      Maps = new List<TRowSetMap>();
    else
      Maps = ExtractRowSetMaps(AResultSets);

    Map.Index  = -1;
    Map.Name   = null;
    Map.Fields = "";

    using (SqlConnection conn = new SqlConnection(ContextConnection))
    {
      conn.Open();
      using (SqlCommand cmd = new SqlCommand(AQuery, conn))
      using (SqlDataReader reader = cmd.ExecuteReader())
      {
        int dataset_no = 0;
        int i = 0, j = 0;

        do
        {
          if (AResultSets == null)
            i = ++dataset_no;
          else
          {
            do
            {
              dataset_no++;
              i = Maps.FindIndex( (delegate(TRowSetMap AMap) { return AMap.Index == dataset_no; }) );
            } while ((i < 0) && reader.NextResult()) ; // Пропускаем ненужные датасеты

            if(i >= 0)
            { 
              if(i != j)
                throw new SystemException("Изменение порядка следования рекордсетов в данной процедуре невозможно!");
              else
                Map = Maps[j++];
            }
            else
              break;

          }

          if (!reader.IsClosed)
          {
            SendTable(reader, Map);
          }
        } // Читаем дальше, если есть что читать и не выбраны все номера нужных рекордсетов
        while (reader.NextResult());
      }
    }
  }

  [SqlProcedure(Name = "Remote Execute")]
  public static void ExecuteRemote(String ALinkedServer, String AQuery, String AResultSets)
  {
    Execute(OpenQueryString(ALinkedServer, AQuery), AResultSets);
  }

  [SqlProcedure(Name = "Execute=>Non Query")]
  public static void ExecuteNonQuery(String AQuery)
  {
    if (String.IsNullOrEmpty(AQuery))
      throw new ArgumentNullException(PARAM_QUERY);

    using (SqlConnection conn = new SqlConnection(ContextConnection))
    {
      conn.Open();
      using (SqlCommand cmd = new SqlCommand(AQuery, conn))
        cmd.ExecuteNonQuery();
    }
  }

  [SqlProcedure(Name = "Remote Execute=>Non Query")]
  public static void ExecuteRemoteNonQuery(String ALinkedServer, String AQuery)
  {
    ExecuteNonQuery(OpenQueryString(ALinkedServer, AQuery));
  }

  private static void SendTable(SqlDataReader reader, TRowSetMap Map)
  {
    //SqlDataRecord ReadRecord = new SqlDataRecord(DataReaderFields(reader));
    DataTable LDataTable = reader.GetSchemaTable();
    SqlDataRecord WriteRecord;

    List<TFieldAlias> Fields = new List<TFieldAlias>();
    TFieldAlias Field;
    string FieldName;
    int FieldCount = reader.FieldCount, WriteFieldCount = 0;
    int i;
    SqlMetaData[] WriteFields;

    if(Map.Fields.Length > 0)
    {
      WriteFields = new SqlMetaData[0];

      foreach (string FieldMap in Map.Fields.Split(new char[] {','}))
      {
        i = FieldMap.IndexOf('=');
        if(i >= 0)
        {
          Field.Name = FieldMap.Substring(0, i);
          FieldName  = FieldMap.Substring(i + 1);
        }
        else
        {
          Field.Name = FieldMap;
          FieldName  = FieldMap;
        }

        for(i = 0; i < FieldCount; i++)
        {
          if(FieldName.ToUpper() == reader.GetName(i).ToUpper())
            break;
        }
        if((i < 0) || (i >= FieldCount))
          throw new SystemException("RowSet Field = [" + FieldName + "] not found.");
        Field.FieldIndex = i;
        Fields.Add(Field);

        Array.Resize(ref WriteFields, ++WriteFieldCount);
        //WriteFields[WriteFieldCount - 1] = SqlMetaData(LDataTable.Rows[WriteFieldCount - 1], Field.Name);
        WriteFields[WriteFieldCount - 1] = SqlMetaData(LDataTable.Rows[Field.FieldIndex], Field.Name);
      }
    }
    else
    {
      WriteFields = new SqlMetaData[FieldCount];
      for (; WriteFieldCount < reader.FieldCount; WriteFieldCount++)
        WriteFields[WriteFieldCount] = SqlMetaData(LDataTable.Rows[WriteFieldCount]);
    }
    WriteRecord = new SqlDataRecord(WriteFields);

    try
    {
      SqlContext.Pipe.SendResultsStart(WriteRecord);
      Object[] values = new Object[FieldCount];

      while (reader.Read())
      {
        reader.GetValues(values);
        if(Map.Fields.Length > 0)
        {
          for(i = 0; i < WriteFieldCount; i++)
            WriteRecord.SetValue(i, values[Fields[i].FieldIndex]);
        }
        else
        {
          WriteRecord.SetValues(values);
        }
        SqlContext.Pipe.SendResultsRow(WriteRecord);
      }
    }
    finally
    {
      SqlContext.Pipe.SendResultsEnd();
    }
  }

  private static SqlMetaData SqlMetaData(DataRow AColumn, string AColumnName = "")
  {
    // Номера строк в таблице, возвращаемой SqlDataReader.GetSchemaTable
    const int COLUMN_COLUMN_NAME        = 0;
    const int COLUMN_LENGTH             = 2;
    const int COLUMN_NUMERIC_PRECISION  = 3;
    const int COLUMN_NUMERIC_SCALE      = 4;
    const int COLUMN_DATA_TYPE          = 24;
      
    if(AColumnName.Length == 0)
      AColumnName = Convert.ToString(AColumn[COLUMN_COLUMN_NAME]);

    SqlDbType LSqlDbType  = SqlServerTypeToSqlDbType(Convert.ToString(AColumn[COLUMN_DATA_TYPE]));
    long      LMaxLength  = Convert.ToInt32(AColumn[COLUMN_LENGTH]);

    switch (LSqlDbType)
    {
      case SqlDbType.Char:
      case SqlDbType.NChar:
      case SqlDbType.VarChar:
      case SqlDbType.NVarChar:
      case SqlDbType.Binary:
      case SqlDbType.VarBinary:
        return new SqlMetaData(AColumnName, LSqlDbType, (LMaxLength > 8000) ? -1 : LMaxLength);
      case SqlDbType.Text:
      case SqlDbType.NText:
      case SqlDbType.Image:
        return new SqlMetaData(AColumnName, LSqlDbType, -1);
      case SqlDbType.Decimal:
      case SqlDbType.DateTime2:
        return new SqlMetaData(AColumnName, LSqlDbType, Convert.ToByte(AColumn[COLUMN_NUMERIC_PRECISION]), Convert.ToByte(AColumn[COLUMN_NUMERIC_SCALE]));
      //case SqlDbType.UniqueIdentifier:
      default:
        return new SqlMetaData(AColumnName, LSqlDbType);
    };
  }

  private static string SqlMetaDataToString(DataRow AColumn)
  {
    // Номера строк в таблице, возвращаемой SqlDataReader.GetSchemaTable
    // const int COLUMN_COLUMN_NAME        = 0;

    const int COLUMN_LENGTH             = 2;
    const int COLUMN_NUMERIC_PRECISION  = 3;
    const int COLUMN_NUMERIC_SCALE      = 4;
    const int COLUMN_DATA_TYPE          = 24;

    String    LTypeName   = Convert.ToString(AColumn[COLUMN_DATA_TYPE]);
    Int64     LMaxLength  = Convert.ToInt64(AColumn[COLUMN_LENGTH]);

    if
    (
      LTypeName.Equals(SqlDbType.Char.ToString(), StringComparison.InvariantCultureIgnoreCase)
      || LTypeName.Equals(SqlDbType.NChar.ToString(), StringComparison.InvariantCultureIgnoreCase)
      || LTypeName.Equals(SqlDbType.VarChar.ToString(), StringComparison.InvariantCultureIgnoreCase)
      || LTypeName.Equals(SqlDbType.NVarChar.ToString(), StringComparison.InvariantCultureIgnoreCase)
      || LTypeName.Equals(SqlDbType.Binary.ToString(), StringComparison.InvariantCultureIgnoreCase)
      || LTypeName.Equals(SqlDbType.VarBinary.ToString(), StringComparison.InvariantCultureIgnoreCase)
    )
      LTypeName += "("  + (LMaxLength == -1 || LMaxLength > 8000 ? "Max" : LMaxLength.ToString()) + ")";
    else if
    (
      LTypeName.Equals(SqlDbType.DateTime2.ToString(), StringComparison.InvariantCultureIgnoreCase)
    )
      LTypeName += "("  + Convert.ToString(AColumn[COLUMN_NUMERIC_SCALE]) + ")";
    else if
    (
      LTypeName.Equals(SqlDbType.Decimal.ToString(), StringComparison.InvariantCultureIgnoreCase)
    )
      LTypeName += "(" + Convert.ToString(AColumn[COLUMN_NUMERIC_PRECISION]) + "," + Convert.ToString(AColumn[COLUMN_NUMERIC_SCALE]) + ")";

    return LTypeName;
  }

  private static SqlDbType SqlServerTypeToSqlDbType(string data_type)
  {
    SqlDbType result;
    try
    {
      result = (SqlDbType)Enum.Parse(typeof(SqlDbType), data_type, true /* case-insensitive */);
    }
    catch (ArgumentException) // Тип не парсится - сюда можно добавлять исключения, пока всегда будет возвращаться как строка
    {
      result = SqlDbType.NVarChar;
    };

    return result;
  }

  struct TFieldAlias
  {
    public String Name;
    public Int32  FieldIndex;
  }

  private static void SqlDataReaderToXml
  (
    SqlDataReader AReader,
    XmlTextWriter AWriter,
    Boolean       ASkipNameColumn,
    String        ARowTag,
    String        ARowIndexColumn,
    TRowSetMap    AMap
  )
  {
    const String ROW_TAG      = "ROW";
    const String METADATA_TAG = "METADATA";
    const String FIELD_TAG    = "FIELD";
    const String NONAME_FIELD_PREFIX = "FIELD";

    int
      i,
      start_field_index   = ASkipNameColumn ? 1 : 0,
      empty_name_field_no = 1;
    String FieldName;

    DataTable LDataTable = AReader.GetSchemaTable();

    List<TFieldAlias> LFields = new List<TFieldAlias>();
    TFieldAlias LField;
    int FieldCount = AReader.FieldCount;

    if(AMap.Fields.Length > 0)
    {
      foreach (String FieldMap in AMap.Fields.Split(new char[] {','}))
      {
        i = FieldMap.IndexOf('=');
        if(i >= 0)
        {
          LField.Name = FieldMap.Substring(0, i);
          FieldName   = FieldMap.Substring(i + 1);
        }
        else
        {
          LField.Name = FieldMap;
          FieldName   = FieldMap;
        }
        for(i = start_field_index; i < FieldCount; i++)
        {
          if(FieldName.Equals(AReader.GetName(i), StringComparison.InvariantCultureIgnoreCase))
            break;
        }
        if((i < 0) || (i >= FieldCount))
          throw new SystemException("RowSet Field = [" + FieldName + "] not found.");
        // LField.Name = XmlConvert.EncodeLocalName(LField.Name);
        LField.FieldIndex = i;
        LFields.Add(LField);

        //AWriter.WriteStartAttribute(LField.Name);
        //AWriter.WriteValue(SqlMetaDataToString(LDataTable.Rows[i]));
      }
    }
    else
    {
      for(LField.FieldIndex = start_field_index; LField.FieldIndex < FieldCount; LField.FieldIndex++)
      {
        FieldName = AReader.GetName(LField.FieldIndex);
        if(FieldName.Length == 0)
          FieldName = NONAME_FIELD_PREFIX + (empty_name_field_no++).ToString();
        LField.Name = FieldName;
        //LField.Name = XmlConvert.EncodeLocalName(FieldName);
        LFields.Add(LField);

        //AWriter.WriteStartAttribute(LField.Name);
        //AWriter.WriteValue(SqlMetaDataToString(LDataTable.Rows[LField.FieldIndex]));
      }
    }

    AWriter.WriteStartElement(METADATA_TAG);
    for (i = 0; i < LFields.Count; i++)
    {
      AWriter.WriteStartElement(FIELD_TAG);
        AWriter.WriteStartAttribute(ATTRIBUTE_INDEX);
        AWriter.WriteValue(i + 1);
        AWriter.WriteStartAttribute(ATTRIBUTE_NAME);
        AWriter.WriteValue(LFields[i].Name);
        AWriter.WriteStartAttribute(ATTRIBUTE_TYPE);
        AWriter.WriteValue(SqlMetaDataToString(LDataTable.Rows[LFields[i].FieldIndex]));
      AWriter.WriteEndElement();
    }
    AWriter.WriteEndElement();

    object Value;
    Int64 LIndex = 0;
    while (AReader.Read())
    {
      AWriter.WriteStartElement(ARowTag ?? ROW_TAG);

      if(!String.IsNullOrEmpty(ARowIndexColumn))
      {
          AWriter.WriteStartAttribute(ARowIndexColumn);
            AWriter.WriteValue(++LIndex);
          AWriter.WriteEndAttribute();
      }

      for (i = 0; i < LFields.Count; i++)
      {
        Value = AReader.GetValue(LFields[i].FieldIndex); 
        if (Value != DBNull.Value) // NULL пропускаем
        {
          AWriter.WriteStartAttribute(XmlConvert.EncodeLocalName(LFields[i].Name));
          try
          {
            AWriter.WriteValue(Value);
          }
          catch (InvalidCastException)
          {
            AWriter.WriteValue(Value.ToString());
          }

          AWriter.WriteEndAttribute();
        }
      }
      AWriter.WriteEndElement();
    }
  }

#if TPARAMS
  //[SqlFunction(Name = "Final SQL(Custom)", DataAccess = DataAccessKind.None, IsDeterministic = true)]
  // EXEC [A].[B] :Param1, :[Param2], :[$Params;Param2]
  public static String FinalSQLEx(String ASQL, UDT.TParams AParams, Boolean AOnlyQuoted)
  {
    if(String.IsNullOrEmpty(ASQL)) return ASQL;

    StringBuilder Result = new StringBuilder(ASQL.Length);

    Sql.ParamsParser Parser =
      new Sql.ParamsParser
          (
            ASQL,
            ':',
            TCommentMethods.DoubleMinus | TCommentMethods.SlashRange,
            new char[] { '[' },
            new char[] { '\'', '"', '[' }
          );

    while (Parser.MoveNext())
    {
      Result.Append(Parser.Current.Gap);
      Object LParamValue;
      if (!String.IsNullOrEmpty(Parser.Current.Value))
        if (AOnlyQuoted && (Parser.Current.Quote == (Char)0))
        {
          Result.Append(':');
          Result.Append(Parser.Current.Value);
        }
        else if (AParams == null)
          Result.Append("NULL");
        else if(Parser.Current.Value[0] == '$')
        {
          String LValues = (Parser.Current.Value.Length == 1 ? AParams.CastAsString() : AParams.CastAsStringCustom(Parser.Current.Value.Substring(1)));
          Result.Append(LValues == null ? "NULL" : Strings.QuoteString(LValues));
        }
        else if(AParams.TryGetValue(Parser.Current.Value, out LParamValue))
        {
          Result.Append(Sql.ValueToText(LParamValue, Sql.ValueDbStyle.SQL, '\''));
        }
        else
          Result.Append("NULL");
    }

    return Result.ToString();
  }

  [SqlFunction(Name = "Final SQL", DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public static String FinalSQL(String ASQL, UDT.TParams AParams)
  {
    return FinalSQLEx(ASQL, AParams, false);
  } 

  /// <summary>
  /// Параметризованный скалярный запрос
  /// </summary>
  [SqlFunction(Name = "Execute Scalar(Params)", DataAccess = DataAccessKind.Read, SystemDataAccess = SystemDataAccessKind.Read, IsDeterministic = false)]
  public static Object ExecuteParameterizedScalar(String AQuery, UDT.TParams AParams)
  {
    return ExecuteScalar(FinalSQL(AQuery, AParams));
  }

  /// <summary>
  /// Параметризованный скалярный запрос к Linked-серверу
  /// </summary>
  [SqlFunction(Name = "Execute Remote Scalar(Params)", DataAccess = DataAccessKind.Read, SystemDataAccess = SystemDataAccessKind.Read, IsDeterministic = false)]
  public static Object ExecuteRemoteParameterizedScalar(String ALinkedServer, String AQuery, UDT.TParams AParams)
  {
    return ExecuteScalar(OpenQueryString(ALinkedServer, FinalSQL(AQuery, AParams)));
  }

  /// <summary>
  /// Выполнение SQL-запроса
  /// </summary>
  private static UDT.TParams InternalExecuteRow(String AQuery)
  {
    using (SqlConnection connection = new SqlConnection(ContextConnection))
    {
      connection.Open();
      using (SqlCommand cmd = new SqlCommand(AQuery, connection))
      using (SqlDataReader reader = cmd.ExecuteReader())
      {
        if (reader.IsClosed) return null;

        object[] values = new object[reader.FieldCount];

        if (!reader.Read()) return null;

        reader.GetSqlValues(values);
        UDT.TParams LFields = UDT.TParams.New();
        for (int i = reader.FieldCount - 1; i >= 0; i--)
        {
          if (!reader.IsDBNull(i))
            LFields.AddParam(reader.GetName(i), values[i]);
        }
        return LFields;
      }
    }
  }

  /// <summary>
  /// Строчный запрос
  /// </summary>
  [SqlFunction(Name = "Execute Row", DataAccess = DataAccessKind.Read, SystemDataAccess = SystemDataAccessKind.Read, IsDeterministic = false)]
  public static UDT.TParams ExecuteRow(String AQuery)
  {
    if (String.IsNullOrEmpty(AQuery)) return null;

    return (UDT.TParams)InternalExecuteRow(AQuery);
  }

  /// <summary>
  /// Строчный запрос к Linked-серверу
  /// </summary>
  [SqlFunction(Name = "Execute Remote Row", DataAccess = DataAccessKind.Read, SystemDataAccess = SystemDataAccessKind.Read, IsDeterministic = false)]
  public static UDT.TParams ExecuteRemoteRow(String ALinkedServer, String AQuery)
  {
    return InternalExecuteRow(OpenQueryString(ALinkedServer, AQuery));
  }

  /// <summary>
  /// Параметризованный строчный запрос
  /// </summary>
  [SqlFunction(Name = "Execute Row(Params)", DataAccess = DataAccessKind.Read, SystemDataAccess = SystemDataAccessKind.Read, IsDeterministic = false)]
  public static UDT.TParams ExecuteParameterizedRow(String AQuery, UDT.TParams AParams)
  {
    if (String.IsNullOrEmpty(AQuery)) return null;

    return InternalExecuteRow(FinalSQL(AQuery, AParams));
  }

  /// <summary>
  /// Параметризованный строчный запрос к Linked-серверу
  /// </summary>
  [SqlFunction(Name = "Execute Remote Row(Params)", DataAccess = DataAccessKind.Read, SystemDataAccess = SystemDataAccessKind.Read, IsDeterministic = false)]
  public static UDT.TParams ExecuteParameterizedRemoteRow(String ALinkedServer, String AQuery, UDT.TParams AParams)
  {
    return InternalExecuteRow(OpenQueryString(ALinkedServer, FinalSQL(AQuery, AParams)));
  }

  /// <summary>
  /// Статический табличный запрос
  /// </summary>
  [SqlFunction(Name = "Open Query", FillRowMethodName = "OpenQueryRow", DataAccess = DataAccessKind.Read, SystemDataAccess = SystemDataAccessKind.Read, TableDefinition = "[Fields] TParams", IsDeterministic = false)]
  public static IEnumerable OpenQuery(String ASQL)
  {
    if (String.IsNullOrEmpty(ASQL)) return null;

      List<UDT.TParams> Rows = new List<UDT.TParams>();

      using (SqlConnection conn = new SqlConnection(ContextConnection))
      {
        conn.Open();
        using (SqlCommand cmd = new SqlCommand(ASQL, conn))
        using (SqlDataReader reader = cmd.ExecuteReader())
        {
          if (!reader.IsClosed)
          {
            object[] values = new object[reader.FieldCount];

            while (reader.Read())
            {
              reader.GetSqlValues(values);
              UDT.TParams LFields = UDT.TParams.New();
              for (int i = reader.FieldCount - 1; i >= 0; i--)
              {
                if (!reader.IsDBNull(i))
                  LFields.AddParam(reader.GetName(i), values[i]);
              }
              Rows.Add(LFields);
              //yield return LFields;
            }
          }
        }
      }

    return Rows;
  }
  public static void OpenQueryRow(object Row, out UDT.TParams Value)
  {
    Value = (UDT.TParams)Row;
  }

  /// <summary>
  /// Статический табличный запрос к Linked-серверу
  /// </summary>
  [SqlFunction(Name = "Open Remote Query", FillRowMethodName = "OpenQueryRow", DataAccess = DataAccessKind.Read, SystemDataAccess = SystemDataAccessKind.Read, TableDefinition = "[Fields] TParams", IsDeterministic = false)]
  public static IEnumerable OpenRemoteQuery(String ALinkedServer, String ASQL)
  {
    return OpenQuery(OpenQueryString(ALinkedServer, ASQL));
  }

  /// <summary>
  /// Параметризированный табличный запрос
  /// </summary>
  [SqlFunction(Name = "Open Query(Params)", FillRowMethodName = "OpenQueryRow", DataAccess = DataAccessKind.Read, SystemDataAccess = SystemDataAccessKind.Read, TableDefinition = "[Fields] TParams", IsDeterministic = false)]
  public static IEnumerable OpenParameterizedQuery(String ASQL, UDT.TParams AParams)
  {
    if (String.IsNullOrEmpty(ASQL)) return null;

    return OpenQuery(FinalSQL(ASQL, AParams));
  }

  /// <summary>
  /// Параметризированный табличный запрос к Linked-серверу
  /// </summary>
  [SqlFunction(Name = "Open Remote Query(Params)", FillRowMethodName = "OpenQueryRow", DataAccess = DataAccessKind.Read, SystemDataAccess = SystemDataAccessKind.Read, TableDefinition = "[Fields] TParams", IsDeterministic = false)]
  public static IEnumerable OpenParameterizedRemoteQuery(String ALinkedServer, String ASQL, UDT.TParams AParams)
  {
    return OpenQuery(OpenQueryString(ALinkedServer, FinalSQL(ASQL, AParams)));
  }

  //[SqlProcedure(Name = "Execute(Params)=>XML")]
  public static void ExecuteParameterizedToXML
  (
    String      AQuery,
    UDT.TParams AParams, 
    String      AResultSets,
out SqlXml      AXml,
    String      ARowsetNamePrefix  = null,
    String      ARootTag           = null,
    String      ARowsetTag         = null,
    String      ARowTag            = null
  )
  {
    if (String.IsNullOrEmpty(AQuery))
      throw new ArgumentNullException(PARAM_QUERY);

    ExecuteToXML
    (
      AQuery            :     FinalSQL(AQuery, AParams), 
      AResultSets       :     AResultSets, 
      AXml              : out AXml, 
      ARowsetNamePrefix : ARowsetNamePrefix,
      ARootTag          : ARootTag,
      ARowsetTag        : ARowsetTag,
      ARowTag           : ARowTag 
    );
  }

  [SqlProcedure(Name = "Execute(Params)")]
  public static void ExecuteParameterized(String AQuery, UDT.TParams AParams, String AResultSets)
  {
    if (String.IsNullOrEmpty(AQuery))
      throw new ArgumentNullException(PARAM_QUERY);

    Execute(FinalSQL(AQuery, AParams), AResultSets);
  }

  [SqlProcedure(Name = "Remote Execute(Params)")]
  public static void ExecuteRemoteParameterized(String ALinkedServer, String AQuery, UDT.TParams AParams, String AResultSets)
  {
    Execute(OpenQueryString(ALinkedServer, FinalSQL(AQuery, AParams)), AResultSets);
  }

  [SqlProcedure(Name = "Execute(Params)=>Non Query")]
  public static void ExecuteParameterizedNonQuery(String AQuery, UDT.TParams AParams)
  {
    if (String.IsNullOrEmpty(AQuery))
      throw new ArgumentNullException(PARAM_QUERY);

    ExecuteNonQuery(FinalSQL(AQuery, AParams));
  }

  [SqlProcedure(Name = "Remote Execute(Params)=>Non Query")]
  public static void ExecuteRemoteParameterizedNonQuery(String ALinkedServer, String AQuery, UDT.TParams AParams)
  {
    ExecuteNonQuery(OpenQueryString(ALinkedServer, FinalSQL(AQuery, AParams)));
  }
#endif
}
