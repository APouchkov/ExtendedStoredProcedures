using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using System.Collections;
using System.Collections.Generic;

public partial class Pub
{
    public struct CodesRow
    {
      public int    Index;
      public String Operation;
      public String Sign;
      public String Code;
      public String Params;
    }

    private static bool IsIdentChar(Char ch)
    {
      return (Char.IsLetterOrDigit(ch) || ch == '[' || ch == ']' || ch == '_');
    }

    //private static void SkipSpace(string str, ref int pos)
    //{
    //  while (pos < str.Length && Char.IsWhiteSpace(str[pos])) pos++;
    //}

    private static bool TryParseQuoted(String str, out String result, ref int pos)
    {
      //SkipSpace(str, ref pos);
      if (str[pos] != '[') { result = null; return false; }

      int   start   = pos;
      bool  quoted  = false;

      while (pos < str.Length)
      {
        if (str[pos] == ']')
          quoted = !quoted;
        else if (quoted) 
          break;
        pos++;
      }

      if (!quoted)
        throw new Exception(String.Format("Unclosed quotation mark after the character string {0}", str.Substring(start)));

      result = str.Substring(start, pos - start);
      return true;
    }

    private static bool TryParseParam(string str, out string result, ref int pos)
    {
      //SkipSpace(str, ref pos);
      if (pos == str.Length || str[pos] != '(') { result = null; return false; }
      int start = pos;
      int quoted = 0;
      while (pos < str.Length)
      {
        if (str[pos] == '(')
          quoted++;
        if (str[pos] == ')')
          quoted--;
        else
          if (quoted == 0) break;
        pos++;
      }

      if (quoted > 0)
        throw new Exception(String.Format("Unclosed quotation mark after the character string {0}", str.Substring(start)));

      result = str.Substring(start + 1, pos - start - 2);
      return true;
    }

    private static bool TryParseCode(string AText, Boolean AParseParams, out string code, out string AParams, ref int pos)
    {
      //SkipSpace(str, ref pos);
      AParams = null;
      if(AParseParams)
        TryParseParam(AText, out AParams, ref pos);

      if (pos == AText.Length) { code = null; return false; }
      if (TryParseQuoted(AText, out code, ref pos)) return true;
      int start = pos;
      while (pos < AText.Length)
      {
        if (!IsIdentChar(AText[pos])) break;
        pos++;
      }
      code = AText.Substring(start, pos - start);
      if (code == "") code = null;
      return (code != null);
    }

    private static bool TryParseOperation(string AText, string AChars, out string AChar, ref int APos)
    {
      //SkipSpace(str, ref pos);
      if (APos >= AText.Length || String.IsNullOrEmpty(AChars) || AChars.IndexOf(AText[APos]) < 0) { AChar = null; return false; }
      AChar = AText[APos++].ToString();
      return true;
    }

    /// <summary>
    /// Разбивает список кодов на элементы
    /// </summary>
    [SqlFunction(Name = "Codes", FillRowMethodName = "CodesRows", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, TableDefinition = "[Code] nvarchar(max), [Index] smallint, [Operation] nchar(1), [Sign] nchar(1), [Params] nvarchar(max)", IsDeterministic = true)]
    public static IEnumerable Codes(String AText, String AOperators, String ASigns, SqlBoolean AParams)
    {
      if (String.IsNullOrEmpty(AText)) return null;
      Boolean LParams = AParams.IsTrue;

      List<CodesRow> rows = new List<CodesRow>();

      int     pos   = 0;
      int     start = pos;
      bool    first = true;

      String  operation = "+";
      String  sign;
      String  code;
      String  param;

      while (pos < AText.Length)
      {
        start = pos;

        // If not first parse Operation
        if (!first)
          if(!TryParseOperation(AText, AOperators, out operation, ref pos))
            throw new Exception(String.Format("TryParseOperator: Incorrect syntax near '{0}'", AText.Substring(start)));

        TryParseOperation(AText, ASigns, out sign, ref pos);

        // Parse Code
        if (TryParseCode(AText, LParams, out code, out param, ref pos))
        {
//          if (operation == null)
//            throw new Exception(String.Format("TryParseCode: Incorrect syntax near '{0}'", AText.Substring(start)));
          CodesRow row = new CodesRow();
          row.Index     = rows.Count + 1;
          row.Code      = code;
          row.Sign      = sign;
          row.Operation = operation; // == "," ? "+" : operation;
          row.Params    = param;
          rows.Add(row);
          operation = null;
          code = null;
        }
        else
          throw new Exception(String.Format("TryParseCode: Incorrect syntax near '{0}'", AText.Substring(start)));

        first = false;
      }
      return rows;
    }

    public static void CodesRows(object row, out SqlString Code, out SqlInt32 Index, out SqlString Operation, out SqlString Sign, out SqlString Params)
    {
      Code      = ((CodesRow)row).Code;
      Index     = ((CodesRow)row).Index;
      Operation = ((CodesRow)row).Operation;
      Sign      = ((CodesRow)row).Sign;
      Params    = ((CodesRow)row).Params;
    }
};

