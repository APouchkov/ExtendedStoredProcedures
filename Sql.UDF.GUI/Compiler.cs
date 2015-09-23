//------------------------------------------------------------------------------
// <copyright file="CSSqlFunction.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
using System;
using System.Text;
using System.Linq;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;

public partial class Compiler
{
  [SqlProcedure(Name = "Compile")]
  public static void Compile(String AText, UDT.TParams AParams, String AComments, Char ALiteral, String ATranslateScalar, out SqlChars AResult)
  {
    if(String.IsNullOrWhiteSpace(AText))
    { 
      AResult = null;
      return;
    }

    if(ATranslateScalar != null && ATranslateScalar.IndexOf("@Text") == -1)
      throw new Exception("Скалярный запрос на межязыковой перевод должен содержать параметр \"@Text\"");
    AResult = new SqlChars((new ScriptParser(AText, AParams, Pub.CommentMethodsParser(AComments), ALiteral, ATranslateScalar)).FText.ToString());
  }
}

/*
{$IF <CONDITION>}
{$ELSE}
{$END}

{$INCLUDE <NAME>}
{$TRANSLATE <TEXT>}
{$EVALUATE <EXPRESSION>}
*/

public class ScriptParser
{
  private const String ContextConnection = "context connection=true";

  private String FString;
  private UDT.TParams FParams;
  private TCommentMethods FComments; // TCommentMethods
  private Char FLiteral;
  private String FTranslateScalar;

  private Char FPriorChar;
  private Char FCurrChar;
  private Char FNextChar;

  private int FLength;
  private int FPosition;

  //private TScriptParseItem FCurrent;
  //public TScriptParseItem Current { get { return FCurrent; } }

  private String  FGap;
  private int     FLine;
  private String  FCommand;
  private String  FValue;
  private Boolean FEof;
  private Boolean FBol;
  private SqlConnection FSqlConnection;
  private SqlCommand    FSqlCommandTranslateScalar;
  private SqlCommand    FSqlCommandOther;

  public StringBuilder FText;

  public void Exception(String AInstruction)
  {
    throw new Exception("Ошибка препроцессорной компиляции в строке " + FLine.ToString() + ": " + AInstruction);
  }

  private enum TScriptParserWaitFor: byte { None = 0, Else = 1, End = 2 };

  public ScriptParser(String AText, UDT.TParams AParams, TCommentMethods AComments, Char ALiteral, String ATranslateScalar)
  {
    FString           = AText;
    FParams           = AParams;
    FComments         = AComments;
    FLiteral          = ALiteral;
    FTranslateScalar  = ATranslateScalar;

    FPosition = -1;
    FLength   = FString.Length;
    FCurrChar = '\0';
    MoveToNextChar();

    // FLine
    FGap     = "";
    FLine    = 1;
    FCommand = "";
    FValue   = "";
    //FBol     = false;

    FText = new StringBuilder(1024);

    if(FParams != null)
    {
      FParams.InitContextConnection();
      FSqlConnection = FParams.ContextConnection;
    }
    else
    {
      FSqlConnection = new SqlConnection(ContextConnection);
      FSqlConnection.Open();
    }

    CallIF();
  }

  private static void InternalDeepQuoteLevel(ref String AString, out byte ADepth)
  {
    if(AString == null || AString.Length < 3 || AString[0] != '<' || AString[2] != '>')
      ADepth = 0;
    else
    {
      ADepth = Convert.ToByte(AString[1].ToString());
      AString = AString.Substring(3, AString.Length - 3).TrimStart();
    }
  }

  private String InternalDeepQuote(String AString, byte ADepth)
  {
    if(ADepth == 0 || String.IsNullOrWhiteSpace(AString))
      return AString;
    else
    {
      int Shl = 1 << ADepth;
      return AString.Replace(new String(FLiteral, 1), new String(FLiteral, Shl));
    }
  }

  private void InternalSkipReturns()
  {
    if(FCurrChar == (Char)13 || FCurrChar == (Char)10)
    {
      MoveToNextChar();
      if((FCurrChar == (Char)13 || FCurrChar == (Char)10) && (FPriorChar != FCurrChar))
        MoveToNextChar();
    }
  }

  private Char[] LSpaces = new Char[3] {' ', '\r', '\n'};

  private Boolean InternalCallIF()
  {
    if(FBol) InternalSkipReturns();

    if(FCommand == "IF")
      return INT.TParams.EvaluateBoolean(FParams, FValue, false);
    else
      return (FParams.Exists(FValue) == (FCommand == "IFDEF"));
  }

  private void InitSqlCommandOther()
  {
    if(FSqlCommandOther == null)
    {
      FSqlCommandOther = FSqlConnection.CreateCommand();
      FSqlCommandOther.CommandType = CommandType.Text;
    }
  }

