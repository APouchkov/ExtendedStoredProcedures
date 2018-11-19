using System;
//using System.Collections;
//using System.Collections.Generic;
using System.Linq;
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

  public static void Write7BitEncodedInt64(System.IO.BinaryWriter w, Int64 IValue)  
  {  
    UInt64 num = (UInt32)IValue;  
  
    while (num >= 128U)  
    {  
      w.Write((byte) (num | 128U));  
      num >>= 7;  
    }  
  
    w.Write((byte) num);  
  }
  
  public static Int64 Read7BitEncodedInt64(System.IO.BinaryReader r)  
  {  
    // some names have been changed to protect the readability  
    Int64 returnValue = 0;  
    int   bitIndex    = 0;  
  
    while (bitIndex != 70)  
    {  
      byte currentByte = r.ReadByte();  
      returnValue |= ((Int64) currentByte & (Int64) sbyte.MaxValue) << bitIndex;  
      bitIndex += 7;  
  
      if (((Int64) currentByte & 128) == 0)  
        return returnValue;  
    }  
  
    throw new Exception("Wrong System.IO.BinaryReader.Read7BitEncodedInt64");
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

  public static Boolean InternalParseEOQ(char ARightQuote, String AString, ref int AOffset, out String AValue, char[] ANextChars)
  {
    int LPos, LNextPos;
    char LNextChar;

    AValue = "";
    LPos = AOffset;
    for (; ; )
    {
      LNextPos = AString.IndexOf(ARightQuote, LPos);
      if (LNextPos == 0)
        return false;

      AValue += AString.Substring(LPos, LNextPos - LPos);
      LPos = LNextPos + 1;
      if (LPos >= AString.Length)
        LNextChar = (char)0;
      else
        LNextChar = AString[LPos];

      if (LNextChar == ARightQuote)
      {
        LPos++;
        AValue += ARightQuote;
      }
      else if ((ANextChars.Length == 0) || ANextChars.Contains(LNextChar))
        break;
      else
        return false;
    }

    AOffset = LPos;
    return true;
  }


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

  public  const String XMLDatePattern               = "yyyy-MM-dd";
  public  const String XMLDateTimePattern           = "yyyy-MM-ddTHH:mm:ss.FFFFFFF";
  private const String XMLDateTimeZeroOffsetPattern = "yyyy-MM-ddTHH:mm:ss.FFFFFFFZ";
  private const String XMLDateTimeOffsetPattern     = "yyyy-MM-ddTHH:mm:ss.FFFFFFFzzz";

  private static readonly String[] DateTimeFormats =
  { 
    SQLDatePattern  , SQLSmallDateTimePattern  , SQLDateTimePattern      , SQLDateTimeOffsetPattern  ,
    XMLDatePattern  ,                            XMLDateTimePattern      , XMLDateTimeOffsetPattern  , XMLDateTimeZeroOffsetPattern,
                      TextSmallDateTimePattern , TextDateTimePattern     , TextDateTimeOffsetPattern
    //"yy-MM-dd HH:mm:ss.FFFFFFF"  , "yy-MM-dd",
    //"yyMMdd HH:mm:ss.FFFFFFF"    , "yyMMdd",
  };

  private static readonly String[] SQLDateFormats =
  { 
    SQLDatePattern,
    XMLDatePattern
  };

  private static readonly String[] SQLDateTimeFormats =
  { 
    SQLDatePattern, SQLSmallDateTimePattern , SQLDateTimePattern, SQLDateTimeOffsetPattern,
    XMLDatePattern,                           XMLDateTimePattern, XMLDateTimeOffsetPattern, XMLDateTimeZeroOffsetPattern
  };

  public enum StringSerializationMethod
  {
    Default       = 0,
    Quoted        = 1,
    BinaryHex     = 2,
    BinaryBase64  = 3
  }
  /// <summary>
  /// Возвращает предпочтительный способ сериализации данных в SQL-тексте
  /// </summary>
  /// <param name="type">Тип данных, заданный в SqlDbType</param>
  /// <returns>Способ сериализации в StringSerializationMethod</returns>
  public static StringSerializationMethod GetSQLSerializationMethod(SqlDbType AType)
  {
    switch (AType)
    {
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
      case SqlDbType.Xml:
      case SqlDbType.Text:
      case SqlDbType.NText:
        return StringSerializationMethod.Quoted;

      case SqlDbType.Bit:
      case SqlDbType.TinyInt:
      case SqlDbType.SmallInt:
      case SqlDbType.Int:
      case SqlDbType.BigInt:
      case SqlDbType.Decimal:
      case SqlDbType.Float:
      case SqlDbType.Money:
      case SqlDbType.Real:
      case SqlDbType.SmallMoney:
        return StringSerializationMethod.Default;

      //case SqlDbType.Binary:
      //case SqlDbType.VarBinary:
      //case SqlDbType.Timestamp:
      //case SqlDbType.Image:
      //case SqlDbType.Udt:
      default:
        return StringSerializationMethod.BinaryHex;
    }
  }

  /// <summary>
  /// Возвращает признак необходимости квотирования данных при сериализации в (N)VarChar
  /// </summary>
  /// <param name="type">Тип данных, заданный в SqlDbType</param>
  /// <returns>Да/Нет</returns>
  //public static Boolean IsQuoteType(SqlDbType type)
  //{
  //  switch (type)
  //  {
  //    case SqlDbType.Bit:
  //    case SqlDbType.TinyInt:
  //    case SqlDbType.SmallInt:
  //    case SqlDbType.Int:
  //    case SqlDbType.BigInt:

  //    case SqlDbType.Decimal:
  //    case SqlDbType.Float:
  //    case SqlDbType.Money:
  //    case SqlDbType.Real:
  //    case SqlDbType.SmallMoney:
  //    //case SqlDbType.Timestamp:
  //      return false;

  //    default:
  //      return true;
  //  }
  //}

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
  public static String InternalValueToString(Object AValue, ValueDbStyle AStyle, out SqlDbType AType, out StringSerializationMethod AStringSerializationMethod)
  {
    AType = GetSqlType(AValue);

    AStringSerializationMethod = StringSerializationMethod.Default;
    switch (AType)
    {
      case SqlDbType.Bit: return ((SqlBoolean)AValue).IsTrue ? "1" : "0";
      case SqlDbType.TinyInt: return XmlConvert.ToString((Byte)(SqlByte)AValue);
      case SqlDbType.SmallInt: return XmlConvert.ToString((Int16)(SqlInt16)AValue);
      case SqlDbType.Int: return XmlConvert.ToString((Int32)(SqlInt32)AValue);
      case SqlDbType.BigInt: return XmlConvert.ToString((Int64)(SqlInt64)AValue);

      case SqlDbType.Float: return XmlConvert.ToString((Double)(SqlDouble)AValue);
      case SqlDbType.Real: return XmlConvert.ToString((Double)(SqlSingle)AValue);
      case SqlDbType.SmallMoney: return XmlConvert.ToString((Decimal)(SqlMoney)AValue);
      case SqlDbType.Money: return XmlConvert.ToString((Decimal)(SqlMoney)AValue);
      case SqlDbType.Decimal: return XmlConvert.ToString((Decimal)(SqlDecimal)AValue);
    }
    //case SqlDbType.Timestamp:
    //case SqlDbType.Image:

    AStringSerializationMethod = StringSerializationMethod.Quoted;
    switch (AType)
    {
      case SqlDbType.DateTime:
        return (AStyle == ValueDbStyle.XML) ?
          XmlConvert.ToString((DateTime)(SqlDateTime)AValue, XmlDateTimeSerializationMode.RoundtripKind)
          :
          (AStyle == ValueDbStyle.SQL) ?
            ((DateTime)(SqlDateTime)AValue).ToString(SQLDateTimePattern, CultureInfo.InvariantCulture)
            :
            ((DateTime)(SqlDateTime)AValue).ToString(TextDateTimePattern, CultureInfo.InvariantCulture);
      case SqlDbType.Date:
        return (AStyle == ValueDbStyle.XML) ?
          XmlConvert.ToString((DateTime)AValue, XMLDatePattern)
          :
          (AStyle == ValueDbStyle.SQL) ?
            ((DateTime)AValue).ToString(SQLDatePattern, CultureInfo.InvariantCulture)
            :
            ((DateTime)AValue).ToString(TextDatePattern, CultureInfo.InvariantCulture);
      case SqlDbType.DateTime2:
        return (AStyle == ValueDbStyle.XML) ?
          XmlConvert.ToString((DateTime)AValue, XmlDateTimeSerializationMode.RoundtripKind)
          :
          (AStyle == ValueDbStyle.SQL) ?
            ((DateTime)AValue).ToString(SQLDateTimePattern, CultureInfo.InvariantCulture)
            :
            ((DateTime)AValue).ToString(TextDateTimePattern, CultureInfo.InvariantCulture);
      case SqlDbType.SmallDateTime:
        return (AStyle == ValueDbStyle.XML) ?
          XmlConvert.ToString((DateTime)AValue, XmlDateTimeSerializationMode.RoundtripKind)
          :
          (AStyle == ValueDbStyle.SQL) ?
            ((DateTime)AValue).ToString(TextSmallDateTimePattern, CultureInfo.InvariantCulture)
            :
            ((DateTime)AValue).ToString(SQLSmallDateTimePattern, CultureInfo.InvariantCulture);
      case SqlDbType.DateTimeOffset:
        return (AStyle == ValueDbStyle.XML) ?
          XmlConvert.ToString((DateTimeOffset)AValue)
          :
          (AStyle == ValueDbStyle.SQL) ?
            ((DateTimeOffset)AValue).ToString(SQLDateTimeOffsetPattern, CultureInfo.InvariantCulture)
            :
            ((DateTimeOffset)AValue).ToString(TextDateTimeOffsetPattern, CultureInfo.InvariantCulture);
      case SqlDbType.Time: return Convert.ToString((TimeSpan)AValue);

      case SqlDbType.UniqueIdentifier: return XmlConvert.ToString((Guid)(SqlGuid)AValue).ToUpper();
    }

    switch (AType)
    {
      case SqlDbType.Char:
      case SqlDbType.VarChar:
      //return ((SqlAnsiString)AValue).ToString();
      case SqlDbType.NChar:
      case SqlDbType.NVarChar:
        if (AValue is SqlChars)
          return (String)((SqlChars)AValue).ToSqlString().Value;
        else
          return (String)(SqlString)AValue;

      case SqlDbType.Xml: return ((SqlXml)AValue).Value;
    }

    switch (AType)
    {
      case SqlDbType.Binary         :
      case SqlDbType.VarBinary      :
                                      byte[] LBytes =
                                        (AValue is SqlBinary) ?
                                          ((SqlBinary)AValue).Value
                                          :
                                          ((SqlBytes)AValue).Value;

                                      if (AStyle == ValueDbStyle.XML)
                                      {
                                        AStringSerializationMethod = StringSerializationMethod.BinaryBase64;
                                        return Convert.ToBase64String(LBytes, Base64FormattingOptions.None);
                                      }

                                      AStringSerializationMethod = StringSerializationMethod.BinaryHex;

                                      StringBuilder LResult = new StringBuilder(LBytes.Length * 2);
                                      Strings.BytesToHex(LBytes, LResult);
                                      return LResult.ToString();

      case SqlDbType.Udt              :
        if (AStyle != ValueDbStyle.SQL)
        {
          AStringSerializationMethod = StringSerializationMethod.Quoted;
          return AValue.ToString();
        }

        AStringSerializationMethod = StringSerializationMethod.BinaryHex;
        
        System.IO.MemoryStream s = new System.IO.MemoryStream();
        System.IO.BinaryWriter w = new System.IO.BinaryWriter(s);
        (AValue as IBinarySerialize).Write(w);
        byte[] LUdtBytes = s.ToArray();

        StringBuilder LUdtResult = new StringBuilder(LUdtBytes.Length * 2);
        Strings.BytesToHex(LUdtBytes, LUdtResult);

        return LUdtResult.ToString();
    }

    // Not support SqlDbType.Image
    // Not support SqlDbType.NText
    // Not support SqlDbType.Structured
    // Not support SqlDbType.Text
    // Not support SqlDbType.Timestamp
    // Not support SqlDbType.Variant

    throw new Exception("Системная ошибка"); // Сюда никогда не должно попасть
  }

  public static String ValueToString(Object AValue, ValueDbStyle AStyle)
  {
    SqlDbType LSqlDbType;
    Sql.StringSerializationMethod LSerializationMethod;

    return InternalValueToString(AValue, AStyle, out LSqlDbType, out LSerializationMethod);
  }

  [SqlFunction(Name = "CastVariantAsString", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  // WITH RETURNS NULL ON NULL INPUT
  public static String CastVariantAsString(Object AValue, String AStyle)
  {
    return ValueToString(AValue, (ValueDbStyle)Enum.Parse(typeof(ValueDbStyle), AStyle, true));
  }


  /// <summary>
  /// Конвертирует значение параметра в SQL-текст
  /// </summary>
  [SqlFunction(Name = "CastVariantAsSQLText", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  // WITH RETURNS NULL ON NULL INPUT
  public static String ValueToSQLText(Object AValue, Char AQuote)
  {
    SqlDbType LType;
    StringSerializationMethod LsqlSerializationMethod;
    String LResult = InternalValueToString(AValue, ValueDbStyle.SQL, out LType, out LsqlSerializationMethod);

    switch (LsqlSerializationMethod)
    {
      case StringSerializationMethod.Quoted:
        return Strings.Quote(LResult, AQuote);
      case StringSerializationMethod.BinaryHex:
        return "0x" + LResult;
    }

    return LResult;
  }

/// <summary>
  /// Конвертирует значение параметра в SQL-текст
  /// </summary>
  [SqlFunction(Name = "CastVariantAsCustomText", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  // WITH RETURNS NULL ON NULL INPUT
  public static String ValueToXMLText(Object AValue, Char[] AExtraChars, Char AQuote)
  {
    SqlDbType LType;
    StringSerializationMethod LsqlSerializationMethod;
    String LResult = InternalValueToString(AValue, ValueDbStyle.XML, out LType, out LsqlSerializationMethod);

    if(LResult.IndexOfAny(AExtraChars) != -1)
        return Strings.Quote(LResult, AQuote);

    return LResult;
  }

  ///// <summary>
  ///// Конвертирует значение параметра в текст (CLR-Версия)
  ///// </summary>
  //[SqlFunction(Name = "CastVariantAsSQLText", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  //// WITH RETURNS NULL ON NULL INPUT
  //public static String CastVariantAsSQL(Object AValue, Char AQuote)
  //{
  //  return ValueToSQLText(AValue, AQuote);
  //}

  /// <summary>
  /// Конвертирует значение параметра из строки
  /// </summary>
  public static Object ValueFromString(String value, SqlDbType type, ValueDbStyle style)
  {
    try
    {
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
                                              DateTime.ParseExact(value, SQLDateFormats, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).Date
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
                                              DateTime.ParseExact(value, SQLDateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
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

  [SqlFunction(Name = "CastStringAsVariant", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  // WITH RETURNS NULL ON NULL INPUT
  public static Object CastStringAsVariant(String AValue, String AType, String AStyle)
  { 
    return ValueFromString(AValue, (SqlDbType)Enum.Parse(typeof(SqlDbType), AType, true), (ValueDbStyle)Enum.Parse(typeof(ValueDbStyle), AStyle, true));
  }

  [SqlFunction(Name = "CastStringAsVarbinary", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  // WITH RETURNS NULL ON NULL INPUT
  public static SqlBytes CastStringAsVarbinary(String AValue)
  { 
    return new SqlBytes(Convert.FromBase64String(AValue));
  }

  [SqlFunction(Name = "VariantReCast", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  // WITH RETURNS NULL ON NULL INPUT
  public static Object VariantReCast(Object AValue, String AType)
  { 
    return
      ValueFromString
      (
        ValueToString(AValue, ValueDbStyle.XML), 
        (SqlDbType)Enum.Parse(typeof(SqlDbType), AType, true), 
        ValueDbStyle.XML
      );
  }
#endregion Converting Params

  public struct TParamParseItem
  {
    public String   Gap;
    public Char     Quote;
    public String   Value;
    public Boolean  Eof;
  }

  public class ParamsParser
  {
    private String FString;
    private Char FPrefix;
    private TCommentMethods FComments; // TCommentMethods
    private Char[] FQuotes;
    private Char[] FLiterals;

    private Char FCurrChar;
    private Char FNextChar;

    private int FLength;
    private int FPosition;

    private TParamParseItem FCurrent;
    public TParamParseItem Current { get { return FCurrent; } }

    public ParamsParser(String ACommandText, Char APrefix, TCommentMethods AComments, Char[] AQuotes, Char[] ALiterals)
    {
      FString = ACommandText;
      FPrefix = APrefix;
      FComments = AComments;
      FQuotes = AQuotes;
      FLiterals = ALiterals;

      FPosition = -1;
      FLength = FString.Length;

      MoveToNextChar();

      FCurrent.Gap   = "";
      FCurrent.Quote = (Char)0;
      FCurrent.Value = "";
    }

    private void MoveToNextChar(Boolean AIncPosition = true)
    {
      if(AIncPosition)
        FPosition++;

      if (FPosition < FLength)
        FCurrChar = FString[FPosition];
      else
      {
        FCurrChar = '\0';
        FCurrent.Eof = true;
      }

      if (FPosition < FLength - 1)
        FNextChar = FString[FPosition + 1];
      else
        FNextChar = '\0';
    }

    private char[] NameDelimeters = new char[] { ' ', '!', '$', '?', ')', '<', '>', '=', '+', '-', '*', '/', '\\', '%', '^', '&', '|', ',', ';', '\'', '"', '`', (char)13, (char)10, (char)0 };
    private Boolean NameDelimiter(Char AChar)
    {
      return NameDelimeters.Contains(AChar);
    }

    private void SkipText(ref int LPosition, Boolean AIncludeCurrent)
    {
      int LWidth = FPosition - LPosition;
      if (AIncludeCurrent) LWidth++;
      FCurrent.Gap = FCurrent.Gap + FString.Substring(LPosition, LWidth);
      LPosition = FPosition + 1;
    }

    public Boolean MoveNext()
    {
      int LPosition;
      TCommentMethod LCurrComment;

      if (FCurrent.Eof)
        return false;

      FCurrent.Gap = "";
      LPosition = FPosition;
      LCurrComment = TCommentMethod.None;

      for (;;) /*(FPosition <= FLength)*/
      {
        switch (LCurrComment)
        {
          case TCommentMethod.Lattice:
          case TCommentMethod.DoubleMinus:
          case TCommentMethod.DoubleSlash:
            if (FCurrChar == (char)10 || FCurrChar == (char)13 || FCurrChar == (char)0)
              LCurrComment = TCommentMethod.None;
            MoveToNextChar();
            continue;
          case TCommentMethod.SlashRange:
            {
              if ((FCurrChar == '*') && (FNextChar == '/'))
              {
                LCurrComment = TCommentMethod.None;
                FPosition++;
              }
              MoveToNextChar();
              continue;
            }
          case TCommentMethod.BracketRange:
            {
              if ((FCurrChar == '*') && (FNextChar == ')'))
              {
                LCurrComment = TCommentMethod.None;
                FPosition++;
              }
              MoveToNextChar();
              continue;
            }
          case TCommentMethod.Braces:
            {
              if (FCurrChar == '}')
                LCurrComment = TCommentMethod.None;
              MoveToNextChar();
              continue;
            }
        }

        switch (FCurrChar)
        {
          case '#':
            if ((TCommentMethods.Lattice & FComments) != 0)
            {
              LCurrComment = TCommentMethod.Lattice;
              MoveToNextChar();
              continue;
            }
            else break;
          case '{':
            if ((TCommentMethods.Braces & FComments) != 0)
            {
              LCurrComment = TCommentMethod.Braces;
              MoveToNextChar();
              continue;
            }
            else break;
          case '-':
            if (((TCommentMethods.DoubleMinus & FComments) != 0) && (FNextChar == '-'))
            {
              LCurrComment = TCommentMethod.DoubleMinus;
              FPosition++;
              MoveToNextChar();
              continue;
            }
            else break;
          case '/':
            if (((TCommentMethods.DoubleSlash & FComments) != 0) && (FNextChar == '/'))
            {
              LCurrComment = TCommentMethod.DoubleSlash;
              FPosition++;
              MoveToNextChar();
              continue;
            }
            else if (((TCommentMethods.SlashRange & FComments) != 0) && (FNextChar == '*'))
            {
              LCurrComment = TCommentMethod.SlashRange;
              FPosition++;
              MoveToNextChar();
              continue;
            }
            else break;
          case '(':
            if (((TCommentMethods.BracketRange & FComments) != 0) && (FNextChar == '*'))
            {
              LCurrComment = TCommentMethod.BracketRange;
              FPosition++;
              MoveToNextChar();
              continue;
            }
            else break;
          case (char)0:
            SkipText(ref LPosition, false);
            FCurrent.Quote = (Char)0;
            FCurrent.Value = "";
            FCurrent.Eof = true;
            return true;
          default:
            if (FCurrChar == FPrefix)
              if (FNextChar == FPrefix)
              {
                FPosition++;
                SkipText(ref LPosition, false);
                MoveToNextChar();
                continue;
              }
              else
              {
                SkipText(ref LPosition, false);
                MoveToNextChar();
                FCurrent.Quote = Strings.InternalGetRightQuote(FCurrChar, FQuotes);
                if (FCurrent.Quote != (char)0)
                {
                  FPosition++;
                  if (InternalParseEOQ(FCurrent.Quote, FString, ref FPosition, out FCurrent.Value, new char[0]))
                  {
                    if (String.IsNullOrEmpty(FCurrent.Value))
                    {
                      LPosition--;
                      SkipText(ref LPosition, false);
                    }
                    else
                    {
                      LPosition = FPosition;
                      FCurrent.Eof = (FPosition >= FLength);
                      if(!FCurrent.Eof) MoveToNextChar(false);
                      return true;
                    }
                  }
                  else
                  {
                    LPosition--;
                    FPosition = FLength;
                    SkipText(ref LPosition, true);
                    FCurrent.Quote = (Char)0;
                    FCurrent.Value = "";
                    FCurrent.Eof = true;
                    return true;
                  }
                }
                else
                {
                  while (!NameDelimiter(FCurrChar))
                    MoveToNextChar();
                  if (LPosition == FPosition)
                    LPosition--;
                  else
                  {
                    FCurrent.Value = FString.Substring(LPosition, FPosition - LPosition);
                    FCurrent.Eof = (FPosition >= FLength);
                    return true;
                  }
                }
                break;
              }
            else
              break;
        }

        if (FLiterals.Contains(FCurrChar))
        {
          Char LEndChar = Strings.InternalGetRightQuote(FCurrChar);
          do
          {
            MoveToNextChar();
            if (FCurrChar == LEndChar)
              if (FNextChar == LEndChar)
              {
                MoveToNextChar();
                //SkipText(ref LPosition, false);
                //FPosition++;
              }
              else
              {
                MoveToNextChar();
                break;
              }
          } while (FPosition < FLength);
        }
        else
          MoveToNextChar();
      }

    }
  }

  [SqlFunction(Name = "Trim VarBinary", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  // RETURNS NULL ON NULL INPUT
  public static SqlBinary TrimVarBinary(SqlBinary AValue)
  {
    if ((AValue.Value.Length == 0) || (AValue.Value[0] > 0))
      return AValue;

    for (int I = 0; I < AValue.Value.Length; I++)
    {
      if (AValue.Value[I] > 0)
      {
        byte[] result = new byte[AValue.Value.Length - I];
        Array.Copy(AValue.Value, I, result, 0, result.Length);
        return new SqlBinary(result);
      }
    }
    return new SqlBinary(new byte[] { 0 });
  }
}
