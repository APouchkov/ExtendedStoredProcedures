using System;

public class Sql
{
#region Converting Params

  /// <summary>
  /// Конвертирует тип значения параметра из строки в SqlDbType
  /// </summary>
  /// <param name="type">Тип параметра, заданный строкой</param>
  /// <returns>Тип значения параметра</returns>
  private SqlDbType TypeFromString(String type)
  {
    if (String.IsNullOrEmpty(type)) return SqlDbType.VarChar;
    //if (type == this.GetType().Name) return SqlDbType.Udt;
    try
    {
      SqlDbType result = (SqlDbType)Enum.Parse(typeof(SqlDbType), type, true);
      if (!Enum.IsDefined(typeof(SqlDbType), result)) throw new Exception();
      switch (result)
      {
        // Not support
        //case SqlDbType.DateTimeOffset:
        case SqlDbType.Image:
        case SqlDbType.NText:
        case SqlDbType.Structured:
        case SqlDbType.Text:
        case SqlDbType.Timestamp:
        case SqlDbType.Variant: 
          throw new Exception("TypeFromString");
        default: 
          return result;
      }
    }
    catch
    {
      throw new Exception(String.Format("Тип '{0}' не поддерживается средой CLR", type));
    }
  }

  /// <summary>
  /// Определяет SQL тип у значения параметра
  /// </summary>
  /// <param name="value">Значение параметра</param>
  /// <returns>Тип значения параметра</returns>
  public static SqlDbType GetSqlType(Object value)
  {
    if (value is SqlByte) return SqlDbType.TinyInt; // TinyInt
    if (value is SqlInt16) return SqlDbType.SmallInt; // SmallInt
    if (value is SqlInt32) return SqlDbType.Int; // Int32
    if (value is SqlInt64) return SqlDbType.BigInt;   // BigInt
    if (value is SqlBytes) return SqlDbType.VarBinary;  // Binary, VarBinary
    if (value is SqlBoolean) return SqlDbType.Bit;    // Bit
    if (value is SqlString) return SqlDbType.VarChar; // Char, NChar, VarChar, NVarChar

    if (value is DateTime && ((DateTime)value) == ((DateTime)value).Date) return SqlDbType.Date; // Date
    if (value is DateTime) return SqlDbType.DateTime2; // DateTime2
    if (value is SqlDateTime) return SqlDbType.DateTime; // DateTime, SmallDateTime
    if (value is TimeSpan) return SqlDbType.Time; // Time
    if (value is DateTimeOffset) return SqlDbType.DateTimeOffset; // DateTimeOffset ??? Не понятно как передать этот тип обратно на сервер ???

    if (value is SqlDecimal) return SqlDbType.Decimal; // Numeric, Decimal
    if (value is SqlDouble) return SqlDbType.Float; // Float
    if (value is SqlMoney) return SqlDbType.Money; // Money
    if (value is SqlSingle) return SqlDbType.Real; // Real
    if (value is SqlGuid) return SqlDbType.UniqueIdentifier; // UniqueIdentifier
    if (value is SqlXml) return SqlDbType.Xml; // UniqueIdentifier
    if (value is TParams) return SqlDbType.Udt; // Udt
    else
      throw new Exception(String.Format("Тип '{0}' не поддерживается средой CLR", value.GetType().Name));
  }

  /// <summary>
  /// Вид значения параметра
  /// </summary>
  public enum ValueDbStyle
  {
    /// <summary>
    /// Значение в текстовом виде
    /// </summary>
    Text = 0,
    /// <summary>
    /// Значение в стандарте XML
    /// </summary>
    Xml = 1,
  }

