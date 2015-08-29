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
  static String NullIfEmpty(SqlString Value)
  {
    return Value.IsNull ? null : NullIfEmpty(Value.Value);
  }

  /// Разбивает массив на элементы по делителю
  [SqlFunction(Name = "Repeat", FillRowMethodName = "RepeatRow", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, TableDefinition = "[Index] int", IsDeterministic = true)]
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
  [SqlFunction(Name = "ForeachChar", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, TableDefinition = "[Char] nchar(1), [Index] smallint", IsDeterministic = true, FillRowMethodName = "StringToCharSetRow")]
  public static IEnumerable ForeachChar(String AText)
  {
    if (String.IsNullOrEmpty(AText)) yield break;

    CharRow LRow;
    for (LRow.Index = 0; LRow.Index < AText.Length;)
    {
      LRow.Char  = AText[LRow.Index];
      LRow.Index++;

      yield return LRow;
    }
  }

  public static void StringToCharSetRow(Object ARow, out Char AChar, out Int16 AIndex)
  {
    AChar  = ((CharRow)ARow).Char;
    AIndex = ((CharRow)ARow).Index;
  }

  struct StringRow
  {
    public Int32  Index;
    public String Value;
  }

  /// Разбивает массив на элементы по делителю
  [SqlFunction(FillRowMethodName = "ArrayToRowSetRow", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, TableDefinition = "[Value] nvarchar(max), [Index] int", IsDeterministic = true)]
  public static IEnumerable ArrayToRowSet(String AText, Char ASeparator)
  {
    if (String.IsNullOrEmpty(AText)) yield break;
    List<String> LUnique = new List<String>();

    StringRow LRow;
    LRow.Index = 1;
    foreach (String LLine in AText.Split(new char[]{ASeparator})) // StringSplitOptions.RemoveEmptyEntries
    {
      if (LLine.Length == 0)
      {
        LRow.Value = null;
      }
      else
        LRow.Value = LLine;
      String LULine = LRow.Value.ToUpper();

      if (LUnique.IndexOf(LULine) >= 0)
        if (AText.Length > 900)
          throw new System.SystemException("Doublicate id = '" + LLine + "' in array");
        else
          throw new System.SystemException("Doublicate id = '" + LLine + "' in array: " + AText);

      yield return LRow;
      LUnique.Add(LULine);
      LRow.Index++;
    }
  }

  public static void ArrayToRowSetRow(object ARow, out String AValue, out Int32 AIndex)
  {
    AValue = ((StringRow)ARow).Value;
    AIndex = ((StringRow)ARow).Index;
  }

  struct StringRowNamed
  {
    public String  Name;
    public String  Value;
    public Int32   Index;
  }

  /// Разбивает массив на элементы (имя, значение) по делителю
  [SqlFunction(FillRowMethodName = "ArrayToRowSetNamedRow", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, TableDefinition = "[Name] nvarchar(4000), [Value] nvarchar(max), [Index] int", IsDeterministic = true)]
  public static IEnumerable ArrayToRowSetNamed(String AText, Char ASeparator)
  {
    if (String.IsNullOrEmpty(AText)) yield break;
    List<String> LUnique = new List<String>();

    StringRowNamed LRow;
	  LRow.Index = 1;
    foreach (String LLine in AText.Split(new char[]{ASeparator}, StringSplitOptions.RemoveEmptyEntries))
    {
      String[] LSubLines = LLine.Split(new char[1]{'='}, 2);
      if (LSubLines.Length < 1) continue;

      LRow.Name  = LSubLines[0];
      String LName = LRow.Name.ToUpper();

			if (LSubLines.Length == 1 || LSubLines[1] == "")
        LRow.Value = null;
      else
        LRow.Value = LSubLines[1];

      if (LUnique.IndexOf(LName) >= 0)
        if (AText.Length > 900)
          throw new System.SystemException("Doublicate id = '" + LRow.Name + "' in array");
        else
          throw new System.SystemException("Doublicate id = '" + LRow.Name + "' in array: " + AText);

      yield return LRow;
      LUnique.Add(LName);
      LRow.Index++;
    }
  }

  public static void ArrayToRowSetNamedRow(Object ARow, out String AName, out String AValue, out Int32 AIndex)
  {
    AName   = ((StringRowNamed)ARow).Name;
    AValue  = ((StringRowNamed)ARow).Value;
    AIndex  = ((StringRowNamed)ARow).Index;
  }

  struct StringRowNamed2
  {
    public String  Name;
    public String  Value1;
    public String  Value2;
    public Int32   Index;
  }

  /// Разбивает массив на элементы (имя, значение1, значение2) по делителю
  [SqlFunction(FillRowMethodName = "ArrayToRowSetNamedRow2", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, TableDefinition = "[Name] nvarchar(4000), [Value:1] nvarchar(max), [Value:2] nvarchar(max), [Index] int", IsDeterministic = true)]
  public static IEnumerable ArrayToRowSetNamed2(String AText, Char ASeparator, Char AValueSeparator)
  {
    if (String.IsNullOrEmpty(AText)) yield break;
    List<String> LUnique = new List<String>();

    StringRowNamed2 LRow;
    LRow.Index = 1;
    foreach (String LLine in AText.Split(new char[]{ASeparator}, StringSplitOptions.RemoveEmptyEntries))
    {
      String[] LNames = LLine.Split(new Char[1]{'='}, 2);
      if (LNames.Length < 2) continue;
      
      LRow.Name = LNames[0];
      String LName = LRow.Name.ToUpper(); 

      String[] LValues = LNames[1].Split(new Char[1]{AValueSeparator}, 2);

      if (LValues.Length == 0 || LValues[0].Length == 0)
        LRow.Value1 = null;
      else
        LRow.Value1 = LValues[0];

      if (LValues.Length <= 1 || LValues[1].Length == 0)
        LRow.Value2 = null;
      else
        LRow.Value2 = LValues[1];

      if (LUnique.IndexOf(LName) >= 0)
        if (AText.Length > 900)
          throw new System.SystemException("Doublicate id = '" + LRow.Name + "' in array");
        else
          throw new System.SystemException("Doublicate id = '" + LRow.Name + "' in array: " + AText);

      yield return LRow;
      LUnique.Add(LName);
      LRow.Index++;
    }
  }

  public static void ArrayToRowSetNamedRow2(Object ARow, out String AName, out String AValue1, out String AValue2, out Int32 AIndex)
  {
      AName   = ((StringRowNamed2)ARow).Name;
      AValue1 = ((StringRowNamed2)ARow).Value1;
      AValue2 = ((StringRowNamed2)ARow).Value2;
      AIndex  = ((StringRowNamed2)ARow).Index;
  }

  struct StringRowNamed3
  {
    public String  Name;
    public String  Value1;
    public String  Value2;
    public String  Value3;
    public Int32   Index;
  }

  /// Разбивает массив на элементы (имя, значение1, значение2, значение3) по делителю
  [SqlFunction(FillRowMethodName = "ArrayToRowSetNamedRow3", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, TableDefinition = "[id] nvarchar(max), [value1] nvarchar(max), [value2] nvarchar(max), [value3] nvarchar(max), [sort_id] int", IsDeterministic = true)]
  public static IEnumerable ArrayToRowSetNamed3(String AText, Char ASeparator, Char AValueSeparator)
  {
    if (String.IsNullOrEmpty(AText)) yield break;
    List<String> LUnique = new List<String>();

    StringRowNamed3 LRow;
    LRow.Index = 1;
    foreach (String LLine in AText.Split(new char[]{ASeparator}, StringSplitOptions.RemoveEmptyEntries))
    {
      String[] LNames = LLine.Split(new Char[1]{'='}, 2);
      if (LNames.Length < 2) continue;
      LRow.Name = LNames[0];
      String LName = LRow.Name.ToUpper();

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

      if (LUnique.IndexOf(LName) >= 0)
        if (AText.Length > 1000)
          throw new System.SystemException("Doublicate id = '" + LRow.Name + "' in array");
        else
          throw new System.SystemException("Doublicate id = '" + LRow.Name + "' in array: " + AText);

      yield return LRow;
      LUnique.Add(LName);
      LRow.Index++;
    }
  }

  public static void ArrayToRowSetNamedRow3(object ARow, out String AName, out String AValue1, out String AValue2, out String AValue3, out Int32 AIndex)
  {
    AName   = ((StringRowNamed3)ARow).Name;
    AValue1 = ((StringRowNamed3)ARow).Value1;
    AValue2 = ((StringRowNamed3)ARow).Value2;
    AValue3 = ((StringRowNamed3)ARow).Value3;
    AIndex  = ((StringRowNamed3)ARow).Index;
  }

  /// Возвращает параметр по индексу из списка параметров
  [SqlFunction(Name = "Extract Value", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  // WITH RETURNS NULL ON NULL INPUT
  public static String ExtractValue(String AText, Int32 AIndex, Char ASeparator)
  {
    if (AIndex <= 0 || String.IsNullOrEmpty(AText)) return null;
    foreach(String LLine in AText.Split(ASeparator))
    {
      if(AIndex == 1)
        return LLine;
      AIndex--;
    }

    return null;
  }

  /// Возвращает параметр по имени из списка параметров
  [SqlFunction(DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  public static String ExtractNamedValue(String AText, String AName, Char ASeparator)
  {
    if (String.IsNullOrEmpty(AText)) return null;

    AName = AName.ToUpper();
//    int i = 0;

    foreach (String LLine in AText.Split(new char[]{ASeparator}, StringSplitOptions.RemoveEmptyEntries))
    {
      String[] LSubLines = LLine.Split("=".ToCharArray(), 2);
      if (LSubLines[0].ToUpper() == AName) return LSubLines[1];
//      i++;
    }

    return null;
  }

  /// Добавляет имя со значением в конец строки, если имя отсутсвует
  [SqlFunction(Name = "SetNamedValue", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  public static String SetNamedValue(String AText, String AName, String AValue, Char ASeparator)
  {
    const String EQUALS = "=";

    //if (Separator.IsNull || (Separator.Length != 1))
    //    throw new System.SystemException("Wrong value for parameter 'Separator'");

    //String
    //  LSeparator      = ASeparator.ToString();

    // Если передан пустой текст, или не передан разделитель - возвращаем "имя=значение"
    if (String.IsNullOrEmpty(AText))
      return AName + EQUALS + AValue;

    int LPos = (ASeparator + AText).IndexOf(ASeparator + AName + EQUALS, StringComparison.InvariantCultureIgnoreCase);
    if (LPos >= 0)
    {
      StringBuilder LBuilder = new StringBuilder(AText.Substring(0, LPos + AName.Length + EQUALS.Length), AText.Length + AName.Length + AValue.Length);
      LBuilder.Append(AValue);
      LPos = AText.IndexOf(ASeparator, LPos + AName.Length + EQUALS.Length);

      if (LPos >= 0)
      {
        LBuilder.Append(ASeparator);
        LBuilder.Append(AText.Substring(LPos + 1));
      }

      return LBuilder.ToString();
    }
    else
      return AText + ASeparator + AName + EQUALS + AValue;
  }

  [SqlFunction(Name = "InArray", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  public static Boolean InArray(String AArray, String AElement, Char ASeparator)
  {
    //String
    //  LArray       = NullIfEmpty(AArray),
    //  LElement     = NullIfEmpty(AElement);

		if (String.IsNullOrEmpty(AArray) || String.IsNullOrEmpty(AElement) || AArray.Equals(ASeparator))
			return false;

    if (AArray.Equals("*"))
      return true;

    if (AArray[0] != ASeparator) AArray = ASeparator + AArray;
    if (AArray[AArray.Length - 1] != ASeparator) AArray += ASeparator;
    return (AArray.IndexOf(ASeparator + AElement + ASeparator) >= 0);
  }

	[SqlFunction(Name = "Arrays Merge", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  public static String ArraysMerge(String AArray1, String AArray2, Char ASeparator)
  {
    //String
    //  LArray1       = NullIfEmpty(AArray1),
    //  LArray2       = NullIfEmpty(AArray2);

		if (String.IsNullOrEmpty(AArray2) || AArray2.Equals(ASeparator))
			return AArray1;

    if (String.IsNullOrEmpty(AArray1) || AArray1.Equals(ASeparator))
      return AArray2;

    if (AArray1.Equals("*") || AArray2.Equals("*"))
      return "*";

    if (AArray1[0] != ASeparator)
      AArray1 = ASeparator + AArray1;

    if (AArray1[AArray1.Length - 1] != ASeparator)
      AArray1 += ASeparator;

    InternalMerge(ref AArray1, AArray2, ASeparator.ToString());
    return AArray1.Substring(1, AArray1.Length - 2);
  }

	[SqlFunction(Name = "Arrays Merge(*)", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  public static String ArraysMergeLimited(String AArray1, String AArray2, Char ASeparator, Int16 AMaxLength)
  {
    AArray1 = ArraysMerge(AArray1, AArray2, ASeparator);
    if(AArray1 != null && AArray1.Length > AMaxLength)
      return "*";
    else
      return AArray1;
  }

	[SqlFunction(Name = "CharSetMerge", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  public static String CharSetMerge(String AArray1, String AArray2)
  {
    //String
    //  LArray1       = NullIfEmpty(AArray1),
    //  LArray2       = NullIfEmpty(AArray2);

		if (String.IsNullOrEmpty(AArray2))
			return AArray1;

    if (String.IsNullOrEmpty(AArray1))
      return AArray2;

    InternalCharSetMerge(ref AArray1, AArray2);
    return AArray1;
  }

  [SqlFunction(Name = "ArraysJoin", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  // RETURNS NULL ON NULL INPUT
  public static String ArraysJoin(String AArray1, String AArray2, Char ASeparator)
  {
    //String
    //  LArray1      = (AArray1.IsNull ? null : NullIfEmpty(AArray1.Value)),
    //  LArray2      = (AArray2.IsNull ? null : NullIfEmpty(AArray2.Value));

    if (String.IsNullOrEmpty(AArray1) || String.IsNullOrEmpty(AArray2)
        ||
        AArray1.Equals(ASeparator) || AArray2.Equals(ASeparator)
       )
        return null;

    if (AArray1.Equals("*"))
      return AArray2;
    if (AArray2.Equals("*"))
      return AArray1;

    StringBuilder
      Result = new StringBuilder(AArray1.Length + AArray2.Length);

    AArray2 = ASeparator + AArray2 + ASeparator;

    int
      LLastPos    = -1,
      LNewPos     = 0;

    String
      LValue;

    while (LNewPos >= 0)
    {
      LNewPos = AArray1.IndexOf(ASeparator, LLastPos + 1);
      if (LNewPos >= 0)
        LValue = AArray1.Substring(LLastPos + 1, LNewPos - LLastPos - 1);
      else
        LValue = AArray1.Substring(LLastPos + 1);

      if ((LValue.Length > 0) && (AArray2.IndexOf(ASeparator + LValue + ASeparator) >= 0))
      {
        if (Result.Length > 0)
					Result.Append(ASeparator);
        Result.Append(LValue);
      }

      LLastPos = LNewPos;
    }

    if (Result.Length > 0)
      return Result.ToString();
    else
      return null;
  }


  public static Int64 ArrayTestInternal(String AArray1, String AArray2, String ASeparator)
  {
    if (
        String.IsNullOrEmpty(AArray1) || String.IsNullOrEmpty(AArray2)
        || AArray1.Equals(ASeparator) || AArray2.Equals(ASeparator)
       )
        return 0;

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
  
  [SqlFunction(Name = "ArrayTest", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  public static SqlInt64 ArrayTest(String AArray1, String AArray2, Char ASeparator)
  {
    //String
    //  LArray1      = (AArray1.IsNull ? null : AArray1.Value),
    //  LArray2      = (AArray2.IsNull ? null : AArray2.Value);

    return ArrayTestInternal(AArray1, AArray2, ASeparator.ToString());
  }

  [SqlFunction(Name = "ArrayTestNonDeterministic", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = false)]
  public static SqlInt64 ArrayTestNonDeterministic(String AArray1, String AArray2, Char ASeparator)
  {
    //String
    //  LArray1      = (AArray1.IsNull ? null : AArray1.Value),
    //  LArray2      = (AArray2.IsNull ? null : AArray2.Value);

    return ArrayTestInternal(AArray1, AArray2, ASeparator.ToString());
  }

  [SqlFunction(Name = "CharSetJoin", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  public static String CharSetJoin(String AArray1, String AArray2)
  {
    //String
    //  LArray1      = (AArray1.IsNull ? null : AArray1.Value),
    //  LArray2      = (AArray2.IsNull ? null : AArray2.Value);

    if (String.IsNullOrEmpty(AArray1) || String.IsNullOrEmpty(AArray2))
      return null;

    int I;
    Char LChar;
    String Result = "";

    for(I = 0; I < AArray1.Length; I++)
    {
      LChar = AArray1[I];
      
      if(AArray2.IndexOf(LChar) >= 0)
        Result = Result + LChar;
    }

    if (Result.Length > 0)
      return Result;
    else
      return null;
  }

  [SqlFunction(Name = "ArraysAntiJoin", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  public static String ArraysAntiJoin(String AArray1, String AArray2, Char ASeparator)
  {
    //String
    //  LArray1      = (AArray1.IsNull ? null : AArray1.Value),
    //  LArray2      = (AArray2.IsNull ? null : AArray2.Value);

    if (String.IsNullOrEmpty(AArray2) || AArray2.Equals(ASeparator))
      return AArray1;

		if (String.IsNullOrEmpty(AArray1) || AArray1.Equals(ASeparator) || AArray2.Equals("*"))
      return null;

    if (AArray1.Equals("*"))
      return AArray1;

    StringBuilder
			Result = new StringBuilder(AArray1.Length > AArray2.Length ? AArray1.Length : AArray2.Length);

    AArray2 = ASeparator + AArray2 + ASeparator;

    int
      LLastPos    = -1,
      LNewPos     = 0;

    String
      LValue;

    while (LNewPos >= 0)
    {
      LNewPos = AArray1.IndexOf(ASeparator, LLastPos + 1);
      if (LNewPos >= 0)
          LValue = AArray1.Substring(LLastPos + 1, LNewPos - LLastPos - 1);
      else
          LValue = AArray1.Substring(LLastPos + 1);

      if ((LValue.Length > 0) && (AArray2.IndexOf(ASeparator + LValue + ASeparator) < 0))
      {
          if (Result.Length > 0)
						Result.Append(ASeparator);
          Result.Append(LValue);
      }

      LLastPos = LNewPos;
    }

    if (Result.Length > 0)
      return Result.ToString();
    else
      return null;
  }

	[SqlFunction(Name = "Arrays Positive Join", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  public static Boolean ArraysPositiveJoin(String AArray1, String AArray2, Char ASeparator)
  {
    //String
    //  LArray1      = (AArray1.IsNull ? null : AArray1.Value),
    //  LArray2      = (AArray2.IsNull ? null : AArray2.Value);

    if (
        String.IsNullOrEmpty(AArray1) || String.IsNullOrEmpty(AArray2)
        ||
        AArray1.Equals(ASeparator) || AArray2.Equals(ASeparator)
       )
        return false;

    if (AArray1.Equals("*"))
      return true;
    if (AArray2.Equals("*"))
      return true;

    AArray2 = ASeparator + AArray2 + ASeparator;

    int
      LLastPos    = -1,
      LNewPos     = 0;

    String
      LValue;

    while (LNewPos >= 0)
    {
      LNewPos = AArray1.IndexOf(ASeparator, LLastPos + 1);
      if (LNewPos >= 0)
        LValue = AArray1.Substring(LLastPos + 1, LNewPos - LLastPos - 1);
      else
        LValue = AArray1.Substring(LLastPos + 1);

      if ((LValue.Length > 0) && (AArray2.IndexOf(ASeparator + LValue + ASeparator) >= 0))
				return true;

      LLastPos = LNewPos;
    }

    return false;
  }

	[SqlFunction(Name = "Arrays Fully Included", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  public static SqlBoolean ArraysFullyIncluded(String AArray, String ASubArray, Char ASeparator)
  {
    //String
    //  LArray      = (AArray.IsNull ? null : AArray.Value),
    //  LSubArray   = (ASubArray.IsNull ? null : ASubArray.Value);

    if (String.IsNullOrEmpty(ASubArray) || ASubArray.Equals(ASeparator))
      return true;

    if (String.IsNullOrEmpty(AArray) || AArray.Equals(ASeparator))
      return false;

    if (AArray.Equals("*"))
      return new SqlBoolean(true);
    if (ASubArray.Equals("*"))
      return new SqlBoolean(false);

    AArray = ASeparator + AArray + ASeparator;
    foreach(String LItem in ASubArray.Split(new char[]{ASeparator}, StringSplitOptions.RemoveEmptyEntries))
    {
      if(AArray.IndexOf(ASeparator + LItem + ASeparator) == -1)
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

  public void Accumulate(String AValue, Char ASeparator)
  {
    if (AValue == null) 
      return;

    if (OSeparator.Length == 0)
    {
      OSeparator = ASeparator.ToString();
      OResult    = OSeparator;
    }
    else if (OSeparator[0] != ASeparator)
      throw new Exception("Parameter 'Separator' cannot be changed since it compiled.");

		if (!OAny)
			if (AValue == "*")
			{
				OResult	= "";
				OAny		= true;
			}
			else
				Pub.InternalMerge(ref OResult, AValue, OSeparator);
  }

  public void Merge(MergeAgg AOther)
  {
    if (AOther.OSeparator.Length == 0)
      return;

    if (OSeparator.Length == 0)
    {
      OSeparator = AOther.OSeparator;
      OResult    = AOther.OResult;
    }
    else if (OSeparator != AOther.OSeparator)
      throw new Exception("Parameter 'Separator' cannot be changed since it compiled.");
    else if (!OAny)
			if (AOther.OAny)
			{
				OResult	= "";
				OAny		= true;
			}
			else
				Pub.InternalMerge(ref OResult, AOther.OResult, OSeparator);
  }

  public SqlString Terminate()
  {
    if (OAny)
      return "*";
    else if (OResult != null && OResult.Length > 2)
      return OResult.Substring(1, OResult.Length - 2);
    else
      return null;
  }

  public void Read(BinaryReader r)
  {
    //if (r == null) throw new ArgumentNullException("r");
		OAny       = r.ReadBoolean();
    if(OAny)
    {
      OResult    = "";
      OSeparator = "";
    }
    else
    { 
      OSeparator = r.ReadChar().ToString();
      if(OSeparator[0] != '\0')
        OResult = r.ReadString();
    }
  }

  public void Write(BinaryWriter w)
  {
    //if (w == null) throw new ArgumentNullException("w");
		w.Write(OAny);
    if(!OAny)
    { 
      if(OSeparator.Length == 0)
        w.Write('\0');
      else
      {
        w.Write(OSeparator[0]);
        w.Write(OResult);
      }
    }
  }
}

[Serializable]
[SqlUserDefinedAggregate(Format.UserDefined, IsInvariantToNulls = true, IsInvariantToDuplicates = false, IsInvariantToOrder = false, MaxByteSize = -1)]
public class CharSetMergeAgg: IBinarySerialize
{
  private String OResult;

  public void Init()
  {
    OResult = "";
  }

  public void Accumulate(String AValue)
  {
    if (AValue == null) 
      return;

    Pub.InternalCharSetMerge(ref OResult, AValue);
  }

  public void Merge(CharSetMergeAgg AOther)
  {
    Pub.InternalCharSetMerge(ref OResult, AOther.OResult);
  }

  public SqlString Terminate()
  {
    if (OResult != null && OResult.Length > 0)
      return OResult;
    else
      return null;
  }

  public void Read(BinaryReader r)
  {
    //if (r == null) throw new ArgumentNullException("r");
    OResult = r.ReadString();
  }

  public void Write(BinaryWriter w)
  {
    //if (w == null) throw new ArgumentNullException("w");
    w.Write(OResult);
  }
};