  private DataTable GetDataTable(SqlCommand ASqlCommand)
  {
    SqlDataAdapter LDataAdapter = new SqlDataAdapter(ASqlCommand);
    DataTable LDataTable = new DataTable();
    LDataAdapter.ReturnProviderSpecificTypes = true;
    LDataAdapter.Fill(LDataTable);

    return LDataTable;
  }

  // Возвращает TRUE, если найден ELSE
  private void CallIF(TScriptParserWaitFor AWaitFor = TScriptParserWaitFor.None, Boolean ASkipText = false, Boolean AParentSkipText = false, StringBuilder AText = null)
  {
    Boolean LSkipText = ASkipText;
    Boolean LElseIfCompleted = (AWaitFor != TScriptParserWaitFor.None && !ASkipText && !AParentSkipText);
    if(AText == null)
      AText = FText;

    while (MoveNext())
    {
      if(!LSkipText) AText.Append(FGap);

      if(!String.IsNullOrEmpty(FCommand))
      { 
        //SqlContext.Pipe.Send("Command: " + FCommand + ", FValue = " + FValue + ", AWaitFor = " + AWaitFor.ToString() + ", LSkipText = " + LSkipText.ToString() + ", LElseIfCompleted = " + LElseIfCompleted.ToString());

        if(FCommand == "IF" || FCommand == "IFDEF" || FCommand == "IFNDEF")
        {
          Boolean LIfTrue = InternalCallIF();

          // SqlContext.Pipe.Send("IF: LIfTrue = " + LIfTrue.ToString() + ", LSkipText = " + LSkipText.ToString());
          CallIF(TScriptParserWaitFor.Else | TScriptParserWaitFor.End, LSkipText || (!LIfTrue), ASkipText);

          // SqlContext.Pipe.Send("IF: LElseFound = " + LElseFound.ToString());
        }
        else if(FCommand == "FOREACH")
        {
          int LPosition = FPosition;
          Char LCurrChar = FCurrChar;

          InitSqlCommandOther();
          FSqlCommandOther.CommandText = DynamicSQL.FinalSQL(FValue, FParams);

          DataTable LDataTable = null;

          try { LDataTable = GetDataTable(FSqlCommandOther); }
          catch (Exception E) { Exception("Ошибка исполнения запроса {" + FSqlCommandOther.CommandText + "}: " + E.Message); }

          foreach(DataRow LDataRow in LDataTable.Rows)
          {
            FPosition = LPosition;
            FCurrChar = LCurrChar;
            foreach(DataColumn LColumn in LDataTable.Columns)
              FParams.AddParam(LColumn.ColumnName, LDataRow[LColumn]);
            CallIF(TScriptParserWaitFor.End);
          }
          foreach(DataColumn LColumn in LDataTable.Columns)
            FParams.DeleteParam(LColumn.ColumnName);

          LDataTable.Clear();
        }

        else if(FCommand == "END" || FCommand == "ENDIF" || FCommand == "IFEND")
        {
          if(FBol) InternalSkipReturns();

          if((AWaitFor & TScriptParserWaitFor.End) == 0)
            Exception("Найдена инструкция {$END} без предшествующей ей инструкции {$IF}");

          // SqlContext.Pipe.Send("END");

          return;
        }
        else if(FCommand == "ELSE")
        {
          if(FBol) InternalSkipReturns();

          // SqlContext.Pipe.Send("ELSE " + FValue);

          if((AWaitFor & TScriptParserWaitFor.Else) == 0)
            Exception("Найдена инструкция {$ELSE} без предшествующей ей инструкции {$IF}");
  
          if(FValue.Length > 0)
          {
            int LSpace = FValue.IndexOfAny(LSpaces); 
            if(LSpace > 0)
            {
              FCommand = FValue.Substring(0, LSpace);
              FValue = FValue.Substring(LSpace + 1);
            }
            else
            {
              FCommand = FValue;
              FValue   = "";
            }

            if(!LElseIfCompleted && !AParentSkipText)
            { 
              if(FCommand == "IF" || FCommand == "IFDEF" || FCommand == "IFNDEF")
              {
                LElseIfCompleted = InternalCallIF();
                if(LElseIfCompleted)
                  LSkipText = false;
              }
              else
                Exception("Неизвестная инструкция {$ELSE " + FCommand + '}');
            }
            else if(LElseIfCompleted && !LSkipText)
            { 
              LSkipText = true;
              //SqlContext.Pipe.Send(">> ELSEIF SET LSkipText = " + LSkipText.ToString());
            }

            //continue;
          }
          else
          { 
            AWaitFor ^= TScriptParserWaitFor.Else;
            LSkipText = LElseIfCompleted || AParentSkipText;
            //SqlContext.Pipe.Send(">> ELSE SET LSkipText = " + LSkipText.ToString());
          }

          //return true;
          continue;
        }
        else if(LSkipText)
          continue;
        else if(FCommand == "I" || FCommand == "INCLUDE")
        {
          byte LDepth;
          String LStringFValue = FValue.Trim();
          InternalDeepQuoteLevel(ref LStringFValue, out LDepth);

          SqlString LValue = INT.TParams.AsNVarChar(FParams, LStringFValue);
          if(!LValue.IsNull)
            AText.Append(InternalDeepQuote(LValue.Value, LDepth));
        }
        else if(FTranslateScalar != null && (FCommand == "T" || FCommand == "TRANSLATE"))
        {
          byte LDepth;
          String LStringFValue = FValue.Trim();
          InternalDeepQuoteLevel(ref LStringFValue, out LDepth);

          if(FSqlCommandTranslateScalar == null)
          {
            FSqlCommandTranslateScalar = FSqlConnection.CreateCommand();
            FSqlCommandTranslateScalar.CommandType = CommandType.Text;
            FSqlCommandTranslateScalar.CommandText = FTranslateScalar;
            FSqlCommandTranslateScalar.Parameters.Add("@Text", SqlDbType.NVarChar, 1000).Direction = ParameterDirection.Input;
          }

          FSqlCommandTranslateScalar.Parameters[0].Value = LStringFValue;
          Object LValue = FSqlCommandTranslateScalar.ExecuteScalar();
          AText.Append(InternalDeepQuote(LValue.ToString(), LDepth));
        }
        else if(FCommand == "SELECT" || FCommand == "EXEC")
        {
          byte LDepth;
          String LStringFValue = FValue;
          InternalDeepQuoteLevel(ref LStringFValue, out LDepth);

          InitSqlCommandOther();
          FSqlCommandOther.CommandText = DynamicSQL.FinalSQL(FCommand + ' ' + LStringFValue, FParams);

          try
          { 
            Object LValue = FSqlCommandOther.ExecuteScalar();
            if(LValue != null && LValue != DBNull.Value)
              AText.Append(InternalDeepQuote(Convert.ToString(LValue), LDepth));
          }
          catch(Exception E)
          {
            Exception("Ошибка исполнения запроса {" + FSqlCommandOther.CommandText + "}: " + E.Message);
          }
        }
        else if(FCommand == "IMPORT")
        {
          InitSqlCommandOther();
          FSqlCommandOther.CommandText = DynamicSQL.FinalSQL(FValue, FParams);

          SqlDataReader LReader = null;

          try { LReader = FSqlCommandOther.ExecuteReader(); }
          catch(Exception E) { Exception("Ошибка исполнения запроса {" + FSqlCommandOther.CommandText + "}: " + E.Message); }

          if (!LReader.IsClosed)
          {
            if (LReader.Read())
            {
              Object[] LValues = new object[LReader.FieldCount];

              LReader.GetSqlValues(LValues);
              for (int i = LReader.FieldCount - 1; i >= 0; i--)
                FParams.AddParam(LReader.GetName(i), LValues[i]);
            }
            LReader.Close();
          }
        }

        else if(FCommand == "DEFINE")
        {
          InitSqlCommandOther();
          FSqlCommandOther.CommandText = DynamicSQL.FinalSQL(FValue, FParams);

          SqlDataReader LReader = null;

          try { LReader = FSqlCommandOther.ExecuteReader(); }
          catch(Exception E) { Exception("Ошибка исполнения запроса {" + FSqlCommandOther.CommandText + "}: " + E.Message); }

          if (!LReader.IsClosed)
          {
            while (LReader.Read() && !LReader.IsDBNull(0))
            {
              String SValues = LReader.GetString(0);
              foreach (String SValue in SValues.Split(new Char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                FParams.AddParam(SValue, (SqlBoolean)true);
            }
            LReader.Close();
          }
        }

        else if(FCommand == "SET" || FCommand == "APPEND")
        {
          int LEqual = FValue.IndexOf('=');
          if (LEqual < 0)
          {
            //Exception("Пропущен символ присвоения '=': " + FCommand + ' ' + FValue);
            Boolean LSet  = (FCommand == "SET");
            String  LName = FValue;

            StringBuilder LText = new StringBuilder();
            CallIF(AWaitFor: TScriptParserWaitFor.End, AText: LText);
            if (LSet)
              FParams.AddParam(LName, new SqlString(LText.ToString()));
            else
            {
              SqlString FOldValue = FParams.AsSQLString(LName);
              if (FOldValue.IsNull)
                FParams.AddParam(LName, new SqlString(LText.ToString()));
              else
                FParams.AddParam(LName, new SqlString(FOldValue.Value + LText.ToString()));
            }
          }
          else
          {
            String LName = FValue.Substring(0, LEqual).Trim();
            if (LName.Length == 0)
              Exception("Имя макроса должно быть непустым: " + FCommand + ' ' + FValue);

            FValue = FValue.Substring(LEqual + 1, FValue.Length - LEqual - 1);
            if (FCommand == "SET")
              FParams.AddParam(LName, new SqlString(FValue));
            else
            {
              SqlString FOldValue = FParams.AsSQLString(LName);
              if (FOldValue.IsNull)
                FParams.AddParam(LName, new SqlString(FValue));
              else
                FParams.AddParam(LName, new SqlString(FOldValue.Value + FValue));
            }
          }
          
          InternalSkipReturns();
        }
        else if(FCommand == "C" || FCommand == "COMMENT")
        {
          InternalSkipReturns();
        }
        else if(FCommand == "EXCEPTION")
        {
          Exception(String.IsNullOrWhiteSpace(FValue) ? "Abstract error" : FValue);
        }
        else
          Exception("Найдена неизвестная инструкция {$" + FCommand + "}");
      }
    }

    if((AWaitFor & TScriptParserWaitFor.End) != 0)
      Exception("Конец сценария достигнут, хотя ожидается инструкция {$END}");

//    return false;
  }

  // Разбор текста
  private void MoveToNextChar()
  {
    FPriorChar = FCurrChar;

    FPosition++;
    if (FPosition < FLength)
      FCurrChar = FString[FPosition];
    else
    {
      FCurrChar = '\0';
      FEof = true;
    }

    if (FPosition < FLength - 1)
      FNextChar = FString[FPosition + 1];
    else
      FNextChar = '\0';
  }

  private void SkipText(ref int LPosition, Boolean AIncludeCurrent)
  {
    if(LPosition >= FString.Length)
      return;

    int LWidth;
    if(FPosition >= FString.Length)
      LWidth = FString.Length - LPosition;
    else 
      LWidth = FPosition - LPosition;

    if (AIncludeCurrent) LWidth++;
    if(LWidth > 0)
      FGap = FGap + FString.Substring(LPosition, LWidth);
    LPosition = FPosition + 1;
  }

  public Boolean MoveNext()
  {
    int LPosition;
    int LLine = FLine;
    Boolean LWaitForCommand = false;
    Boolean LInLiteral = false;
    TCommentMethod LCurrComment;

    if (FEof)
      return false;

    FGap = "";
    LPosition = FPosition;
    LCurrComment = TCommentMethod.None;

    for (;;) /*(FPosition <= FLength)*/
    {
      if(FCurrChar == (char)13) LLine++;

      if(LWaitForCommand && FCurrChar == '}')
      {
        LWaitForCommand = false;
        FCommand = FString.Substring(LPosition, FPosition - LPosition).TrimEnd();
        int LIndex = 0;
        int LLength = FCommand.Length;
        for(; LIndex < LLength && !LSpaces.Contains(FCommand[LIndex]); LIndex++);

        if(LIndex < LLength)
        {
          FValue   = FCommand.Substring(LIndex + 1);
          FCommand = FCommand.Substring(0, LIndex);
        }
        else
          FValue = "";
        FEof   = (FPosition >= FLength);
        MoveToNextChar();
        return true;
      }
      else if(!LInLiteral && !LWaitForCommand && LCurrComment != TCommentMethod.None)
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

      if(FCurrChar == (char)0)
      { 
        SkipText(ref LPosition, false);
        FCommand = "";
        FValue   = "";
        FEof   = true;
        FLine  = LLine;
        return true;
      }
      else if (!LWaitForCommand && FCurrChar == '{' && FNextChar == '$')
      {
        LCurrComment = TCommentMethod.Braces;
        LWaitForCommand = true;
        SkipText(ref LPosition, false);
        FBol = (FPriorChar == (Char)13 || FPriorChar == (Char)10);
        MoveToNextChar(); MoveToNextChar(); LPosition++;
        FLine = LLine;
        continue;
      }
      else if(!LInLiteral && !LWaitForCommand && LCurrComment == TCommentMethod.None)
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
        }

        if(!LInLiteral && FCurrChar == FLiteral)
          LInLiteral = true;
        else if(/*!LWaitForCommand &&*/ LInLiteral && FCurrChar == FLiteral)
          if(FNextChar == FLiteral)
            MoveToNextChar();
          else
            LInLiteral = false;

      MoveToNextChar();
    }
  }
}
