using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Text.RegularExpressions;
using Microsoft.SqlServer.Server;
using System.Collections;

namespace UDF.RegEx
{
  public partial class ScalarFunctions
  {
    public static RegexOptions RegexOptionsParser(String AOptions)
    {
      RegexOptions LResult = RegexOptions.None;

      // Если параметры не переданы, то ничего не парсим
      if (!String.IsNullOrEmpty(AOptions))
        foreach (String LItem in AOptions.Split(','))
        {
          try
          {
            LResult |= (RegexOptions)Enum.Parse(typeof(RegexOptions), LItem, true);
          }
          catch (ArgumentException)
          {
            throw new Exception("Неизвестное значение перечисления RegexOptions: '" + LItem + '\'');
          }
        }

      return LResult;
    }

    /// <summary>
    /// Проверяет соответствует ли строка шаблону поиска
    /// </summary>
    /// <param name="Input">Исходная строка в которой производится поиск</param>
    /// <param name="Pattern">Регулярный шаблон для поиска</param>
    /// <param name="Options">Текстовые параметры через запятую Regular Expression Options http://msdn.microsoft.com/en-us/library/yd1hzczs </param>
    /// <returns>True-строка соответствует шаблону поиска, False-не соответствует</returns>
    [Microsoft.SqlServer.Server.SqlFunction(IsDeterministic = true, IsPrecise = true)]
    public static Boolean RegExIsMatch(String AText, String APattern, String AOptions)
    {
      if (AText == null)
        return false;

      if (String.IsNullOrEmpty(APattern))
          throw new Exception("Параметр \"Pattern\" не может быть NULL или Empty.");

      return Regex.IsMatch(AText, APattern, RegexOptionsParser(AOptions));
    }

    /// <summary>
    /// Ищет в строке подстроку по шаблону и заменяет её
    /// </summary>
    /// <param name="Input">Исходная строка в которой производится поиск</param>
    /// <param name="Pattern">Регулярный шаблон для поиска</param>
    /// <param name="Replacement">Подстрока которая заменить найденный фрагмент текста</param>
    /// <param name="Options">Текстовые параметры через запятую Regular Expression Options http://msdn.microsoft.com/en-us/library/yd1hzczs </param>
    /// <returns>Строка с заменёнными символами</returns>
    [Microsoft.SqlServer.Server.SqlFunction(IsDeterministic = true, IsPrecise = true)]
    public static String RegExReplace(String AText, String APattern, String AReplacement, String AOptions)
    {
      if (AText == null)
        return null;

      if (String.IsNullOrEmpty(APattern))
          throw new Exception("Параметр \"Pattern\" не может быть NULL или Empty.");

      return Regex.Replace(AText, APattern, AReplacement, RegexOptionsParser(AOptions));
    }
	};

	public class TableValuedFunctions
	{
		/// <summary>
		/// Разбивает входную строку по регулярному шаблону и каждое совпадение представляется строкой таблицы
		/// </summary>
		/// <param name="Input">Исходная строка в которой производится поиск</param>
		/// <param name="Pattern">Регулярный шаблон для поиска</param>
		/// <param name="Options">Текстовые параметры через запятую Regular Expression Options http://msdn.microsoft.com/en-us/library/yd1hzczs </param>
		/// <returns>Таблица со значениями соответствующими шаблону поиска</returns>
		struct RegExMatchRow
		{
			public Int16	MatchIndex;
			public Byte		GroupIndex;
			public String	Match;
      public Int32  Index;
      public Int32  Length;
		}

		[
			SqlFunction
			(
				FillRowMethodName = "RegExMatchesRows",
				DataAccess				= DataAccessKind.Read,
				TableDefinition		= "[MatchIndex] smallint, [GroupIndex] tinyint, [Text] nvarchar(max), [Index] int, [Length] int",
				IsDeterministic		= true
			)
		]

		public static IEnumerable RegExMatches(String AText, String APattern, String AOptions)
		{
      if (AText == null)
        return null;

      if (String.IsNullOrEmpty(APattern))
          throw new Exception("Параметр \"Pattern\" не может быть NULL или Empty.");

      List<RegExMatchRow> LRows = new List<RegExMatchRow>();
			RegExMatchRow LRow;

			MatchCollection LMatches = Regex.Matches(AText, APattern, ScalarFunctions.RegexOptionsParser(AOptions));
			LRow.MatchIndex = 0;
			foreach (Match LMatch in LMatches)
			{
				LRow.MatchIndex++;
				for(Byte I = 0; I < LMatch.Groups.Count; I++)
 				{
					LRow.GroupIndex  = I;
					LRow.Match       = LMatch.Groups[I].Value;
          LRow.Index       = LMatch.Groups[I].Index + 1;
          LRow.Length      = LMatch.Groups[I].Length;
					LRows.Add(LRow);
				}
			}
			return LRows;
		}

		public static void RegExMatchesRows(Object obj, out SqlInt16 MatchIndex, out SqlByte GroupIndex, out SqlString Match, out SqlInt32 Index, out SqlInt32 Length)
		{
			MatchIndex	= ((RegExMatchRow)obj).MatchIndex;
			GroupIndex	= ((RegExMatchRow)obj).GroupIndex;
			Match				= ((RegExMatchRow)obj).Match;
			Index 			= ((RegExMatchRow)obj).Index;
			Length			= ((RegExMatchRow)obj).Length;
		}
	}
}
