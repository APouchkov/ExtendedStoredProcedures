using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;

namespace INT
{
  /// <summary>
  /// Список параметров типа SqlDbValue
  /// Не поддерживает типы: Image, NText, Structured, Text, Timestamp, Variant
  /// </summary>
  [Serializable]
  public class TParams : IBinarySerialize, IXmlSerializable
  {
    private const Char ListSeparator      = ';';
    //private static readonly Char[] ListSeparators  = new Char[3] { ';', '\r', '\n' };

    protected const String Const_ContextConnection = "context connection=true";

    /// <summary>
    /// Инициализация объекта
    /// </summary>
    public void Init()
    {
      //FData = new SortedList<String, Object>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Признак Null
    /// </summary>
    public bool IsNull
    {
      get
      {
        return false; // Если Null, то нельзя вызвать AddParam
      }
    }

    public int Count
    {
      get
      {
        return FData.Count;
      }
    }

    public SqlConnection ContextConnection { get { return FContextConnection; } set { FContextConnection = value; } }
    public void InitContextConnection()
    {
      if(FContextConnection == null)
      { 
        FContextConnection = new SqlConnection(Const_ContextConnection);
        FContextConnection.Open();
      }
    }

    /// <summary>
    /// Квотирует название
    /// </summary>
    private static String EncodeName(String AName)
    {
      return "[" + AName.Replace("]", "]]") + "]";
    }

    /// <summary>
    /// Деквотирует название
    /// </summary>
    private static String DecodeName(String AName)
    {
      AName = AName.Trim();
      if (String.IsNullOrEmpty(AName)) return "";
      if (AName.Length < 2 || AName[0] != '[' || AName[AName.Length - 1] != ']') return AName;

      return AName.Substring(1, AName.Length - 2).Replace("]]", "]");
    }

    public Boolean TryGetValue(String AName, out Object AValue)
    {
      return FData.TryGetValue(AName, out AValue);
    }

    /// <summary>
    /// Преобразует данные в строку
    /// </summary>
    /// <returns>Возвращает список параметров строкой</returns>
    public override String ToString()
    {
      StringBuilder w = new StringBuilder();

      // Параметры
      foreach (KeyValuePair<String, Object> LDataPair in FData)
        if (LDataPair.Key[0] != '=')
        {
          SqlDbType type = Sql.GetSqlType(LDataPair.Value);
          if(w.Length > 0)
            w.Append(ListSeparator);

          w.Append
          (
            String.Format
            (
              "{0}{1}={2}", 
              EncodeName(LDataPair.Key),
              type == SqlDbType.NVarChar ? "" : (":" + (type == SqlDbType.Udt ? ((SqlUdt)LDataPair.Value).TypeName : type.ToString())),
              Sql.IsQuoteType(type) ?
                Strings.Quote(Sql.ValueToString(LDataPair.Value, Sql.ValueDbStyle.SQL), '"')
                :
                Sql.ValueToString(LDataPair.Value, Sql.ValueDbStyle.SQL))
            );
        }

      return w.ToString();
    }

    /// <summary>
    /// Преобразует данные в строку XML
    /// </summary>
    /// <returns>Возвращает список параметров строкой XML</returns>
    public String ToXMLString(String AElement = null)
    {
      StringBuilder sb = new StringBuilder();
      XmlWriterSettings s = new XmlWriterSettings();
      s.ConformanceLevel = ConformanceLevel.Fragment;
      s.Indent = true;
      XmlWriter w = XmlWriter.Create(sb, s);
      //w.Settings.

      if(AElement != null)
        w.WriteStartElement(AElement);

      foreach (KeyValuePair<String, Object> LDataPair in FData)
      {
        String LName  = XmlConvert.EncodeLocalName(LDataPair.Key);
        String LValue = Sql.ValueToString(LDataPair.Value, Sql.ValueDbStyle.XML);
        if (AElement != null)
        {
          w.WriteStartAttribute(LName);
          w.WriteValue(LValue);
          w.WriteEndAttribute();
        }
        else
          w.WriteElementString(LName, LValue);
      }

      if(AElement != null)
        w.WriteEndElement();

      w.Close();
      return sb.ToString();
    }

    /// <summary>
    /// Преобразует перечисленные параметры в строку
    /// </summary>
    /// <returns>Возвращает список параметров строкой</returns>
    public virtual String ToStringEx(String ANames)
    {
      if(String.IsNullOrEmpty(ANames)) return null;
      StringBuilder w = new StringBuilder();

      // Параметры
      foreach (String LName in ANames.Split(new Char[]{';'}, StringSplitOptions.RemoveEmptyEntries))
      {
        Object LValue;
        if (!FData.TryGetValue(LName, out LValue)) continue;
        SqlDbType type = Sql.GetSqlType(LValue);
        if(w.Length > 0)
          w.Append(ListSeparator);

        w.Append
        (
          String.Format
          (
            "{0}{1}={2}", 
            EncodeName(LName),
            type == SqlDbType.NVarChar ? "" : (":" + (type == SqlDbType.Udt ? ((SqlUdt)LValue).TypeName : type.ToString())),
            Sql.IsQuoteType(type) ?
              Strings.Quote(Sql.ValueToString(LValue, Sql.ValueDbStyle.SQL), '"')
              :
              Sql.ValueToString(LValue, Sql.ValueDbStyle.SQL))
          );
      }

      return w.Length == 0 ? null : w.ToString();
    }

    public static Object TextToValue(String AValue, String AType)
    {
      SqlDbType LSqlDbType = Sql.TypeFromString(AType);

      if (LSqlDbType == SqlDbType.Udt)
        return new SqlUdt(AType, AValue);
      else
        return Sql.ValueFromString(AValue, LSqlDbType, Sql.ValueDbStyle.SQL);
    }

    /// <summary>
    /// Конверитрует список параметров из строки
    /// </summary>
    /// <param name="s">Список параметров, заданный строкой</param>
    public void FromString(String s)
    {
      Clear();
      if (String.IsNullOrEmpty(s)) return;

      Pub.NamedItemsParser Parser =
        new Pub.NamedItemsParser
            (
              s,
              new Char[] {ListSeparator, '\r', '\n' },
              true
             );

      while (Parser.MoveNext())
      {
        try
        {
          // Парсим значение
          SqlDbType LSqlDbType = Sql.TypeFromString(Parser.Current.CastAs);
          //if (Sql.IsQuoteType(LSqlDbType))
          //  SValue = Strings.UnQuote(SValue, new Char[] { '"' });

          Object LValue;
          if (LSqlDbType == SqlDbType.Udt)
            LValue = new SqlUdt(Parser.Current.CastAs, Parser.Current.Value);
          else
            LValue = Sql.ValueFromString(Parser.Current.Value, LSqlDbType, Sql.ValueDbStyle.SQL);

          AddParam(DecodeName(Parser.Current.Name), LValue);
        }
        catch (Exception E)
        {
          throw new Exception(String.Format("Неверно задано значение TParams '{0}': {1}", s, E.Message));
        }
      }
    }
/*
    public void FromString(String s)
    {
      Clear();
      if (String.IsNullOrEmpty(s)) return;

      s += ListSeparator;

      String LName = "";
      String LType = null;

      Int32 startName  = 0;
      Int32 startType  = 0;
      Int32 startValue = 0;

      Boolean skip = false; // Если следующий символ <]> не в VALUE или <"> в VALUE
      Boolean next = false;

      Char LQuoteChar = '\0';

      for (Int32 i = 0; i < s.Length; i++)
      {
        if (skip) { skip = false; continue; };

        Char LCurrChar = s[i];
        if (LQuoteChar != '\0')
        {
          if(LCurrChar != LQuoteChar) continue;
          if(i < s.Length - 1 && s[i + 1] == LQuoteChar)
          {

          }
        }

        if (LCurrChar != '\r' && LCurrChar != '\n' && LCurrChar != ListSeparator)
          switch (LCurrChar)
          {
            case ':':
              if (startValue == 0 && startType == 0)
              {
                LName = s.Substring(startName, i - startName);
                startType = i + 1;
              }
              next = true;
              break;

            case '=':
              if (startValue == 0)
              {
                if (startType > 0)
                {
                  LType = s.Substring(startType, i - startType);
                  LName = s.Substring(startName, startType - startName - 1);
                }
                else
                  LName = s.Substring(startName, i - startName);
                startValue = i + 1;
              }
              next = true;
              break;

            case '[':
              if (startValue == 0)
              {
                if (LQuoteChar == '\0')
                  LQuoteChar = ']';
                else
                  LQuoteChar = '\0';
              }
              next = true;
              break;

            case ']':
              if (startValue == 0)
              {
                if (i < s.Length - 1 && s[i + 1] == ']')
                  skip = true;
                else
                  LQuoteChar = '\0';
              }
              next = true;
              break;

            case '"':
              if (startValue != 0)
              {
                if (LQuoteChar == '\0')
                  LQuoteChar = '"';
                else if (i < s.Length - 1 && s[i + 1] == '"')
                  skip = true;
                else
                  LQuoteChar = '\0';
              }
              next = true;
              break;

            default:
              next = true;
              break;
          }

        if (next)
          next = false;
        else if (startValue > 0)
        {
          try
          {
            // Парсим значение
            String SValue = s.Substring(startValue, i - startValue);
            SqlDbType LSqlDbType = Sql.TypeFromString(LType);
            if (Sql.IsQuoteType(LSqlDbType))
              SValue = Strings.UnQuote(SValue, new Char[] { '"' });

            Object LValue;
            if (LSqlDbType == SqlDbType.Udt)
              LValue = new SqlUdt(LType, SValue);
            else
              LValue = Sql.ValueFromString(SValue, LSqlDbType, Sql.ValueDbStyle.SQL);

            // Добавляем параметр
            AddParam(DecodeName(LName), LValue);

            LName = "";
            LType = null;

            startName   = i + 1;
            startType   = 0;
            startValue  = 0;
          }
          catch (Exception E)
          {
            throw new Exception(String.Format("Неверно задано значение TParams '{0}': {1}", s, E.Message));
          }
        }
        else if (LQuoteChar == '\0' && s.Substring(startName).Trim() != "")
          throw new Exception(String.Format("Неверно задано значение TParams '{0}': отсутствует значение", s));
      }

      if (LQuoteChar != '\0')
        throw new Exception(String.Format("Неверно задано значение TParams '{0}': отсутствует '{1}'", s, LQuoteChar));
    }
*/

    /// <summary>
    /// Проверяет имя параметра на корректность
    /// </summary>
    /// <param name="name">Имя параметра</param>
    /// <param name="preparedParams">Признак режима подготовки параметров</param>
    private void CheckName(String Name, Boolean PreparedParams)
    {
      if (FIgnoreCheckName) return;
      if (String.IsNullOrEmpty(Name))
        throw new Exception("Имя параметра не может быть пустым");
      if (!PreparedParams && Name[0] == '=')
        throw new Exception(String.Format("Неверное имя параметра '{0}', параметр начинающийся с '=' может быть задан(изменен) только в функции подготовки параметров", Name));
      if (PreparedParams && Name[0] != '=')
        throw new Exception(String.Format("Неверное имя параметра '{0}' в функции подготовки параметров, должно начинаться с '='", Name));
      if (Name.Contains(";"))
        throw new Exception(String.Format("Неверное имя параметра '{0}', имя содержит знак разделителя ';'", Name));
    }

    /// <summary>
    /// Возвращает значение параметра
    /// </summary>
    /// <param name="name">Имя параметра</param>
    /// <returns>Значение параметра</returns>
    protected Object AsVariant(String AName)
    {
      Object LValue;
      if (!FData.TryGetValue(AName, out LValue)) return null;

      if (LValue is DateTimeOffset) return Sql.ValueToString(LValue, Sql.ValueDbStyle.SQL);

      return LValue;
    }

    /// <summary>
    /// Возвращает значение параметра типа Bit
    /// </summary>
    /// <param name="name">Имя параметра</param>
    /// <returns>Значение параметра типа Bit</returns>
    private static SqlBoolean AsBit(Object AValue)
    {
      if (AValue is SqlBoolean) return (SqlBoolean)AValue;
      else return SqlBoolean.Parse(Sql.ValueToString(AValue, Sql.ValueDbStyle.Text));
    }
    protected SqlBoolean AsBit(String AName)
    {
      Object LValue;
      if (!FData.TryGetValue(AName, out LValue)) return SqlBoolean.Null;
      try
      {
        return AsBit(LValue);
      }
      catch
      {
        throw new Exception(String.Format("Не удалось сконвертировать значение '{0}' параметра '{1}' в тип Bit", Sql.ValueToString(LValue, Sql.ValueDbStyle.Text), AName));
      }
    }

    /// <summary>
    /// Возвращает значение параметра типа VarChar
    /// </summary>
    /// <param name="name">Имя параметра</param>
    /// <returns>Значение параметра типа VarChar</returns>
    //protected SqlBinary AsVarChar(String AName)
    //{
    //  Object LValue;
    //  if (!FData.TryGetValue(AName, out LValue)) return SqlBinary.Null;

    //  if (LValue is Sql.SqlAnsiString) return ((Sql.SqlAnsiString)LValue).Buffer;
    //  else return (new Sql.SqlAnsiString(Sql.ValueToString(LValue, Sql.ValueDbStyle.Text))).Buffer;
    //}
    //protected SqlBytes AsVarCharMax(String AName)
    //{
    //  Object LValue;
    //  if (!FData.TryGetValue(AName, out LValue)) return SqlBytes.Null;

    //  if (LValue is Sql.SqlAnsiString) return new SqlBytes(((Sql.SqlAnsiString)LValue).Buffer);
    //  else return new SqlBytes((new Sql.SqlAnsiString(Sql.ValueToString(LValue, Sql.ValueDbStyle.Text))).Buffer);
    //}
    /// <summary>
    /// Возвращает значение параметра типа NVarChar
    /// </summary>
    /// <param name="name">Имя параметра</param>
    /// <returns>Значение параметра типа NVarChar</returns>
    protected SqlString AsNVarChar(String AName)
    {
      Object LValue;
      if (!FData.TryGetValue(AName, out LValue)) return SqlString.Null;

      if (LValue is SqlString) return (SqlString)LValue;
      else if (LValue is SqlChars) return ((SqlChars)LValue).ToSqlString();
      else return (SqlString)Sql.ValueToString(LValue, Sql.ValueDbStyle.Text);
    }
    protected SqlChars AsNVarCharMax(String AName)
    {
      Object LValue;
      if (!FData.TryGetValue(AName, out LValue)) return SqlChars.Null;

      if (LValue is SqlChars) return (SqlChars)LValue;
      else if (LValue is SqlString) return new SqlChars((SqlString)LValue);
      else return new SqlChars(Sql.ValueToString(LValue, Sql.ValueDbStyle.Text));
    }

    public static SqlString AsNVarChar(TParams AParams, String AName)
    {
      if(AParams == null) return SqlString.Null;
      return AParams.AsNVarChar(AName);
    }


    /// <summary>
    /// Возвращает значение параметра строкой в формате SQL
    /// </summary>
    /// <param name="name">Имя параметра</param>
    /// <returns>Значение параметра в формате SQL</returns>
    protected SqlString AsSQLString(String AName)
    {
      Object LValue;
      if (!FData.TryGetValue(AName, out LValue)) return SqlString.Null;

      if (LValue is SqlString) return (SqlString)LValue;
      return (SqlString)Sql.ValueToString(LValue, Sql.ValueDbStyle.SQL);
    }

    /// <summary>
    /// Возвращает значение параметра строкой для кода SQL
    /// </summary>
    /// <param name="name">Имя параметра</param>
    /// <returns>Значение параметра для строки SQL</returns>
    protected SqlString AsSQLText(String AName)
    {
      Object LValue;
      if (!FData.TryGetValue(AName, out LValue)) return "NULL";

      return (SqlString)Sql.ValueToText(LValue, Sql.ValueDbStyle.SQL, '\'');
    }

    /// <summary>
    /// Возвращает значение параметра типа Time
    /// </summary>
    /// <param name="name">Имя параметра</param>
    /// <returns>Значение параметра типа Date</returns>
    protected TimeSpan? AsTime(String AName)
    {
      Object LValue;
      if (!FData.TryGetValue(AName, out LValue)) return null;

      try
      {
        if (LValue is TimeSpan) return (TimeSpan)LValue;
        if (LValue is DateTime) return ((DateTime)LValue).TimeOfDay;
        if (LValue is DateTimeOffset) return ((DateTimeOffset)LValue).TimeOfDay;
        if (LValue is SqlDateTime) return ((DateTime)(SqlDateTime)LValue).TimeOfDay;
        else return TimeSpan.Parse(Sql.ValueToString(LValue, Sql.ValueDbStyle.Text));
      }
      catch
      {
        throw new Exception(String.Format("Не удалось сконвертировать значение '{0}' параметра '{1}' в тип Time", Sql.ValueToString(LValue, Sql.ValueDbStyle.Text), AName));
      }
    }

    /// <summary>
    /// Возвращает значение параметра типа Date
    /// </summary>
    /// <param name="name">Имя параметра</param>
    /// <returns>Значение параметра типа Date</returns>
    protected DateTime? AsDate(String AName)
    {
      Object LValue;
      if (!FData.TryGetValue(AName, out LValue)) return null;

      try
      {
        if (LValue is DateTime) return ((DateTime)LValue).Date;
        if (LValue is DateTimeOffset) return ((DateTimeOffset)LValue).Date;
        if (LValue is SqlDateTime) return ((SqlDateTime)LValue).Value.Date;
        else return ((DateTime)Sql.ValueFromString(Sql.ValueToString(LValue, Sql.ValueDbStyle.Text), SqlDbType.Date, Sql.ValueDbStyle.Text)).Date;
      }
      catch
      {
        throw new Exception(String.Format("Не удалось сконвертировать значение '{0}' параметра '{1}' в тип Date", Sql.ValueToString(LValue, Sql.ValueDbStyle.Text), AName));
      }
    }

    /// <summary>
    /// Возвращает значение параметра типа DateTime
    /// </summary>
    /// <param name="name">Имя параметра</param>
    /// <returns>Значение параметра типа DateTime</returns>
    protected SqlDateTime AsDateTime(String AName)
    {
      Object LValue;
      if (!FData.TryGetValue(AName, out LValue)) return SqlDateTime.Null;

      try
      {
        if (LValue is DateTime || LValue is SqlDateTime) return (SqlDateTime)LValue;
        if (LValue is DateTimeOffset) return (SqlDateTime)((DateTimeOffset)LValue).DateTime;
        else return (SqlDateTime)Sql.ValueFromString(Sql.ValueToString(LValue, Sql.ValueDbStyle.Text), SqlDbType.DateTime, Sql.ValueDbStyle.Text);
      }
      catch
      {
        throw new Exception(String.Format("Не удалось сконвертировать значение '{0}' параметра '{1}' в тип DateTime", Sql.ValueToString(LValue, Sql.ValueDbStyle.Text), AName));
      }
    }

    /// <summary>
    /// Возвращает значение параметра типа DateTime
    /// </summary>
    /// <param name="name">Имя параметра</param>
    /// <returns>Значение параметра типа DateTime</returns>
    private static DateTime AsDateTime2(Object AValue)
    {
      if (AValue is DateTime) return (DateTime)AValue;
      if (AValue is SqlDateTime) return ((SqlDateTime)AValue).Value;
      if (AValue is TimeSpan) return new DateTime(((TimeSpan)AValue).Ticks, DateTimeKind.Utc);
      if (AValue is DateTimeOffset) return ((DateTimeOffset)AValue).DateTime;
      else return (DateTime)Sql.ValueFromString(Sql.ValueToString(AValue, Sql.ValueDbStyle.Text), SqlDbType.DateTime, Sql.ValueDbStyle.Text);
    }

    protected DateTime? AsDateTime2(String AName)
    {
      Object LValue;
      if (!FData.TryGetValue(AName, out LValue)) return null;

      try
      {
        return AsDateTime2(LValue);
      }
      catch
      {
        throw new Exception(String.Format("Не удалось сконвертировать значение '{0}' параметра '{1}' в тип DateTime", Sql.ValueToString(LValue, Sql.ValueDbStyle.Text), AName));
      }
    }

    /// <summary>
    /// Возвращает значение параметра типа DateTime
    /// </summary>
    /// <param name="name">Имя параметра</param>
    /// <returns>Значение параметра типа DateTime</returns>
    private static DateTimeOffset AsDateTimeOffset(Object AValue)
    {
      if (AValue is DateTimeOffset) return (DateTimeOffset)AValue;
      if (AValue is DateTime) return new DateTimeOffset((DateTime)AValue, TimeSpan.Zero);
      if (AValue is SqlDateTime) return new DateTimeOffset(((SqlDateTime)AValue).Value, TimeSpan.Zero);
      if (AValue is TimeSpan) return new DateTimeOffset(((TimeSpan)AValue).Ticks, TimeSpan.Zero);
      else return (DateTimeOffset)Sql.ValueFromString(Sql.ValueToString(AValue, Sql.ValueDbStyle.Text), SqlDbType.DateTimeOffset, Sql.ValueDbStyle.Text);
    }

    protected DateTimeOffset? AsDateTimeOffset(String AName)
    {
      Object LValue;
      if (!FData.TryGetValue(AName, out LValue)) return null;

      try
      {
        return AsDateTimeOffset(LValue);
      }
      catch
      {
        throw new Exception(String.Format("Не удалось сконвертировать значение '{0}' параметра '{1}' в тип DateTimeOffset", Sql.ValueToString(LValue, Sql.ValueDbStyle.Text), AName));
      }
    }

    /// <summary>
    /// Возвращает значение параметра типа TinyInt
    /// </summary>
    /// <param name="name">Имя параметра</param>
    /// <returns>Значение параметра типа TinyInt</returns>
    protected SqlByte AsTinyInt(String AName)
    {
      Object LValue;
      if (!FData.TryGetValue(AName, out LValue)) return SqlByte.Null;

      try
      {
        if (LValue is SqlByte) return (SqlByte)LValue;

        if (LValue is SqlBoolean) return ((SqlBoolean)LValue).ToSqlByte();
        if (LValue is SqlInt16)   return ((SqlInt16)LValue).ToSqlByte();
        if (LValue is SqlInt32)   return ((SqlInt32)LValue).ToSqlByte();
        if (LValue is SqlInt64)   return ((SqlInt64)LValue).ToSqlByte();
        if (LValue is SqlDecimal) return ((SqlDecimal)LValue).ToSqlByte();
        if (LValue is SqlDouble)  return ((SqlDouble)LValue).ToSqlByte();
        if (LValue is SqlMoney)   return ((SqlMoney)LValue).ToSqlByte();
        if (LValue is SqlSingle)  return ((SqlSingle)LValue).ToSqlByte();
        else return (SqlByte)Sql.ValueFromString(Sql.ValueToString(LValue, Sql.ValueDbStyle.Text), SqlDbType.TinyInt, Sql.ValueDbStyle.Text);
      }
      catch
      {
        throw new Exception(String.Format("Не удалось сконвертировать значение '{0}' параметра '{1}' в тип TinyInt", Sql.ValueToString(LValue, Sql.ValueDbStyle.Text), AName));
      }
    }

    /// <summary>
    /// Возвращает значение параметра типа SmallInt
    /// </summary>
    /// <param name="name">Имя параметра</param>
    /// <returns>Значение параметра типа SmallInt</returns>
    protected SqlInt16 AsSmallInt(String AName)
    {
      Object LValue;
      if (!FData.TryGetValue(AName, out LValue)) return SqlInt16.Null;

      try
      {
        if (LValue is SqlInt16)    return (SqlInt16)LValue;

        if (LValue is SqlBoolean)  return ((SqlBoolean)LValue).ToSqlInt16();
        if (LValue is SqlByte)     return ((SqlByte)LValue).ToSqlInt16();
        if (LValue is SqlInt32)    return ((SqlInt32)LValue).ToSqlInt16();
        if (LValue is SqlInt64)    return ((SqlInt64)LValue).ToSqlInt16();
        if (LValue is SqlDecimal)  return ((SqlDecimal)LValue).ToSqlInt16();
        if (LValue is SqlDouble)   return ((SqlDouble)LValue).ToSqlInt16();
        if (LValue is SqlMoney)    return ((SqlMoney)LValue).ToSqlInt16();
        if (LValue is SqlSingle)   return ((SqlSingle)LValue).ToSqlInt16();
        else return (SqlInt16)Sql.ValueFromString(Sql.ValueToString(LValue, Sql.ValueDbStyle.Text), SqlDbType.SmallInt, Sql.ValueDbStyle.Text);
      }
      catch
      {
        throw new Exception(String.Format("Не удалось сконвертировать значение '{0}' параметра '{1}' в тип SmallInt", Sql.ValueToString(LValue, Sql.ValueDbStyle.Text), AName));
      }
    }

    /// <summary>
    /// Возвращает значение параметра типа Int32
    /// </summary>
    /// <param name="name">Имя параметра</param>
    /// <returns>Значение параметра типа Int32</returns>
    protected SqlInt32 AsInt(String AName)
    {
      Object LValue;
      if (!FData.TryGetValue(AName, out LValue)) return SqlInt32.Null;

      try
      {
        if (LValue is SqlInt32) return (SqlInt32)LValue;

        if (LValue is SqlBoolean)  return ((SqlBoolean)LValue).ToSqlInt32();
        if (LValue is SqlByte)     return ((SqlByte)LValue).ToSqlInt32();
        if (LValue is SqlInt16)    return ((SqlInt16)LValue).ToSqlInt32();
        if (LValue is SqlInt64)    return ((SqlInt64)LValue).ToSqlInt32();
        if (LValue is SqlDecimal)  return ((SqlDecimal)LValue).ToSqlInt32();
        if (LValue is SqlDouble)   return ((SqlDouble)LValue).ToSqlInt32();
        if (LValue is SqlMoney)    return ((SqlMoney)LValue).ToSqlInt32();
        if (LValue is SqlSingle)   return ((SqlSingle)LValue).ToSqlInt32();
        else return (SqlInt32)Sql.ValueFromString(Sql.ValueToString(LValue, Sql.ValueDbStyle.Text), SqlDbType.Int, Sql.ValueDbStyle.Text);
      }
      catch
      {
        throw new Exception(String.Format("Не удалось сконвертировать значение '{0}' параметра '{1}' в тип Int32", Sql.ValueToString(LValue, Sql.ValueDbStyle.Text), AName));
      }
    }

    /// <summary>
    /// Возвращает значение параметра типа BigInt
    /// </summary>
    /// <param name="name">Имя параметра</param>
    /// <returns>Значение параметра типа BigInt</returns>
    private static SqlInt64 AsBigInt(Object AValue)
    {
      if (AValue is SqlInt64)    return (SqlInt64)AValue;

      if (AValue is SqlBoolean)  return ((SqlBoolean)AValue).ToSqlInt64();
      if (AValue is SqlByte)     return ((SqlByte)AValue).ToSqlInt64();
      if (AValue is SqlInt16)    return ((SqlInt16)AValue).ToSqlInt64();
      if (AValue is SqlInt32)    return ((SqlInt32)AValue).ToSqlInt64();
      if (AValue is SqlDecimal)  return ((SqlDecimal)AValue).ToSqlInt64();
      if (AValue is SqlDouble)   return ((SqlDouble)AValue).ToSqlInt64();
      if (AValue is SqlMoney)    return ((SqlMoney)AValue).ToSqlInt64();
      if (AValue is SqlSingle)   return ((SqlSingle)AValue).ToSqlInt64();
      else return (SqlInt64)Sql.ValueFromString(Sql.ValueToString(AValue, Sql.ValueDbStyle.Text), SqlDbType.BigInt, Sql.ValueDbStyle.Text);
    }
    protected SqlInt64 AsBigInt(String AName)
    {
      Object LValue;
      if (!FData.TryGetValue(AName, out LValue)) return SqlInt64.Null;

      try
      {
        return AsBigInt(LValue);
      }
      catch
      {
        throw new Exception(String.Format("Не удалось сконвертировать значение '{0}' параметра '{1}' в тип BigInt", Sql.ValueToString(LValue, Sql.ValueDbStyle.Text), AName));
      }
    }


    /// <summary>
    /// Возвращает значение параметра типа Decimal
    /// </summary>
    /// <param name="name">Имя параметра</param>
    /// <returns>Значение параметра типа Decimal</returns>
    //public SqlDecimal AsNumeric(SqlString AName)
    //{
      //Object LValue;
      //if (!FData.TryGetValue(AName, out LValue)) return SqlDecimal.Null;

    //    try
    //    {
    //        if (value is SqlDecimal) return (SqlDecimal)value;
    //        if (value is SqlByte) return ((SqlByte)value).ToSqlDecimal();
    //        if (value is SqlInt16) return ((SqlInt16)value).ToSqlDecimal();
    //        if (value is SqlInt32) return ((SqlInt32)value).ToSqlDecimal();
    //        if (value is SqlBoolean) return ((SqlBoolean)value).ToSqlDecimal();
    //        if (value is SqlInt64) return ((SqlInt64)value).ToSqlDecimal();
    //        if (value is SqlDouble) return ((SqlDouble)value).ToSqlDecimal();
    //        if (value is SqlMoney) return ((SqlMoney)value).ToSqlDecimal();
    //        if (value is SqlSingle) return ((SqlSingle)value).ToSqlDecimal();
    //        else return (SqlDecimal)ValueFromString(ValueToString(value, ValueDbStyle.Text), SqlDbType.Decimal, ValueDbStyle.Text);
    //    }
    //    catch
    //    {
    //        throw new Exception(String.Format("Не удалось сконвертировать значение '{0}' параметра '{1}' в тип BigInt", ValueToString(value, ValueDbStyle.Text), name.Value));
    //    }
    //}

    /// <summary>
    /// Возвращает значение параметра типа Xml
    /// </summary>
    protected SqlXml AsXml(String AName)
    {
      Object LValue;
      if (!FData.TryGetValue(AName, out LValue)) return SqlXml.Null;

      try
      {
        if (LValue is SqlXml) return (SqlXml)LValue;
        else if (LValue is SqlString)
        {
          XmlReader r = XmlReader.Create(new System.IO.StringReader((String)(SqlString)LValue));
          return new SqlXml(r);
        }
        else
          throw new Exception();
      }
      catch
      {
        throw new Exception(String.Format("Не удалось сконвертировать значение '{0}' параметра '{1}' в тип Xml", Sql.ValueToString(LValue, Sql.ValueDbStyle.Text), AName));
      }
    }

    /// <summary>
    /// Возвращает значение параметра типа VarBinary
    /// </summary>
    protected SqlBinary AsVarBinary(String AName)
    {
      Object LValue;
      if (!FData.TryGetValue(AName, out LValue)) return SqlBinary.Null;

      try
      {
        if (LValue is SqlBinary) return (SqlBinary)LValue;
        else if (LValue is SqlBytes) return ((SqlBytes)LValue).ToSqlBinary();
        //else if (LValue is Sql.SqlAnsiString) return new SqlBinary(((Sql.SqlAnsiString)LValue).Buffer);
        else if (LValue is SqlString || LValue is SqlChars) return (SqlBinary)Sql.ValueFromString(Sql.ValueToString(LValue, Sql.ValueDbStyle.Text), SqlDbType.VarBinary, Sql.ValueDbStyle.SQL);
        else
          throw new Exception();
      }
      catch
      {
        throw new Exception(String.Format("Не удалось сконвертировать значение '{0}' параметра '{1}' в тип VarBinary", Sql.ValueToString(LValue, Sql.ValueDbStyle.Text), AName));
      }
    }

    protected SqlBytes AsVarBinaryMax(String AName)
    {
      Object LValue;
      if (!FData.TryGetValue(AName, out LValue)) return SqlBytes.Null;

      try
      {
        if (LValue is SqlBytes) return (SqlBytes)LValue;
        else if (LValue is SqlBinary) return new SqlBytes((SqlBinary)LValue);
        //else if (LValue is Sql.SqlAnsiString) return new SqlBytes(((Sql.SqlAnsiString)LValue).Buffer);
        else if (LValue is SqlString || LValue is SqlChars) return (SqlBytes)Sql.ValueFromString(Sql.ValueToString(LValue, Sql.ValueDbStyle.Text), SqlDbType.VarBinary, Sql.ValueDbStyle.SQL);
        else
          throw new Exception();
      }
      catch
      {
        throw new Exception(String.Format("Не удалось сконвертировать значение '{0}' параметра '{1}' в тип VarBinary", Sql.ValueToString(LValue, Sql.ValueDbStyle.Text), AName));
      }
    }

    /// <summary>
    /// Возвращает значение параметра типа TListString
    /// </summary>
    protected TListString AsTListString(String AName)
    {
      Object LValue;
      if (!FData.TryGetValue(AName, out LValue)) return null;

      try
      {
        if(LValue is SqlUdt)
          LValue = ((SqlUdt)LValue).CreateUdtObject(true);

        if (LValue is TListString)
          return (TListString)LValue;
        else
        {
          TListString LResult = new TListString();
          if (LValue is SqlString)
            LResult.FromString(((SqlString)LValue).Value);
          else if (LValue is SqlChars)
            LResult.FromString(((SqlChars)LValue).ToString());
          //else if (LValue is Sql.SqlAnsiString) return new SqlBinary(((Sql.SqlAnsiString)LValue).Buffer);
          else
          { 
            System.IO.BinaryReader r;
            if (LValue is SqlBytes)
              r = new System.IO.BinaryReader(((SqlBytes)LValue).Stream);
            else if (LValue is SqlBinary)
              r = new System.IO.BinaryReader(new System.IO.MemoryStream(((SqlBinary)LValue).Value));
            else
              throw new Exception();
            LResult.Read(r);
          }

          return LResult;
        }
      }
      catch
      {
        throw new Exception(String.Format("Не удалось сконвертировать значение '{0}' параметра '{1}' в тип VarBinary", Sql.ValueToString(LValue, Sql.ValueDbStyle.Text), AName));
      }
    }

    /// <summary>
    /// Возвращает значение параметра типа TListInt8
    /// </summary>
    protected TListInt8 AsTListInt8(String AName)
    {
      Object LValue;
      if (!FData.TryGetValue(AName, out LValue)) return null;

      try
      {
        if(LValue is SqlUdt)
          LValue = ((SqlUdt)LValue).CreateUdtObject(true);

        if (LValue is TListInt8)
          return (TListInt8)LValue;
        else
        {
          TListInt8 LResult = new TListInt8();
          if (LValue is SqlString)
            LResult.FromString(((SqlString)LValue).Value);
          else if (LValue is SqlChars)
            LResult.FromString(((SqlChars)LValue).ToString());
          //else if (LValue is Sql.SqlAnsiString) return new SqlBinary(((Sql.SqlAnsiString)LValue).Buffer);
          else
          { 
            System.IO.BinaryReader r;
            if (LValue is SqlBytes)
              r = new System.IO.BinaryReader(((SqlBytes)LValue).Stream);
            else if (LValue is SqlBinary)
              r = new System.IO.BinaryReader(new System.IO.MemoryStream(((SqlBinary)LValue).Value));
            else
              throw new Exception();
            LResult.Read(r);
          }

          return LResult;
        }
      }
      catch
      {
        throw new Exception(String.Format("Не удалось сконвертировать значение '{0}' параметра '{1}' в тип TListInt8", Sql.ValueToString(LValue, Sql.ValueDbStyle.Text), AName));
      }
    }

    /// <summary>
    /// Возвращает значение параметра типа TListInt16
    /// </summary>
    protected TListInt16 AsTListInt16(String AName)
    {
      Object LValue;
      if (!FData.TryGetValue(AName, out LValue)) return null;

      try
      {
        if(LValue is SqlUdt)
          LValue = ((SqlUdt)LValue).CreateUdtObject(true);

        if (LValue is TListInt16)
          return (TListInt16)LValue;
        else
        {
          TListInt16 LResult = new TListInt16();
          if (LValue is SqlString)
            LResult.FromString(((SqlString)LValue).Value);
          else if (LValue is SqlChars)
            LResult.FromString(((SqlChars)LValue).ToString());
          //else if (LValue is Sql.SqlAnsiString) return new SqlBinary(((Sql.SqlAnsiString)LValue).Buffer);
          else
          { 
            System.IO.BinaryReader r;
            if (LValue is SqlBytes)
              r = new System.IO.BinaryReader(((SqlBytes)LValue).Stream);
            else if (LValue is SqlBinary)
              r = new System.IO.BinaryReader(new System.IO.MemoryStream(((SqlBinary)LValue).Value));
            else
              throw new Exception();
            LResult.Read(r);
          }

          return LResult;
        }
      }
      catch
      {
        throw new Exception(String.Format("Не удалось сконвертировать значение '{0}' параметра '{1}' в тип TListInt16", Sql.ValueToString(LValue, Sql.ValueDbStyle.Text), AName));
      }
    }

    /// <summary>
    /// Возвращает значение параметра типа TListInt32
    /// </summary>
    protected TListInt32 AsTListInt32(String AName)
    {
      Object LValue;
      if (!FData.TryGetValue(AName, out LValue)) return null;

      try
      {
        if(LValue is SqlUdt)
          LValue = ((SqlUdt)LValue).CreateUdtObject(true);

        if (LValue is TListInt32)
          return (TListInt32)LValue;
        else
        {
          TListInt32 LResult = new TListInt32();
          if (LValue is SqlString)
            LResult.FromString(((SqlString)LValue).Value);
          else if (LValue is SqlChars)
            LResult.FromString(((SqlChars)LValue).ToString());
          //else if (LValue is Sql.SqlAnsiString) return new SqlBinary(((Sql.SqlAnsiString)LValue).Buffer);
          else
          { 
            System.IO.BinaryReader r;
            if (LValue is SqlBytes)
              r = new System.IO.BinaryReader(((SqlBytes)LValue).Stream);
            else if (LValue is SqlBinary)
              r = new System.IO.BinaryReader(new System.IO.MemoryStream(((SqlBinary)LValue).Value));
            else
              throw new Exception();
            LResult.Read(r);
          }

          return LResult;
        }
      }
      catch
      {
        throw new Exception(String.Format("Не удалось сконвертировать значение '{0}' параметра '{1}' в тип TListInt32", Sql.ValueToString(LValue, Sql.ValueDbStyle.Text), AName));
      }
    }


    /// <summary>
    /// Возвращает значение параметра типа TListInt64
    /// </summary>
    protected TListInt64 AsTListInt64(String AName)
    {
      Object LValue;
      if (!FData.TryGetValue(AName, out LValue)) return null;

      try
      {
        if(LValue is SqlUdt)
          LValue = ((SqlUdt)LValue).CreateUdtObject(true);

        if (LValue is TListInt64)
          return (TListInt64)LValue;
        else
        {
          TListInt64 LResult = new TListInt64();
          if (LValue is SqlString)
            LResult.FromString(((SqlString)LValue).Value);
          else if (LValue is SqlChars)
            LResult.FromString(((SqlChars)LValue).ToString());
          //else if (LValue is Sql.SqlAnsiString) return new SqlBinary(((Sql.SqlAnsiString)LValue).Buffer);
          else
          { 
            System.IO.BinaryReader r;
            if (LValue is SqlBytes)
              r = new System.IO.BinaryReader(((SqlBytes)LValue).Stream);
            else if (LValue is SqlBinary)
              r = new System.IO.BinaryReader(new System.IO.MemoryStream(((SqlBinary)LValue).Value));
            else
              throw new Exception();
            LResult.Read(r);
          }

          return LResult;
        }
      }
      catch
      {
        throw new Exception(String.Format("Не удалось сконвертировать значение '{0}' параметра '{1}' в тип TListInt64", Sql.ValueToString(LValue, Sql.ValueDbStyle.Text), AName));
      }
    }

    /// <summary>
    /// Возвращает значение параметра типа TDictionaryStringString
    /// </summary>
    protected TDictionaryStringString AsTDictionaryStringString(String AName)
    {
      Object LValue;
      if (!FData.TryGetValue(AName, out LValue)) return null;

      try
      {
        if(LValue is SqlUdt)
          LValue = ((SqlUdt)LValue).CreateUdtObject(true);

        if (LValue is TDictionaryStringString)
          return (TDictionaryStringString)LValue;
        else
        {
          TDictionaryStringString LResult = new TDictionaryStringString();
          if (LValue is SqlString)
            LResult.FromString(((SqlString)LValue).Value);
          else if (LValue is SqlChars)
            LResult.FromString(((SqlChars)LValue).ToString());
          //else if (LValue is Sql.SqlAnsiString) return new SqlBinary(((Sql.SqlAnsiString)LValue).Buffer);
          else
          { 
            System.IO.BinaryReader r;
            if (LValue is SqlBytes)
              r = new System.IO.BinaryReader(((SqlBytes)LValue).Stream);
            else if (LValue is SqlBinary)
              r = new System.IO.BinaryReader(new System.IO.MemoryStream(((SqlBinary)LValue).Value));
            else
              throw new Exception();
            LResult.Read(r);
          }

          return LResult;
        }
      }
      catch
      {
        throw new Exception(String.Format("Не удалось сконвертировать значение '{0}' параметра '{1}' в тип TDictionaryStringString", Sql.ValueToString(LValue, Sql.ValueDbStyle.Text), AName));
      }
    }

    /// <summary>
    /// Возвращает значение параметра типа TDictionaryStringInt32
    /// </summary>
    protected TDictionaryStringInt32 AsTDictionaryStringInt32(String AName)
    {
      Object LValue;
      if (!FData.TryGetValue(AName, out LValue)) return null;

      try
      {
        if(LValue is SqlUdt)
          LValue = ((SqlUdt)LValue).CreateUdtObject(true);

        if (LValue is TDictionaryStringInt32)
          return (TDictionaryStringInt32)LValue;
        else
        {
          TDictionaryStringInt32 LResult = new TDictionaryStringInt32();
          if (LValue is SqlString)
            LResult.FromString(((SqlString)LValue).Value);
          else if (LValue is SqlChars)
            LResult.FromString(((SqlChars)LValue).ToString());
          //else if (LValue is Sql.SqlAnsiString) return new SqlBinary(((Sql.SqlAnsiString)LValue).Buffer);
          else
          { 
            System.IO.BinaryReader r;
            if (LValue is SqlBytes)
              r = new System.IO.BinaryReader(((SqlBytes)LValue).Stream);
            else if (LValue is SqlBinary)
              r = new System.IO.BinaryReader(new System.IO.MemoryStream(((SqlBinary)LValue).Value));
            else
              throw new Exception();
            LResult.Read(r);
          }

          return LResult;
        }
      }
      catch
      {
        throw new Exception(String.Format("Не удалось сконвертировать значение '{0}' параметра '{1}' в тип TDictionaryStringInt32", Sql.ValueToString(LValue, Sql.ValueDbStyle.Text), AName));
      }
    }

    /// <summary>
    /// Возвращает значение параметра типа TDictionaryStringInt32
    /// </summary>
    protected TDictionaryInt32String AsTDictionaryInt32String(String AName)
    {
      Object LValue;
      if (!FData.TryGetValue(AName, out LValue)) return null;

      try
      {
        if(LValue is SqlUdt)
          LValue = ((SqlUdt)LValue).CreateUdtObject(true);

        if (LValue is TDictionaryInt32String)
          return (TDictionaryInt32String)LValue;
        else
        {
          TDictionaryInt32String LResult = new TDictionaryInt32String();
          if (LValue is SqlString)
            LResult.FromString(((SqlString)LValue).Value);
          else if (LValue is SqlChars)
            LResult.FromString(((SqlChars)LValue).ToString());
          //else if (LValue is Sql.SqlAnsiString) return new SqlBinary(((Sql.SqlAnsiString)LValue).Buffer);
          else
          { 
            System.IO.BinaryReader r;
            if (LValue is SqlBytes)
              r = new System.IO.BinaryReader(((SqlBytes)LValue).Stream);
            else if (LValue is SqlBinary)
              r = new System.IO.BinaryReader(new System.IO.MemoryStream(((SqlBinary)LValue).Value));
            else
              throw new Exception();
            LResult.Read(r);
          }

          return LResult;
        }
      }
      catch
      {
        throw new Exception(String.Format("Не удалось сконвертировать значение '{0}' параметра '{1}' в тип TDictionaryInt32String", Sql.ValueToString(LValue, Sql.ValueDbStyle.Text), AName));
      }
    }


/*
    /// <summary>
    /// Возвращает значение параметра типа TParams
    /// </summary>
    public TParams AsParams(SqlString name)
    {
      if (!FData.ContainsKey(name.Value)) return null;
      Object value = FData[name.Value];
      if (value is TParams) return (TParams)value;
      else if (value is SqlString)
      {
        TParams result = (TParams)System.Activator.CreateInstance(this.GetType());
        result.FromString((String)(SqlString)value);
        return result;
      }
      else if (value is SqlXml)
      {
        TParams result = TParams::New();
        result.ReadXml((value as SqlXml).CreateReader());
        return result;
      }
      else
        throw new Exception(String.Format("Не удалось сконвертировать значение '{0}' параметра '{1}' в тип {2}", Sql.ValueToString(value, Sql.ValueDbStyle.Text), name.Value, this.GetType().Name));
    }
*/

    /// <summary>
    /// Определяет, присутствует ли параметр в списке
    /// </summary>
    /// <param name="name">Имя параметра</param>
    /// <returns>Признак присутствия параметра в списке</returns>
    protected bool ExistsParam(String AName)
    {
      return (FData.ContainsKey(AName));
    }

    /// <summary>
    /// Очищает список параметров
    /// </summary>
    protected void Clear()
    {
      if (FPreparedObjects != null)
        throw new Exception("Нельзя использовать процедуру очистки параметров 'Clear' в режиме подготовки параметров");
      FData.Clear();
      FPreparedObjects = null;
      FRegisteredDependedParams = null;
    }

    /// <summary>
    /// Возвращает имена параметров
    /// </summary>
    protected string GetNames()
    {
      StringBuilder sb = new StringBuilder();
      foreach (KeyValuePair<String, Object> LDataPair in FData)
        if (LDataPair.Key[0] != '=')
          sb.Append((sb.Length == 0 ? "" : ";") + LDataPair.Key);

      return sb.ToString();
    }

    public struct TParamRow
    {
      public String     Name;
      public SqlDbType  Type;
      public Object     Value;
    }
    public IEnumerable ListParams() //(Boolean AIncludePrepared = false)
    {
      List<TParamRow> Rows = new List<TParamRow>();
      TParamRow Row;

      foreach (KeyValuePair<String, Object> LDataPair in FData)
        if (/*AIncludePrepared ||*/ LDataPair.Key[0] != '=')
        {
          Row.Name  = LDataPair.Key;
          Row.Type  = Sql.GetSqlType(LDataPair.Value);
          Row.Value = LDataPair.Value;
          Rows.Add(Row);
        }

      return Rows;
    }

    public struct TParamStringRow
    {
      public String     Name;
      public SqlDbType  Type;
      public String     Value;
    }
    public IEnumerable ListParamsAsText(String AStyle)
    {
      Sql.ValueDbStyle LStyle = Sql.ValueDbStyle.Text;
      if(!String.IsNullOrEmpty(AStyle))
        LStyle = (Sql.ValueDbStyle)Enum.Parse(typeof(Sql.ValueDbStyle), AStyle, true);

      List<TParamStringRow> Rows = new List<TParamStringRow>();
      TParamStringRow Row;

      foreach (KeyValuePair<String, Object> LDataPair in FData)
        if (LDataPair.Key[0] != '=')
        {
          Row.Name  = LDataPair.Key;
          Row.Type  = Sql.GetSqlType(LDataPair.Value);
          Row.Value = Sql.ValueToString(LDataPair.Value, LStyle);
          Rows.Add(Row);
        }

      return Rows;
    }

    /// <summary>
    /// Добавляет параметр, если параметр существует в списке, то переопределяет его значение
    /// </summary>
    /// <param name="name">Имя параметра</param>
    /// <param name="value">Значение параметра</param>
    protected void AddParam(String AName, Object AValue)
    {
      CheckName(AName, FPreparedObjects != null);

      if (FPreparedData != null)
      {
        // Удаляем зависимые объекты
        List<Int32> LDeleted = new List<Int32>();
        String UName = AName.ToUpper();

        foreach (KeyValuePair<Int32, TPreparedDataItem> LPreparedDataPair in FPreparedData)
        {
          if (
                LPreparedDataPair.Value.DependedParams != null 
                &&
                LPreparedDataPair.Value.DependedParams.IndexOf(UName) >= 0
                // ??? Если не изменилось значение зависимого параметра ??? && !EqualValues(value, FData[name.Value])
             )
             LDeleted.Add(LPreparedDataPair.Key);
        }

        foreach (Int32 LObjectId in LDeleted)
        {
          List<String> LCalculatedParams = FPreparedData[LObjectId].CalculatedParams;
          FPreparedData.Remove(LObjectId);
          foreach (String LParamName in LCalculatedParams)
          {
            if (!IsCalculatedParams(LParamName)) FData.Remove(LParamName);
          }
        }
      }

      // Добавляем / изменяем / удаляем параметр
      Boolean IsValueNull = (AValue == null || AValue is DBNull || (AValue is INullable && ((INullable)AValue).IsNull));
      int LIndex = FData.IndexOfKey(AName);
      if (LIndex == -1)
      {
        if (!IsValueNull)
          FData.Add(AName, AValue);
      }
      else
      {
        if (IsValueNull)
          FData.RemoveAt(LIndex);
        else
          FData[AName] = AValue;
      }
    }

    //public void AddVarCharMax(String AName, SqlBytes AValue)
    //{
    //  if(AValue == null)
    //    DeleteParam(AName);
    //  else
    //    AddParam(AName, new Sql.SqlAnsiString(AValue.Value));
    //}

    /// <summary>
    /// Проверяет пересечение параметров, если общие параметры равны, то истина иначе ложь
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    protected Boolean Crossing(TParams AParams)
    {
      // -=MD=-: Я посчитал что правильно вернуть фальс
      // if (IsNull || value.IsNull) return false;

      if (AParams == null) return true;
      foreach (KeyValuePair<String, Object> LDataPair in AParams.FData)
        if (LDataPair.Key[0] != '=')
        {
          Object LValue;
          if (FData.TryGetValue(LDataPair.Key, out LValue))
            if (!EqualValues(LValue, LDataPair.Value))
              return false;
        }
      return true;
    }

    /// <summary>
    /// Возвращает пересечение двух списков параметров
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    //protected void CrossParams(TParams AParams)
    //{
    //  if (AParams == null)
    //  {
    //    Clear();
    //    return;
    //  };

    //  List<String> LKeys = new List<String>();
    //  foreach (KeyValuePair<String, Object> LDataPair in AParams.FData)
    //  {
    //    Object LValue;
    //    if (FData.TryGetValue(LDataPair.Key, out LValue))
    //      if(EqualValues(LValue, LDataPair.Value)) LKeys.Add(LDataPair.Key);
    //  }
    //  for (Int32 i = FData.Count - 1; i >= 0; i--)
    //  {
    //    if (LKeys.IndexOf(FData.Keys[i]) < 0) FData.RemoveAt(i);
    //  }
    //  return;
    //}

    /// <summary>
    /// Объединяет два списка параметров и возвращает результат
    /// </summary>
    protected void MergeParams(TParams ASource)
    {
      if (ASource == null) return;
      foreach (KeyValuePair<String, Object> LDataPair in ASource.FData)
        if (LDataPair.Key[0] != '=')
          AddParam(LDataPair.Key, LDataPair.Value);
    }

    /// <summary>
    /// Удаляет параметры из списка параметров
    /// </summary>
    /// <param name="name"></param>
    protected void Load(TParams ASource, String AAliases, TLoadValueCondition ALoadValueCondition = TLoadValueCondition.lvcAlways)
    {
      if (ASource == null) return;

      String LTarget, LSource;
      foreach (String LAlias in AAliases.Split(';'))
      {
        int LEqual = LAlias.IndexOf('=');
        if (LEqual == -1)
        {
          LTarget = LAlias;
          LSource = LAlias;
        }
        else
        {
          LTarget = LAlias.Substring(0, LEqual);
          LSource = LAlias.Substring(LEqual + 1);
        }

        Object LValue;
        if (ASource.FData.TryGetValue(LSource, out LValue))
        {
          if (ALoadValueCondition != TLoadValueCondition.lvcIfNotPresent || !FData.ContainsKey(LTarget))
            AddParam(LTarget, LValue);
        }
        else if (ALoadValueCondition == TLoadValueCondition.lvcAlways)
          AddParam(LTarget, DBNull.Value);
      }
    }

    /// <summary>
    /// Удаляет параметр из списка параметров именно через AddParam (!!!) т.к. там есть контроль Depended'ов
    /// </summary>
    /// <param name="name"></param>
    protected void DeleteParam(String AName)
    {
      CheckName(AName, false);
      if(FData.ContainsKey(AName))
        AddParam(AName, DBNull.Value);
    }

    /// <summary>
    /// Удаляет параметры из списка параметров
    /// </summary>
    /// <param name="name"></param>
    protected void DeleteParams(String ANames, Boolean AInsteadOf = false)
    {
      if (AInsteadOf)
      {
        ANames = ';' + ANames + ';';
        IList<String> LKeys = FData.Keys;
        foreach (String LKey in LKeys)
        {
          if (LKey[0] != '=' && ANames.IndexOf(';' + LKey + ';') == -1)
            AddParam(LKey, DBNull.Value);
        }
      }
      else
        foreach (String LName in ANames.Split(';'))
        {
          CheckName(LName, false);
          if (FData.ContainsKey(LName))
            AddParam(LName, DBNull.Value);
        }
    }

    protected bool EqualValues(Object value1, Object value2)
    {
      SqlDbType type1 = Sql.GetSqlType(value1);
      SqlDbType type2 = Sql.GetSqlType(value2);
      if (type2 == SqlDbType.Binary || type2 == SqlDbType.VarBinary)
      {
        if (type1 != SqlDbType.Binary && type1 != SqlDbType.VarBinary) return false;
        if (((SqlBytes)value1).Length != ((SqlBytes)value2).Length) return false;
        for (Int32 i = 0; i < ((SqlBytes)value2).Length - 1; i++)
          if (((SqlBytes)value2).Buffer[i] != ((SqlBytes)value1).Buffer[i]) return false;
      }
      else if (type1 != SqlDbType.Binary && type1 != SqlDbType.VarBinary)
      {
        if (Sql.ValueToString(value2, Sql.ValueDbStyle.SQL) != Sql.ValueToString(value1, Sql.ValueDbStyle.SQL)) return false;
      }
      else
        return false;
      return true;
    }

    /// <summary>
    /// Определяет, равны ли списки параметров
    /// </summary>
    /// <param name="arg"></param>
    /// <returns>Список параметров, который требуется сравнить с текущим</returns>
    protected bool Equals(TParams AParams)
    {
      if (AParams == null) return false;
      if (FData.Count != AParams.FData.Count) return false;

      foreach (KeyValuePair<String, Object> LDataPair in FData)
        if (LDataPair.Key[0] != '=')
        {
          Object LValue;
          if (!AParams.FData.TryGetValue(LDataPair.Key, out LValue)) return false;
          if (!EqualValues(LValue, LDataPair.Value)) return false;
        }
      return true;
    }

    /// <summary>
    /// Возвращет ObjectId и значение расширенного свойства
    /// </summary>
    private Int32 GetExtendedProperty(String AObjectName, String APropertyName, out String APropertyValue)
    {
      APropertyValue = null;
      Int32 objectId = -1;
      // SqlCommand cmd = new SqlCommand(String.Format("SELECT OBJECT_ID('{0}'), (SELECT '[' + OBJECT_SCHEMA_NAME(OBJECT_ID(CAST([value] AS VARCHAR(MAX)))) + '].[' + OBJECT_NAME(OBJECT_ID(CAST([value] AS VARCHAR(MAX)))) + ']' FROM [sys].[fn_listextendedproperty] ('TParams::{1}', 'schema', PARSENAME('{0}', 2), 'FUNCTION', PARSENAME('{0}', 1), default, default))", objectName, propName), connection);
      SqlCommand cmd = new SqlCommand(String.Format("SELECT OBJECT_ID('{0}'), (SELECT CAST([value] AS VARCHAR(MAX)) FROM [sys].[fn_listextendedproperty] ('TParams::{1}', 'schema', PARSENAME('{0}', 2), 'FUNCTION', PARSENAME('{0}', 1), default, default))", AObjectName, APropertyName), FContextConnection);
      SqlDataReader rdr = cmd.ExecuteReader();
      if (rdr.Read())
      {
        if (rdr.IsDBNull(0))
          throw new Exception(String.Format("Не найден объект '{0}'", AObjectName));
        objectId = rdr.GetInt32(0);
        if (!rdr.IsDBNull(1)) APropertyValue = rdr.GetString(1);
      }
      rdr.Close();
      return objectId;
    }

    /// <summary>
    /// Выполняет функцию принимающую и возвращающую список параметров
    /// </summary>
    private TParams ExecSQLFunction(String funcName)
    {
      //TParams result = null;
      SqlCommand cmd = FContextConnection.CreateCommand();
      cmd.CommandText = "SELECT " + funcName + "(@Params)";
      cmd.CommandType = CommandType.Text;
          
      SqlParameter param = new SqlParameter("@Params", SqlDbType.Udt);
      param.UdtTypeName = "[dbo].[TParams]";
      param.Direction = ParameterDirection.Input;
      param.Value = this;
      cmd.Parameters.Add(param);

      return (TParams)cmd.ExecuteScalar();
    }

    public static object InternalEvaluate(TParams AParams, String AExpression, SqlConnection AContextConnection = null)
    {
      if(AContextConnection == null)
        if(AParams == null)
        { 
          AContextConnection = new SqlConnection(Const_ContextConnection);
          AContextConnection.Open();
        }
        else
        {
          AParams.InitContextConnection();
          AContextConnection = AParams.ContextConnection;
        }

      SqlCommand LSqlCommand = AContextConnection.CreateCommand();
      LSqlCommand.CommandText = AExpression; //"SELECT (" + AExpression + ")";
      LSqlCommand.CommandType = CommandType.Text;

      Sql.ParamsParser Parser =
        new Sql.ParamsParser
            (
              AExpression,
              '@',
              TCommentMethods.None,
              new char[0],
              new char[] {'\''}
             );

      while (Parser.MoveNext() && !String.IsNullOrEmpty(Parser.Current.Value))
        if (LSqlCommand.Parameters.IndexOf('@' + Parser.Current.Value) == -1)
        {
          Object LValue; 
          if (AParams != null && AParams.FData.TryGetValue(Parser.Current.Value, out LValue))
            LSqlCommand.Parameters.AddWithValue('@' + Parser.Current.Value, LValue).Direction = ParameterDirection.Input;
          else
            LSqlCommand.Parameters.Add('@' + Parser.Current.Value, SqlDbType.VarChar, 1).Direction = ParameterDirection.Input;
        }

      return LSqlCommand.ExecuteScalar();
    }

    public static object Evaluate(TParams AParams, String AExpression, SqlConnection AContextConnection = null)
    {
      if (String.IsNullOrEmpty(AExpression)) return null;

      return InternalEvaluate(AParams, "SELECT (" + AExpression + ")", AContextConnection);
    }

    public static Boolean EvaluateBoolean(TParams AParams, String AExpression, Boolean ADefault = false, SqlConnection AContextConnection = null)
    {
      if (String.IsNullOrEmpty(AExpression)) return ADefault;

      object LResult = InternalEvaluate(AParams, "SELECT Cast(1 AS Bit) WHERE (" + AExpression + ")", AContextConnection);
      if (LResult == null || LResult == DBNull.Value) return false;
      return (Boolean)LResult;
    }

    /// <summary>
    /// Подготавливает параметры
    /// </summary>
    /// <param name="name">Имя функции, для которой необходимо подготовить параметры</param>
    protected void Prepare(String AName)
    {
      InitContextConnection();

      // Получем имя функции для подготовки параметров
      String  LFuncName;
      Int32   LObjectId = GetExtendedProperty(AName, "Prepare", out LFuncName);

      // Если для функции параметры уже подготовленны, то выходим
      if (Prepared(LObjectId)) return;

      // Добавляем признак подготовленности параметров
      if (FPreparedData == null) FPreparedData = new SortedList<Int32, TPreparedDataItem>();
      TPreparedDataItem LPreparedDataItem = new TPreparedDataItem();
      FPreparedData.Add(LObjectId, LPreparedDataItem);
      if (LFuncName == null) return;

      // Добавляем объект в стек подготовки параметров
      if (FPreparedObjects == null) FPreparedObjects = new List<Int32>();
      FPreparedObjects.Add(LObjectId);

      // Выполняем функцию подготовки параметров
      TParams LResult = ExecSQLFunction(LFuncName);

      if (LResult != null)
      {
        // Добавляем подготовленные объекты и зависимые параметры
        if (LResult.FPreparedData != null)
          foreach (KeyValuePair<Int32, TPreparedDataItem> LResultPreparedDataPair in LResult.FPreparedData)
          {
            TPreparedDataItem LFindPreparedDataItem;
            if (FPreparedData.TryGetValue(LResultPreparedDataPair.Key, out LFindPreparedDataItem))
              LFindPreparedDataItem.DependedParams = LResultPreparedDataPair.Value.DependedParams;
            else
              FPreparedData.Add(LResultPreparedDataPair.Key, LResultPreparedDataPair.Value);
          }

        // Добавляем новые параметры
        if (LResult.FData != null)
          foreach (KeyValuePair<String, Object> LDataPair in LResult.FData)
            if (!FData.ContainsKey(LDataPair.Key))
            {
              AddParam(LDataPair.Key, LDataPair.Value);
              if (LPreparedDataItem.CalculatedParams == null)
                LPreparedDataItem.CalculatedParams = new List<String>();
              LPreparedDataItem.CalculatedParams.Add(LDataPair.Key.ToUpper());
            }
      }


      // Удаляем объект из стека подготовки параметров
      FPreparedObjects.RemoveAt(FPreparedObjects.Count - 1);
      if (FPreparedObjects.Count == 0)
      {
        FPreparedObjects          = null;
        FRegisteredDependedParams = null;
      }
    }

    /// <summary>
    /// Убирает подготовку параметров
    /// </summary>
    protected void UnPrepare()
    {
      if (FPreparedData == null) return;

      // Удаляем расчитанные параметры
      List<String> LDeleted = new List<String>();

      foreach (KeyValuePair<String, Object> LDataPair in FData)
        if (LDataPair.Key[0] == '=')
          LDeleted.Add(LDataPair.Key);

      foreach (String LParam in LDeleted)
        FData.Remove(LParam);

      // Убираем признак подготовленности параметров
      FPreparedData.Clear();
    }

    /// <summary>
    /// Возвращает признак подготовленности параметров
    /// </summary>
    /// <param name="ObjectId">ObjectId объекта</param>
    /// <returns>Признак подготовленности параметров</returns>
    protected SqlBoolean Prepared(SqlInt32 ObjectId)
    {
      if (ObjectId.IsNull) return false;
      if (FPreparedData != null && FPreparedData.ContainsKey((Int32)ObjectId)) return true;
      return false;
    }

    /// <summary>
    /// Регистрирует зависимые параметры
    /// </summary>
    /// <param name="value">Список параметров, разделенных запятой</param>
    protected void RegisterDependedParams(String AParamNames)
    {
      // Функция может вызываться только из функции подготовки параметров
      if (FPreparedObjects == null)
        throw new Exception("Метод RegisterDependedParams может использоваться только в функции подготовки параметров");

      // Игнорируем пустые значения
      if (String.IsNullOrEmpty(AParamNames)) return;

      // Добавляем параметры во все подготавливаемые объекты
      String[] LDependedParams = AParamNames.ToUpper().Split(',');
      foreach (String LParamName in LDependedParams)
        foreach (Int32 LObjectId in FPreparedObjects)
        {
          TPreparedDataItem FData = FPreparedData[LObjectId];
          if (FData.DependedParams == null)
          {
            FData.DependedParams = new List<String>();
            FData.DependedParams.Add(LParamName);
          }
          else if (FData.DependedParams.IndexOf(LParamName) < 0)
            FData.DependedParams.Add(LParamName);
        }
    }

    /// <summary>
    /// Определяет создан ли параметр функцией подготовки параметров
    /// </summary>
    private bool IsCalculatedParams(String AName)
    {
      if (FPreparedData == null) return false;
      AName = AName.ToUpper();
      foreach (KeyValuePair<Int32, TPreparedDataItem> LPreparedDataPair in FPreparedData)
      {
        if (LPreparedDataPair.Value.CalculatedParams != null)
          foreach (String LParamName in LPreparedDataPair.Value.CalculatedParams)
          {
            if (LParamName == AName) return true;
          }
      }
      return false;
    }

    enum FormatTypes { TinyInt, SmallInt, Int, BigInt, Hex, Date, Time, DateTime, DateTimeOffset, String, VarChar, NVarChar, Float, Decimal, Numeric, Boolean, Bit };
    protected static String Format(TParams AParams, String AFormat)
    {
      const Char CParamChar  = ':';
      const Char CStickQuote = '|';
      const Char CEqualQuote = '=';

      if (String.IsNullOrEmpty(AFormat))
        return AFormat;

      int LStartIndex = 0;
      StringBuilder LResult = new StringBuilder(AFormat.Length);

      while (true)
      {
        if (LStartIndex >= AFormat.Length) break;
        int LIndex = AFormat.IndexOf(CParamChar, LStartIndex);                  // Находим ":" - начало переменной для замены

        Char LRightQuote;
        if ((LIndex < 0) || (LIndex == AFormat.Length - 1))                     // последний букавок в строке
        {
          LResult.Append(AFormat, LStartIndex, AFormat.Length - LStartIndex);   // не осталось переменных для автозамены выходим
          break;
        }
        if (LIndex > LStartIndex)
        {
          LResult.Append(AFormat, LStartIndex, LIndex - LStartIndex);           // не осталось переменных для автозамены выходим
          LStartIndex = LIndex;
        }

        LIndex++;  //Двигаемся на 1 элемент вперед 
        int LRightQuoteIndex = -1;

        //Ожидаем квотик
        switch (AFormat[LIndex])
        {
          case '{':
            LRightQuote = '}';
            break;
          case '[':
            LRightQuote = ']';
            break;
          case CParamChar: //попалось еще ":" - превратим их 2х в 1
            LResult.Append(AFormat, LStartIndex, LIndex - LStartIndex);
            LStartIndex = LIndex + 1;
            continue;

          default: //Нету квотика значит это было ввообще не наше двоеточие
            char LChar = AFormat[LStartIndex];

#region NotQoutedName
            LRightQuote = '0';
            LRightQuoteIndex = LIndex;
            while (LRightQuoteIndex < AFormat.Length)
            {
              LChar = AFormat[LRightQuoteIndex];
              if (
                      (LChar >= 'A') && (LChar <= 'Z')
                      ||
                      (LChar >= 'a') && (LChar <= 'z')
                      ||
                      (LChar >= '0') && (LChar <= '9')
                      ||
                      (LChar == '_') || (LChar == '#') || (LChar == '.') || (LChar == '{') || (LChar == '}')
                  )
                LRightQuoteIndex++;
              else
                break;

            }
            break;
#endregion
        }

        //Нашли левый квотик ищем где же правый
        String LParamName     = null;
        String LDisplayType   = null;
        String LDisplayFormat = null;
        String LNullValue     = null;

        if (LRightQuoteIndex >= 0)
        {
          LParamName = AFormat.Substring(LIndex, LRightQuoteIndex - LIndex);
          LRightQuoteIndex--;
        }
        else
        {
          LRightQuoteIndex = AFormat.IndexOf(LRightQuote, LIndex + 1);

          if (LRightQuoteIndex < 0) //не нашли
          {
            LResult.Append(AFormat, LStartIndex, AFormat.Length - LStartIndex);
            break;
          }
          LResult.Append(AFormat, LStartIndex, LIndex - LStartIndex - 1);
          //Теперь у нас имя от LIndex до LRightQuoteIndex парсим его

          int EqualIndex = AFormat.IndexOf(CEqualQuote, LIndex, LRightQuoteIndex - LIndex); //находим знак равно

          int TypeIndex = AFormat.IndexOf(CStickQuote, LIndex, LRightQuoteIndex - LIndex); //находим палку
          int NullIndex = -1;

          if (EqualIndex > 0)
          {
            if (TypeIndex > EqualIndex)
            {
              NullIndex = TypeIndex;
              TypeIndex = -1;
            }
            else
            {
              NullIndex = AFormat.IndexOf(CStickQuote, EqualIndex, LRightQuoteIndex - EqualIndex);
            }
          }

          if ((TypeIndex > LIndex + 1) && (TypeIndex < LRightQuoteIndex))
          {
            LParamName = AFormat.Substring(LIndex + 1, TypeIndex - LIndex - 1);
            if (EqualIndex > 0)
              LDisplayType = AFormat.Substring(TypeIndex + 1, EqualIndex - TypeIndex - 1);
            else
              LDisplayType = AFormat.Substring(TypeIndex + 1, LRightQuoteIndex - TypeIndex - 1);
          }

          if (EqualIndex > 0)
          {
            if (LParamName == null)
              LParamName = AFormat.Substring(LIndex + 1, EqualIndex - LIndex - 1);

            if (NullIndex > 0)
            {
              LDisplayFormat = AFormat.Substring(EqualIndex + 1, NullIndex - EqualIndex - 1);
              LNullValue = AFormat.Substring(NullIndex + 1, LRightQuoteIndex - NullIndex - 1);
            }
            else
            {
              LDisplayFormat = AFormat.Substring(EqualIndex + 1, LRightQuoteIndex - EqualIndex - 1);
            }
          }
          if (LParamName == null)
          {
            LParamName = AFormat.Substring(LIndex + 1, LRightQuoteIndex - LIndex - 1);
          }
        }

        Object LValue;
        if (AParams == null || !AParams.FData.TryGetValue(LParamName, out LValue))
        {
          if (LNullValue == null)
            return null;
          else
            LResult.Append(LNullValue);
        }
        else
        {
          FormatTypes 
            LFormat = (LDisplayType == null)
                      ?
                        FormatTypes.String
                        :
                        (FormatTypes)System.Enum.Parse(typeof(FormatTypes), LDisplayType, true);
          if (LValue == null)
          {
            if (LNullValue == null)
              return null;
            else
              LResult.Append(LNullValue);
          }
          else if (String.IsNullOrEmpty(LDisplayFormat))
            LResult.Append(LValue.ToString());
          else
          {
            switch (LFormat)
            {
              case FormatTypes.Bit:
              case FormatTypes.Boolean:
                try
                {
                  LResult.Append(Formats.FormatBoolean(LDisplayFormat, AsBit(LValue).IsTrue));
                }
                catch (Exception E)
                {
                  throw new Exception("Ошибка форматирования Boolean = " + LValue.ToString() + ": " + E.Message);
                }
                break;

              case FormatTypes.TinyInt:
              case FormatTypes.SmallInt:
              case FormatTypes.Int:
              case FormatTypes.BigInt:
              case FormatTypes.Hex:
                try
                {
                  LResult.Append(Formats.InternalFormatInteger(LDisplayFormat, AsBigInt(LValue).Value));
                }
                catch (Exception E)
                {
                  throw new Exception("Ошибка форматирования Int64 = " + LValue.ToString() + ": " + E.Message);
                }
                break;

              case FormatTypes.Date:
              case FormatTypes.Time:
              case FormatTypes.DateTime:
                try
                {
                  LResult.Append(Formats.FormatDateTime2(LDisplayFormat, AsDateTime2(LValue)));
                }
                catch (Exception E)
                {
                  throw new Exception("Ошибка форматирования DateTime = " + LValue.ToString() + ": " + E.Message);
                }
                break;

              case FormatTypes.DateTimeOffset:
                try
                { 
                  LResult.Append(Formats.FormatDateTimeOffset(LDisplayFormat, AsDateTimeOffset(LValue)));
                }
                catch (Exception E)
                {
                  throw new Exception("Ошибка форматирования DateTimeOffset = " + LValue.ToString() + ": " + E.Message);
                }
                break;

              case FormatTypes.VarChar:
              case FormatTypes.NVarChar:
              case FormatTypes.String:
                LResult.Append(Formats.FormatString(Int16.Parse(LDisplayFormat), LValue.ToString()));
                break;

              case FormatTypes.Float:
              case FormatTypes.Decimal:
              case FormatTypes.Numeric:
                try
                {
                  LResult.Append(Formats.InternalFormatDecimal(LDisplayFormat, LValue.ToString(), "."));
                }
                catch (Exception E)
                {
                  throw new Exception("Ошибка форматирования Decimal = " + LValue.ToString() + ": " + E.Message);
                }
                break;
            }
          }
        }
        LStartIndex = LRightQuoteIndex + 1;
      }
      return LResult.ToString();
    }

    #region Serialization

    public SortedDictionary<string, SqlDbType> ReadXmlSchema(XmlReader r)
    {
      SortedDictionary<string, SqlDbType> result;

      XmlSchema schemaInfo = XmlSchema.Read(r, delegate(Object sender, ValidationEventArgs e)
      {
        throw new Exception(e.Message);
      });

      result = new SortedDictionary<string, SqlDbType>(StringComparer.OrdinalIgnoreCase);
      XmlSchemaElement element = (XmlSchemaElement)schemaInfo.Items[0];
      XmlSchemaComplexType complexType = (XmlSchemaComplexType)element.SchemaType;

      if (complexType.Attributes.Count > 0)
      {
        XmlSchemaObjectEnumerator enumerator = complexType.Attributes.GetEnumerator();
        String str = "";
        while (enumerator.MoveNext())
        {
          XmlSchemaAttribute attribute = (XmlSchemaAttribute)enumerator.Current;
          String attributeName = XmlConvert.EncodeLocalName(attribute.Name);

          if (attribute.SchemaTypeName.Namespace == SqlTypesNamespace)
            result.Add(attributeName, Sql.TypeFromString(attribute.SchemaTypeName.Name));
          else if (attribute.SchemaType != null && attribute.SchemaType.Content is XmlSchemaSimpleTypeRestriction && ((XmlSchemaSimpleTypeRestriction)attribute.SchemaType.Content).BaseTypeName.Namespace == SqlTypesNamespace)
          {
            result.Add(attributeName, Sql.TypeFromString(((XmlSchemaSimpleTypeRestriction)attribute.SchemaType.Content).BaseTypeName.Name));
          }
          else
            throw new Exception(String.Format("Не поддерживаемый тип параметра '{0}'", attributeName));
        }
        if (str != "") throw new Exception(str);
      }

      return result;
    }

    public void ReadXmlValues(XmlReader r, SortedDictionary<string, SqlDbType> types)
    {
      try
      {
        FIgnoreCheckName = true;
        for (Int32 i = 0; i < r.AttributeCount; i++)
        {
          r.MoveToAttribute(i);
          if (r.Name != "xmlns")
          {
            String LParamName = XmlConvert.DecodeName(r.Name);
            SqlDbType LParamType;
            if (types == null)
              AddParam(LParamName, new SqlString(r.Value));
            else if (types.TryGetValue(LParamName, out LParamType))
              AddParam(LParamName, Sql.ValueFromString(r.Value, LParamType, Sql.ValueDbStyle.XML));
            else
              AddParam(LParamName, Sql.ValueFromString(r.Value, SqlDbType.VarChar, Sql.ValueDbStyle.XML));
          }
        }
      }
      finally
      {
        FIgnoreCheckName = false;
      }
    }

    public void ReadXml(XmlReader r)
    {
      SortedDictionary<string, SqlDbType> types = null;
      FData.Clear();

      while (r.Read())
      {
        if (r.NodeType != XmlNodeType.Element) continue;

        // Схема параметров
        if (r.Name == "xsd:schema")
          types = ReadXmlSchema(r);
        // Информация о подготовленных параметрах 
        else if (r.Name == "Options")
        {
          while (r.Read())
            if (r.Name == "Options" && r.NodeType == XmlNodeType.EndElement) break;
            else if (r.Name == "Prepared")
            {
              if (FPreparedData == null) FPreparedData = new SortedList<Int32, TPreparedDataItem>();
              TPreparedDataItem prepared = new TPreparedDataItem();

              if (r.GetAttribute("Calculated") != null)
              {
                foreach (String str in r.GetAttribute("Calculated").Split(',')) prepared.CalculatedParams.Add(str);
                if (prepared.CalculatedParams.Count == 0) prepared.CalculatedParams = null;
              }

              if (r.GetAttribute("Depended") != null)
              {
                foreach (String str in r.GetAttribute("Depended").Split(',')) prepared.DependedParams.Add(str);
                if (prepared.DependedParams.Count == 0) prepared.DependedParams = null;
              }
              FPreparedData.Add(Int32.Parse(r.GetAttribute("ObjectId")), prepared);
            }
        }
        // Параметры
        else
          ReadXmlValues(r, types);

      }

      // Проверка целостности параметров
      if (FData != null)
        foreach (KeyValuePair<string, Object> LDataPair in FData)
          if (LDataPair.Key[0] == '=' && !IsCalculatedParams(LDataPair.Key))
            throw new Exception(String.Format("Неверное имя параметра '{0}', параметр начинающийся с '=' может быть задан(изменен) только в функции подготовки параметров", LDataPair.Key));
    }

    public XmlSchema GetSchema()
    {
      return null;
    }

    public void WriteXmlSchema(XmlWriter w)
    {
      w.WriteStartElement("xsd", "schema", XmlSchema.Namespace);

      w.WriteAttributeString("xmlns", "sqltypes", null, SqlTypesNamespace);
      w.WriteAttributeString("targetNamespace", "Params");

      w.WriteStartElement("xsd", "import", XmlSchema.Namespace);
      w.WriteAttributeString("namespace", SqlTypesNamespace);
      w.WriteAttributeString("schemaLocation", SqlTypesNamespaceLocation);
      w.WriteEndElement();

      w.WriteStartElement("xsd", "element", XmlSchema.Namespace);
      w.WriteAttributeString("name", "Params");
      w.WriteStartElement("xsd", "complexType", XmlSchema.Namespace);

      foreach (KeyValuePair<String, Object> LDataPair in FData)
      {
        w.WriteStartElement("xsd", "attribute", XmlSchema.Namespace);
        w.WriteAttributeString("name", XmlConvert.EncodeLocalName(LDataPair.Key));
        w.WriteAttributeString("type", "sqltypes:" + Sql.GetSqlType(LDataPair.Value).ToString().ToLower());
        w.WriteEndElement();
      }

      w.WriteEndElement();
      w.WriteEndElement();
      w.WriteEndElement();

      // Схема ("правильный" вариант, но в нем используется Xml.Serializator, а под него нужна статическая сборка для сериализации :( )
      // XmlSchema s = new XmlSchema();
      // s.Namespaces.Add("xsd", sqltypesSchemaDefault);
      // s.Namespaces.Add("sqltypes", SqlTypesNamespace);
      // s.TargetNamespace = "Params";
      // s.ElementFormDefault = XmlSchemaForm.Qualified;
      // XmlSchemaImport import = new XmlSchemaImport();
      // import.Namespace = sqltypesSchema;
      // import.SchemaLocation = SqlTypesNamespaceLocation;
      // s.Includes.Add(import);
      // XmlSchemaElement element = new XmlSchemaElement();
      // s.Items.Add(element);
      // element.Name = "Params";
      // XmlSchemaComplexType complexType = new XmlSchemaComplexType();
      // element.SchemaType = complexType;
      // foreach (KeyValuePair<String, SqlDbType> type in types)
      // {
      //     XmlSchemaAttribute attribute = new XmlSchemaAttribute();
      //     complexType.Attributes.Add(attribute);
      //     attribute.Name = XmlConvert.EncodeLocalName(type.Key);
      //     attribute.SchemaTypeName = new XmlQualifiedName("sqltypes:" + type.Value.ToString().ToLower());
      // }
      // s.Write(w);
    }

    public void WriteXmlValues(XmlWriter w)
    {
      w.WriteStartElement("Params");
      w.WriteAttributeString("xmlns", "Params");
      foreach (KeyValuePair<String, Object> LDataPair in FData)
      {
        w.WriteStartAttribute(XmlConvert.EncodeLocalName(LDataPair.Key));
        w.WriteValue(Sql.ValueToString(LDataPair.Value, Sql.ValueDbStyle.XML));
        w.WriteEndAttribute();
      }
      w.WriteEndElement();
    }

    public void WriteXml(XmlWriter w)
    {
      if (FData.Count == 0) return;

      // Схема параметров
      WriteXmlSchema(w);

      // Параметры
      WriteXmlValues(w);

      // Информация о подготовленных параметрах
      if (FPreparedData != null)
      {
        w.WriteStartElement("Options");
        foreach (KeyValuePair<Int32, TPreparedDataItem> LPreparedDataPair in FPreparedData)
        {
          w.WriteStartElement("Prepared");
          w.WriteAttributeString("ObjectId", LPreparedDataPair.Key.ToString());
          String result = "";
          foreach (String str in LPreparedDataPair.Value.CalculatedParams) result += (result == "" ? "" : ";") + str;
          if (result != "") w.WriteAttributeString("Calculated", result);
          result = "";
          foreach (String str in LPreparedDataPair.Value.DependedParams) result += (result == "" ? "" : ";") + str;
          if (result != "") w.WriteAttributeString("Depended", result);
          w.WriteEndElement();
        }
        w.WriteEndElement();
      }
    }

    private Boolean ReadValues(System.IO.BinaryReader r)
    {
      Int32 count;
      if(r.BaseStream.Length == 0) return false;
      count = Sql.Read7BitEncodedInt(r);
     
      if (count == 0) return true;

      for (Int32 i = 0; i < count; i++)
      {
        String    name  = r.ReadString();
        SqlDbType LType = (SqlDbType)r.ReadUInt16();
        Object    value = null;
        Int32 len;
        //Int32 lcid;
        //SqlCompareOptions co;

        switch (LType)
        {
          case SqlDbType.Bit      : value = new SqlBoolean(r.ReadBoolean()); break;
          case SqlDbType.TinyInt  : value = new SqlByte(r.ReadByte()); break;
          case SqlDbType.SmallInt : value = new SqlInt16((Int16)r.ReadInt16()); break;
          case SqlDbType.Int      : value = new SqlInt32((Int32)r.ReadInt32()); break;
          case SqlDbType.BigInt   : value = new SqlInt64(r.ReadInt64()); break;

          case SqlDbType.Binary   :
          case SqlDbType.VarBinary: len = r.ReadUInt16(); value = new SqlBytes(r.ReadBytes(len)); break;

          case SqlDbType.Char     :
          case SqlDbType.VarChar  : //value = new Sql.SqlAnsiString(r); break;
          case SqlDbType.NChar:
          case SqlDbType.NVarChar:
            //co = (SqlCompareOptions)r.ReadUInt16();
            //lcid = r.ReadInt32();
            //value = new SqlString(r.ReadString(), lcid, co);
            value = new SqlString(r.ReadString());
            break;

          case SqlDbType.DateTime     : value = new SqlDateTime(DateTime.FromBinary(r.ReadInt64())); break;
          case SqlDbType.SmallDateTime:
          case SqlDbType.Date         :
          case SqlDbType.DateTime2    : value = DateTime.FromBinary(r.ReadInt64()); break;
          case SqlDbType.Time         : value = TimeSpan.FromTicks(r.ReadInt64()); break;
          case SqlDbType.DateTimeOffset:
            DateTime LDateTime = DateTime.FromBinary(r.ReadInt64());
            value = new DateTimeOffset(LDateTime, TimeSpan.FromTicks(r.ReadInt64()));
            break;

          case SqlDbType.Decimal: value = new SqlDecimal(r.ReadDecimal()); break;
          case SqlDbType.Float  : value = new SqlDouble(r.ReadDouble()); break;
          // Not support SqlDbType.Image
          case SqlDbType.Money  : value = new SqlMoney(r.ReadDecimal()); break;
          case SqlDbType.Real   : value = new SqlSingle(r.ReadDouble()); break;
          case SqlDbType.SmallMoney: value = new SqlMoney(r.ReadDecimal()); break;
          // Not support SqlDbType.Structured
          // Not support SqlDbType.Text
          // Not support SqlDbType.Timestamp
          case SqlDbType.UniqueIdentifier: value = new SqlGuid(r.ReadString()); break;
          // Not support SqlDbType.Variant
          case SqlDbType.Xml:
            XmlReader rXml = XmlReader.Create(new System.IO.StringReader(r.ReadString()));
            value = new SqlXml(rXml);
            break;

          case SqlDbType.Udt:
            // TODO: Пока поддержа только TParams
            //String LTypeName = r.ReadString();
            //value = CreateUdtObject(LTypeName);
            //if (value is IBinarySerialize)
            //  (value as IBinarySerialize).Read(r);
            //else
            //  throw new Exception(String.Format("Невозможно прочитать данные типа UDT '{0}' - не поддерживается IBinarySerialize", LTypeName));
            value = new SqlUdt(r);
            break;

          default:
            throw new Exception(String.Format("Невозможно прочитать данные, тип '{0}' не поддерживается текущей версией {1}", LType.ToString(), this.GetType().Name));
          // Not support SqlDbType.NText
        }
        if (value != null) FData.Add(name, value);
      }

      return true;
    }

    public void Read(System.IO.BinaryReader r)
    {
      // Инфа по параметрам
      if (!ReadValues(r)) return;

      //System.IO.Stream s = r.BaseStream;
      //if(s.Length < s.Position) return;

      // Информация о подготовленных параметрах
      if(r.BaseStream.Position == r.BaseStream.Length) return;

      Int32 count;
      count = Sql.Read7BitEncodedInt(r);

      if (count == 0)
      {
        FPreparedData = null;
      }
      else
      {
        FPreparedData = new SortedList<Int32, TPreparedDataItem>();
        for (Int32 i = 0; i < count; i++)
        {
          Int32 ObjectId = r.ReadInt32();
          TPreparedDataItem prepared = new TPreparedDataItem();

          Int32 count1 = Sql.Read7BitEncodedInt(r);
          if (count1 > 0)
          {
            for (Int32 j = 0; j < count1; j++)
            {
              prepared.DependedParams.Add(r.ReadString());
            }
          }

          count1 = Sql.Read7BitEncodedInt(r);
          if (count1 > 0)
          {
            for (Int32 j = 0; j < count1; j++)
            {
              prepared.CalculatedParams.Add(r.ReadString());
            }
          }

          FPreparedData.Add(ObjectId, prepared);
        }
      }

      // Информация об объектах, для которых происходит подготовка параметров
      if(r.BaseStream.Position == r.BaseStream.Length) return;
      count = Sql.Read7BitEncodedInt(r);

      if (count == 0)
      {
        FPreparedObjects = null;
      }
      else
      {
        FPreparedObjects = new List<Int32>();
        for (Int32 j = 0; j < count; j++)
        {
          FPreparedObjects.Add(r.ReadInt32());
        }
      }

      // Зависимые параметры         
      if(r.BaseStream.Position == r.BaseStream.Length) return;
      count = Sql.Read7BitEncodedInt(r);

      if (count == 0)
      {
        FRegisteredDependedParams = null;
      }
      else
      {
        FRegisteredDependedParams = new List<String>();
        for (Int32 j = 0; j < count; j++)
        {
          FRegisteredDependedParams.Add(r.ReadString());
        }
      }

      // На будущее
      //r.ReadInt32();
    }

    public void WriteValues(System.IO.BinaryWriter w)
    {
      //w.Write((UInt16)FData.Count);
      Sql.Write7BitEncodedInt(w, (Int32)FData.Count);

      foreach (KeyValuePair<String, Object> LDataPair in FData)
      {
        w.Write(LDataPair.Key);
        SqlDbType type = Sql.GetSqlType(LDataPair.Value);
        w.Write((UInt16)type);

        switch (type)
        {
          case SqlDbType.Bit      : w.Write((bool)((SqlBoolean)LDataPair.Value)); break;
          case SqlDbType.TinyInt  : w.Write((Byte)((SqlByte)LDataPair.Value)); break;
          case SqlDbType.SmallInt : w.Write((Int16)((SqlInt16)LDataPair.Value)); break;
          case SqlDbType.Int      : w.Write((Int32)((SqlInt32)LDataPair.Value).Value); break;
          case SqlDbType.BigInt   : w.Write((Int64)((SqlInt64)LDataPair.Value)); break;

          case SqlDbType.Binary   :
          case SqlDbType.VarBinary: 
            if(LDataPair.Value is SqlBinary)
            {
              w.Write((UInt16)((SqlBinary)LDataPair.Value).Length);
              w.Write(((SqlBinary)LDataPair.Value).Value, 0, (Int32)((SqlBinary)LDataPair.Value).Length);
            }
            else
            {
              w.Write((UInt16)((SqlBytes)LDataPair.Value).Length);
              w.Write(((SqlBytes)LDataPair.Value).Buffer, 0, (Int32)((SqlBytes)LDataPair.Value).Length);
            }
            break;

          case SqlDbType.Char:
          case SqlDbType.VarChar: //((Sql.SqlAnsiString)LDataPair.Value).Write(w); break;
          case SqlDbType.NChar:
          case SqlDbType.NVarChar:
            SqlString NVarChar;
            if(LDataPair.Value is SqlChars)
              NVarChar = ((SqlChars)LDataPair.Value).ToSqlString().Value;
            else
              NVarChar = (SqlString)LDataPair.Value;
            //w.Write((UInt16)NVarChar.SqlCompareOptions);
            //w.Write(NVarChar.LCID);
            w.Write(NVarChar.Value);
            break;

          case SqlDbType.DateTime     : w.Write((Int64)((DateTime)((SqlDateTime)LDataPair.Value).Value).ToBinary()); break;
          case SqlDbType.SmallDateTime:
          case SqlDbType.Date         :
          case SqlDbType.DateTime2    : w.Write((Int64)((DateTime)LDataPair.Value).ToBinary()); break;
          case SqlDbType.Time         : w.Write((Int64)((TimeSpan)LDataPair.Value).Ticks); break;
          case SqlDbType.DateTimeOffset: 
            DateTimeOffset LDateTimeOffset = (DateTimeOffset)LDataPair.Value;
            w.Write((Int64)LDateTimeOffset.DateTime.ToBinary());
            w.Write((Int64)LDateTimeOffset.Offset.Ticks);
            break;

          case SqlDbType.Decimal    : w.Write(((SqlDecimal)LDataPair.Value).Value); break;
          case SqlDbType.Float      : w.Write(((SqlDouble)LDataPair.Value).Value); break;
          case SqlDbType.Real       : w.Write((Double)((SqlSingle)LDataPair.Value).Value); break;
          case SqlDbType.SmallMoney :
          case SqlDbType.Money      : w.Write(((SqlMoney)LDataPair.Value).Value); break;

          case SqlDbType.Udt:
            ((SqlUdt)LDataPair.Value).Write(w);
            // TODO: Пока поддержа только TParams
            //if (LDataPair.Value is IBinarySerialize)
            //{
            // // w.Write(LDataPair.Value.GetType().Assembly.FullName);
            //  w.Write(LDataPair.Value.GetType().FullName);
            //  (LDataPair.Value as IBinarySerialize).Write(w);
            //}
            //else
            //  throw new Exception(String.Format("Невозможно записать данные, тип UDT '{0}' не поддерживается текущей версией {1}", LDataPair.Value.GetType().Name, this.GetType().Name));

            //(LDataPair.Value as IBinarySerialize).Write(w);
            break;

          case SqlDbType.UniqueIdentifier: w.Write(((Guid)((SqlGuid)LDataPair.Value).Value).ToString()); break;

          case SqlDbType.Xml: w.Write(((SqlXml)LDataPair.Value).Value); break;
          default:
            throw new Exception(String.Format("Невозможно записать данные, тип '{0}' не поддерживается текущей версией {1}", type.ToString(), this.GetType().Name));

          // Not support SqlDbType.Image
          // Not support SqlDbType.NText
          // Not support SqlDbType.Structured
          // Not support SqlDbType.Text
          // Not support SqlDbType.Timestamp
          // Not support SqlDbType.Variant
        }
      }
    }

    public void Write(System.IO.BinaryWriter w)
    {
      // Информация о подготовленных параметрах
      int PreparedDataCount = (FPreparedData == null) ? 0 : FPreparedData.Count;
      // Информация об объектах, для которых происходит подготовка параметров
      int PreparedObjectsCount = (FPreparedObjects == null) ? 0 : FPreparedObjects.Count;
      // Зависимые параметры
      int RegisteredDependedParamsCount = (FRegisteredDependedParams == null) ? 0 : FRegisteredDependedParams.Count;

      if (FData.Count == 0 && PreparedDataCount == 0 && PreparedObjectsCount == 0 && RegisteredDependedParamsCount == 0)
      {
        w.Write((Byte)0);
        return;
      }
      // Инфа по параметрам
      WriteValues(w);

      if (PreparedDataCount == 0 && PreparedObjectsCount == 0 && RegisteredDependedParamsCount == 0) return;
      //w.Write((Byte)PreparedDataCount);
      Sql.Write7BitEncodedInt(w, PreparedDataCount);

      if (PreparedDataCount != 0)
      {
        foreach (KeyValuePair<int, TPreparedDataItem> LPreparedDataPair in FPreparedData)
        {
          w.Write(LPreparedDataPair.Key);

          int PreparedValueDependedParamsCount = (LPreparedDataPair.Value.DependedParams == null) ? 0 : LPreparedDataPair.Value.DependedParams.Count;
          //w.Write((Byte)PreparedValueDependedParamsCount);
          Sql.Write7BitEncodedInt(w, PreparedValueDependedParamsCount);
          if (PreparedValueDependedParamsCount > 0)
            foreach (String paramName in LPreparedDataPair.Value.DependedParams)
            {
              w.Write(paramName);
            }

          int PreparedValueCalculatedParamsCount = (LPreparedDataPair.Value.CalculatedParams == null) ? 0 : LPreparedDataPair.Value.CalculatedParams.Count;
          //w.Write((Byte)PreparedValueCalculatedParamsCount);
          Sql.Write7BitEncodedInt(w, PreparedValueCalculatedParamsCount);
          if (PreparedValueCalculatedParamsCount > 0)
            foreach (String paramName in LPreparedDataPair.Value.CalculatedParams)
            {
              w.Write(paramName);
            }
        }
      }

      if (PreparedObjectsCount == 0 && RegisteredDependedParamsCount == 0) return;
      // Информация об объектах, для которых происходит подготовка параметров
      //w.Write((Byte)PreparedObjectsCount);
      Sql.Write7BitEncodedInt(w, PreparedObjectsCount);
      if (PreparedObjectsCount != 0)
      {
        foreach (int objectId in FPreparedObjects)
        {
          w.Write(objectId);
        }
      }

      if (RegisteredDependedParamsCount == 0) return;
      // Зависимые параметры
      //w.Write((Byte)RegisteredDependedParamsCount);
      Sql.Write7BitEncodedInt(w, RegisteredDependedParamsCount);
      if (RegisteredDependedParamsCount != 0)
      {
        foreach (String param in FRegisteredDependedParams)
        {
          w.Write(param);
        }
      }
    }

    #endregion Serialization

    protected class TPreparedDataItem
    {
      public List<String> DependedParams   = new List<String>();
      public List<String> CalculatedParams = new List<String>();
    }

    protected const string SqlTypesNamespace = "http://schemas.microsoft.com/sqlserver/2004/sqltypes";
    protected const string SqlTypesNamespaceLocation = "http://schemas.microsoft.com/sqlserver/2004/sqltypes/sqltypes.xsd";

    private SortedList<String, Object>          FData                      = new SortedList<String, Object>(StringComparer.OrdinalIgnoreCase);
    private SortedList<Int32, TPreparedDataItem>FPreparedData;
    private List<Int32>                         FPreparedObjects;
    private List<String>                        FRegisteredDependedParams;
    private SqlConnection                       FContextConnection;

    private bool FIgnoreCheckName = false;
  }
}