  /// <summary>
  /// Конвертирует значение параметра в строку
  /// </summary>
  public String ValueToString(Object value, ValueDbStyle style)
  {
    SqlDbType type = GetSqlType(value);
    if (style == ValueDbStyle.Xml)
      switch (type)
      {
        case SqlDbType.BigInt: return XmlConvert.ToString((Int64)(SqlInt64)value);
        case SqlDbType.Binary: return Convert.ToBase64String(((SqlBytes)value).Value);
        case SqlDbType.Bit: return (Boolean)(SqlBoolean)value ? "1" : "0";
        case SqlDbType.Char: return (String)(SqlString)value;
        case SqlDbType.Date: return XmlConvert.ToString((DateTime)value, XmlDateTimeSerializationMode.RoundtripKind);
        case SqlDbType.DateTime: return XmlConvert.ToString((DateTime)(SqlDateTime)value, XmlDateTimeSerializationMode.RoundtripKind);
        case SqlDbType.SmallDateTime: return XmlConvert.ToString((DateTime)(SqlDateTime)value, XmlDateTimeSerializationMode.RoundtripKind);
        case SqlDbType.DateTime2: return XmlConvert.ToString((DateTime)value, XmlDateTimeSerializationMode.RoundtripKind);
        case SqlDbType.Time: return Convert.ToString((TimeSpan)value);  // XmlConvert.ToString((TimeSpan)value);
        case SqlDbType.DateTimeOffset: return XmlConvert.ToString((DateTimeOffset)value);

        case SqlDbType.Decimal: return XmlConvert.ToString((Decimal)(SqlDecimal)value);
        case SqlDbType.Float: return XmlConvert.ToString((Double)(SqlDouble)value);
        case SqlDbType.Int: return XmlConvert.ToString((Int32)(SqlInt32)value);
        case SqlDbType.Money: return XmlConvert.ToString((Decimal)(SqlMoney)value);
        case SqlDbType.NChar: return (String)(SqlString)value;
        case SqlDbType.NVarChar: return (String)(SqlString)value;
        case SqlDbType.Real: return XmlConvert.ToString((Double)(SqlSingle)value);
        case SqlDbType.SmallInt: return XmlConvert.ToString((Int16)(SqlInt16)value);
        case SqlDbType.SmallMoney: return XmlConvert.ToString((Decimal)(SqlMoney)value);
        case SqlDbType.TinyInt: return XmlConvert.ToString((Byte)(SqlByte)value);
        case SqlDbType.UniqueIdentifier: return XmlConvert.ToString((Guid)(SqlGuid)value).ToUpper();
        case SqlDbType.VarBinary: return Convert.ToBase64String(((SqlBytes)value).Value);
        case SqlDbType.VarChar: return (String)(SqlString)value;

        case SqlDbType.Xml: return ((SqlXml)value).Value;
        case SqlDbType.Udt: return value.ToString();

        // Not support SqlDbType.Image
        // Not support SqlDbType.NText
        // Not support SqlDbType.Structured
        // Not support SqlDbType.Text
        // Not support SqlDbType.Timestamp
        // Not support SqlDbType.Variant
      }
    else if (style == ValueDbStyle.Text)
    {
      switch (type)
      {
        case SqlDbType.Bit: return Convert.ToString((Boolean)(SqlBoolean)value, CultureInfo);
        case SqlDbType.TinyInt: return Convert.ToString((Byte)(SqlByte)value, CultureInfo);
        case SqlDbType.SmallInt: return Convert.ToString((Int16)(SqlInt16)value, CultureInfo);
        case SqlDbType.Int: return Convert.ToString((Int32)(SqlInt32)value, CultureInfo);
        case SqlDbType.BigInt: return Convert.ToString((Int64)(SqlInt64)value, CultureInfo);

        case SqlDbType.Binary: return Convert.ToBase64String(((SqlBytes)value).Value);
        case SqlDbType.Char: return ((SqlString)value).ToString();
        case SqlDbType.Decimal: return Convert.ToString((Decimal)(SqlDecimal)value, CultureInfo);
        case SqlDbType.Float: return Convert.ToString((Double)(SqlDouble)value, CultureInfo);

        case SqlDbType.Date: return ((DateTime)value).ToString(CultureInfo.DateTimeFormat.ShortDatePattern, CultureInfo);
        case SqlDbType.DateTime: return Convert.ToString((DateTime)(SqlDateTime)value, CultureInfo);
        case SqlDbType.SmallDateTime: return Convert.ToString((DateTime)(SqlDateTime)value, CultureInfo);
        case SqlDbType.DateTime2: return Convert.ToString((DateTime)value, CultureInfo);
        case SqlDbType.Time: return Convert.ToString((TimeSpan)value, CultureInfo);
        case SqlDbType.DateTimeOffset: return Convert.ToString((DateTimeOffset)value, CultureInfo);

        // Not support SqlDbType.Image  
        case SqlDbType.Money: return Convert.ToString((Decimal)(SqlMoney)value, CultureInfo);
        case SqlDbType.NChar: return ((SqlString)value).ToString();

        // Not support SqlDbType.NText
        case SqlDbType.NVarChar: return ((SqlString)value).ToString();
        case SqlDbType.Real: return Convert.ToString((Double)(SqlSingle)value, CultureInfo);
        case SqlDbType.SmallMoney: return Convert.ToString((Decimal)(SqlMoney)value, CultureInfo);
        // Not support SqlDbType.Structured
        // Not support SqlDbType.Text
        // Not support SqlDbType.Timestamp
        case SqlDbType.UniqueIdentifier: return ((SqlGuid)value).ToString();

        case SqlDbType.VarBinary: return Convert.ToBase64String(((SqlBytes)value).Value);
        case SqlDbType.VarChar: return ((SqlString)value).ToString();

        // Not support SqlDbType.Variant
        case SqlDbType.Xml: return ((SqlXml)value).Value;
        case SqlDbType.Udt: return value.ToString();
      }
    }
    throw new Exception("Системная ошибка"); // Сюда никогда не должно попасть
  }

