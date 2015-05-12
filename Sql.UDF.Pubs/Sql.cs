using System;
//using System.Collections;
//using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.Xml;
//using System.Xml.Schema;
//using System.Xml.Serialization;
using System.Data;
//using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server; 

[Serializable]
public class Sql
{
  public static void Write7BitEncodedInt(System.IO.BinaryWriter w, Int32 IValue)  
  {  
    UInt32 num = (UInt32)IValue;  
  
    while (num >= 128U)  
    {  
      w.Write((byte) (num | 128U));  
      num >>= 7;  
    }  
  
    w.Write((byte) num);  
  }
  
  public static Int32 Read7BitEncodedInt(System.IO.BinaryReader r)  
  {  
    // some names have been changed to protect the readability  
    Int32 returnValue = 0;  
    Int32 bitIndex    = 0;  
  
    while (bitIndex != 35)  
    {  
      byte currentByte = r.ReadByte();  
      returnValue |= ((Int32) currentByte & (Int32) sbyte.MaxValue) << bitIndex;  
      bitIndex += 7;  
  
      if (((Int32) currentByte & 128) == 0)  
        return returnValue;  
    }  
  
    throw new Exception("Wrong System.IO.BinaryReader.Read7BitEncodedInt");
  }

/* // SqlAnsiString
  public struct SqlAnsiString
  {
    private byte[] FBuffer;
    public byte[] Buffer { get { return FBuffer; } }

    public SqlAnsiString(byte[] ABuffer)
    {
      FBuffer = ABuffer;
    }
    public SqlAnsiString(String AString)
    {
      if(AString == null)
        FBuffer = null;
      else
      {
        FBuffer = new byte[AString.Length];
        for(int i = 0; i < AString.Length; i++)
        {
          UInt16 LChar = Convert.ToUInt16(AString[i]); 
          if(LChar > 255) 
            FBuffer[i] = 8;
          else
            FBuffer[i] = (Byte)LChar;
        }
      }
    }

    public new String ToString()
    {
      if(FBuffer == null) return null;
      StringBuilder LResult = new StringBuilder(FBuffer.Length);

      for(int i = 0; i < FBuffer.Length; i++)
        LResult.Append((Char)FBuffer[i]);

      return LResult.ToString();
    }

    public SqlAnsiString(System.IO.BinaryReader r) //Read
    {
      //Int32 Len = r.ReadUInt16();
      Int32 Len = Read7BitEncodedInt(r);
      //throw new Exception("Len = " + Len.ToString());
      FBuffer = r.ReadBytes(Len);
    }

    public void Write(System.IO.BinaryWriter w)
    {
      //w.Write((UInt16)FBuffer.Length);
      Write7BitEncodedInt(w, (Int32)FBuffer.Length);
      w.Write(FBuffer);
    }
  }
*/

#region Converting Params

  // ODBC, ISO
  private const String TextDatePattern              = "yyyy-MM-dd";
  private const String TextSmallDateTimePattern     = "yyyy-MM-dd HH:mm";
  private const String TextDateTimePattern          = "yyyy-MM-dd HH:mm:ss.FFFFFFF";
  private const String TextDateTimeOffsetPattern    = "yyyy-MM-dd HH:mm:ss.FFFFFFF zzz";

  private const String SQLDatePattern               = "yyyyMMdd";
  private const String SQLSmallDateTimePattern      = "yyyyMMdd HH:mm";
  private const String SQLDateTimePattern           = "yyyyMMdd HH:mm:ss.FFFFFFF";
  private const String SQLDateTimeOffsetPattern     = "yyyyMMdd HH:mm:ss.FFFFFFFzzz";

  private const String XMLDateTimePattern           = "yyyy-MM-ddTHH:mm:ss.FFFFFFF";
  private const String XMLDateTimeZeroOffsetPattern = "yyyy-MM-ddTHH:mm:ss.FFFFFFFZ";
  private const String XMLDateTimeOffsetPattern     = "yyyy-MM-ddTHH:mm:ss.FFFFFFFzzz";

  private static readonly String[] DateTimeFormats =
  { 
    TextDateTimePattern, TextDatePattern, TextDateTimeOffsetPattern,
    SQLDateTimePattern, SQLDatePattern, SQLDateTimeOffsetPattern,
    "yy-MM-dd HH:mm:ss.FFFFFFF"  , "yy-MM-dd",
    "yyMMdd HH:mm:ss.FFFFFFF"    , "yyMMdd",
    XMLDateTimePattern, XMLDateTimeOffsetPattern, XMLDateTimeZeroOffsetPattern
  };

