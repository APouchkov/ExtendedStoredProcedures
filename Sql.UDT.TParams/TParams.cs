using Microsoft.SqlServer.Server;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
//using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace UDT
{
  [XmlRoot("Params")]
  [SqlUserDefinedType(Microsoft.SqlServer.Server.Format.UserDefined, IsByteOrdered = true, MaxByteSize = -1, Name = "TParams")]
  // ??? [SqlFacetAttribute(IsNullable = false)]
  public class TParams : INT.TParams, INullable//, IBinarySerialize, IXmlSerializable
  {
    /// <summary>
    /// Создает новый объект TParams
    /// </summary>
    [SqlMethod(Name = "New", OnNullCall = false, IsDeterministic = true)]
    public static TParams New()
    {
      TParams result = new TParams();
      result.Init();
      return result;
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
    public static TParams Null
    {
      get
      {
        TParams result = new TParams();
        return result;
      }
    }

    [SqlFunction(Name = "Enum", FillRowMethodName = "EnumRow", DataAccess = DataAccessKind.Read, TableDefinition = "[Name] NVarChar(127), [Type] NVarChar(127), [Value] SQL_Variant", IsDeterministic = true)]
    public static IEnumerable Enum(TParams Params)
    {
      return Params.ListParams();
    }
    public static void EnumRow(object Row, out SqlString Name, out SqlString Type, out Object Value)
    {
      Name  = ((TParamRow)Row).Name;
      Type  = ((TParamRow)Row).Type.ToString();
      Value = ((TParamRow)Row).Value;
    }

    [SqlFunction(Name = "Enum(Text)", FillRowMethodName = "EnumRowAsText", DataAccess = DataAccessKind.Read, TableDefinition = "[Name] NVarChar(127), [Type] NVarChar(127), [Value] NVarChar(Max)", IsDeterministic = true)]
    public static IEnumerable EnumAsText(TParams Params, String AStyle)
    {
      return Params.ListParamsAsText(AStyle);
    }
    public static void EnumRowAsText(object Row, out SqlString Name, out SqlString Type, out String Value)
    {
      Name  = ((TParamStringRow)Row).Name;
      Type  = ((TParamStringRow)Row).Type.ToString();
      Value = ((TParamStringRow)Row).Value;
    }

    /// <summary>
    /// Преобразует данные в строку
    /// </summary>
    [SqlMethod(Name = "ToString", OnNullCall = false, IsDeterministic = true)]
    public override String ToString()
    {
      return base.ToString();
    }

    /// <summary>
    /// Преобразует перечисленные параметры в строку
    /// </summary>
    [SqlMethod(Name = "ToStringEx", OnNullCall = false, IsDeterministic = true)]
    public override String ToStringEx(String ANames)
    {
      return base.ToStringEx(ANames);
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
    [
     System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Src"),
     SqlMethod(Name = "Parse", OnNullCall = false, IsDeterministic = true)
    ]
    public static TParams Parse(SqlString Src)
    {
      if (Src.IsNull) return null;
      TParams result = new TParams();
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
    [SqlMethod(Name = "Exists", IsDeterministic = true, OnNullCall = true)]
    public bool Exists(String Name)
    {
      return base.ExistsParam(Name);
    }

    /// <summary>
    /// Возвращает наименование параметров строкой
    /// </summary>
    public SqlString Names
    {
      get
      {
        return base.GetNames();
      }
    }

    /// <summary>
    /// Добавляет параметр, если существует переопределяет его значение
    /// </summary>
    [SqlMethod(Name = "AddParam", IsMutator = true, OnNullCall = true)]
    public new void AddParam(String AName, Object AValue)
    {
      base.AddParam(AName, AValue);
    }

    /// <summary>
    /// Добавляет параметр, если существует переопределяет его значение и возвращает это в новом объекте
    /// </summary>
    [
      System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters"), 
      SqlMethod(Name = "Add", IsMutator = false, OnNullCall = true)
    ]
    public TParams Add(String AName, Object AValue)
    {
      this.AddParam(AName, AValue);
      return this;
    }

    /// <summary>
    /// Добавляет параметр типа varchar(max), если существует переопределяет его значение
    /// </summary>
    //[SqlMethod(Name = "AddVarCharMax", IsMutator = true, OnNullCall = true)]
    //public new void AddVarCharMax(String AName, SqlBytes AValue)
    //{
    //  base.AddVarCharMax(AName, AValue);
    //}

    /// <summary>
    /// Добавляет параметр типа nvarchar(max), если существует переопределяет его значение
    /// </summary>
    [SqlMethod(Name = "AddNVarCharMax", IsMutator = true, OnNullCall = true)]
    public void AddNVarCharMax(String AName, SqlChars AValue)
    {
      base.AddParam(AName, AValue);
    }

    /// <summary>
    /// Добавляет параметр типа VarChar(max), если существует переопределяет его значение
    /// </summary>
    [SqlMethod(Name = "AddXml", IsMutator = true, OnNullCall = true)]
    public void AddXml(String Name, SqlXml Value)
    {
      base.AddParam(Name, Value);
    }

    /// <summary>
    /// Добавляет параметр типа VarBinary, если существует переопределяет его значение
    /// </summary>
    [SqlMethod(Name = "AddVarBinaryMax", IsMutator = true, OnNullCall = true)]
    public void AddVarBinaryMax(String Name, SqlBytes Value)
    {
      base.AddParam(Name, Value);
    }

    /// <summary>
    /// Добавляет параметр типа UDT, если существует переопределяет его значение
    /// </summary>
    //[SqlMethod(Name = "AddParams", IsMutator = true, OnNullCall = true)]
    //public void AddParams(String Name, TParams Value)
    //{
    //  base.AddParam(Name, Value);
    //}

    /// <summary>
    /// Добавляет параметр типа TList(String), если существует переопределяет его значение
    /// </summary>
    [SqlMethod(Name = "AddTListString", IsMutator = true, OnNullCall = true)]
    public void AddTListString(String AName, TListString AValue)
    {
      base.AddParam(AName, new SqlUdt(AValue));
    }

    /// <summary>
    /// Добавляет параметр типа TList(Int8), если существует переопределяет его значение
    /// </summary>
    [SqlMethod(Name = "AddTListInt8", IsMutator = true, OnNullCall = true)]
    public void AddTListInt8(String AName, TListInt8 AValue)
    {
      base.AddParam(AName, new SqlUdt(AValue));
    }

    /// <summary>
    /// Добавляет параметр типа TList(Int16), если существует переопределяет его значение
    /// </summary>
    [SqlMethod(Name = "AddTListInt16", IsMutator = true, OnNullCall = true)]
    public void AddTListInt16(String AName, TListInt16 AValue)
    {
      base.AddParam(AName, new SqlUdt(AValue));
    }

    /// <summary>
    /// Добавляет параметр типа TList(Int32), если существует переопределяет его значение
    /// </summary>
    [SqlMethod(Name = "AddTListInt32", IsMutator = true, OnNullCall = true)]
    public void AddTListInt32(String AName, TListInt32 AValue)
    {
      base.AddParam(AName, new SqlUdt(AValue));
    }

    /// <summary>
    /// Добавляет параметр типа TList(Int64), если существует переопределяет его значение
    /// </summary>
    [SqlMethod(Name = "AddTListInt64", IsMutator = true, OnNullCall = true)]
    public void AddTListInt64(String AName, TListInt64 AValue)
    {
      base.AddParam(AName, new SqlUdt(AValue));
    }

    /// <summary>
    /// Добавляет параметр типа TDictionary<String,String>, если существует переопределяет его значение
    /// </summary>
    [SqlMethod(Name = "AddTDictionaryStringString", IsMutator = true, OnNullCall = true)]
    public void AddTDictionaryStringString(String AName, TDictionaryStringString AValue)
    {
      base.AddParam(AName, new SqlUdt(AValue));
    }

    /// <summary>
    /// Добавляет параметр типа TDictionary<String,Int32>, если существует переопределяет его значение
    /// </summary>
    [SqlMethod(Name = "AddTDictionaryStringInt32", IsMutator = true, OnNullCall = true)]
    public void AddTDictionaryStringInt32(String AName, TDictionaryStringInt32 AValue)
    {
      base.AddParam(AName, new SqlUdt(AValue));
    }

    /// <summary>
    /// Добавляет параметр типа TDictionary<Int32,String>, если существует переопределяет его значение
    /// </summary>
    [SqlMethod(Name = "AddTDictionaryInt32String", IsMutator = true, OnNullCall = true)]
    public void AddTDictionaryInt32String(String AName, TDictionaryInt32String AValue)
    {
      base.AddParam(AName, new SqlUdt(AValue));
    }

    /// <summary>
    /// Вычисляет произвольное математическое выражение, используя обратный вызов срезы SQL
    /// </summary>
    [SqlMethod(Name = "Evaluate", IsMutator = false, OnNullCall = false, DataAccess = DataAccessKind.Read)]
    public object Evaluate(String AExpression)
    {
      return Evaluate(this, AExpression);
    }

    /// <summary>
    /// Вычисляет произвольное логическое выражение, используя обратный вызов срезы SQL
    /// </summary>
    [SqlMethod(Name = "EvaluateBoolean", IsMutator = false, OnNullCall = false, DataAccess = DataAccessKind.Read)]
    public Boolean EvaluateBoolean(String AExpression, Boolean ADefault)
    {
      return EvaluateBoolean(this, AExpression, ADefault);
    }

    /// <summary>
    /// Добавляет параметр, если существует переопределяет его значение и возвращает подготовленные параметры
    /// </summary>
    //[SqlMethod(Name = "PrepareParam", IsMutator = false, OnNullCall = true, DataAccess = DataAccessKind.Read, SystemDataAccess = SystemDataAccessKind.Read)]
    //public TParams PrepareParam(String Name, Object Value, String FunctionName)
    //{
    //  AddParam(Name, Value);
    //  Prepare(FunctionName);
    //  return this;
    //}

    /// <summary>
    /// Проверяет пересечение параметров, если общие параметры равны, то истина иначе ложь
    /// </summary>
    [
      System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters"),
      SqlMethod(Name = "Crossing", IsDeterministic = true, IsMutator = false, OnNullCall = true)
    ]
    public Boolean Crossing(TParams value)
    {
      return base.Crossing(value);
    }

    /// <summary>
    /// Возвращает пересечение двух списков параметров
    /// </summary>
    //[
    //  System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters"),
    //  SqlMethod(Name = "CrossParams", IsMutator = false, OnNullCall = false)
    // ]
    //public TParams CrossParams(TParams value)
    //{
    //  base.CrossParams(value);
    //  return this;
    //}

    /// <summary>
    /// Объединяет два списка параметров
    /// </summary>
    [
      System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters"),
      SqlMethod(Name = "MergeParams", IsMutator = true, OnNullCall = true)
    ]
    public void MergeParams(TParams value)
    {
      base.MergeParams(value);
    }

    [
      System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters"),
      SqlMethod(Name = "Merge", IsDeterministic = true, IsMutator = false, OnNullCall = true)
     ]
    public TParams Merge(TParams value)
    {
      base.MergeParams(value);
      return this;
    }

    /// <summary>
    /// Удаляет параметр
    /// </summary>
    [SqlMethod(Name = "DeleteParam", IsMutator = true, OnNullCall = false)]
    public new void DeleteParam(String Name)
    {
      base.DeleteParam(Name);
    }

    [SqlMethod(Name = "Delete", IsDeterministic = true, IsMutator = false, OnNullCall = false)]
    public TParams Delete(String Name)
    {
      base.DeleteParam(Name);
      return this;
    }

    /// <summary>
    /// Сравнивает параметры
    /// </summary>
    [
      System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters"),
      SqlMethod(Name = "Equals", IsDeterministic = true, OnNullCall = true)
    ]
    public bool Equals(TParams args)
    {
      return base.Equals(args);
    }

    /// <summary>
    /// Возвращает значение параметра
    /// </summary>
    [SqlMethod(Name = "AsVariant", IsDeterministic = true, IsPrecise = true, OnNullCall = false)]
    public new Object AsVariant(String Name)
    {
      return base.AsVariant(Name);
    }

    /// <summary>
    /// Возвращает значение параметра типа VarChar
    /// </summary>
    [SqlMethod(Name = "AsBit", IsDeterministic = true, IsPrecise = true, OnNullCall = false)]
    public new SqlBoolean AsBit(String Name)
    {
      return base.AsBit(Name);
    }

    /// <summary>
    /// Возвращает значение параметра типа VarChar (С 2008R2 только NVarChar)
    /// </summary>
    //[SqlMethod(Name = "AsVarChar", IsDeterministic = true, IsPrecise = true, OnNullCall = false)]
    //public new SqlBinary AsVarChar(String Name)
    //{
    //  return base.AsVarChar(Name);
    //}
    /// <summary>
    /// Возвращает значение параметра типа NVarChar
    /// </summary>
    [SqlMethod(Name = "AsNVarChar", IsDeterministic = true, IsPrecise = true, OnNullCall = false)]
    public new SqlString AsNVarChar(String Name)
    {
      return base.AsNVarChar(Name);
    }

    /// <summary>
    /// Возвращает значение параметра типа VarChar (С 2008R2 только NVarChar)
    /// </summary>
    //[SqlMethod(Name = "AsVarCharMax", IsDeterministic = true, IsPrecise = true, OnNullCall = false)]
    //public new SqlBytes AsVarCharMax(String Name)
    //{
    //  return base.AsVarCharMax(Name);
    //}
    [SqlMethod(Name = "AsNVarCharMax", IsDeterministic = true, IsPrecise = true, OnNullCall = false)]
    public new SqlChars AsNVarCharMax(String Name)
    {
      return base.AsNVarCharMax(Name);
    }

    /// <summary>
    /// Возвращает значение параметра в формате SQL
    /// </summary>
    [SqlMethod(Name = "AsSQLString", IsDeterministic = true, IsPrecise = true, OnNullCall = false)]
    public new SqlString AsSQLString(String Name)
    {
      return base.AsSQLString(Name);
    }

    /// <summary>
    /// Возвращает значение параметра типа VarChar
    /// </summary>
    [SqlMethod(Name = "AsSQLText", IsDeterministic = true, IsPrecise = true, OnNullCall = false)]
    public new SqlString AsSQLText(String Name)
    {
      return base.AsSQLText(Name);
    }

    /// <summary>
    /// Возвращает значение параметра типа Date
    /// </summary>
    [SqlMethod(Name = "AsDate", IsDeterministic = true, IsPrecise = true, OnNullCall = false)]
    public new DateTime? AsDate(String Name)
    {
      return base.AsDate(Name);
    }

    /// <summary>
    /// Возвращает значение параметра типа Time
    /// </summary>
    [SqlMethod(Name = "AsTime", IsDeterministic = true, IsPrecise = true, OnNullCall = false)]
    public new TimeSpan? AsTime(String Name)
    {
      return base.AsTime(Name);
    }

    /// <summary>
    /// Возвращает значение параметра типа DateTime
    /// </summary>
    [SqlMethod(Name = "AsDateTime", IsDeterministic = true, IsPrecise = true, OnNullCall = false)]
    public new SqlDateTime AsDateTime(String Name)
    {
      return base.AsDateTime(Name);
    }

    /// <summary>
    /// Возвращает значение параметра типа DateTimeOffset
    /// </summary>
    [SqlMethod(Name = "AsDateTime2", IsDeterministic = true, IsPrecise = true, OnNullCall = false)]
    public new DateTime? AsDateTime2(String Name)
    {
      return base.AsDateTime2(Name);
    }

    /// <summary>
    /// Возвращает значение параметра типа DateTimeOffset
    /// </summary>
    [SqlMethod(Name = "AsDateTimeOffset", IsDeterministic = true, IsPrecise = true, OnNullCall = false)]
    public new DateTimeOffset? AsDateTimeOffset(String Name)
    {
      return base.AsDateTimeOffset(Name);
    }

    /// <summary>
    /// Возвращает значение параметра типа TinyInt
    /// </summary>
    [SqlMethod(Name = "AsTinyInt", IsDeterministic = true, IsPrecise = true, OnNullCall = false)]
    public new SqlByte AsTinyInt(String Name)
    {
      return base.AsTinyInt(Name);
    }

    /// <summary>
    /// Возвращает значение параметра типа SmallInt
    /// </summary>
    [SqlMethod(Name = "AsSmallInt", IsDeterministic = true, IsPrecise = true, OnNullCall = false)]
    public new SqlInt16 AsSmallInt(String Name)
    {
      return base.AsSmallInt(Name);
    }

    /// <summary>
    /// Возвращает значение параметра типа Int
    /// </summary>
    [SqlMethod(Name = "AsInt", IsDeterministic = true, IsPrecise = true, OnNullCall = false)]
    public new SqlInt32 AsInt(String Name)
    {
      return base.AsInt(Name);
    }

    /// <summary>
    /// Возвращает значение параметра типа BigInt
    /// </summary>
    [SqlMethod(Name = "AsBigInt", IsDeterministic = true, IsPrecise = true, OnNullCall = false)]
    public new SqlInt64 AsBigInt(String Name)
    {
      return base.AsBigInt(Name);
    }

    /// <summary>
    /// Возвращает значение параметра типа Decimal
    /// </summary>
    //[SqlMethod(Name = "AsNumeric", IsDeterministic = true, IsPrecise = true, OnNullCall = false)]
    //public new SqlDecimal AsNumeric(String Name)
    //{
    //    return base.AsNumeric(Name);
    //}

    /*
        /// <summary>
        /// Возвращает значение параметра типа TParams
        /// </summary>
        [SqlMethod(Name = "AsParams", IsDeterministic = true, IsPrecise = true, OnNullCall = false)]
        public new TParams AsParams(String Name)
        {
          return (TParams)base.AsParams(Name);
        }
    */

    /// <summary>
    /// Возвращает значение параметра типа Xml
    /// </summary>
    [SqlMethod(Name = "AsXml", IsDeterministic = true, IsPrecise = true, OnNullCall = false)]
    public new SqlXml AsXml(String Name)
    {
      return base.AsXml(Name);
    }

    /// <summary>
    /// Возвращает значение параметра типа VarBinary
    /// </summary>
    [SqlMethod(Name = "AsVarBinaryMax", IsDeterministic = true, IsPrecise = true, OnNullCall = false)]
    public new SqlBytes AsVarBinaryMax(String Name)
    {
      return base.AsVarBinaryMax(Name);
    }

    [SqlMethod(Name = "AsVarBinary", IsDeterministic = true, IsPrecise = true, OnNullCall = false)]
    public new SqlBinary AsVarBinary(String Name)
    {
      return base.AsVarBinary(Name);
    }

    /// <summary>
    /// Подготавливает параметры
    /// </summary>
    [SqlMethod(Name = "PrepareFunction", IsMutator = true, OnNullCall = false, DataAccess = DataAccessKind.Read, SystemDataAccess = SystemDataAccessKind.Read)]
    public void PrepareFunction(String Name)
    {
      base.Prepare(Name);
    }

    [SqlMethod(Name = "Prepare", IsMutator = false, OnNullCall = false, DataAccess = DataAccessKind.Read, SystemDataAccess = SystemDataAccessKind.Read)]
    public new TParams Prepare(String Name)
    {
      base.Prepare(Name);
      return this;
    }

    /// <summary>
    /// Убирает подготовку параметров
    /// </summary>
    [
      System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Un"),
      SqlMethod(Name = "UnPrepare", IsMutator = true, OnNullCall = true)
    ]
    public new void UnPrepare()
    {
      base.UnPrepare();
    }

    /// <summary>
    /// Возвращает признак подготовленности параметров
    /// </summary>
    [SqlMethod(Name = "Prepared", IsMutator = false, IsDeterministic = true, IsPrecise = true, OnNullCall = true)]
    public new SqlBoolean Prepared(SqlInt32 objectId)
    {
      return base.Prepared(objectId);
    }

    /// <summary>
    /// Регистрирует зависимые параметры
    /// </summary>
    [SqlMethod(Name = "RegisterDependedParams", IsMutator = true, OnNullCall = true)]
    public new void RegisterDependedParams(String Value)
    {
      base.RegisterDependedParams(Value);
    }

    /// <summary>
    /// Форматирует строку
    /// </summary>
    [SqlMethod(Name = "Format", IsMutator = false, IsDeterministic = true, OnNullCall = false)]
    public SqlString Format(String AValue)
    {
      return INT.TParams.Format(this, AValue);
    }
    [SqlFunction(Name = "Format", DataAccess = DataAccessKind.None, IsDeterministic = true)]
    public static SqlString InternalFormat(TParams AParams, String AValue)
    {
      return INT.TParams.Format(AParams, AValue);
    }

    /// <summary>
    /// Сверяет два набора
    /// </summary>
    [SqlFunction(Name = "Is Equal", DataAccess = DataAccessKind.None, IsDeterministic = true)]
    public static Boolean IsEqual(TParams AParams1, TParams AParams2)
    {
      if (AParams1 == null && AParams2 == null) return true;
      if (AParams1 == null || AParams2 == null) return false;

      return AParams1.Equals(AParams2);
    }

    /// <summary>
    /// Возвращает коллекцию TListString
    /// </summary>
    [SqlMethod(Name = "AsTListString", IsDeterministic = true, IsPrecise = true, OnNullCall = false)]
    public new TListString AsTListString(String AName)
    {
      return base.AsTListString(AName);
    }

    [SqlFunction(Name = "TList.Enum<String>", FillRowMethodName = "TListStringEnumRow", DataAccess = DataAccessKind.None, TableDefinition = "[Value] NVarChar(4000), [Index] Int", IsDeterministic = true)]
    public static IEnumerable TListStringEnum(TParams AParams, String AName)
    {
      if (AParams == null) return null;

      TListString LList = AParams.AsTListString(AName);
      if(LList == null) return null;
      return TListString.Enum(LList);
    }
    public static void TListStringEnumRow(object ARow, out String AValue, out Int32 AIndex)
    {
      AValue = ((KeyValuePair<String, Int32>)ARow).Key;
      AIndex = ((KeyValuePair<String, Int32>)ARow).Value;
    }

    /// <summary>
    /// Возвращает коллекцию TListInt8
    /// </summary>
    [SqlMethod(Name = "AsTListInt8", IsDeterministic = true, IsPrecise = true, OnNullCall = false)]
    public new TListInt8 AsTListInt8(String AName)
    {
      return base.AsTListInt8(AName);
    }

    [SqlFunction(Name = "TList.Enum<Int8>", FillRowMethodName = "TListInt8EnumRow", DataAccess = DataAccessKind.None, TableDefinition = "[Value] TinyInt, [Index] SmallInt", IsDeterministic = true)]
    public static IEnumerable TListInt8Enum(TParams AParams, String AName)
    {
      if (AParams == null) return null;

      TListInt8 LList = AParams.AsTListInt8(AName);
      if(LList == null) return null;
      return TListInt8.Enum(LList);
    }
    public static void TListInt8EnumRow(object ARow, out Byte AValue, out Int16 AIndex)
    {
      AValue = ((KeyValuePair<Byte, Int16>)ARow).Key;
      AIndex = ((KeyValuePair<Byte, Int16>)ARow).Value;
    }

    /// <summary>
    /// Возвращает коллекцию TListInt16
    /// </summary>
    [SqlMethod(Name = "AsTListInt16", IsDeterministic = true, IsPrecise = true, OnNullCall = false)]
    public new TListInt16 AsTListInt16(String AName)
    {
      return base.AsTListInt16(AName);
    }

    [SqlFunction(Name = "TList.Enum<Int16>", FillRowMethodName = "TListInt16EnumRow", DataAccess = DataAccessKind.None, TableDefinition = "[Value] SmallInt, [Index] Int", IsDeterministic = true)]
    public static IEnumerable TListInt16Enum(TParams AParams, String AName)
    {
      if (AParams == null) return null;

      TListInt16 LList = AParams.AsTListInt16(AName);
      if(LList == null) return null;
      return TListInt16.Enum(LList);
    }
    public static void TListInt16EnumRow(object ARow, out Int16 AValue, out Int32 AIndex)
    {
      AValue = ((KeyValuePair<Int16, Int32>)ARow).Key;
      AIndex = ((KeyValuePair<Int16, Int32>)ARow).Value;
    }

    /// <summary>
    /// Возвращает коллекцию TListInt32
    /// </summary>
    [SqlMethod(Name = "AsTListInt32", IsDeterministic = true, IsPrecise = true, OnNullCall = false)]
    public new TListInt32 AsTListInt32(String AName)
    {
      return base.AsTListInt32(AName);
    }

    [SqlFunction(Name = "TList.Enum<Int32>", FillRowMethodName = "TListInt32EnumRow", DataAccess = DataAccessKind.None, TableDefinition = "[Value] Int, [Index] Int", IsDeterministic = true)]
    public static IEnumerable TListInt32Enum(TParams AParams, String AName)
    {
      if (AParams == null) return null;

      TListInt32 LList = AParams.AsTListInt32(AName);
      if(LList == null) return null;
      return TListInt32.Enum(LList);
    }
    public static void TListInt32EnumRow(object ARow, out Int32 AValue, out Int32 AIndex)
    {
      AValue = ((KeyValuePair<Int32, Int32>)ARow).Key;
      AIndex = ((KeyValuePair<Int32, Int32>)ARow).Value;
    }

    /// <summary>
    /// Возвращает коллекцию TListInt64
    /// </summary>
    [SqlMethod(Name = "AsTListInt64", IsDeterministic = true, IsPrecise = true, OnNullCall = false)]
    public new TListInt64 AsTListInt64(String AName)
    {
      return base.AsTListInt64(AName);
    }

    [SqlFunction(Name = "TList.Enum<Int64>", FillRowMethodName = "TListInt64EnumRow", DataAccess = DataAccessKind.None, TableDefinition = "[Value] BigInt, [Index] Int", IsDeterministic = true)]
    public static IEnumerable TListInt64Enum(TParams AParams, String AName)
    {
      if (AParams == null) return null;

      TListInt64 LList = AParams.AsTListInt64(AName);
      if(LList == null) return null;
      return TListInt64.Enum(LList);
    }
    public static void TListInt64EnumRow(object ARow, out Int64 AValue, out Int32 AIndex)
    {
      AValue = ((KeyValuePair<Int64, Int32>)ARow).Key;
      AIndex = ((KeyValuePair<Int64, Int32>)ARow).Value;
    }

    /// <summary>
    /// Возвращает коллекцию TDictionaryStringString
    /// </summary>
    [SqlMethod(Name = "AsTDictionaryStringString", IsDeterministic = true, IsPrecise = true, OnNullCall = false)]
    public new TDictionaryStringString AsTDictionaryStringString(String AName)
    {
      return base.AsTDictionaryStringString(AName);
    }

    [SqlFunction(Name = "TDictionary.Enum<String,String>", FillRowMethodName = "TDictionaryStringStringEnumRow", DataAccess = DataAccessKind.None, TableDefinition = "[Name] NVarChar(4000), [Value] NVarChar(Max), [Index] Int", IsDeterministic = true)]
    public static IEnumerable TDictionaryStringStringEnum(TParams AParams, String AName)
    {
      if (AParams == null) return null;
      TDictionaryStringString LList = AParams.AsTDictionaryStringString(AName);
      if(LList == null) return null;
      return TDictionaryStringString.Enum(LList);
    }
    public static void TDictionaryStringStringEnumRow(object ARow, out String AKey, out String AValue, out Int32 AIndex)
    {
      AKey   = ((KeyValueIndexPair<String,String,Int32>)ARow).Key;
      AValue = ((KeyValueIndexPair<String,String,Int32>)ARow).Value;
      AIndex = ((KeyValueIndexPair<String,String,Int32>)ARow).Index;
    }


    /// <summary>
    /// Возвращает коллекцию TDictionaryStringInt32
    /// </summary>
    [SqlMethod(Name = "AsTDictionaryStringInt32", IsDeterministic = true, IsPrecise = true, OnNullCall = false)]
    public new TDictionaryStringInt32 AsTDictionaryStringInt32(String AName)
    {
      return base.AsTDictionaryStringInt32(AName);
    }

    [SqlFunction(Name = "TDictionary.Enum<String,Int32>", FillRowMethodName = "TDictionaryStringInt32EnumRow", DataAccess = DataAccessKind.None, TableDefinition = "[Name] NVarChar(4000), [Value] Int, [Index] Int", IsDeterministic = true)]
    public static IEnumerable TDictionaryStringInt32Enum(TParams AParams, String AName)
    {
      if (AParams == null) return null;
      TDictionaryStringInt32 LList = AParams.AsTDictionaryStringInt32(AName);
      if(LList == null) return null;
      return TDictionaryStringInt32.Enum(LList);
    }
    public static void TDictionaryStringInt32EnumRow(object ARow, out String AKey, out Int32 AValue, out Int32 AIndex)
    {
      AKey   = ((KeyValueIndexPair<String,Int32,Int32>)ARow).Key;
      AValue = ((KeyValueIndexPair<String,Int32,Int32>)ARow).Value;
      AIndex = ((KeyValueIndexPair<String,Int32,Int32>)ARow).Index;
    }

    /// <summary>
    /// Возвращает коллекцию TDictionaryInt32String
    /// </summary>
    [SqlMethod(Name = "AsTDictionaryInt32String", IsDeterministic = true, IsPrecise = true, OnNullCall = false)]
    public new TDictionaryInt32String AsTDictionaryInt32String(String AName)
    {
      return base.AsTDictionaryInt32String(AName);
    }

    [SqlFunction(Name = "TDictionary.Enum<Int32,String>", FillRowMethodName = "TDictionaryInt32StringEnumRow", DataAccess = DataAccessKind.None, TableDefinition = "[Name] NVarChar(4000), [Value] Int, [Index] Int", IsDeterministic = true)]
    public static IEnumerable TDictionaryInt32StringEnum(TParams AParams, String AName)
    {
      if (AParams == null) return null;
      TDictionaryInt32String LList = AParams.AsTDictionaryInt32String(AName);
      if(LList == null) return null;
      return TDictionaryInt32String.Enum(LList);
    }
    public static void TDictionaryInt32StringEnumRow(object ARow, out Int32 AKey, out String AValue, out Int32 AIndex)
    {
      AKey   = ((KeyValueIndexPair<Int32,String,Int32>)ARow).Key;
      AValue = ((KeyValueIndexPair<Int32,String,Int32>)ARow).Value;
      AIndex = ((KeyValueIndexPair<Int32,String,Int32>)ARow).Index;
    }
  }


  [Serializable]
  [SqlUserDefinedAggregate(Format.UserDefined, IsInvariantToNulls = true, IsInvariantToDuplicates = false, IsInvariantToOrder = false, MaxByteSize = -1)]
  public class TParamsAggregate: IBinarySerialize
  {
    private TParams OResult;

    public void Init()
    {
      OResult = new TParams();
    }

    public void Accumulate(String AName, Object AValue)
    {
      if (AValue == null) 
        return;

		  OResult.AddParam(AName, AValue);
    }

    public void Merge(TParamsAggregate AOther)
    {
      if(AOther != null && AOther.OResult != null)
        OResult.MergeParams(AOther.OResult);
    }

    public TParams Terminate()
    {
      if (OResult != null && OResult.Count > 0)
        return OResult;
      else
        return null;
    }

    public void Read(System.IO.BinaryReader r)
    {
      //if (r == null) throw new ArgumentNullException("r");
      if(OResult == null)
        OResult = new TParams();
      OResult.Read(r);
    }

    public void Write(System.IO.BinaryWriter w)
    {
      //if (w == null) throw new ArgumentNullException("w");
      if(OResult != null)
        OResult.Write(w);
    }
  }
}
