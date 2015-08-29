using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using System.Text;
using System.IO;
using System.Collections;
using System.Globalization;

/// <summary>
/// Собирает строку из строк с разделителем
/// </summary>
[Serializable]
[SqlUserDefinedAggregate(Format.UserDefined, IsInvariantToNulls = true, IsInvariantToDuplicates = false, IsInvariantToOrder = false, MaxByteSize = -1)]
public class Concat : IBinarySerialize
{
  private StringBuilder intermediateResult;
  private String separatorResult;

  public void Init()
  {
    intermediateResult = new StringBuilder();
    separatorResult = "";
  }

  public void Accumulate(SqlString value, SqlString separator)
  {
    if (value.IsNull || separator.IsNull) return;
    if (intermediateResult.Length == 0)
      intermediateResult.Append(value.Value);
    else
      intermediateResult.Append(value.ToString().Insert(0, separator.Value));
    separatorResult = separator.Value;
  }

  public void Merge(Concat other)
  {
    separatorResult = other.separatorResult;
    if (intermediateResult.Length == 0 || separatorResult == String.Empty)
      intermediateResult.Append(other.intermediateResult);
    else
      intermediateResult.Append(other.intermediateResult.Insert(0, separatorResult));
  }

  public SqlString Terminate()
  {
    if (intermediateResult != null && intermediateResult.Length > 0)
      return (SqlString)(intermediateResult.ToString(0, intermediateResult.Length));
    else
      return null;
  }

  public void Read(BinaryReader r)
  {
    if (r == null) throw new ArgumentNullException("r");
    intermediateResult = new StringBuilder(r.ReadString());
    separatorResult = r.ReadString();
  }

  public void Write(BinaryWriter w)
  {
    if (w == null) throw new ArgumentNullException("w");
    w.Write(intermediateResult.ToString());
    w.Write(separatorResult);
  }
}


public partial class Pub
{
  /// <summary>
  /// Форматирует параметры
  /// </summary>            string pattern = @"(?<argname>/[^\s=]+)=(?<argvalue>.+)";
  public static string InternalFormatDateTime(string AFormat, object value)
  {
    if (value is SqlDateTime)
      return (((SqlDateTime)value).Value.ToString(AFormat.Replace("mm", "MM").Replace("nn", "mm")));
    else
      return (String.Format("{0,-" + AFormat.Replace("mm", "MM").Replace("nn", "mm") + "}", value.ToString()));
  }

  public static string InternalFormatString(string AFormat, object value)
  {
    return String.Format("{0,-" + AFormat + "}", value);
  }

  public static string InternalFormatInteger(string AFormat, object value)
  {
    int ri;
    if (int.TryParse(value.ToString(), out ri))
      return ri.ToString("D" + AFormat);
    else
      return value.ToString();
  }
  public static string InternalFormatHex(string AFormat, object value)
  {
    int rh;
    if (int.TryParse(value.ToString(), out rh))
      return rh.ToString("X" + AFormat);
    else
      return value.ToString();
  }

  public static string InternalFormatFloat(string AFormat, object value)
  {
    if ((AFormat == null) || (AFormat == ""))
      return value.ToString();
    else
    {
      NumberFormatInfo numberFormatInfo = new CultureInfo("en-US").NumberFormat;
      StringBuilder FloatFormat = new StringBuilder();
      int Start = 0;
      if (!char.IsDigit(AFormat[0]))
      {
        if ((AFormat.Length > 1) && !char.IsDigit(AFormat[1]))
        {
          numberFormatInfo.NumberGroupSeparator = AFormat.Substring(0, 1);
          numberFormatInfo.NumberDecimalSeparator = AFormat.Substring(1, 1);
          Start = 2;
          FloatFormat.Append("#,##0.");
        }
        else
        {
          numberFormatInfo.NumberDecimalSeparator = AFormat.Substring(0, 1);
          Start = 1;
          FloatFormat.Append("0.");
          FloatFormat.Append(AFormat[0]);
        }
      }
      else
        FloatFormat.Append("0.");

      string[] Numbers = AFormat.Substring(Start, AFormat.Length - Start).Split('-');
      if (Numbers.Length == 0)
        FloatFormat.Append("#########################");
      else
      {
        int RequiedDecimal = 0;
        if (int.TryParse(Numbers[0], out RequiedDecimal))
          FloatFormat.Append(new String('0', RequiedDecimal));
        if (Numbers.Length > 1)
        {
          int OptionalDecimal = 0;
          if (int.TryParse(Numbers[1], out OptionalDecimal))
            FloatFormat.Append(new String('#', OptionalDecimal));
        }
      }
      //IFormatProvider formatprovider = System.Globalization.CultureInfo.InvariantCulture;
      decimal rd;
      if (decimal.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out rd))
        return (rd.ToString(FloatFormat.ToString(), numberFormatInfo));
      else
        return (value.ToString() + "--Error Parsing Decimal Value--");
    }
  }
}



public partial class Pub
{
    /// <summary>
    /// Разбивает строку на подстроки по делителю
    /// </summary>
    [SqlFunction(FillRowMethodName = "SplitRow", DataAccess = DataAccessKind.None,  TableDefinition = "value nvarchar(max)",  IsDeterministic = true)]
    public static IEnumerable Split(SqlString Text, SqlChars Separator)
    {
        if (Text.IsNull || (Text.Value.ToString() == "")) return null;
        if (Separator.IsNull || (Separator.Value.ToString() == ""))
            return Text.Value.Split(Separator.Value, 1);
        return Text.Value.Split(Separator.Value);
    }

    public static void SplitRow(object row, out SqlString value)
    {
        value = (SqlString)row.ToString();
    }
    /// <summary>
    /// Форматирует строку
    /// </summary>
    [SqlFunction(FillRowMethodName = "Format DateTime", DataAccess = DataAccessKind.None, TableDefinition = "value nvarchar(max)", IsDeterministic = true)]
    public static SqlString FormatDateTime(SqlString AFormat, SqlDateTime ADateTime)
    {
      return new SqlString(InternalFormatDateTime(AFormat.Value, ADateTime));
    }

    [SqlFunction(FillRowMethodName = "Format String", DataAccess = DataAccessKind.None, TableDefinition = "value nvarchar(max)", IsDeterministic = true)]
    public static SqlString FormatString(SqlString AFormat, SqlString AString)
    {
      return new SqlString(InternalFormatString(AFormat.Value, AString));
    }

    [SqlFunction(FillRowMethodName = "Format Integer", DataAccess = DataAccessKind.None, TableDefinition = "value nvarchar(max)", IsDeterministic = true)]
    public static SqlString FormatInteger(SqlString AFormat, SqlInt64 AInt64)
    {
      return new SqlString(InternalFormatInteger(AFormat.Value, AInt64));
    }

    [SqlFunction(FillRowMethodName = "Format Hex", DataAccess = DataAccessKind.None, TableDefinition = "value nvarchar(max)", IsDeterministic = true)]
    public static SqlString FormatHex(SqlString AFormat, SqlInt64 AInt64)
    {
      return new SqlString(InternalFormatHex(AFormat.Value, AInt64));
    }

    [SqlFunction(FillRowMethodName = "Format Float", DataAccess = DataAccessKind.None, TableDefinition = "value nvarchar(max)", IsDeterministic = true)]
    public static SqlString FormatFloat(SqlString AFormat, SqlDecimal ADecimal)
    {
      return new SqlString(InternalFormatFloat(AFormat.Value, ADecimal));
    }
}