  public static bool IsQuoteType(SqlDbType type)
  {
    switch (type)
    {
      case SqlDbType.VarBinary:
      case SqlDbType.Char:
      case SqlDbType.VarChar:
      case SqlDbType.NChar:
      case SqlDbType.NVarChar:

      case SqlDbType.SmallDateTime:
      case SqlDbType.Date:
      case SqlDbType.DateTime:
      case SqlDbType.DateTime2:
      case SqlDbType.Time:
      case SqlDbType.DateTimeOffset:
      case SqlDbType.UniqueIdentifier:
      case SqlDbType.Udt:
        return true;

      default:
        return false;
    }
  }

  /// <summary>
  /// Конвертирует тип значения параметра из строки в SqlDbType
  /// </summary>
  /// <param name="type">Тип параметра, заданный строкой</param>
  /// <returns>Тип значения параметра</returns>
  public static SqlDbType TypeFromString(String AType)
  {
    if (String.IsNullOrEmpty(AType)) return SqlDbType.NVarChar;

    SqlDbType LResult;
    if(!SqlDbType.TryParse(AType, true, out LResult))
      return SqlDbType.Udt;

    switch (LResult)
    {
      // Not support
      case SqlDbType.Image:
      case SqlDbType.NText:
      case SqlDbType.Structured:
      case SqlDbType.Text:
      case SqlDbType.Timestamp:
      case SqlDbType.Variant: 
        throw new Exception(String.Format("Тип '{0}' не поддерживается средой CLR", AType));
      default: 
        return LResult;
    }
  }

  /// <summary>
  /// Определяет SQL тип у значения параметра
  /// </summary>
  /// <param name="value">Значение параметра</param>
  /// <returns>Тип значения параметра</returns>
  public static SqlDbType GetSqlType(Object value)
  {
    if (value is SqlBoolean)    return SqlDbType.Bit;      // Bit
    if (value is SqlByte)       return SqlDbType.TinyInt;  // TinyInt
    if (value is SqlInt16)      return SqlDbType.SmallInt; // SmallInt
    if (value is SqlInt32)      return SqlDbType.Int;      // Int32
    if (value is SqlInt64)      return SqlDbType.BigInt;   // BigInt

    if (value is SqlBinary)     return SqlDbType.VarBinary;  // Binary, VarBinary
    if (value is SqlBytes)      return SqlDbType.VarBinary;  // Binary, VarBinary

    //if (value is SqlAnsiString) return SqlDbType.VarChar;   // Char, NChar, VarChar, NVarChar
    if (value is SqlString)     return SqlDbType.NVarChar;   // Char, NChar, VarChar, NVarChar
    if (value is SqlChars)      return SqlDbType.NVarChar;   // Char, NChar, VarChar, NVarChar

    if (value is SqlDateTime)   return SqlDbType.DateTime; // DateTime, SmallDateTime
    if (value is DateTime)
      if((DateTime)value == ((DateTime)value).Date) 
                                return SqlDbType.Date; // Date
      else
                                return SqlDbType.DateTime2; // DateTime2

    if (value is DateTimeOffset)  return SqlDbType.DateTimeOffset; // DateTimeOffset ??? Не понятно как передать этот тип обратно на сервер ???
    if (value is TimeSpan)        return SqlDbType.Time; // Time

    if (value is SqlDecimal)    return SqlDbType.Decimal; // Numeric, Decimal
    if (value is SqlDouble)     return SqlDbType.Float; // Float
    if (value is SqlMoney)      return SqlDbType.Money; // Money
    if (value is SqlSingle)     return SqlDbType.Real; // Real

    if (value is SqlGuid)       return SqlDbType.UniqueIdentifier; // UniqueIdentifier

    if (value is SqlXml)        return SqlDbType.Xml; // UniqueIdentifier

    if (value is IBinarySerialize) return SqlDbType.Udt; // Udt
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
    /// Значение в стандарте SQL
    /// </summary>
    SQL = 1,
    /// <summary>
    /// Значение в стандарте XML
    /// </summary>
    XML = 2
  }

