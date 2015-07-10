using System;
using System.IO;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public partial class Pub
{
  static String NullIfEmpty(String Value)
  {
    return Value == "" ? null : Value;
  }

  /// Разбивает массив на элементы по делителю
  [SqlFunction(Name = "Repeat", FillRowMethodName = "RepeatRow", DataAccess = DataAccessKind.None, TableDefinition = "[Index] int", IsDeterministic = true)]
  public static IEnumerable Repeat(Int32 AFrom, Int32 ATo)
  {
    for(; AFrom <= ATo; AFrom++)
      yield return AFrom;
  }
  public static void RepeatRow(Object ARow, out Int32 AIndex)
  {
    AIndex = (Int32)ARow;
  }

  public static void InternalMerge(ref String AQuotedArray1, String AArray2, String ASeparator)
  {
    int
      LLastPos    = -1,
      LNewPos     = 0;

    String
      LValue;

    while (LNewPos >= 0)
    {
      LNewPos = AArray2.IndexOf(ASeparator, LLastPos + 1);
      if (LNewPos >= 0)
        LValue = AArray2.Substring(LLastPos + 1, LNewPos - LLastPos - 1);
      else
        LValue = AArray2.Substring(LLastPos + 1);

			if (LValue == "*")
				throw new System.SystemException("Wrong array item value = '*'");

			if ((LValue.Length > 0) && (AQuotedArray1.IndexOf(ASeparator + LValue + ASeparator) < 0))
        AQuotedArray1 += LValue + ASeparator;

      LLastPos = LNewPos;
    }
  }

  public static void InternalCharSetMerge(ref String AArray1, String AArray2)
  {
    int I;
    string LChar;

    for(I = 0; I < AArray2.Length; I++)
    {
      LChar = AArray2.Substring(I, 1); 
      if(AArray1.IndexOf(LChar) < 0)
        AArray1 = AArray1 + LChar;
    }
  }

  // ---------------------------------------------------------------------------
  struct CharRow
  {
    public Char   Char;
    public Int16  Index;
  }
  /// Разбивает массив на элементы по делителю
  [SqlFunction(FillRowMethodName = "StringToCharSetRow", DataAccess = DataAccessKind.None, TableDefinition = "[Char] nchar(1), [Index] smallint", IsDeterministic = true)]
  public static IEnumerable ForeachChar(SqlString AText)
  {
    if (AText.IsNull || (AText.Value.Length == 0)) yield break;
    String LText = AText.Value;

    CharRow LRow;
    for (LRow.Index = 0; LRow.Index < LText.Length;)
    {
      LRow.Char  = LText[LRow.Index];
      LRow.Index++;

      yield return LRow;
    }
  }

  public static void StringToCharSetRow(Object ARow, out SqlString AChar, out SqlInt16 AIndex)
  {
    AChar  = (((CharRow)ARow).Char).ToString();
    AIndex = ((CharRow)ARow).Index;
  }

  struct StringRow
  {
    public Int32  Index;
    public String Value;
  }

  /// Разбивает массив на элементы по делителю
  [SqlFunction(FillRowMethodName = "ArrayToRowSetRow", DataAccess = DataAccessKind.None, TableDefinition = "[Value] nvarchar(max), [Index] int", IsDeterministic = true)]
  public static IEnumerable ArrayToRowSet(SqlString AText, Char ASeparator)
  {
    if (AText.IsNull || (AText.Value.Length == 0)) yield break;
    List<String> LUnique = new List<String>();

    StringRow LRow;
    LRow.Index = 1;
    foreach (String LLine in AText.Value.Split(new char[]{ASeparator}, StringSplitOptions.RemoveEmptyEntries))
    {
      //if (LLine.Length == 0) continue;
      LRow.Value = LLine;

      if (LUnique.IndexOf(LLine.ToUpper()) >= 0)
        if (AText.Value.Length > 1000)
          throw new System.SystemException("Doublicate id = '" + LLine + "' in array");
        else
          throw new System.SystemException("Doublicate id = '" + LLine + "' in array: " + AText.Value);

      yield return LRow;
      LUnique.Add(LRow.Value.ToUpper());
      LRow.Index++;
    }
  }

  public static void ArrayToRowSetRow(object ARow, out SqlString AValue, out SqlInt32 AIndex)
  {
    AValue = ((StringRow)ARow).Value;
    AIndex = ((StringRow)ARow).Index;
  }

  struct StringRowNamed
  {
    public SqlString  Name;
    public SqlString  Value;
    public Int32      Index;
  }

  /// Разбивает массив на элементы (имя, значение) по делителю
  [SqlFunction(FillRowMethodName = "ArrayToRowSetNamedRow", DataAccess = DataAccessKind.None, TableDefinition = "[Name] nvarchar(4000), [Value] nvarchar(max), [Index] int", IsDeterministic = true)]
  public static IEnumerable ArrayToRowSetNamed(SqlString AText, Char ASeparator)
  {
    if (AText.IsNull || (AText.Value.Length == 0)) yield break;
    List<String> LUnique = new List<String>();

    StringRowNamed LRow;
	  LRow.Index = 1;
    foreach (String LLine in AText.Value.Split(new char[]{ASeparator}, StringSplitOptions.RemoveEmptyEntries))
    {
      //if (LLine.Length == 0) continue;
      String[] LSubLines = LLine.Split(new char[1]{'='}, 2);
      if (LSubLines.Length < 1) continue;

      LRow.Name  = LSubLines[0];

			if (LSubLines.Length == 1 || LSubLines[1] == "")
        LRow.Value = null;
      else
        LRow.Value = LSubLines[1];

      if (LUnique.IndexOf(LRow.Name.Value.ToUpper()) >= 0)
        if (AText.Value.Length > 1000)
          throw new System.SystemException("Doublicate id = '" + LRow.Name.Value + "' in array");
        else
          throw new System.SystemException("Doublicate id = '" + LRow.Name.Value + "' in array: " + AText.Value);

      yield return LRow;
      LUnique.Add(LRow.Name.Value.ToUpper());
      LRow.Index++;
    }
  }

  public static void ArrayToRowSetNamedRow(Object ARow, out SqlString AName, out SqlString AValue, out SqlInt32 AIndex)
  {
    AName   = ((StringRowNamed)ARow).Name;
    AValue  = ((StringRowNamed)ARow).Value;
    AIndex  = ((StringRowNamed)ARow).Index;
  }

  struct StringRowNamed2
  {
      public SqlString  Name;
      public SqlString  Value1;
      public SqlString  Value2;
      public Int32      Index;
  }

  /// Разбивает массив на элементы (имя, значение1, значение2) по делителю
  [SqlFunction(FillRowMethodName = "ArrayToRowSetNamedRow2", DataAccess = DataAccessKind.None, TableDefinition = "[Name] nvarchar(4000), [Value:1] nvarchar(max), [Value:2] nvarchar(max), [Index] int", IsDeterministic = true)]
  public static IEnumerable ArrayToRowSetNamed2(SqlString AText, Char ASeparator, Char AValueSeparator)
  {
    if (AText.IsNull || (AText.Value.Length == 0)) yield break;
    List<String> LUnique = new List<String>();

    StringRowNamed2 LRow;
    LRow.Index = 1;
    foreach (String LLine in AText.Value.Split(new char[]{ASeparator}, StringSplitOptions.RemoveEmptyEntries))
    {
      //if (LLine.Length == 0) continue;
      String[] LNames = LLine.Split(new Char[1]{'='}, 2);
      if (LNames.Length < 2) continue;
      LRow.Name = LNames[0];

      String[] LValues = LNames[1].Split(new Char[1]{AValueSeparator}, 2);

      //String[] LSubLines = new String[3] { LNames[0], LValues[0], LValues[1] };
      //if (LSubLines.Length < 3) continue;

      if (LValues.Length == 0 || LValues[0].Length == 0)
        LRow.Value1 = null;
      else
        LRow.Value1 = LValues[0];

      if (LValues.Length <= 1 || LValues[1].Length == 0)
        LRow.Value2 = null;
      else
        LRow.Value2 = LValues[1];

      if (LUnique.IndexOf(LRow.Name.Value.ToUpper()) >= 0)
        if (AText.Value.Length > 1000)
          throw new System.SystemException("Doublicate id = '" + LRow.Name.Value + "' in array");
        else
          throw new System.SystemException("Doublicate id = '" + LRow.Name.Value + "' in array: " + AText.Value);

      yield return LRow;
      LUnique.Add(LRow.Name.Value.ToUpper());
      LRow.Index++;
    }
  }

  public static void ArrayToRowSetNamedRow2(Object ARow, out SqlString AName, out SqlString AValue1, out SqlString AValue2, out SqlInt32 AIndex)
  {
      AName   = ((StringRowNamed2)ARow).Name;
      AValue1 = ((StringRowNamed2)ARow).Value1;
      AValue2 = ((StringRowNamed2)ARow).Value2;
      AIndex  = ((StringRowNamed2)ARow).Index;
  }

  struct StringRowNamed3
  {
    public SqlString  Name;
    public SqlString  Value1;
    public SqlString  Value2;
    public SqlString  Value3;
    public Int32      Index;
  }

  /// Разбивает массив на элементы (имя, значение1, значение2, значение3) по делителю
  [SqlFunction(FillRowMethodName = "ArrayToRowSetNamedRow3", DataAccess = DataAccessKind.None, TableDefinition = "[id] nvarchar(max), [value1] nvarchar(max), [value2] nvarchar(max), [value3] nvarchar(max), [sort_id] int", IsDeterministic = true)]
  public static IEnumerable ArrayToRowSetNamed3(SqlString AText, Char ASeparator, Char AValueSeparator)
  {
    if (AText.IsNull || (AText.Value.Length == 0)) yield break;
    List<String> LUnique = new List<String>();

    StringRowNamed3 LRow;
    LRow.Index = 1;
    foreach (String LLine in AText.Value.Split(new char[]{ASeparator}, StringSplitOptions.RemoveEmptyEntries))
    {
      //if (LLine.Length == 0) continue;

      String[] LNames = LLine.Split(new Char[1]{'='}, 2);
      if (LNames.Length < 2) continue;
      LRow.Name = LNames[0];
      String[] LValues = LNames[1].Split(new Char[1]{AValueSeparator}, 3);

      if (LValues.Length == 0 || LValues[0].Length == 0)
        LRow.Value1 = null;
      else
        LRow.Value1 = LValues[0];

      if (LValues.Length <= 1 || LValues[1].Length == 0)
        LRow.Value2 = null;
      else
        LRow.Value2 = LValues[1];

      if (LValues.Length <= 2 || LValues[2].Length == 0)
        LRow.Value3 = null;
      else
        LRow.Value3 = LValues[2];

      if (LUnique.IndexOf(LRow.Name.Value.ToUpper()) >= 0)
        if (AText.Value.Length > 1000)
          throw new System.SystemException("Doublicate id = '" + LRow.Name.Value + "' in array");
        else
          throw new System.SystemException("Doublicate id = '" + LRow.Name.Value + "' in array: " + AText.Value);

      yield return LRow;
      LUnique.Add(LRow.Name.Value.ToUpper());
      LRow.Index++;
    }
  }

  public static void ArrayToRowSetNamedRow3(object ARow, out SqlString AName, out SqlString AValue1, out SqlString AValue2, out SqlString AValue3, out SqlInt32 AIndex)
  {
    AName   = ((StringRowNamed3)ARow).Name;
    AValue1 = ((StringRowNamed3)ARow).Value1;
    AValue2 = ((StringRowNamed3)ARow).Value2;
    AValue3 = ((StringRowNamed3)ARow).Value3;
    AIndex  = ((StringRowNamed3)ARow).Index;
  }

  /// Возвращает параметр по индексу из списка параметров
  [SqlFunction(Name = "Extract Value", DataAccess = DataAccessKind.None, IsDeterministic = true)] // WITH RETURNS NULL ON NULL INPUT
  public static SqlString ExtractValue(SqlString AText, Int32 AIndex, Char ASeparator)
  {
    if (AText.IsNull || (AText.Value.Length == 0)) return null;
    String[] LLines = AText.Value.Split(ASeparator);
    if (LLines.Length >= AIndex && LLines[AIndex - 1].Length > 0)
    {
      return (SqlString)LLines[AIndex - 1];
    }

    return null;
  }

  /// Возвращает параметр по имени из списка параметров
  [SqlFunction(DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public static SqlString ExtractNamedValue(SqlString Text, SqlString Name, SqlChars Separator)
  {
      if (Text.IsNull || (Text.Value.ToString() == "") || Separator.IsNull) return null;
      String[] lines = Text.Value.Split(Separator.Value, StringSplitOptions.RemoveEmptyEntries);

      int i = 0;
      foreach (String line in lines)
      {
        //if (line == "") continue;
        String[] sublines = line.Split("=".ToCharArray(), 2);
        if (sublines[0].ToUpper() == Name.Value.ToUpper()) return (SqlString)sublines[1];
        i++;
      }

      return null;
  }

  /// Добавляет имя со значением в конец строки, если имя отсутсвует
  [SqlFunction(DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public static SqlString SetNamedValue(SqlString Text, SqlString Name, SqlString Value, SqlChars Separator)
  {
    const string EQUALS = "=";

    if (Separator.IsNull || (Separator.Length != 1))
        throw new System.SystemException("Wrong value for parameter 'Separator'");

    string
      Separator_value = Separator.ToSqlString().Value,
      Text_value      = (Text.IsNull  ? null : Text.Value),
      Name_value      = (Name.IsNull  ? null : Name.Value),
      Value_value     = (Value.IsNull ? null : Value.Value);

    // Если передан пустой текст, или не передан разделитель - возвращаем "имя=значение"
    if ((String.IsNullOrEmpty(Text_value)) || Separator.IsNull)
        return new SqlString(Name_value + EQUALS + Value_value);

    int pos = (Separator_value + Text_value).IndexOf(Separator_value + Name_value + EQUALS, StringComparison.CurrentCultureIgnoreCase);
    if (pos >= 0)
    {
      StringBuilder builder = new StringBuilder(Text_value.Substring(0, pos + Name_value.Length + EQUALS.Length), Text_value.Length + Name_value.Length + Value_value.Length);
      builder.Append(Value_value);
      pos = Text_value.IndexOf(Separator_value, pos + Name_value.Length + EQUALS.Length);

      if (pos >= 0)
      {
        builder.Append(Separator_value);
        builder.Append(Text_value.Substring(pos + 1));
      }

      return builder.ToString();
    }
    else
      return Text_value + Separator_value + Name_value + EQUALS + Value_value;
  }

  [SqlFunction(DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public static Boolean InArray(SqlString AArray, SqlString AElement, Char ASeparator)
  {
    String
      LArray       = (AArray.IsNull		? null : NullIfEmpty(AArray.Value)),
      LElement     = (AElement.IsNull	? null : NullIfEmpty(AElement.Value));

		if (String.IsNullOrEmpty(LArray) || LArray.Equals(ASeparator) || String.IsNullOrEmpty(LElement))
			return false;

    if (LArray.Equals("*"))
      return true;

    if (LArray[0] != ASeparator) LArray = ASeparator + LArray;
    if (LArray[LArray.Length - 1] != ASeparator) LArray += ASeparator;
    return (LArray.IndexOf(ASeparator + LElement + ASeparator) >= 0);
  }

	[SqlFunction(DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public static SqlString ArraysMerge(SqlString AArray1, SqlString AArray2, Char ASeparator)
  {
    String
      LArray1       = (AArray1.IsNull ? null : NullIfEmpty(AArray1.Value)),
      LArray2       = (AArray2.IsNull ? null : NullIfEmpty(AArray2.Value));

		if (String.IsNullOrEmpty(LArray2) || LArray2.Equals(ASeparator))
			return new SqlString(LArray1);

    if (String.IsNullOrEmpty(LArray1) || LArray1.Equals(ASeparator))
      return new SqlString(LArray2);

    if (LArray1.Equals("*") || LArray2.Equals("*"))
      return new SqlString("*");

    if (LArray1[0] != ASeparator)
      LArray1 = ASeparator + LArray1;

    if (LArray1[LArray1.Length - 1] != ASeparator)
      LArray1 += ASeparator;

    InternalMerge(ref LArray1, LArray2, ASeparator.ToString());
    return new SqlString(LArray1.Substring(1, LArray1.Length - 2));
  }

	[SqlFunction(DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public static SqlString CharSetMerge(SqlString AArray1, SqlString AArray2)
  {
    String
      LArray1       = (AArray1.IsNull ? null : NullIfEmpty(AArray1.Value)),
      LArray2       = (AArray2.IsNull ? null : NullIfEmpty(AArray2.Value));

		if (String.IsNullOrEmpty(LArray2))
			return new SqlString(LArray1);

    if (String.IsNullOrEmpty(LArray1))
      return new SqlString(LArray2);

    InternalCharSetMerge(ref LArray1, LArray2);
    return new SqlString(LArray1);
  }

  [SqlFunction(DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public static SqlString ArraysJoin(SqlString AArray1, SqlString AArray2, Char ASeparator)
  {
    String
      LArray1      = (AArray1.IsNull ? null : AArray1.Value),
      LArray2      = (AArray2.IsNull ? null : AArray2.Value);

    if (
        String.IsNullOrEmpty(LArray1)
        || String.IsNullOrEmpty(LArray2)
        || LArray1.Equals(ASeparator)
        || LArray2.Equals(ASeparator)
        )
        return null;

    if (LArray1.Equals("*"))
      return new SqlString(LArray2);
    if (LArray2.Equals("*"))
      return new SqlString(LArray1);

    StringBuilder
      Result = new StringBuilder(LArray1.Length + LArray2.Length);

    LArray2 = ASeparator + LArray2 + ASeparator;

    int
      LLastPos    = -1,
      LNewPos     = 0;

    String
      LValue;

    while (LNewPos >= 0)
    {
      LNewPos = LArray1.IndexOf(ASeparator, LLastPos + 1);
      if (LNewPos >= 0)
        LValue = LArray1.Substring(LLastPos + 1, LNewPos - LLastPos - 1);
      else
        LValue = LArray1.Substring(LLastPos + 1);

      if ((LValue.Length > 0) && (LArray2.IndexOf(ASeparator + LValue + ASeparator) >= 0))
      {
        if (Result.Length > 0)
					Result.Append(ASeparator);
        Result.Append(LValue);
      }

      LLastPos = LNewPos;
    }

    if (Result.Length > 0)
      return new SqlString(Result.ToString());
    else
      return null;
  }


  public static Int64 ArrayTestInternal(String AArray1, String AArray2, String ASeparator)
  {
    if (
        String.IsNullOrEmpty(AArray1)
        || String.IsNullOrEmpty(AArray2)
        || AArray1.Equals(ASeparator)
        || AArray2.Equals(ASeparator)
        )
        return 0;

    //if (LArray1.Equals("*"))
    //  return new SqlString(LArray2);
    //if (LArray2.Equals("*"))
    //  return new SqlString(LArray1);

    Boolean LAllFields;
    if (AArray1.Equals("*"))
      LAllFields = true;
    else
    {
      LAllFields = false;
      AArray1 = ASeparator + AArray1 + ASeparator;
    }

    int
      LLastPos    = -1,
      LNewPos     = 0;
    Int64 
      BitPos = 1,
      Result = 0;

    String
      LValue;

    while (LNewPos >= 0)
    {
      LNewPos = AArray2.IndexOf(ASeparator, LLastPos + 1);
      if (LNewPos >= 0)
        LValue = AArray2.Substring(LLastPos + 1, LNewPos - LLastPos - 1);
      else
        LValue = AArray2.Substring(LLastPos + 1);

      if ((LValue.Length > 0) && (LAllFields || AArray1.IndexOf(ASeparator + LValue + ASeparator) >= 0))
        Result |= BitPos;

      LLastPos = LNewPos;
      BitPos <<= 1;
    }

    return Result;
  }
  
  [SqlFunction(DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public static SqlInt64 ArrayTest(SqlString AArray1, SqlString AArray2, Char ASeparator)
  {
    String
      LArray1      = (AArray1.IsNull ? null : AArray1.Value),
      LArray2      = (AArray2.IsNull ? null : AArray2.Value);

    return ArrayTestInternal(LArray1, LArray2, ASeparator.ToString());
  }

  [SqlFunction(DataAccess = DataAccessKind.None, IsDeterministic = false)]
  public static SqlInt64 ArrayTestNonDeterministic(SqlString AArray1, SqlString AArray2, Char ASeparator)
  {
    String
      LArray1      = (AArray1.IsNull ? null : AArray1.Value),
      LArray2      = (AArray2.IsNull ? null : AArray2.Value);

    return ArrayTestInternal(LArray1, LArray2, ASeparator.ToString());
  }

  [SqlFunction(DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public static SqlString CharSetJoin(SqlString AArray1, SqlString AArray2)
  {
    String
      LArray1      = (AArray1.IsNull ? null : AArray1.Value),
      LArray2      = (AArray2.IsNull ? null : AArray2.Value);

    if (
        String.IsNullOrEmpty(LArray1)
        || String.IsNullOrEmpty(LArray2)
        )
        return null;

    int I;
    String LChar;
    String Result = "";

    for(I = 0; I < LArray1.Length; I++)
    {
      LChar = LArray1.Substring(I, 1); 
      if(LArray2.IndexOf(LChar) >= 0)
        Result = Result + LChar;
    }

    if (Result.Length > 0)
      return new SqlString(Result.ToString());
    else
      return null;
  }

  [SqlFunction(DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public static SqlString ArraysAntiJoin(SqlString AArray1, SqlString AArray2, Char ASeparator)
  {
    String
      LArray1      = (AArray1.IsNull ? null : AArray1.Value),
      LArray2      = (AArray2.IsNull ? null : AArray2.Value);

    if (String.IsNullOrEmpty(LArray2) || LArray2.Equals(ASeparator))
      return AArray1;

		if (String.IsNullOrEmpty(LArray1) || LArray1.Equals(ASeparator) || LArray2.Equals("*"))
      return null;

    if (LArray1.Equals("*"))
      return AArray1;

    StringBuilder
			Result = new StringBuilder(LArray1.Length > LArray2.Length ? LArray1.Length : LArray2.Length);

    LArray2 = ASeparator + LArray2 + ASeparator;

    int
      LLastPos    = -1,
      LNewPos     = 0;

    String
      LValue;

    while (LNewPos >= 0)
    {
      LNewPos = LArray1.IndexOf(ASeparator, LLastPos + 1);
      if (LNewPos >= 0)
          LValue = LArray1.Substring(LLastPos + 1, LNewPos - LLastPos - 1);
      else
          LValue = LArray1.Substring(LLastPos + 1);

      if ((LValue.Length > 0) && (LArray2.IndexOf(ASeparator + LValue + ASeparator) < 0))
      {
          if (Result.Length > 0)
						Result.Append(ASeparator);
          Result.Append(LValue);
      }

      LLastPos = LNewPos;
    }

    if (Result.Length > 0)
      return new SqlString(Result.ToString());
    else
      return null;
  }

	[SqlFunction(Name = "Arrays Positive Join", DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public static Boolean ArraysPositiveJoin(SqlString AArray1, SqlString AArray2, Char ASeparator)
  {
    String
      LArray1      = (AArray1.IsNull ? null : AArray1.Value),
      LArray2      = (AArray2.IsNull ? null : AArray2.Value);

    if (
        String.IsNullOrEmpty(LArray1)
        || String.IsNullOrEmpty(LArray2)
        || LArray1.Equals(ASeparator)
        || LArray2.Equals(ASeparator)
        )
        return false;

    if (LArray1.Equals("*"))
      return true;
    if (LArray2.Equals("*"))
      return true;

    LArray2 = ASeparator + LArray2 + ASeparator;

    int
      LLastPos    = -1,
      LNewPos     = 0;

    String
      LValue;

    while (LNewPos >= 0)
    {
      LNewPos = LArray1.IndexOf(ASeparator, LLastPos + 1);
      if (LNewPos >= 0)
        LValue = LArray1.Substring(LLastPos + 1, LNewPos - LLastPos - 1);
      else
        LValue = LArray1.Substring(LLastPos + 1);

      if ((LValue.Length > 0) && (LArray2.IndexOf(ASeparator + LValue + ASeparator) >= 0))
				return true;

      LLastPos = LNewPos;
    }

    return false;
  }

	[SqlFunction(Name = "Arrays Fully Included", DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public static SqlBoolean ArraysFullyIncluded(SqlString AArray, SqlString ASubArray, Char ASeparator)
  {
    String
      LArray      = (AArray.IsNull ? null : AArray.Value),
      LSubArray   = (ASubArray.IsNull ? null : ASubArray.Value);

    if (String.IsNullOrEmpty(LSubArray) || LSubArray.Equals(ASeparator))
      return true;

    if (String.IsNullOrEmpty(LArray) || LArray.Equals(ASeparator))
      return false;

    if (LArray.Equals("*"))
      return new SqlBoolean(true);
    if (LSubArray.Equals("*"))
      return new SqlBoolean(false);

    LArray = ASeparator + LArray + ASeparator;
    foreach(String LItem in LSubArray.Split(new char[]{ASeparator}, StringSplitOptions.RemoveEmptyEntries))
    {
      if(LArray.IndexOf(ASeparator + LItem + ASeparator) == -1)
        return false;
    }

    return true;
  }
}

[Serializable]
[SqlUserDefinedAggregate(Format.UserDefined, IsInvariantToNulls = true, IsInvariantToDuplicates = false, IsInvariantToOrder = false, MaxByteSize = -1)]
public class MergeAgg : IBinarySerialize
{
  private String  OResult;
  private String  OSeparator;
	private Boolean OAny;

  public void Init()
  {
    OResult     = "";
    OSeparator  = "";
		OAny				= false;
  }

  public void Accumulate(SqlString AValue, Char ASeparator)
  {
    if (AValue.IsNull) 
      return;

    //string
    //  LSeparator = ASeparator.IsNull ? null : ASeparator.Value;

    //if (String.IsNullOrEmpty(LSeparator) || (LSeparator.Length != 1))
    //  throw new System.SystemException("Wrong value for parameter 'Separator'");

    if (OSeparator.Length == 0)
    {
      OSeparator = ASeparator.ToString();
      OResult    = OSeparator;
    }
    else if (OSeparator[0] != ASeparator)
      throw new System.SystemException("Parameter 'Separator' cannot be changed since it compiled.");

		if (!OAny)
			if (AValue.Value == "*")
			{
				OResult	= "";
				OAny		= true;
			}
			else
				Pub.InternalMerge(ref OResult, AValue.Value, OSeparator);
  }

  public void Merge(MergeAgg Other)
  {
    if (Other.OSeparator.Length == 0)
      return;

    if (OSeparator.Length == 0)
    {
      OSeparator = Other.OSeparator;
      OResult = Other.OResult;
    }
    else if (OSeparator != Other.OSeparator)
      throw new System.SystemException("Parameter 'Separator' cannot be changed since it compiled.");
    else if (!OAny)
			if (Other.OAny)
			{
				OResult	= "";
				OAny		= true;
			}
			else
				Pub.InternalMerge(ref OResult, Other.OResult, OSeparator);
  }

  public SqlString Terminate()
  {
    if (OAny)
      return "*";
    else if (OResult != null && OResult.Length > 2)
      return (SqlString)(OResult.Substring(1, OResult.Length - 2));
    else
      return null;
  }

  public void Read(BinaryReader r)
  {
    if (r == null) throw new ArgumentNullException("r");
		OAny = r.ReadBoolean();
    OResult = r.ReadString();
    OSeparator = r.ReadString();
  }

  public void Write(BinaryWriter w)
  {
    if (w == null) throw new ArgumentNullException("w");
		w.Write(OAny);
    w.Write(OResult);
    w.Write(OSeparator);
  }
}

[Serializable]
[SqlUserDefinedAggregate(Format.UserDefined, IsInvariantToNulls = true, IsInvariantToDuplicates = false, IsInvariantToOrder = false, MaxByteSize = -1)]
public class CharSetMergeAgg: IBinarySerialize
{
  private String  OResult;

  public void Init()
  {
    OResult = "";
  }

  public void Accumulate(SqlString AValue)
  {
    if (AValue.IsNull) 
      return;

    Pub.InternalCharSetMerge(ref OResult, AValue.Value);
  }

  public void Merge(CharSetMergeAgg Other)
  {
    Pub.InternalCharSetMerge(ref OResult, Other.OResult);
  }

  public SqlString Terminate()
  {
    if (OResult != null && OResult.Length > 0)
      return (SqlString)(OResult);
    else
      return null;
  }

  public void Read(BinaryReader r)
  {
    if (r == null) throw new ArgumentNullException("r");
    OResult = r.ReadString();
  }

  public void Write(BinaryWriter w)
  {
    if (w == null) throw new ArgumentNullException("w");
    w.Write(OResult);
  }
};