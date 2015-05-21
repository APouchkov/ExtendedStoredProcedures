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

    return "EXEC(" + Pub.Quote(AQuery, '\'') + ") AT " + Pub.Quote(ALinkedServer, '[');
  }

  //[SqlFunction(Name = "Final SQL(Custom)", DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public static String FinalSQLEx(String ASQL, UDT.TParams AParams, Boolean AOnlyQuoted)
  {
    if(String.IsNullOrEmpty(ASQL)) return ASQL;

    StringBuilder Result = new StringBuilder(ASQL.Length);

    SQLParamsParser Parser =
      new SQLParamsParser
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
        else if (AParams != null && AParams.TryGetValue(Parser.Current.Value, out LParamValue))
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
        UDT.TParams LFields = new UDT.TParams();
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
              UDT.TParams LFields = new UDT.TParams();
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

  //

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
    public int    RowSet;
    public string Fields;
  }

  public static List<TRowSetMap> ExtractRowSetMaps(String AResultSets)
  {
    List<TRowSetMap> AMaps = new List<TRowSetMap>();
    TRowSetMap LMap;

    try
    {
      foreach (string MapText in AResultSets.Split(new char[] {';'}))
      {
        int Idx = MapText.IndexOf(':');
        if(Idx >= 0)
        {
          LMap.RowSet = Convert.ToInt16(MapText.Substring(0, Idx));
          LMap.Fields = MapText.Substring(Idx + 1);
        }
        else
        {
          LMap.RowSet = Convert.ToInt16(MapText);
          LMap.Fields = "";
        }
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
  public static void ExecuteToXML(String AQuery, String AResultSets, out SqlXml AXml, String ATag) // , SqlBoolean AIncludeMetadata
  {
    const string ROWSET_TAG = "ROWSET";

    if (String.IsNullOrEmpty(AQuery))
      throw new ArgumentNullException(PARAM_QUERY);

    if (String.IsNullOrEmpty(ATag))
      ATag = ROWSET_TAG;

    List<TRowSetMap> Maps;
    TRowSetMap Map;

    if (AResultSets == null)
      Maps = new List<TRowSetMap>();
    else
      Maps = ExtractRowSetMaps(AResultSets);

    Map.RowSet = -1;
    Map.Fields = "";

    using (SqlConnection conn = new SqlConnection(ContextConnection))
    {
      conn.Open();
      using (SqlCommand cmd = new SqlCommand(AQuery, conn))
      using (SqlDataReader reader = cmd.ExecuteReader())
      {
        int dataset_no = 0;
        int i = 0;
        // !!! Нельзя делать ms.Dispose, потому что данные тянет из ms уже в T-SQL-коде ("извне" этой процедуры )
        MemoryStream mem_stream = new MemoryStream();
        XmlTextWriter xml_result = new XmlTextWriter(mem_stream, Encoding.Unicode); // Encoding.GetEncoding(1251)); // Windows-кодировка

        do
        {
          if (AResultSets == null)
            i = ++dataset_no;
          else
          {
            do
            {
              dataset_no++;
              i = Maps.FindIndex( (delegate(TRowSetMap AMap) { return AMap.RowSet == dataset_no; }) );
            } while ((i < 0) && reader.NextResult()) ; // Пропускаем ненужные датасеты
            if(i >= 0)
              Map = Maps[i++];
          }

          if (!reader.IsClosed)
          {
            xml_result.WriteStartElement(ATag);
            xml_result.WriteStartAttribute(ATTRIBUTE_INDEX);
            xml_result.WriteValue(i.ToString());

            SqlDataReaderToXml(reader, xml_result, Map);
            xml_result.WriteEndElement();
          }
        } // Читаем дальше, если есть что читать и не выбраны все номера нужных рекордсетов
        while (reader.NextResult());

        xml_result.Flush();

        AXml = new SqlXml(mem_stream);
      }
    }
  }

  //[SqlProcedure(Name = "Execute(Params)=>XML")]
  public static void ExecuteParameterizedToXML(String AQuery, UDT.TParams AParams, String AResultSets, out SqlXml AXml, String ATag) // , SqlBoolean AIncludeMetadata
  {
    if (String.IsNullOrEmpty(AQuery))
      throw new ArgumentNullException(PARAM_QUERY);

    ExecuteToXML(FinalSQL(AQuery, AParams), AResultSets, out AXml, ATag);
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

    Map.RowSet = -1;
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
              i = Maps.FindIndex( (delegate(TRowSetMap AMap) { return AMap.RowSet == dataset_no; }) );
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
      object[] values = new object[FieldCount];

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

    string    LTypeName   = Convert.ToString(AColumn[COLUMN_DATA_TYPE]);
    long      LMaxLength  = Convert.ToInt32(AColumn[COLUMN_LENGTH]);

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

  private static void SqlDataReaderToXml(SqlDataReader reader, XmlTextWriter writer, TRowSetMap Map)
  {
    const String ROW_TAG      = "ROW";
    const String METADATA_TAG = "METADATA";
    const String FIELD_TAG    = "FIELD";
    const String NONAME_FIELD_PREFIX = "FIELD";

    int i, empty_name_field_no = 1;
    string FieldName;

    DataTable LDataTable = reader.GetSchemaTable();
    List<TFieldAlias> Fields = new List<TFieldAlias>();
    TFieldAlias Field;
    int FieldCount = reader.FieldCount;
    // SqlDataRecord Record = new SqlDataRecord(DataReaderFields(reader));

    if(Map.Fields.Length > 0)
    {
      foreach (String FieldMap in Map.Fields.Split(new char[] {','}))
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
          if(FieldName.Equals(reader.GetName(i), StringComparison.InvariantCultureIgnoreCase))
            break;
        }
        if((i < 0) || (i >= FieldCount))
          throw new SystemException("RowSet Field = [" + FieldName + "] not found.");
        // Field.Name = XmlConvert.EncodeLocalName(Field.Name);
        Field.FieldIndex = i;
        Fields.Add(Field);

        //writer.WriteStartAttribute(Field.Name);
        //writer.WriteValue(SqlMetaDataToString(LDataTable.Rows[i]));
      }
    }
    else
    {
      for(Field.FieldIndex = 0; Field.FieldIndex < FieldCount; Field.FieldIndex++)
      {
        FieldName = reader.GetName(Field.FieldIndex);
        if(FieldName.Length == 0)
          FieldName = NONAME_FIELD_PREFIX + (empty_name_field_no++).ToString();
        Field.Name = FieldName;
        //Field.Name = XmlConvert.EncodeLocalName(FieldName);
        Fields.Add(Field);

        //writer.WriteStartAttribute(Field.Name);
        //writer.WriteValue(SqlMetaDataToString(LDataTable.Rows[Field.FieldIndex]));
      }
    }

    writer.WriteStartElement(METADATA_TAG);
    for (i = 0; i < Fields.Count; i++)
    {
      writer.WriteStartElement(FIELD_TAG);
        writer.WriteStartAttribute(ATTRIBUTE_INDEX);
        writer.WriteValue(i + 1);
        writer.WriteStartAttribute(ATTRIBUTE_NAME);
        writer.WriteValue(Fields[i].Name);
        writer.WriteStartAttribute(ATTRIBUTE_TYPE);
        writer.WriteValue(SqlMetaDataToString(LDataTable.Rows[Fields[i].FieldIndex]));
      writer.WriteEndElement();
    }
    writer.WriteEndElement();

    object Value;
    while (reader.Read())
    {
      writer.WriteStartElement(ROW_TAG);

      for (i = 0; i < Fields.Count; i++)
      {
        Value = reader.GetValue(Fields[i].FieldIndex); 
        if (Value != DBNull.Value) // NULL пропускаем
        {
          writer.WriteStartAttribute(XmlConvert.EncodeLocalName(Fields[i].Name));
          try
          {
            writer.WriteValue(Value);
          }
          catch (InvalidCastException)
          {
            writer.WriteValue(Value.ToString());
          }

          writer.WriteEndAttribute();
        }
      }
      writer.WriteEndElement();
    }
  }
}