  /// <summary>
  /// Конвертирует значение параметра в строку
  /// </summary>
  public static String ValueToString(Object value, ValueDbStyle style)
  {
    SqlDbType type = GetSqlType(value);
    switch (type)
    {
      case SqlDbType.Bit            : return ((SqlBoolean)value).IsTrue ? "1" : "0";
      case SqlDbType.TinyInt        : return XmlConvert.ToString((Byte)(SqlByte)value);
      case SqlDbType.SmallInt       : return XmlConvert.ToString((Int16)(SqlInt16)value);
      case SqlDbType.Int            : return XmlConvert.ToString((Int32)(SqlInt32)value);
      case SqlDbType.BigInt         : return XmlConvert.ToString((Int64)(SqlInt64)value);

      case SqlDbType.Char           :
      case SqlDbType.VarChar        :
          //return ((SqlAnsiString)value).ToString();
      case SqlDbType.NChar          :
      case SqlDbType.NVarChar       :
        if(value is SqlChars)
          return (String)((SqlChars)value).ToSqlString().Value;
        else
          return (String)(SqlString)value;

      case SqlDbType.Binary         :
      case SqlDbType.VarBinary      : return (value is SqlBinary) ? Convert.ToBase64String(((SqlBinary)value).Value) : Convert.ToBase64String(((SqlBytes)value).Value);

      case SqlDbType.DateTime       : return (style == ValueDbStyle.XML) ?
                                        XmlConvert.ToString((DateTime)(SqlDateTime)value, XmlDateTimeSerializationMode.RoundtripKind)
                                        :
                                        (style == ValueDbStyle.SQL) ?
                                          ((DateTime)(SqlDateTime)value).ToString(SQLDateTimePattern, CultureInfo.InvariantCulture)
                                          :
                                          ((DateTime)(SqlDateTime)value).ToString(TextDateTimePattern, CultureInfo.InvariantCulture);
      case SqlDbType.Date           : return (style == ValueDbStyle.XML) ?
                                        XmlConvert.ToString((DateTime)value, XmlDateTimeSerializationMode.RoundtripKind)
                                        :
                                        (style == ValueDbStyle.SQL)?
                                          ((DateTime)value).ToString(SQLDatePattern, CultureInfo.InvariantCulture)
                                          :
                                          ((DateTime)value).ToString(TextDatePattern, CultureInfo.InvariantCulture);
      case SqlDbType.DateTime2      : return (style == ValueDbStyle.XML)?
                                        XmlConvert.ToString((DateTime)value, XmlDateTimeSerializationMode.RoundtripKind)
                                        :
                                        (style == ValueDbStyle.SQL)?
                                          ((DateTime)value).ToString(SQLDateTimePattern, CultureInfo.InvariantCulture)
                                          :
                                          ((DateTime)value).ToString(TextDateTimePattern, CultureInfo.InvariantCulture);
      case SqlDbType.SmallDateTime  : return (style == ValueDbStyle.XML)?
                                        XmlConvert.ToString((DateTime)value, XmlDateTimeSerializationMode.RoundtripKind)
                                        :
                                        (style == ValueDbStyle.SQL)?
                                          ((DateTime)value).ToString(TextSmallDateTimePattern, CultureInfo.InvariantCulture)
                                          :
                                          ((DateTime)value).ToString(SQLSmallDateTimePattern, CultureInfo.InvariantCulture);
      case SqlDbType.DateTimeOffset : return (style == ValueDbStyle.XML)?
                                        XmlConvert.ToString((DateTimeOffset)value)
                                        :
                                        (style == ValueDbStyle.SQL) ?
                                          ((DateTimeOffset)value).ToString(SQLDateTimeOffsetPattern, CultureInfo.InvariantCulture)
                                          :
                                          ((DateTimeOffset)value).ToString(TextDateTimeOffsetPattern, CultureInfo.InvariantCulture);
      case SqlDbType.Time: return Convert.ToString((TimeSpan)value);

      //case SqlDbType.Date: return ((DateTime)value).ToString(CultureInfo.DateTimeFormat.ShortDatePattern, CultureInfo);
      //case SqlDbType.DateTime: return Convert.ToString((DateTime)(SqlDateTime)value, CultureInfo);
      //case SqlDbType.SmallDateTime: return Convert.ToString((DateTime)(SqlDateTime)value, CultureInfo);
      //case SqlDbType.DateTime2: return Convert.ToString((DateTime)value, CultureInfo);
      //case SqlDbType.Time: return Convert.ToString((TimeSpan)value, CultureInfo);
      //case SqlDbType.DateTimeOffset: return Convert.ToString((DateTimeOffset)value, CultureInfo);

      case SqlDbType.Float          : return XmlConvert.ToString((Double)(SqlDouble)value);
      case SqlDbType.Real           : return XmlConvert.ToString((Double)(SqlSingle)value);
      case SqlDbType.SmallMoney     : return XmlConvert.ToString((Decimal)(SqlMoney)value);
      case SqlDbType.Money          : return XmlConvert.ToString((Decimal)(SqlMoney)value);
      case SqlDbType.Decimal        : return XmlConvert.ToString((Decimal)(SqlDecimal)value);

      case SqlDbType.UniqueIdentifier : return XmlConvert.ToString((Guid)(SqlGuid)value).ToUpper();

      case SqlDbType.Xml              : return ((SqlXml)value).Value;
      case SqlDbType.Udt              : return value.ToString();

      // Not support SqlDbType.Image
      // Not support SqlDbType.NText
      // Not support SqlDbType.Structured
      // Not support SqlDbType.Text
      // Not support SqlDbType.Timestamp
      // Not support SqlDbType.Variant
    }
    throw new Exception("Системная ошибка"); // Сюда никогда не должно попасть
  }

