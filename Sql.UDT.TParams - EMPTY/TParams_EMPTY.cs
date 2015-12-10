using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;


namespace INT_EMPTY
{
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
    /// Список параметров типа SqlDbValue
    /// Не поддерживает типы: DateTimeOffset, Image, NText, Structured, Text, Timestamp, Variant
    /// </summary>
    [Serializable]
    public class TParams_EMPTY : IBinarySerialize, IXmlSerializable
    {
      public static void Write7BitEncodedInt(System.IO.BinaryWriter w, Int32 value)  
      {  
        UInt32 num = (UInt32) value;  
  
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


        /// <summary>
        /// Инициализация объекта
        /// </summary>
        public void Init()
        {
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

        /// <summary>
        /// Квотирует название
        /// </summary>
        private static String EncodeName(String str)
        {
//            for (Int32 i = str.Length - 1; i >= 0; i--)
//                if (str[i] == ']') str = str.Insert(i + 1, "]]");
            return "[" + str.Replace("]", "]]") + "]";
        }

        /// <summary>
        /// Деквотирует название
        /// </summary>
        private static String DecodeName(String str)
        {
            str = str.Trim();
            if (str == null || str == "") return "";
            if (str.Length < 2 || str[0] != '[' || str[str.Length - 1] != ']') return str;

          return str.Substring(1, str.Length - 2).Replace("]]", "]");
        }

        private static bool IsQuoteType(SqlDbType type)
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
        /// Квотирует строку
        /// </summary>
        private static String QuoteValue(String str)
        {
//          for (Int32 i = str.Length - 1; i >= 0; i--)
//            if (str[i] == '"') str = str.Insert(i + 1, "\"");
          return "\"" + str.Replace("\"", "\"\"") + "\"";
        }

        /// <summary>
        /// Деквотирует строку
        /// </summary>
        private static String UnQuoteValue(String str)
        {
            str = str.Trim();
            if (string.IsNullOrEmpty(str)) return "";
            if (str.Length < 2 || str[0] != '"' || str[str.Length - 1] != '"') return str;

            return str.Substring(1, str.Length - 2).Replace("\"\"", "\"");
        }

        /// <summary>
        /// Преобразует данные в строку
        /// </summary>
        /// <returns>Возвращает список параметров строкой</returns>
        public override String ToString()
        {
            StringBuilder w = new StringBuilder();

            // Параметры
            foreach (KeyValuePair<String, Object> param in FData)
            {
                if (param.Key[0] == '=') continue;
                SqlDbType type = GetSqlType(param.Value);
                bool isQuote = IsQuoteType(type);
                w.Append((w.Length == 0 ? "" : CultureInfo.TextInfo.ListSeparator)
                    + String.Format("{0}{1}={2}", EncodeName(param.Key), 
                              type == SqlDbType.NVarChar ? "" : (":" + (type == SqlDbType.Udt ? param.Value.GetType().Name : type.ToString())),
                              isQuote ? QuoteValue(ValueToString(param.Value, ValueDbStyle.Text)) : ValueToString(param.Value, ValueDbStyle.Text)));
            }

            return w.ToString();
        }

        /// <summary>
        /// Конверитрует список параметров из строки
        /// </summary>
        /// <param name="s">Список параметров, заданный строкой</param>
        public void FromString(SqlString s)
        {
            Clear();
            if (s.IsNull || s.Value.Trim() == "") return;

            String str = s.Value + CultureInfo.TextInfo.ListSeparator;
            String name = "";
            String type = "";
            Int32 startName = 0;
            Int32 startType = 0;
            Int32 startValue = 0;
            Boolean skip = false;
            Boolean next = false;
            Char? quotedChar = null;

            for (Int32 i = 0; i < str.Length; i++)
            {
                if (skip) { skip = false; continue; };
                if (quotedChar != null && str[i] != quotedChar) continue;

                if (str.Length < i + CultureInfo.TextInfo.ListSeparator.Length || str.Substring(i, CultureInfo.TextInfo.ListSeparator.Length) != CultureInfo.TextInfo.ListSeparator)
                switch (str[i])
                {
                    case ':':
                        if (startValue == 0 && startType == 0)
                        {
                            name = str.Substring(startName, i - startName);
                            startType = i + 1;
                        }
                        next = true;
                        break;

                    case '=':
                        if (startValue == 0)
                        {
                            if (startType > 0)
                            {
                                type = str.Substring(startType, i - startType);
                                name = str.Substring(startName, startType - startName - 1);
                            }
                            else
                                name = str.Substring(startName, i - startName);
                            startValue = i + 1;
                        }
                        next = true;
                        break;

                    case '[':
                        if (startValue == 0)
                        {
                            if (quotedChar == null) quotedChar = ']';
                            else quotedChar = null;
                        }
                        next = true;
                        break;

                    case ']':
                        if (startValue == 0)
                        {
                            if (i < str.Length - 1 && str[i + 1] == ']') skip = true;
                            else quotedChar = null;
                        }
                        next = true;
                        break;

                    case '"':
                        if (startValue != 0)
                        {
                            if (quotedChar == null) quotedChar = '"';
                            else if (i < str.Length - 1 && str[i + 1] == '"') skip = true;
                            else quotedChar = null;
                        }
                        next = true;
                        break;

                    default: next = true;
                        break;
                }

                if (next)
                    next = false;
                else
                if (startValue > 0)
                    try
                    {
                        // Парсим значение
                        String value = str.Substring(startValue, i - startValue);
                        SqlDbType dbType = TypeFromString(type);
                        if (IsQuoteType(dbType)) value = UnQuoteValue(value);

                        // Добавляем параметр
                        AddParam(DecodeName(name), ValueFromString(value, dbType, ValueDbStyle.Text));
                        name = "";
                        type = "";
                        startName = i + 1;
                        startType = 0;
                        startValue = 0;
                    }
                    catch (Exception E)
                    {
                        throw new Exception(String.Format("Неверно задано значение TParams_EMPTY '{0}': {1}", s, E.Message));
                    }
                else
                    if (quotedChar == null && str.Substring(startName).Trim() != "")
                        throw new Exception(String.Format("Неверно задано значение TParams_EMPTY '{0}': отсутствует значение", s));
            }
            if (quotedChar != null)
                throw new Exception(String.Format("Неверно задано значение TParams_EMPTY '{0}': отсутствует '{1}'", s, quotedChar));
        }

        /// <summary>
        /// Проверяет имя параметра на корректность
        /// </summary>
        /// <param name="name">Имя параметра</param>
        /// <param name="preparedParams">Признак режима подготовки параметров</param>
        private void CheckName(SqlString name, Boolean preparedParams)
        {
            if (FIgnoreCheckName) return;
            if (name == null || name.IsNull || name.Value.ToString() == "")
                throw new Exception("Имя параметра не может быть пустым");
            if (!preparedParams && name.Value[0] == '=')
                throw new Exception(String.Format("Неверное имя параметра '{0}', параметр начинающийся с '=' может быть задан(изменен) только в функции подготовки параметров", name.Value));
            if (preparedParams && name.Value[0] != '=')
                throw new Exception(String.Format("Неверное имя параметра '{0}' в функции подготовки параметров, должно начинаться с '='", name));
            if (name.Value.Contains(";"))
                throw new Exception(String.Format("Неверное имя параметра '{0}', имя содержит знак разделителя ','", name));
        }

        /// <summary>
        /// Возвращает значение параметра
        /// </summary>
        /// <param name="name">Имя параметра</param>
        /// <returns>Значение параметра</returns>
        public Object AsVariant(SqlString name)
        {
            if (!FData.ContainsKey(name.Value)) return null;
            return FData[name.Value];
        }

        /// <summary>
        /// Возвращает значение параметра типа Bit
        /// </summary>
        /// <param name="name">Имя параметра</param>
        /// <returns>Значение параметра типа Bit</returns>
        public SqlBoolean AsBit(SqlString name)
        {
            if (!FData.ContainsKey(name.Value)) return SqlBoolean.Null;
            Object value = FData[name.Value];
            try
            {
                if (value is SqlBoolean) return (SqlBoolean)value;
                else return SqlBoolean.Parse(ValueToString(value, ValueDbStyle.Text));
            }
            catch
            {
                throw new Exception(String.Format("Не удалось сконвертировать значение '{0}' параметра '{1}' в тип Bit", ValueToString(value, ValueDbStyle.Text), name.Value));
            }
        }

        /// <summary>
        /// Возвращает значение параметра типа VarChar
        /// </summary>
        /// <param name="name">Имя параметра</param>
        /// <returns>Значение параметра типа VarChar</returns>
        public SqlString AsNVarChar(SqlString name)
        {
            if (!FData.ContainsKey(name.Value)) return SqlString.Null;
            Object value = FData[name.Value];
            if (value is SqlString) return (SqlString)value;
            else return (SqlString)ValueToString(value, ValueDbStyle.Text);
        }
        //public SqlString AsVarChar(SqlString name)
        //{
        //  return AsNVarChar(name);
        //}

        /// <summary>
        /// Возвращает значение параметра типа Time
        /// </summary>
        /// <param name="name">Имя параметра</param>
        /// <returns>Значение параметра типа Date</returns>
        public TimeSpan? AsTime(SqlString name)
        {
            if (!FData.ContainsKey(name.Value)) return null;
            Object value = FData[name.Value];
            try
            {
                if (value is TimeSpan) return (TimeSpan)value;
                if (value is DateTime) return ((DateTime)value).TimeOfDay;
                if (value is SqlDateTime) return ((DateTime)(SqlDateTime)value).TimeOfDay;
                else return TimeSpan.Parse(ValueToString(value, ValueDbStyle.Text));
            }
            catch
            {
                throw new Exception(String.Format("Не удалось сконвертировать значение '{0}' параметра '{1}' в тип Time", ValueToString(value, ValueDbStyle.Text), name.Value));
            }
        }

        /// <summary>
        /// Возвращает значение параметра типа Date
        /// </summary>
        /// <param name="name">Имя параметра</param>
        /// <returns>Значение параметра типа Date</returns>
        public DateTime? AsDate(SqlString name)
        {
            if (!FData.ContainsKey(name.Value)) return null;
            Object value = FData[name.Value];
            try
            {
                if (value is DateTime) return ((DateTime)value).Date;
                else return (DateTime.Parse(ValueToString(value, ValueDbStyle.Text), CultureInfo)).Date;
            }
            catch
            {
                throw new Exception(String.Format("Не удалось сконвертировать значение '{0}' параметра '{1}' в тип Date", ValueToString(value, ValueDbStyle.Text), name.Value));
            }
        }

        /// <summary>
        /// Возвращает значение параметра типа DateTime
        /// </summary>
        /// <param name="name">Имя параметра</param>
        /// <returns>Значение параметра типа DateTime</returns>
        public SqlDateTime AsDateTime(SqlString name)
        {
            if (!FData.ContainsKey(name.Value)) return SqlDateTime.Null;
            Object value = FData[name.Value];
            try
            {
                if (value is SqlDateTime) return (SqlDateTime)value;
                else return (SqlDateTime)DateTime.Parse(ValueToString(value, ValueDbStyle.Text), CultureInfo);
            }
            catch
            {
                throw new Exception(String.Format("Не удалось сконвертировать значение '{0}' параметра '{1}' в тип DateTime", ValueToString(value, ValueDbStyle.Text), name.Value));
            }
        }

        public DateTime? AsDateTime2(SqlString name)
        {
          if (!FData.ContainsKey(name.Value)) return null;
          Object value = FData[name.Value];
          try
          {
            if (value is DateTime) return (DateTime)value;
            else return DateTime.Parse(ValueToString(value, ValueDbStyle.Text), CultureInfo);
          }
          catch
          {
            throw new Exception(String.Format("Не удалось сконвертировать значение '{0}' параметра '{1}' в тип DateTime2", ValueToString(value, ValueDbStyle.Text), name.Value));
          }
        }

        public DateTimeOffset? AsDateTimeOffset(SqlString name)
        {
          if (!FData.ContainsKey(name.Value)) return null;
          Object value = FData[name.Value];
          try
          {
            if (value is DateTimeOffset) return (DateTimeOffset)value;
            else return DateTimeOffset.Parse(ValueToString(value, ValueDbStyle.Text), CultureInfo);
          }
          catch
          {
            throw new Exception(String.Format("Не удалось сконвертировать значение '{0}' параметра '{1}' в тип DateTimeOffset", ValueToString(value, ValueDbStyle.Text), name.Value));
          }
        }


        /// <summary>
        /// Возвращает значение параметра типа TinyInt
        /// </summary>
        /// <param name="name">Имя параметра</param>
        /// <returns>Значение параметра типа TinyInt</returns>
        public SqlByte AsTinyInt(SqlString name)
        {
            if (!FData.ContainsKey(name.Value)) return SqlByte.Null;
            Object value = FData[name.Value];
            try
            {
                if (value is SqlByte) return (SqlByte)value;
                if (value is SqlInt16) return ((SqlInt16)value).ToSqlByte();
                if (value is SqlInt32) return ((SqlInt32)value).ToSqlByte();
                if (value is SqlInt64) return ((SqlInt64)value).ToSqlByte();
                if (value is SqlBoolean) return ((SqlBoolean)value).ToSqlByte();
                if (value is SqlDecimal) return ((SqlDecimal)value).ToSqlByte();
                if (value is SqlDouble) return ((SqlDouble)value).ToSqlByte();
                if (value is SqlMoney) return ((SqlMoney)value).ToSqlByte();
                if (value is SqlSingle) return ((SqlSingle)value).ToSqlByte();
                else return (SqlByte)Byte.Parse(ValueToString(value, ValueDbStyle.Text), CultureInfo);
            }
            catch
            {
                throw new Exception(String.Format("Не удалось сконвертировать значение '{0}' параметра '{1}' в тип TinyInt", ValueToString(value, ValueDbStyle.Text), name.Value));
            }
        }

        /// <summary>
        /// Возвращает значение параметра типа SmallInt
        /// </summary>
        /// <param name="name">Имя параметра</param>
        /// <returns>Значение параметра типа SmallInt</returns>
        public SqlInt16 AsSmallInt(SqlString name)
        {
            if (!FData.ContainsKey(name.Value)) return SqlInt16.Null;
            Object value = FData[name.Value];
            try
            {
                if (value is SqlInt16) return (SqlInt16)value;
                if (value is SqlByte) return ((SqlByte)value).ToSqlInt16();
                if (value is SqlInt32) return ((SqlInt32)value).ToSqlInt16();
                if (value is SqlInt64) return ((SqlInt64)value).ToSqlInt16();
                if (value is SqlBoolean) return ((SqlBoolean)value).ToSqlInt16();
                if (value is SqlDecimal) return ((SqlDecimal)value).ToSqlInt16();
                if (value is SqlDouble) return ((SqlDouble)value).ToSqlInt16();
                if (value is SqlMoney) return ((SqlMoney)value).ToSqlInt16();
                if (value is SqlSingle) return ((SqlSingle)value).ToSqlInt16();
                else return (SqlInt16)Int16.Parse(ValueToString(value, ValueDbStyle.Text), CultureInfo);
            }
            catch
            {
                throw new Exception(String.Format("Не удалось сконвертировать значение '{0}' параметра '{1}' в тип SmallInt", ValueToString(value, ValueDbStyle.Text), name.Value));
            }
        }

        /// <summary>
        /// Возвращает значение параметра типа Int32
        /// </summary>
        /// <param name="name">Имя параметра</param>
        /// <returns>Значение параметра типа Int32</returns>
        public SqlInt32 AsInt(SqlString name)
        {
            if (!FData.ContainsKey(name.Value)) return SqlInt32.Null;
            Object value = FData[name.Value];
            try
            {
                if (value is SqlInt32) return (SqlInt32)value;
                if (value is SqlByte) return ((SqlByte)value).ToSqlInt32();
                if (value is SqlInt16) return ((SqlInt16)value).ToSqlInt32();
                if (value is SqlInt64) return ((SqlInt64)value).ToSqlInt32();
                if (value is SqlBoolean) return ((SqlBoolean)value).ToSqlInt32();
                if (value is SqlDecimal) return ((SqlDecimal)value).ToSqlInt32();
                if (value is SqlDouble) return ((SqlDouble)value).ToSqlInt32();
                if (value is SqlMoney) return ((SqlMoney)value).ToSqlInt32();
                if (value is SqlSingle) return ((SqlSingle)value).ToSqlInt32();
                else return (SqlInt32)Int32.Parse(ValueToString(value, ValueDbStyle.Text), CultureInfo);
            }
            catch
            {
                throw new Exception(String.Format("Не удалось сконвертировать значение '{0}' параметра '{1}' в тип Int32", ValueToString(value, ValueDbStyle.Text), name.Value));
            }
        }

        /// <summary>
        /// Возвращает значение параметра типа BigInt
        /// </summary>
        /// <param name="name">Имя параметра</param>
        /// <returns>Значение параметра типа BigInt</returns>
        public SqlInt64 AsBigInt(SqlString name)
        {
            if (!FData.ContainsKey(name.Value)) return SqlInt64.Null;
            Object value = FData[name.Value];
            try
            {
                if (value is SqlInt64) return (SqlInt64)value;
                if (value is SqlByte) return ((SqlByte)value).ToSqlInt64();
                if (value is SqlInt16) return ((SqlInt16)value).ToSqlInt64();
                if (value is SqlInt32) return ((SqlInt32)value).ToSqlInt64();
                if (value is SqlBoolean) return ((SqlBoolean)value).ToSqlInt64();
                if (value is SqlDecimal) return ((SqlDecimal)value).ToSqlInt64();
                if (value is SqlDouble) return ((SqlDouble)value).ToSqlInt64();
                if (value is SqlMoney) return ((SqlMoney)value).ToSqlInt64();
                if (value is SqlSingle) return ((SqlSingle)value).ToSqlInt64();
                else return (SqlInt64)Int64.Parse(ValueToString(value, ValueDbStyle.Text), CultureInfo);
            }
            catch
            {
                throw new Exception(String.Format("Не удалось сконвертировать значение '{0}' параметра '{1}' в тип BigInt", ValueToString(value, ValueDbStyle.Text), name.Value));
            }
        }


        /// <summary>
        /// Возвращает значение параметра типа Decimal
        /// </summary>
        /// <param name="name">Имя параметра</param>
        /// <returns>Значение параметра типа Decimal</returns>
        //public SqlDecimal AsNumeric(SqlString name)
        //{
        //    if (!FData.ContainsKey(name.Value)) return SqlDecimal.Null;
        //    Object value = FData[name.Value];
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
        public SqlXml AsXml(SqlString name)
        {
            if (!FData.ContainsKey(name.Value)) return SqlXml.Null;
            Object value = FData[name.Value];
            try
            {
                if (value is SqlXml) return (SqlXml)value;
                else if (value is SqlString)
                {
                    XmlReader r = XmlReader.Create(new System.IO.StringReader((String)(SqlString)value));
                    return new SqlXml(r);
                }
                else
                    throw new Exception();
            }
            catch
            {
                throw new Exception(String.Format("Не удалось сконвертировать значение '{0}' параметра '{1}' в тип Xml", ValueToString(value, ValueDbStyle.Text), name.Value));
            }
        }

        /// <summary>
        /// Возвращает значение параметра типа VarBinary
        /// </summary>
        public SqlByte AsVarBinary(SqlString name)
        {
            if (!FData.ContainsKey(name.Value)) return SqlByte.Null;
            Object value = FData[name.Value];
            try
            {
                if (value is SqlByte) return (SqlByte)value;
                else
                    throw new Exception();
            }
            catch
            {
                throw new Exception(String.Format("Не удалось сконвертировать значение '{0}' параметра '{1}' в тип VarBinary", ValueToString(value, ValueDbStyle.Text), name.Value));
            }
        }
        public SqlByte AsVarBinaryMax(SqlString name)
        {
            if (!FData.ContainsKey(name.Value)) return SqlByte.Null;
            Object value = FData[name.Value];
            try
            {
                if (value is SqlByte) return (SqlByte)value;
                else
                    throw new Exception();
            }
            catch
            {
                throw new Exception(String.Format("Не удалось сконвертировать значение '{0}' параметра '{1}' в тип VarBinary", ValueToString(value, ValueDbStyle.Text), name.Value));
            }
        }

        /// <summary>
        /// Возвращает значение параметра типа TParams_EMPTY
        /// </summary>
        public TParams_EMPTY AsParams(SqlString name)
        {
            if (!FData.ContainsKey(name.Value)) return null;
            Object value = FData[name.Value];
            if (value is TParams_EMPTY) return (TParams_EMPTY)value;
            else if (value is SqlString)
            {
                TParams_EMPTY result = (TParams_EMPTY)System.Activator.CreateInstance(this.GetType());
                result.FromString((String)(SqlString)value);
                return result;
            }
            else if (value is SqlXml)
            {
                TParams_EMPTY result = new TParams_EMPTY();
                result.ReadXml((value as SqlXml).CreateReader());
                return result;
            }
            else
                throw new Exception(String.Format("Не удалось сконвертировать значение '{0}' параметра '{1}' в тип {2}", ValueToString(value, ValueDbStyle.Text), name.Value, this.GetType().Name));
        }

        /// <summary>
        /// Определяет, присутствует ли параметр в списке
        /// </summary>
        /// <param name="name">Имя параметра</param>
        /// <returns>Признак присутствия параметра в списке</returns>
        public bool ExistsParam(SqlString name)
        {
            return (FData.ContainsKey(name.Value));
        }

        /// <summary>
        /// Очищает список параметров
        /// </summary>
        public void Clear()
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
        public string StringNames()
        {
          StringBuilder sb = new StringBuilder();
          foreach (KeyValuePair<String, Object> param in FData)
          {
              if (param.Key[0] != '=') sb.Append((sb.Length == 0 ? "" : ";") + param.Key);
          }
          return sb.ToString();
        }

        public SqlString Names
        {
          get
          {
            return StringNames();
          }
        }
        public string GetNames()
        {
            return StringNames();
        }
        public List<String> ListNames()
        {
          List<String> List = new List<String>();
          foreach (KeyValuePair<String, Object> param in FData)
          {
            if (param.Key[0] != '=')
              List.Add(param.Key);
          }
          return List;
        }

        /// <summary>
        /// Добавляет параметр, если параметр существует в списке, то переопределяет его значение
        /// </summary>
        /// <param name="name">Имя параметра</param>
        /// <param name="value">Значение параметра</param>
        public void AddParam(SqlString name, Object value)
        {
            CheckName(name, FPreparedObjects != null);

            // Удаляем зависимые объекты
            List<Int32> deleted = new List<Int32>();
            if (FPreparedData != null)
            {
                foreach (KeyValuePair<Int32, TPreparedDataItem> prepared in FPreparedData)
                {
                    if (prepared.Value.DependedParams != null
                        && prepared.Value.DependedParams.IndexOf(name.Value.ToUpper()) >= 0
                        // ??? Если не изменилось значение зависимого параметра ??? && !EqualValues(value, FData[name.Value])
                       ) deleted.Add(prepared.Key);
                }
            }
            foreach (Int32 objectId in deleted)
            {
                List<String> CalculatedParams = FPreparedData[objectId].CalculatedParams;
                FPreparedData.Remove(objectId);
                foreach (String paramName in CalculatedParams)
                {
                    if (!IsCalculatedParams(paramName)) FData.Remove(paramName);
                }
            }

            // Добавляем / изменяем / удаляем параметр
            if (FData.ContainsKey(name.Value))
            {
                if (value == null || value is DBNull || (value is INullable && ((INullable)value).IsNull))
                    FData.Remove(name.Value);
                else
                    FData[name.Value] = value;
            }
            else if (!(value == null || value is DBNull || (value is INullable && ((INullable)value).IsNull)))
                FData.Add(name.Value, value);
        }

        /// <summary>
        /// Проверяет пересечение параметров, если общие параметры равны, то истина иначе ложь
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public Boolean Crossing(TParams_EMPTY value)
        {
            // -=MD=-: Я посчитал что правильно вернуть фальс
            // if (IsNull || value.IsNull) return false;

            if (value == null) return true;
            foreach (KeyValuePair<String, Object> param in value.FData)
            {
                if (FData.ContainsKey(param.Key) && !EqualValues(FData[param.Key], param.Value)) return false;
            }
            return true;
        }

        /// <summary>
        /// Возвращает пересечение двух списков параметров
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public TParams_EMPTY CrossParams(TParams_EMPTY value)
        {
            if (value == null)
            {
                Clear();
                return this;
            };
            List<String> keys = new List<String>();
            foreach (KeyValuePair<String, Object> param in value.FData)
            {
                if (FData.ContainsKey(param.Key) && EqualValues(FData[param.Key], param.Value)) keys.Add(param.Key);
            }
            for (Int32 i = FData.Count - 1; i >= 0; i--)
            {
                if (keys.IndexOf(FData.Keys[i]) < 0) FData.RemoveAt(i);
            }
            return this;
        }

        /// <summary>
        /// Объединяет два списка параметров и возвращает результат
        /// </summary>
        public TParams_EMPTY MergeParams(TParams_EMPTY value)
        {
            if (value == null) return this;
            foreach (KeyValuePair<String, Object> param in value.FData)
            {
                AddParam(param.Key, param.Value);
            }
            return this;
        }

        /// <summary>
        /// Удаляет параметр из списка параметров
        /// </summary>
        /// <param name="name"></param>
        public void DeleteParam(SqlString name)
        {
            CheckName(name, false);
            if (FData.ContainsKey(name.Value))
                AddParam(name, DBNull.Value);
        }

        protected bool EqualValues(Object value1, Object value2)
        {
            SqlDbType type1 = GetSqlType(value1);
            SqlDbType type2 = GetSqlType(value2);
            if (type2 == SqlDbType.Binary || type2 == SqlDbType.VarBinary)
            {
                if (type1 != SqlDbType.Binary && type1 != SqlDbType.VarBinary) return false;
                if (((SqlBytes)value1).Length != ((SqlBytes)value2).Length) return false;
                for (Int32 i = 0; i < ((SqlBytes)value2).Length - 1; i++)
                    if (((SqlBytes)value2).Buffer[i] != ((SqlBytes)value1).Buffer[i]) return false;
            }
            else if (type1 != SqlDbType.Binary && type1 != SqlDbType.VarBinary)
            {
                if (ValueToString(value2, ValueDbStyle.Text) != ValueToString(value1, ValueDbStyle.Text)) return false;
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
        public bool Equals(TParams_EMPTY arg)
        {
            if (arg == null) return false;
            if (FData.Count != arg.FData.Count) return false;
            foreach (KeyValuePair<String, Object> param in FData)
            {
                if (!arg.FData.ContainsKey(param.Key)) return false;
                if (!EqualValues(arg.FData[param.Key], param.Value)) return false;
            }
            return true;
        }

        /// <summary>
        /// Возвращет ObjectId и значение расширенного свойства
        /// </summary>
        private Int32 GetExtendedProperty(SqlConnection connection, String objectName, String propName, out String value)
        {
            value = null;
            Int32 objectId = -1;
            // SqlCommand cmd = new SqlCommand(String.Format("SELECT OBJECT_ID('{0}'), (SELECT '[' + OBJECT_SCHEMA_NAME(OBJECT_ID(CAST([value] AS VARCHAR(MAX)))) + '].[' + OBJECT_NAME(OBJECT_ID(CAST([value] AS VARCHAR(MAX)))) + ']' FROM [sys].[fn_listextendedproperty] ('TParams_EMPTY::{1}', 'schema', PARSENAME('{0}', 2), 'FUNCTION', PARSENAME('{0}', 1), default, default))", objectName, propName), connection);
            SqlCommand cmd = new SqlCommand(String.Format("SELECT OBJECT_ID('{0}'), (SELECT CAST([value] AS NVARCHAR(MAX)) FROM [sys].[fn_listextendedproperty] ('TParams_EMPTY::{1}', 'schema', PARSENAME('{0}', 2), 'FUNCTION', PARSENAME('{0}', 1), default, default))", objectName, propName), connection);
            SqlDataReader rdr = cmd.ExecuteReader();
            if (rdr.Read())
            {
                if (rdr.IsDBNull(0))
                    throw new Exception(String.Format("Не найден объект '{0}'", objectName));
                objectId = rdr.GetInt32(0);
                if (!rdr.IsDBNull(1)) value = rdr.GetString(1);
            }
            rdr.Close();
            return objectId;
        }

        /// <summary>
        /// Выполняет функцию принимающую и возвращающую список параметров
        /// </summary>
        private TParams_EMPTY ExecSQLFunction(SqlConnection connection, String funcName)
        {
            try
            {
                TParams_EMPTY result = null;
                SqlCommand cmd = connection.CreateCommand();
                cmd.CommandText = String.Format("SELECT {0}(@Params)", funcName);
                cmd.CommandType = CommandType.Text;
                SqlParameter param = new SqlParameter("@Params", SqlDbType.Udt);
                param.UdtTypeName = "[dbo].[TParams_EMPTY]";
                param.Direction = ParameterDirection.Input;
                param.Value = this;
                cmd.Parameters.Add(param);
                SqlDataReader rdr = cmd.ExecuteReader();
                if (rdr.Read() && !rdr.IsDBNull(0))
                    result = (TParams_EMPTY)rdr.GetSqlValue(0);
                rdr.Close();
                return result;
            }
            catch (Exception E)
            {
                throw E;
            }
        }

        /// <summary>
        /// Подготавливает параметры
        /// </summary>
        /// <param name="name">Имя функции, для которой необходимо подготовить параметры</param>
        public void Prepare(SqlString name)
        {
            SqlConnection connection = new SqlConnection("context connection=true");
            connection.Open();

            // Получем имя функции для подготовки параметров
            String funcName;
            Int32 objectId = GetExtendedProperty(connection, name.Value, "Prepare", out funcName);

            // Если для функции параметры уже подготовленны, то выходим
            if (Prepared(objectId)) return;

            // Добавляем признак подготовленности параметров
            if (FPreparedData == null) FPreparedData = new SortedList<Int32, TPreparedDataItem>();
            TPreparedDataItem prepared = new TPreparedDataItem();
            FPreparedData.Add(objectId, prepared);
            if (funcName == null) return;

            // Добавляем объект в стек подготовки параметров
            if (FPreparedObjects == null) FPreparedObjects = new List<Int32>();
            FPreparedObjects.Add(objectId);

            // Выполняем функцию подготовки параметров
            TParams_EMPTY result = ExecSQLFunction(connection, funcName);

            if (result != null)
            {
                // Добавляем подготовленные объекты и зависимые параметры
                foreach (KeyValuePair<Int32, TPreparedDataItem> resultPrepared in result.FPreparedData)
                {
                    if (FPreparedData.ContainsKey(resultPrepared.Key))
                        FPreparedData[resultPrepared.Key].DependedParams = resultPrepared.Value.DependedParams;
                    else
                        FPreparedData.Add(resultPrepared.Key, resultPrepared.Value);
                }

                // Добавляем новые параметры
                foreach (KeyValuePair<String, Object> param in result.FData)
                {
                    if (!FData.ContainsKey(param.Key))
                    {
                        AddParam(param.Key, param.Value);
                        if (prepared.CalculatedParams == null)
                            prepared.CalculatedParams = new List<String>();
                        prepared.CalculatedParams.Add(param.Key.ToUpper());
                    }
                }
            }

            // Удаляем объект из стека подготовки параметров
            FPreparedObjects.RemoveAt(FPreparedObjects.Count - 1);
            if (FPreparedObjects.Count == 0)
            {
                FPreparedObjects = null;
                FRegisteredDependedParams = null;
            }
        }

        /// <summary>
        /// Убирает подготовку параметров
        /// </summary>
        public void UnPrepare()
        {
            if (FPreparedData == null) return;

            // Удаляем расчитанные параметры
            List<String> deleted = new List<String>();
            foreach (KeyValuePair<String, Object> param in FData)
            {
                if (param.Key[0] == '=')
                {
                    deleted.Add(param.Key);
                }
            }
            foreach (String param in deleted)
            {
                FData.Remove(param);
            }

            // Убираем признак подготовленности параметров
            FPreparedData.Clear();
        }

        /// <summary>
        /// Возвращает признак подготовленности параметров
        /// </summary>
        /// <param name="objectId">ObjectId объекта</param>
        /// <returns>Признак подготовленности параметров</returns>
        public SqlBoolean Prepared(SqlInt32 objectId)
        {
            if (objectId.IsNull) return false;
            if (FPreparedData != null && FPreparedData.ContainsKey((Int32)objectId)) return true;
            return false;
        }

        /// <summary>
        /// Регистрирует зависимые параметры
        /// </summary>
        /// <param name="value">Список параметров, разделенных запятой</param>
        public void RegisterDependedParams(SqlString value)
        {
            // Функция может вызываться только из функции подготовки параметров
            if (FPreparedObjects == null)
                throw new Exception("Метод RegisterDependedParams может использоваться только в функции подготовки параметров");

            // Игнорируем пустые значения
            if (value == null || value.ToString() == "") return;

            // Добавляем параметры во все подготавливаемые объекты
            String[] DependedParams = value.Value.ToUpper().Split(',');
            foreach (String param in DependedParams)
                foreach (Int32 objectId in FPreparedObjects)
                {
                    TPreparedDataItem FData = FPreparedData[objectId];
                    if (FData.DependedParams == null)
                    {
                        FData.DependedParams = new List<String>();
                        FData.DependedParams.Add(param);
                    }
                    else if (FData.DependedParams.IndexOf(param) < 0)
                        FData.DependedParams.Add(param);
                }
        }

        /// <summary>
        /// Определяет создан ли параметр функцией подготовки параметров
        /// </summary>
        private bool IsCalculatedParams(String name)
        {
            if (FPreparedData == null) return false;
            name = name.ToUpper();
            foreach (KeyValuePair<Int32, TPreparedDataItem> prepared in FPreparedData)
            {
                if (prepared.Value.CalculatedParams != null)
                    foreach (String paramName in prepared.Value.CalculatedParams)
                    {
                        if (paramName == name) return true;
                    }
            }
            return false;
        }

        #region Converting Params

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
            if (value is SqlChars)  return SqlDbType.NVarChar; // Char, NChar, VarChar, NVarChar
            if (value is SqlString) return SqlDbType.NVarChar; // Char, NChar, VarChar, NVarChar
            if (value is DateTime && ((DateTime)value) == ((DateTime)value).Date) return SqlDbType.Date; // Date
            if (value is DateTime) return SqlDbType.DateTime2; // DateTime2
            if (value is SqlDateTime) return SqlDbType.DateTime; // DateTime, SmallDateTime
            if (value is TimeSpan) return SqlDbType.Time; // Time
            // if (value is System.DateTimeOffset) return SqlDbType.DateTimeOffset; // DateTimeOffset ??? Не понятно как передать этот тип обратно на сервер ???
            if (value is SqlDecimal) return SqlDbType.Decimal; // Numeric, Decimal
            if (value is SqlDouble) return SqlDbType.Float; // Float
            if (value is SqlMoney) return SqlDbType.Money; // Money
            if (value is SqlSingle) return SqlDbType.Real; // Real
            if (value is SqlGuid) return SqlDbType.UniqueIdentifier; // UniqueIdentifier
            if (value is SqlXml) return SqlDbType.Xml; // UniqueIdentifier
            if (value is TParams_EMPTY) return SqlDbType.Udt; // Udt
            else
                throw new Exception(String.Format("Тип '{0}' не поддерживается текущей версией TParams_EMPTY", value.GetType().Name));
        }

        /// <summary>
        /// Конвертирует тип значения параметра из строки в SqlDbType
        /// </summary>
        /// <param name="type">Тип параметра, заданный строкой</param>
        /// <returns>Тип значения параметра</returns>
        private SqlDbType TypeFromString(String AType)
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
        /// Конвертирует значение параметра из строки
        /// </summary>
        public Object ValueFromString(String value, SqlDbType type, ValueDbStyle style)
        {
            try
            {
                if (style == ValueDbStyle.XML)
                    switch (type)
                    {
                        case SqlDbType.BigInt: return new SqlInt64(XmlConvert.ToInt64(value));                            
                        case SqlDbType.Binary: return new SqlBytes(Convert.FromBase64String(value));
                        case SqlDbType.Bit: return new SqlBoolean(XmlConvert.ToBoolean(value));
                        case SqlDbType.Char: return new SqlString(value);
                        case SqlDbType.Date: return XmlConvert.ToDateTime(value, XmlDateTimeSerializationMode.RoundtripKind).Date;
                        case SqlDbType.DateTime: return new SqlDateTime(XmlConvert.ToDateTime(value, XmlDateTimeSerializationMode.RoundtripKind));
                        // Not support SqlDbType.DateTimeOffset
                        case SqlDbType.DateTime2: return XmlConvert.ToDateTime(value, XmlDateTimeSerializationMode.RoundtripKind);
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
                        case SqlDbType.Time: TimeSpan resultTime; return TimeSpan.TryParse(value, out resultTime) ? resultTime : XmlConvert.ToTimeSpan(value);
                        // Not support SqlDbType.Timestamp
                        case SqlDbType.TinyInt: return new SqlByte(XmlConvert.ToByte(value));
                        case SqlDbType.Udt:
                            {
                                TParams_EMPTY result = (TParams_EMPTY)System.Activator.CreateInstance(this.GetType());
                                XmlReader r = XmlReader.Create(new System.IO.StringReader(value));
                                result.ReadXml(r);
                                return result;
                            }
                        case SqlDbType.UniqueIdentifier: return new SqlGuid(XmlConvert.ToGuid(value));
                        case SqlDbType.VarBinary: return new SqlBytes(Convert.FromBase64String(value));
                        case SqlDbType.VarChar: return new SqlString(value);
                        // Not support SqlDbType.Variant
                        case SqlDbType.Xml:
                            {
                                XmlReader r = XmlReader.Create(new System.IO.StringReader(value));
                                return new SqlXml(r);
                            }
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
                        case SqlDbType.Udt:
                            TParams_EMPTY result = (TParams_EMPTY)System.Activator.CreateInstance(this.GetType());
                            result.FromString(value);
                            return result;
                        case SqlDbType.UniqueIdentifier: return SqlGuid.Parse(value);
                        case SqlDbType.VarBinary: return new SqlBytes(Convert.FromBase64String(value));
                        case SqlDbType.VarChar: return new SqlString(value);
                        // Not support SqlDbType.Variant
                        case SqlDbType.Xml:
                            XmlReader r = XmlReader.Create(new System.IO.StringReader(value));
                            return new SqlXml(r);
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
                        result.Add(attributeName, TypeFromString(attribute.SchemaTypeName.Name));
                    else if (attribute.SchemaType != null && attribute.SchemaType.Content is XmlSchemaSimpleTypeRestriction && ((XmlSchemaSimpleTypeRestriction)attribute.SchemaType.Content).BaseTypeName.Namespace == SqlTypesNamespace)
                    {
                        result.Add(attributeName, TypeFromString(((XmlSchemaSimpleTypeRestriction)attribute.SchemaType.Content).BaseTypeName.Name));
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
                        String paramName = XmlConvert.DecodeName(r.Name);
                        if (types == null)
                            AddParam(paramName, new SqlString(r.Value));
                        else if (types.ContainsKey(paramName))
                            AddParam(paramName, ValueFromString(r.Value, types[paramName], ValueDbStyle.XML));
                        else
                            AddParam(paramName, ValueFromString(r.Value, SqlDbType.VarChar, ValueDbStyle.XML));
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
            foreach (KeyValuePair<string, Object> param in FData)
            {
                if (param.Key[0] == '=' && !IsCalculatedParams(param.Key))
                    throw new Exception(String.Format("Неверное имя параметра '{0}', параметр начинающийся с '=' может быть задан(изменен) только в функции подготовки параметров", param.Key));
            } 
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

            foreach (KeyValuePair<String, Object> param in FData)
            {
                w.WriteStartElement("xsd", "attribute", XmlSchema.Namespace);
                w.WriteAttributeString("name", XmlConvert.EncodeLocalName(param.Key));
                w.WriteAttributeString("type", "sqltypes:" + GetSqlType(param.Value).ToString().ToLower());
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
            foreach (KeyValuePair<String, Object> param in FData)
            {
                w.WriteStartAttribute(XmlConvert.EncodeLocalName(param.Key));
                w.WriteValue(ValueToString(param.Value, ValueDbStyle.XML));
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
                foreach (KeyValuePair<Int32, TPreparedDataItem> param in FPreparedData)
                {
                    w.WriteStartElement("Prepared");
                    w.WriteAttributeString("ObjectId", param.Key.ToString());
                    String result = "";
                    foreach (String str in param.Value.CalculatedParams) result += (result == "" ? "" : ";") + str;
                    if (result != "") w.WriteAttributeString("Calculated", result);
                    result = "";
                    foreach (String str in param.Value.DependedParams) result += (result == "" ? "" : ";") + str;
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
          count = Read7BitEncodedInt(r);
     
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
          count = Read7BitEncodedInt(r);

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

              Int32 count1 = Read7BitEncodedInt(r);
              if (count1 > 0)
              {
                for (Int32 j = 0; j < count1; j++)
                {
                  prepared.DependedParams.Add(r.ReadString());
                }
              }

              count1 = Read7BitEncodedInt(r);
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
          count = Read7BitEncodedInt(r);

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
          count = Read7BitEncodedInt(r);

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
          Write7BitEncodedInt(w, (Int32)FData.Count);

          foreach (KeyValuePair<String, Object> LDataPair in FData)
          {
            w.Write(LDataPair.Key);
            SqlDbType type = GetSqlType(LDataPair.Value);
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

          Write7BitEncodedInt(w, PreparedDataCount);
          if (PreparedDataCount != 0)
          {
            foreach (KeyValuePair<int, TPreparedDataItem> prepared in FPreparedData)
            {
              w.Write(prepared.Key);

              int PreparedValueDependedParamsCount = (prepared.Value.DependedParams == null) ? 0 : prepared.Value.DependedParams.Count;
              Write7BitEncodedInt(w, PreparedValueDependedParamsCount);
              if (PreparedValueDependedParamsCount > 0)
              {
                foreach (String paramName in prepared.Value.DependedParams)
                {
                  w.Write(paramName);
                }
              }

              int PreparedValueCalculatedParamsCount = (prepared.Value.CalculatedParams == null) ? 0 : prepared.Value.CalculatedParams.Count;
              Write7BitEncodedInt(w, PreparedValueCalculatedParamsCount);
              if (PreparedValueCalculatedParamsCount > 0)
              {
                foreach (String paramName in prepared.Value.CalculatedParams)
                {
                  w.Write(paramName);
                }
              }
            }
          }

          if (PreparedObjectsCount == 0 && RegisteredDependedParamsCount == 0) return;
          // Информация об объектах, для которых происходит подготовка параметров
          Write7BitEncodedInt(w, PreparedObjectsCount);
          if (PreparedObjectsCount != 0)
          {
            foreach (int objectId in FPreparedObjects)
            {
              w.Write(objectId);
            }
          }

          if (RegisteredDependedParamsCount == 0) return;
          // Зависимые параметры
          Write7BitEncodedInt(w, RegisteredDependedParamsCount);
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

        // ODBC, ISO
        private const String TextDatePattern            = "yyyy-MM-dd";
        private const String TextSmallDateTimePattern   = "yyyy-MM-dd HH:mm";
        private const String TextDateTimePattern        = "yyyy-MM-dd HH:mm:ss.FFFFFFF";
        private const String TextDateTimeOffsetPattern  = "yyyy-MM-dd HH:mm:ss.FFFFFFF zzz";

        private const String SQLDatePattern             = "yyyyMMdd";
        private const String SQLSmallDateTimePattern    = "yyyyMMdd HH:mm";
        private const String SQLDateTimePattern         = "yyyyMMdd HH:mm:ss.FFFFFFF";
        private const String SQLDateTimeOffsetPattern   = "yyyyMMdd HH:mm:ss.FFFFFFFzzz";

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

        private CultureInfo cultureInfo;
        public CultureInfo CultureInfo
        {
            get
            {
                if (cultureInfo == null)
                {
                    cultureInfo = new CultureInfo("en-US", false);
                    cultureInfo.DateTimeFormat.ShortDatePattern = "yyyy-MM-dd";
                    cultureInfo.DateTimeFormat.LongTimePattern = "HH:mm:ss.FFFFFFF";
                    cultureInfo.NumberFormat.NumberDecimalSeparator = ".";
                    cultureInfo.TextInfo.ListSeparator = ";";
                }
                return cultureInfo;
            }
        }

        public const string SqlTypesNamespace = "http://schemas.microsoft.com/sqlserver/2004/sqltypes";
        public const string SqlTypesNamespaceLocation = "http://schemas.microsoft.com/sqlserver/2004/sqltypes/sqltypes.xsd";

        private SortedList<String, Object>          FData                      = new SortedList<String, Object>(StringComparer.OrdinalIgnoreCase);
        private SortedList<Int32, TPreparedDataItem>FPreparedData;
        private List<Int32>                         FPreparedObjects;
        private List<String>                        FRegisteredDependedParams;
        //private SqlConnection                       FContextConnection;

        private bool FIgnoreCheckName = false;
    }
}

namespace UDT_EMPTY
{
    [XmlRoot("Params")]
    [SqlUserDefinedType(Microsoft.SqlServer.Server.Format.UserDefined, IsByteOrdered = true, MaxByteSize = -1, Name = "TParams_EMPTY")]
    // ??? [SqlFacetAttribute(IsNullable = false)]

    [Serializable]
		public class TParams_EMPTY: INT_EMPTY.TParams_EMPTY, INullable//, IBinarySerialize, IXmlSerializable
    {

        /// <summary>
        /// Создает новый объект TParams_EMPTY
        /// </summary>
        public static TParams_EMPTY New()
        {
            TParams_EMPTY result = new TParams_EMPTY();
            result.Init();
            return result;
        }

        /// <summary>
        /// Возвращает тип параметра
        /// </summary>
        //public static SqlString TypeOf(Object value)
        //{
        //    return Sql.INT_EMPTY.TParams_EMPTY.GetSqlType(value).ToString();
        //}
        public SqlString GetSqlType(SqlString value)
        {
          return INT_EMPTY.TParams_EMPTY.GetSqlType(base.AsVariant(value.Value)).ToString();
        }
        public object Eval(SqlString value)
        {
          return null;
        }


        /// <summary>
        /// Проверка на Null
        /// </summary>
        bool INullable.IsNull
        {
            get
            {
                return base.IsNull;
            }
        }

        /// <summary>
        /// Возвращает Null значение
        /// </summary>
        public static TParams_EMPTY Null
        {
            get
            {
                TParams_EMPTY result = new TParams_EMPTY();
                return result;
            }
        }

        /// <summary>
        /// Преобразует данные в строку
        /// </summary>
        public override String ToString()
        {
            return base.ToString();
        }
		
				[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Target")]
				public new void Write(System.IO.BinaryWriter Target)
				{
						base.Write(Target);
				}

				[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Src")]
				public new void Read(System.IO.BinaryReader Src)
				{
						base.Read(Src);
				}

				[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Src")]
				public new void ReadXml(XmlReader Src)
				{
						base.ReadXml(Src);
				}

				[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Target")]
				public new void WriteXml(XmlWriter Target)
				{
						base.WriteXml(Target);
				}
		

        /// <summary>
        /// Преобразует строку в данные
        /// </summary>
				[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Src"), SqlMethod(Name = "Parse", OnNullCall = false)]
        public static TParams_EMPTY Parse(SqlString Src)
        {
            if (Src.IsNull) return Null;
            TParams_EMPTY result = new TParams_EMPTY();
            result.FromString(Src.Value);
            return result;
        }

        /// <summary>
        /// Очищает список параметров
        /// </summary>
        [SqlMethod(Name = "Clear", IsMutator = true, OnNullCall = false)]
        public new void Clear()
        {
            base.Clear();
        }

        /// <summary>
        /// Определяет, существует ли параметр
        /// </summary>
        [SqlMethod(Name = "Exists", OnNullCall = true)]
        public bool Exists(SqlString name)
        {
            return base.ExistsParam(name);
        }
        [SqlMethod(Name = "ExistsParam", OnNullCall = true)]
        public new bool ExistsParam(SqlString name)
        {
            return base.ExistsParam(name);
        }

        /// <summary>
        /// Возвращает наименование параметров строкой
        /// </summary>
        public new SqlString Names
        {
          get
          {
            return base.GetNames();
          }
        }
        [SqlMethod(Name = "GetNames", IsDeterministic = true, IsPrecise = true, OnNullCall = false)]
        public new SqlString GetNames()
        {
          return base.GetNames();
        }

        /// <summary>
        /// Возвращает наименования параметров табличным способом
        /// </summary>
        //[SqlMethod(FillRowMethodName = "ListNamesRow", DataAccess = DataAccessKind.None,  TableDefinition = "name nvarchar(512)",  IsDeterministic = true)]
        //public new IEnumerable ListNames()
        //{
        //  return new List<String>(); //base.ListNames();
        //}
        //public static void ListNamesRow(object row, out SqlString name)
        //{
        //  name = (SqlString)row.ToString();
        //}

        /// <summary>
        /// Добавляет параметр, если существует переопределяет его значение
        /// </summary>
        [SqlMethod(Name = "AddParam", IsMutator = true, OnNullCall = true)]
        public new void AddParam(SqlString name, Object value)
        {
            base.AddParam(name, value);
        }

        [SqlMethod(Name = "NewParam", IsMutator = false, OnNullCall = true)]
        public TParams_EMPTY NewParam(SqlString name, Object value)
        {
          base.AddParam(name, value);
          return this;
        }

        [SqlMethod(Name = "Add", IsMutator = false, OnNullCall = true)]
        public TParams_EMPTY Add(SqlString name, Object value)
        {
          base.AddParam(name, value);
          return this;
        }

        /// <summary>
        /// Добавляет параметр типа varchar(max), если существует переопределяет его значение
        /// </summary>
        //[SqlMethod(Name = "AddVarCharMax", IsMutator = true, OnNullCall = true)]
        //public void AddVarCharMax(SqlString name, SqlString value)
        //{
        //    base.AddParam(name, value);
        //}
        [SqlMethod(Name = "AddNVarCharMax", IsMutator = true, OnNullCall = true)]
        public void AddNVarCharMax(SqlString name, SqlString value)
        {
            base.AddParam(name, value);
        }

        /// <summary>
        /// Добавляет параметр типа VarChar(max), если существует переопределяет его значение
        /// </summary>
        [SqlMethod(Name = "AddXml", IsMutator = true, OnNullCall = true)]
        public void AddXml(SqlString name, SqlXml value)
        {
            base.AddParam(name, value);
        }

        /// <summary>
        /// Добавляет параметр типа VarBinary, если существует переопределяет его значение
        /// </summary>
        [SqlMethod(Name = "AddVarBinary", IsMutator = true, OnNullCall = true)]
        public void AddVarBinary(SqlString name, SqlBinary value)
        {
            base.AddParam(name, value);
        }
        [SqlMethod(Name = "AddVarBinaryMax", IsMutator = true, OnNullCall = true)]
        public void AddVarBinaryMax(SqlString name, SqlBytes value)
        {
            base.AddParam(name, value);
        }

        /// <summary>
        /// Добавляет параметр типа UDT, если существует переопределяет его значение
        /// </summary>
        [SqlMethod(Name = "AddParams", IsMutator = true, OnNullCall = true)]
        public void AddParams(SqlString name, TParams_EMPTY value)
        {
            base.AddParam(name, value);
        }

        [SqlMethod(Name = "AddTDictionaryStringInt32", IsMutator = true, OnNullCall = true)]
        public void AddTDictionaryStringInt32(SqlString name, Object value)
        {
          base.AddParam(name, value);
        }
        [SqlMethod(Name = "AddTDictionaryStringString", IsMutator = true, OnNullCall = true)]
        public void AddTDictionaryStringString(SqlString name, Object value)
        {
          base.AddParam(name, value);
        }
        [SqlMethod(Name = "AddTDictionaryInt32String", IsMutator = true, OnNullCall = true)]
        public void AddTDictionaryInt32String(SqlString name, Object value)
        {
          base.AddParam(name, value);
        }

        [SqlMethod(Name = "Evaluate", IsMutator = false, OnNullCall = false, DataAccess = DataAccessKind.Read)]
        public static object Evaluate(TParams_EMPTY AParams, String AExpression)
        {
          return null;
        }

        [SqlMethod(Name = "EvaluateBoolean", IsMutator = false, OnNullCall = false, DataAccess = DataAccessKind.Read)]
        public static Boolean EvaluateBoolean(TParams_EMPTY AParams, String AExpression, Boolean ADefault = false)
        {
          return ADefault;
        }


        /// <summary>
        /// Добавляет параметр, если существует переопределяет его значение и возвращает подготовленные параметры
        /// </summary>
        //[SqlMethod(Name = "PrepareParam", IsMutator = false, OnNullCall = true, DataAccess = DataAccessKind.Read, SystemDataAccess = SystemDataAccessKind.Read)]
        //public TParams_EMPTY PrepareParam(SqlString name, Object value, SqlString funcName)
        //{
        //    AddParam(name, value);
        //    Prepare(funcName);
        //    return this;
        //}

        /// <summary>
        /// Проверяет пересечение параметров, если общие параметры равны, то истина иначе ложь
        /// </summary>
				[
          System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters"), 
          SqlMethod(Name = "Crossing", IsMutator = false, OnNullCall = true)
        ]
        public Boolean Crossing(TParams_EMPTY value)
        {
          return false;
        }

        /// <summary>
        /// Возвращает пересечение двух списков параметров
        /// </summary>
				[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters"), SqlMethod(Name = "CrossParams", IsMutator = false, OnNullCall = false)]
        public TParams_EMPTY CrossParams(TParams_EMPTY value)
        {
          return null;
        }

        /// <summary>
        /// Объединяет два списка параметров
        /// </summary>
				[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters"), SqlMethod(Name = "MergeParams", IsMutator = true, OnNullCall = true)]
        public TParams_EMPTY MergeParams(TParams_EMPTY value)
        {
          return null;
        }
				[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters"), SqlMethod(Name = "Merge", IsMutator = false, OnNullCall = true)]
        public static TParams_EMPTY Merge(TParams_EMPTY Value1, TParams_EMPTY Value2)
        {
          return null;
        }

        /// <summary>
        /// Удаляет параметр
        /// </summary>
        [SqlMethod(Name = "DeleteParam", IsMutator = true, OnNullCall = false)]
        public new void DeleteParam(SqlString name)
        {
            base.DeleteParam(name);
        }

        /// <summary>
        /// Сравнивает параметры
        /// </summary>
				[
          System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters"), 
          SqlMethod
          (
            Name = "Equals", 
            OnNullCall = true
          )
        ]
        public bool Equals(TParams_EMPTY args)
        {
          return false;
        }

				[SqlMethod(Name = "Equal", OnNullCall = true)]
        public static bool Equal(TParams_EMPTY args1, TParams_EMPTY args2)
        {
          return false;
        }

        /// <summary>
        /// Возвращает значение параметра
        /// </summary>
        [SqlMethod(Name = "AsVariant", IsDeterministic = true, IsPrecise = true, OnNullCall = false)]
        public new Object AsVariant(SqlString name)
        {
            return null;
        }

        /// <summary>
        /// Возвращает значение параметра типа VarChar
        /// </summary>
        [SqlMethod(Name = "AsBit", IsDeterministic = true, IsPrecise = true, OnNullCall = false)]
        public new SqlBoolean AsBit(SqlString name)
        {
            return false;
        }

        /// <summary>
        /// Возвращает значение параметра типа VarChar
        /// </summary>
        //[SqlMethod(Name = "AsVarChar", IsDeterministic = true, IsPrecise = true, OnNullCall = false)]
        //public new SqlString AsVarChar(SqlString name)
        //{
        //    return base.AsNVarChar(name);
        //}
        [SqlMethod(Name = "AsNVarChar", IsDeterministic = true, IsPrecise = true, OnNullCall = false)]
        public new SqlString AsNVarChar(SqlString name)
        {
            return SqlString.Null;
        }

        /// <summary>
        /// Возвращает значение параметра в формате SQL
        /// </summary>
        [SqlMethod(Name = "AsSQLString", IsDeterministic = true, IsPrecise = true, OnNullCall = false)]
        public SqlString AsSQLString(String Name)
        {
          return SqlString.Null;
        }

        /// <summary>
        /// Возвращает значение параметра типа VarChar
        /// </summary>
        [SqlMethod(Name = "AsSQLText", IsDeterministic = true, IsPrecise = true, OnNullCall = false)]
        public SqlString AsSQLText(String Name)
        {
          return base.AsNVarChar(Name);
        }

        /// <summary>
        /// Возвращает значение параметра типа Date
        /// </summary>
        [SqlMethod(Name = "AsDate", IsDeterministic = true, IsPrecise = true, OnNullCall = false)]
        public new DateTime? AsDate(SqlString name)
        {
            return base.AsDate(name);
        }

        /// <summary>
        /// Возвращает значение параметра типа Time
        /// </summary>
        [SqlMethod(Name = "AsTime", IsDeterministic = true, IsPrecise = true, OnNullCall = false)]
        public new TimeSpan? AsTime(SqlString name)
        {
            return base.AsTime(name);
        }

        /// <summary>
        /// Возвращает значение параметра типа DateTime
        /// </summary>
        [SqlMethod(Name = "AsDateTime", IsDeterministic = true, IsPrecise = true, OnNullCall = false)]
        public new SqlDateTime AsDateTime(SqlString name)
        {
            return base.AsDateTime(name);
        }

        /// <summary>
        /// Возвращает значение параметра типа Date
        /// </summary>
        [SqlMethod(Name = "AsDateTime2", IsDeterministic = true, IsPrecise = true, OnNullCall = false)]
        public new DateTime? AsDateTime2(SqlString name)
        {
          return base.AsDateTime2(name);
        }

        /// <summary>
        /// Возвращает значение параметра типа Date
        /// </summary>
        [SqlMethod(Name = "AsDateTimeOffset", IsDeterministic = true, IsPrecise = true, OnNullCall = false)]
        public new DateTimeOffset? AsDateTimeOffset(SqlString name)
        {
          return base.AsDateTimeOffset(name);
        }

        /// <summary>
        /// Возвращает значение параметра типа TinyInt
        /// </summary>
        [SqlMethod(Name = "AsTinyInt", IsDeterministic = true, IsPrecise = true, OnNullCall = false)]
        public new SqlByte AsTinyInt(SqlString name)
        {
            return base.AsTinyInt(name);
        }

        /// <summary>
        /// Возвращает значение параметра типа SmallInt
        /// </summary>
        [SqlMethod(Name = "AsSmallInt", IsDeterministic = true, IsPrecise = true, OnNullCall = false)]
        public new SqlInt16 AsSmallInt(SqlString name)
        {
            return base.AsSmallInt(name);
        }

        /// <summary>
        /// Возвращает значение параметра типа Int
        /// </summary>
        [SqlMethod(Name = "AsInt", IsDeterministic = true, IsPrecise = true, OnNullCall = false)]
        public new SqlInt32 AsInt(SqlString name)
        {
            return base.AsInt(name);
        }

        /// <summary>
        /// Возвращает значение параметра типа BigInt
        /// </summary>
        [SqlMethod(Name = "AsBigInt", IsDeterministic = true, IsPrecise = true, OnNullCall = false)]
        public new SqlInt64 AsBigInt(SqlString name)
        {
            return base.AsBigInt(name);
        }

        /// <summary>
        /// Возвращает значение параметра типа Decimal
        /// </summary>
        //[SqlMethod(Name = "AsNumeric", IsDeterministic = true, IsPrecise = true, OnNullCall = false)]
        //public new SqlDecimal AsNumeric(SqlString name)
        //{
        //    return base.AsNumeric(name);
        //}

        /// <summary>
        /// Возвращает значение параметра типа TParams_EMPTY
        /// </summary>
        [SqlMethod(Name = "AsParams", IsDeterministic = true, IsPrecise = true, OnNullCall = false)]
        public new TParams_EMPTY AsParams(SqlString name)
        {
            return (TParams_EMPTY)base.AsParams(name);            
        }

        /// <summary>
        /// Возвращает значение параметра типа VarChar
        /// </summary>
        //[SqlMethod(Name = "AsVarCharMax", IsDeterministic = true, IsPrecise = true, OnNullCall = false)]
        //public SqlChars AsVarCharMax(SqlString name)
        //{
        //    return new SqlChars(base.AsNVarChar(name));
        //}
        [SqlMethod(Name = "AsNVarCharMax", IsDeterministic = true, IsPrecise = true, OnNullCall = false)]
        public SqlChars AsNVarCharMax(SqlString name)
        {
            return new SqlChars(base.AsNVarChar(name));
        }

        /// <summary>
        /// Возвращает значение параметра типа Xml
        /// </summary>
        [SqlMethod(Name = "AsXml", IsDeterministic = true, IsPrecise = true, OnNullCall = false)]
        public new SqlXml AsXml(SqlString name)
        {
          return base.AsXml(name);
        }

        /// <summary>
        /// Возвращает значение параметра типа VarBinary
        /// </summary>
        [SqlMethod(Name = "AsVarBinary", IsDeterministic = true, IsPrecise = true, OnNullCall = false)]
        public new SqlByte AsVarBinary(SqlString name)
        {
            return base.AsVarBinary(name);
        }

        /// <summary>
        /// Подготавливает параметры
        /// </summary>
        //[SqlMethod(Name = "Prepare", IsMutator = true, OnNullCall = false, DataAccess = DataAccessKind.Read, SystemDataAccess = SystemDataAccessKind.Read)]
        //public new void Prepare(SqlString name)
        //{
        //  base.Prepare(name);
        //}

        /// <summary>
        /// Подготавливает параметры
        /// </summary>
        [SqlMethod(Name = "PrepareFunction", IsMutator = true, OnNullCall = false, DataAccess = DataAccessKind.Read, SystemDataAccess = SystemDataAccessKind.Read)]
        public void PrepareFunction(SqlString name)
        {
          base.Prepare(name);
        }

        [SqlMethod(Name = "Prepare", IsMutator = false, OnNullCall = false, DataAccess = DataAccessKind.Read, SystemDataAccess = SystemDataAccessKind.Read)]
        public new TParams_EMPTY Prepare(SqlString name)
        {
          base.Prepare(name);
          return this;
        }

        /// <summary>
        /// Убирает подготовку параметров
        /// </summary>
				[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Un"), SqlMethod(Name = "UnPrepare", IsMutator = true, OnNullCall = true)]
        public new void UnPrepare()
        {
            base.UnPrepare();
        }

        /// <summary>
        /// Возвращает признак подготовленности параметров
        /// </summary>
        [SqlMethod(Name = "Prepared", IsDeterministic = true, IsPrecise = true, OnNullCall = true)]
        public new SqlBoolean Prepared(SqlInt32 objectId)
        {
            return base.Prepared(objectId);
        }

        /// <summary>
        /// Регистрирует зависимые параметры
        /// </summary>
        [SqlMethod(Name = "RegisterDependedParams", IsMutator = true, OnNullCall = true)]
        public new void RegisterDependedParams(SqlString value)
        {
            base.RegisterDependedParams(value);
        }
 
      /// <summary>
      /// Форматирует строку
      /// </summary>
      [SqlMethod(Name = "Format", OnNullCall = true)]
      public static String Format(SqlString value, TParams_EMPTY AParams)
      {
        return null;
      }

      [SqlMethod(Name = "FormatMax", OnNullCall = true)]
      public static SqlChars FormatMax(SqlString value, TParams_EMPTY AParams)
      {
        return SqlChars.Null;
      }

      [SqlMethod(Name = "Load", IsMutator = true, OnNullCall = true, IsDeterministic = true, DataAccess = DataAccessKind.Read, SystemDataAccess = SystemDataAccessKind.Read)]
      public void Load(TParams_EMPTY ASource, String AAliases, Char ALoadValueCondition = 'A')
      {
      }

      [SqlMethod(Name = "Copy", IsMutator = false, OnNullCall = false, IsDeterministic = true, DataAccess = DataAccessKind.Read, SystemDataAccess = SystemDataAccessKind.Read)]
      public TParams_EMPTY Copy(String Names)
      {
        return null;
      }

      [SqlMethod(Name = "Overwrite", IsMutator = false, OnNullCall = true, IsDeterministic = true, DataAccess = DataAccessKind.Read, SystemDataAccess = SystemDataAccessKind.Read)]
      public static TParams_EMPTY Overwrite(TParams_EMPTY AStorage, TParams_EMPTY ALoad, String ALoadAliases, Char ALoadValueCondition = 'A')
      {
        return null;
      }

      [SqlMethod(Name = "ToStringEx", OnNullCall = false, IsDeterministic = true)]
      public SqlString ToStringEx(String ANames, String AListSeparator)
      {
        return SqlString.Null;
      }

      [SqlMethod(Name = "ToXMLString", OnNullCall = true, IsDeterministic = true)]
      public String ToXMLString(String AElement = null)
      {
        return null;
      }

    }
}