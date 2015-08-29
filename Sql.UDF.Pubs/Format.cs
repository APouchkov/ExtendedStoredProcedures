using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using System.Text;
using System.Collections;
using System.Globalization;

public partial class Pub
{
  /// <summary>
  /// Форматирует параметры
  /// </summary>            string pattern = @"(?<argname>/[^\s=]+)=(?<argvalue>.+)";
  /// 

  [SqlFunction(Name = "Format DateTime2", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  // RETURNS NULL ON NULL INPUT
  public static String FormatDateTime2(String AFormat, DateTime AValue)
  {
    return AValue.ToString(AFormat.Replace('m', 'M').Replace('n', 'm').Replace('h','H'), CultureInfo.InvariantCulture);
  }

  [SqlFunction(Name = "Format DateTime", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  // RETURNS NULL ON NULL INPUT
  public static String FormatDateTime(String AFormat, SqlDateTime AValue)
  {
    return FormatDateTime2(AFormat, AValue.Value);
  }

  [SqlFunction(Name = "Format DateTimeOffset", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  // RETURNS NULL ON NULL INPUT
  public static String FormatDateTimeOffset(String AFormat, DateTimeOffset AValue)
  {
    return AValue.ToString(AFormat.Replace('m', 'M').Replace('n', 'm').Replace('h', 'H'), CultureInfo.InvariantCulture);
  }

  [SqlFunction(Name = "Format String", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  // RETURNS NULL ON NULL INPUT
  public static String FormatString(Int16 AFormat, String AValue)
  {
    return AValue.PadRight(AFormat, ' ');
  }

  /* - very slow version
  public static string InternalFormatInteger(string AFormat, Int64 AValue)
  {
    int D;
    if ((AFormat != "") && (Int32.TryParse(AFormat, out D)))
      return AValue.ToString("D" + AFormat);
    else
    {
      NumberFormatInfo numberFormatInfo = new CultureInfo("en-US").NumberFormat;
      numberFormatInfo.NumberGroupSeparator = AFormat;
      return AValue.ToString("#,##0", numberFormatInfo);
    }
  } 
  */
  public static String InternalFormatInteger(String AFormat, Int64 AValue)
  {
    int D;
    if ((AFormat != "") && (Int32.TryParse(AFormat, out D)))
      return AValue.ToString().PadLeft(D, '0');
    else
    {
      StringBuilder sb = new StringBuilder(AValue.ToString());
      int Length = sb.Length;
      for (int I = (Length / 3 - 1) * 3 + (Length % 3); I > 0; I -= 3) sb.Insert(I, AFormat);
      return sb.ToString();
    }
  }

  [SqlFunction(Name = "Format Integer", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  // RETURNS NULL ON NULL INPUT
  public static String FormatInteger(String AFormat, Int64 AValue)
  {
    return InternalFormatInteger(AFormat, AValue);
  }

  [SqlFunction(Name = "Format Boolean", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  // RETURNS NULL ON NULL INPUT
  public static String FormatBoolean(String AFormat, Boolean AValue)
  {
    const Char CDelimiter = '/';
    String[] FalseTrue = AFormat.Split(CDelimiter);
    if (FalseTrue.Length != 2)
      throw new Exception("В формате Boolean ожидается два слова через разделитель \"/\", а передано: \"" + AFormat + "\"");

    return FalseTrue[Convert.ToByte(AValue)];
  }

  public static String InternalFormatDecimal(String AFormat, decimal AValue)
  {
    return InternalFormatDecimal(AFormat, AValue.ToString(), ".");
  }

  [SqlFunction(Name = "Format Decimal", DataAccess = DataAccessKind.None, IsDeterministic = true)]
  // RETURNS NULL ON NULL INPUT
  public static String FormatDecimal(String AFormat, SqlDecimal AValue)
  {
    return InternalFormatDecimal(AFormat, AValue.ToString(), ".");
  }

  public static String InternalFormatDecimal(String AFormat, String AValue, String ADecimalSeparator = "")
  {
    if (String.IsNullOrEmpty(AFormat))
      return AValue.ToString();
    else
    {
      StringBuilder FloatFormat = new StringBuilder();
      String LNumberGroupSeparator   = "";
      String LNumberDecimalSeparator = "";
      NumberFormatInfo formatprovider = System.Globalization.CultureInfo.CurrentCulture.NumberFormat;
      string LDecimalSeparator = (ADecimalSeparator == "" ? formatprovider.NumberDecimalSeparator : ADecimalSeparator);
      int RequiedDecimal = -1;
      int OptionalDecimal = -1;
      int Start = 0;
      if (!char.IsDigit(AFormat[0]))
      {
        if ((AFormat.Length > 1) && !char.IsDigit(AFormat[1]))
        {
          LNumberGroupSeparator = AFormat.Substring(0, 1);
          LNumberDecimalSeparator = AFormat.Substring(1, 1);
          Start = 2;
        }
        else
        {
          LNumberDecimalSeparator = AFormat.Substring(0, 1);
          Start = 1;
        }
      }

      string[] Numbers = AFormat.Substring(Start, AFormat.Length - Start).Split('-');
      if ((Numbers.Length != 0) && (Numbers[0] != ""))
      {
        if (!int.TryParse(Numbers[0], out RequiedDecimal))
          throw new Exception("Ожидалось количество знаков после точки а встретили " + Numbers[0]);
        if (Numbers.Length > 1)
        {
          if (!int.TryParse(Numbers[1], out OptionalDecimal))
            throw new Exception("Ожидалось количество знаков после точки а встретили " + Numbers[0]);
        }
      }
      string result = AValue.ToString().TrimEnd('0');
      StringBuilder sb = new StringBuilder(result);
      int DecimalPoint = result.IndexOf(LDecimalSeparator);
      if (DecimalPoint < 0)
      {
        sb.Append(LDecimalSeparator);
        DecimalPoint = result.Length;
      }
      if (RequiedDecimal >= 0)
      {
        if ((result.Length - DecimalPoint) > ((OptionalDecimal < 0) ? RequiedDecimal : OptionalDecimal))
          sb.Length = DecimalPoint + ((OptionalDecimal < 0) ? RequiedDecimal : OptionalDecimal) + 1;
        for (int I = result.Length - DecimalPoint; I <= RequiedDecimal; I++) sb.Append('0');
      }
      if (sb[sb.Length - 1] == LDecimalSeparator[0])
      {
        sb.Remove(sb.Length - 1, 1);
      }
      else
        if (LNumberDecimalSeparator != "")
          sb.Replace(LDecimalSeparator, LNumberDecimalSeparator);
      if (LNumberGroupSeparator != "")
        for (int I = (DecimalPoint / 3 - 1) * 3 + (DecimalPoint % 3); I > 0; I -= 3) sb.Insert(I, LNumberGroupSeparator);

      return sb.ToString();
    }
  }
}