  /// <summary>
  /// Конвертирует значение параметра в текст
  /// </summary>
  public static String ValueToText(Object AValue, ValueDbStyle AStyle, Char AQuote)
  {
    String LResult = ValueToString(AValue, AStyle);
    if (IsQuoteType(GetSqlType(AValue)))
      LResult = Pub.Quote(LResult, AQuote);
    return LResult;
  }

  /// <summary>
  /// Конвертирует значение параметра из строки
  /// </summary>
  public static Object ValueFromString(String value, SqlDbType type, ValueDbStyle style)
  {
    try
    {
//      if (style == ValueDbStyle.XML)
        switch (type)
        {
          case SqlDbType.Bit      : return new SqlBoolean(XmlConvert.ToBoolean(value));
          case SqlDbType.TinyInt  : return new SqlByte(XmlConvert.ToByte(value));
          case SqlDbType.SmallInt : return new SqlInt16(XmlConvert.ToInt16(value));
          case SqlDbType.Int      : return new SqlInt32(XmlConvert.ToInt32(value));
          case SqlDbType.BigInt   : return new SqlInt64(XmlConvert.ToInt64(value));

          case SqlDbType.Char     :
          case SqlDbType.VarChar  : //return new SqlAnsiString(value);
          case SqlDbType.NChar    :
          case SqlDbType.NVarChar : return new SqlString(value);

          case SqlDbType.Binary   :
          case SqlDbType.VarBinary: return new SqlBytes(Convert.FromBase64String(value));

          case SqlDbType.Date           : return (style == ValueDbStyle.XML)?
                                            XmlConvert.ToDateTime(value, XmlDateTimeSerializationMode.RoundtripKind).Date
                                            :
                                            (style == ValueDbStyle.SQL)?
                                              DateTime.ParseExact(value, SQLDatePattern, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).Date
                                              :
                                              DateTime.ParseExact(value, DateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).Date;
          case SqlDbType.SmallDateTime  : 
          case SqlDbType.DateTime       : 
          case SqlDbType.DateTime2      :
            DateTime LDateTime =
                                          (
                                            (style == ValueDbStyle.XML)?
                                              XmlConvert.ToDateTime(value, XmlDateTimeSerializationMode.RoundtripKind)
                                              :
                                              (style == ValueDbStyle.SQL)?
                                              DateTime.ParseExact(value, SQLDateTimePattern, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
                                              :
                                              DateTime.ParseExact(value, DateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
                                          );
            if(type == SqlDbType.DateTime2)
              return LDateTime;
            else
              return new SqlDateTime(LDateTime);
          case SqlDbType.DateTimeOffset: return (style == ValueDbStyle.XML) ?
                                           XmlConvert.ToDateTimeOffset(value)
                                           :
                                           (style == ValueDbStyle.SQL) ?
                                             DateTimeOffset.ParseExact(value, SQLDateTimeOffsetPattern, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind | DateTimeStyles.AssumeUniversal)
                                             :
                                             DateTimeOffset.ParseExact(value, DateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind | DateTimeStyles.AssumeUniversal);
          case SqlDbType.Time           :
            TimeSpan resultTime; 
            return TimeSpan.TryParse(value, out resultTime) ? resultTime : XmlConvert.ToTimeSpan(value);

          case SqlDbType.Decimal    : return new SqlDecimal(XmlConvert.ToDecimal(value));
          case SqlDbType.Float      : return new SqlDouble(XmlConvert.ToDouble(value));
          case SqlDbType.Real       : return new SqlSingle(XmlConvert.ToDouble(value));
          case SqlDbType.SmallMoney : return new SqlMoney(XmlConvert.ToDecimal(value));
          case SqlDbType.Money      : return new SqlMoney(XmlConvert.ToDecimal(value));

          case SqlDbType.UniqueIdentifier: return new SqlGuid(XmlConvert.ToGuid(value));

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

          // Not support SqlDbType.Variant
          // Not support SqlDbType.Structured
          // Not support SqlDbType.Text
          // Not support SqlDbType.Timestamp
          // Not support SqlDbType.Image
          // Not support SqlDbType.NText
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
