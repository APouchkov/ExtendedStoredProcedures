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

  public static char InternalGetRightQuote(char ALeftQuote)
  {
    switch (ALeftQuote)
    {
      case '[': return ']';
      case '{': return '}';
      case '(': return ')';
      case '<': return '>';
      default: return ALeftQuote; // == !!!!
    }
  }
  public static String Quote(String AValue, Char AQuote)
  {
    //if (AValue.IsNull) return AValue;
    Char RQuote = InternalGetRightQuote(AQuote);
    return AQuote + AValue.Replace(new String(RQuote, 1), new String(RQuote, 2)) + RQuote;
  }

  /// <summary>
  /// Обёртка OPENQUERY вокруг SQL-запроса
  /// </summary>
  private static String OpenQueryString(String ALinkedServer, String AQuery)
  {
    if (String.IsNullOrEmpty(ALinkedServer) || ALinkedServer.Equals("LOCAL", StringComparison.InvariantCultureIgnoreCase))
      return AQuery;

    return "EXEC(" + Quote(AQuery, '\'') + ") AT " + Quote(ALinkedServer, '[');
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
    if (String.IsNullOrEmpty(AQuery)) return null;

    return ExecuteScalar(OpenQueryString(ALinkedServer, AQuery));
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
    if (String.IsNullOrEmpty(ALinkedServer))
      Execute(AQuery, AResultSets);
    else if (String.IsNullOrEmpty(AQuery))
      throw new ArgumentNullException(PARAM_QUERY);
    else
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
    public string Name;
    public int FieldIndex;
  }

  //private static List<TFieldAlias> CreateFieldAliases(string AFields)
  //{
  //  List<TFieldAlias> Result = new List<TFieldAlias>();

  //  return Result;
  //}

  //private static string SqlMetaDataToString(SqlMetaData MetaData)
  //{
  //  string Result = MetaData.SqlDbType.ToString();
  //  switch (MetaData.SqlDbType)
  //  {
  //      case SqlDbType.Char:
  //      case SqlDbType.NChar:
  //      case SqlDbType.VarBinary:
  //      case SqlDbType.VarChar:
  //      case SqlDbType.NVarChar:
  //        Result += "("  + (MetaData.MaxLength == -1 ? "Max" : MetaData.MaxLength.ToString()) + ")";
  //        break;
  //      case SqlDbType.DateTime2:
  //        Result += "("  + MetaData.Scale.ToString() + ")";
  //        break;
  //      case SqlDbType.Decimal:
  //        Result += "(" + MetaData.Precision.ToString() + "," + MetaData.Scale.ToString() + ")";
  //        break;
  //  }

  //  return Result;
  //}

  private static void SqlDataReaderToXml(SqlDataReader reader, XmlTextWriter writer, TRowSetMap Map)
  {
    const string ROW_TAG      = "ROW";
    const string METADATA_TAG = "METADATA";
    const string FIELD_TAG    = "FIELD";
    const string NONAME_FIELD_PREFIX = "FIELD";

    int i, empty_name_field_no = 1;
    string FieldName;

    DataTable LDataTable = reader.GetSchemaTable();
    List<TFieldAlias> Fields = new List<TFieldAlias>();
    TFieldAlias Field;
    int FieldCount = reader.FieldCount;
    // SqlDataRecord Record = new SqlDataRecord(DataReaderFields(reader));

    if(Map.Fields.Length > 0)
    {
      foreach (string FieldMap in Map.Fields.Split(new char[] {','}))
      {
        i = FieldMap.IndexOf('=');
        if(i >= 0)
        {
          Field.Name = FieldMap.Substring(0, i);
          FieldName = FieldMap.Substring(i + 1);
        }
        else
        {
          Field.Name = FieldMap;
          FieldName = FieldMap;
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