  /// <summary>
  /// Конвертирует значение параметра из строки
  /// </summary>
  public Object ValueFromString(String value, SqlDbType type, ValueDbStyle style)
  {
    try
    {
      if (style == ValueDbStyle.Xml)
        switch (type)
        {
          case SqlDbType.BigInt: return new SqlInt64(XmlConvert.ToInt64(value));
          case SqlDbType.Binary: return new SqlBytes(Convert.FromBase64String(value));
          case SqlDbType.Bit: return new SqlBoolean(XmlConvert.ToBoolean(value));
          case SqlDbType.Char: return new SqlString(value);

          case SqlDbType.Date: return XmlConvert.ToDateTime(value, XmlDateTimeSerializationMode.RoundtripKind).Date;
          case SqlDbType.DateTime: return new SqlDateTime(XmlConvert.ToDateTime(value, XmlDateTimeSerializationMode.RoundtripKind));
          case SqlDbType.DateTime2: return XmlConvert.ToDateTime(value, XmlDateTimeSerializationMode.RoundtripKind);
          case SqlDbType.Time: TimeSpan resultTime; return TimeSpan.TryParse(value, out resultTime) ? resultTime : XmlConvert.ToTimeSpan(value);
          case SqlDbType.DateTimeOffset: return XmlConvert.ToDateTimeOffset(value);
          // Not support SqlDbType.Timestamp

          case SqlDbType.Decimal: return new SqlDecimal(XmlConvert.ToDecimal(value));
          case SqlDbType.Float: return new SqlDouble(XmlConvert.ToDouble(value));
          // Not support SqlDbType.Image
          case SqlDbType.Int: return new SqlInt32(XmlConvert.ToInt32(value));
          case SqlDbType.Money: return new SqlMoney(XmlConvert.ToDecimal(value));
          case SqlDbType.NChar: return new SqlString(value);
          // Not support SqlDbType.NText
          case SqlDbType.NVarChar: return new SqlString(value);
          case SqlDbType.Real: return new SqlSingle(XmlConvert.ToDouble(value));
          case SqlDbType.SmallDateTime: return new SqlDateTime(XmlConvert.ToDateTime(value, XmlDateTimeSerializationMode.RoundtripKind));
          case SqlDbType.SmallInt: return new SqlInt16(XmlConvert.ToInt16(value));
          case SqlDbType.SmallMoney: return new SqlMoney(XmlConvert.ToDecimal(value));
          // Not support SqlDbType.Structured
          // Not support SqlDbType.Text
          case SqlDbType.TinyInt: return new SqlByte(XmlConvert.ToByte(value));
          case SqlDbType.UniqueIdentifier: return new SqlGuid(XmlConvert.ToGuid(value));
          case SqlDbType.VarBinary: return new SqlBytes(Convert.FromBase64String(value));
          case SqlDbType.VarChar: return new SqlString(value);
          // Not support SqlDbType.Variant

          case SqlDbType.Xml:
            {
              XmlReader r = XmlReader.Create(new System.IO.StringReader(value));
              return new SqlXml(r);
            }

          //case SqlDbType.Udt:
          //  {
          //    TParams result = (TParams)System.Activator.CreateInstance(this.GetType());
          //    XmlReader r = XmlReader.Create(new System.IO.StringReader(value));
          //    result.ReadXml(r);
          //    return result;
          //  }
        }
      else if (style == ValueDbStyle.Text)
      {
        switch (type)
        {
          case SqlDbType.BigInt: return (SqlInt64)Int64.Parse(value, CultureInfo);
          case SqlDbType.Binary: return new SqlBytes(Convert.FromBase64String(value));
          case SqlDbType.Bit: return SqlBoolean.Parse(value);
          case SqlDbType.Char: return new SqlString(value);
          case SqlDbType.Date: return DateTime.ParseExact(value, DateTimeFormats, CultureInfo, DateTimeStyles.RoundtripKind).Date;
          case SqlDbType.DateTime: return (SqlDateTime)DateTime.ParseExact(value, DateTimeFormats, CultureInfo, DateTimeStyles.RoundtripKind);
          case SqlDbType.DateTime2: return DateTime.ParseExact(value, DateTimeFormats, CultureInfo, DateTimeStyles.RoundtripKind);
          case SqlDbType.Decimal: return (SqlDecimal)Decimal.Parse(value, CultureInfo);
          case SqlDbType.Float: return (SqlDouble)Double.Parse(value, CultureInfo);
          // Not support SqlDbType.Image
          case SqlDbType.Int: return (SqlInt32)Int32.Parse(value, CultureInfo);
          case SqlDbType.Money: return (SqlMoney)Decimal.Parse(value, CultureInfo);
          case SqlDbType.NChar: return new SqlString(value);
          // Not support SqlDbType.NText
          case SqlDbType.NVarChar: return new SqlString(value);
          case SqlDbType.Real: return (SqlSingle)Double.Parse(value, CultureInfo);
          case SqlDbType.SmallDateTime: return (SqlDateTime)DateTime.ParseExact(value, DateTimeFormats, CultureInfo, DateTimeStyles.RoundtripKind);
          case SqlDbType.SmallInt: return (SqlInt16)Int16.Parse(value, cultureInfo);
          case SqlDbType.SmallMoney: return (SqlMoney)Decimal.Parse(value, CultureInfo);
          // Not support SqlDbType.Structured
          // Not support SqlDbType.Text
          case SqlDbType.Time: return TimeSpan.Parse(value);
          // Not support SqlDbType.Timestamp
          case SqlDbType.TinyInt: return (SqlByte)Byte.Parse(value, CultureInfo);
          case SqlDbType.UniqueIdentifier: return SqlGuid.Parse(value);
          case SqlDbType.VarBinary: return new SqlBytes(Convert.FromBase64String(value));
          case SqlDbType.VarChar: return new SqlString(value);
          // Not support SqlDbType.Variant

          case SqlDbType.Xml:
            XmlReader r = XmlReader.Create(new System.IO.StringReader(value));
            return new SqlXml(r);

          //case SqlDbType.Udt:
          //  TParams result = (TParams)System.Activator.CreateInstance(this.GetType());
          //  result.FromString(value);
          //  return result;
        }
      }
      throw new Exception("Системная ошибка"); // Сюда никогда не должно попасть
    }
    catch
    {
      throw new Exception(String.Format("Не удалось сконвертировать значение '{0}' в тип {1}", value, type.ToString()));
    }
  }

#endregion Converting Params
}
