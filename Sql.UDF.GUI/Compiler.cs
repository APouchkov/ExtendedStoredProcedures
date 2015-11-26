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

/*
{$IF <CONDITION>}
{$ELSE}
{$END}

{$INCLUDE <NAME>}
{$TRANSLATE <TEXT>}
{$SELECT <EXPRESSION>} 
{$EXEC <EXPRESSION>}
{$IMPORT <EXPRESSION>}

{$USE <UNIT> <PARAMS>}
*/

public class ScriptParser
{
  private const String ContextConnection = "context connection=true";

  private static readonly Char[] Returns = new Char[2] {'\r', '\n'};
  private static readonly Char[] Spaces  = new Char[3] {' ', '\r', '\n'};

  private TParsingText  FInput;

  private String FTranslateScalar;
  private String FUseScalar;

  //private TScriptParseItem FCurrent;
  //public TScriptParseItem Current { get { return FCurrent; } }

  private SqlConnection FSqlConnection;
  private SqlCommand    FSqlCommandTranslateScalar;
  private SqlCommand    FSqlCommandUseScalar;
  private SqlCommand    FSqlCommandOther;

  public StringBuilder FOutput;

  public enum TScriptParserWaitFor: byte { None = 0, Else = 1, End = 2, EndIf = 4 };

  public struct TParsingPosition
  {
    public int Index;
    public int Line;

    public Char PriorChar;
    public Char CurrChar;
    public Char NextChar;

    public void Init()
    {
      Index = -1;
      Line  = 1;

      PriorChar = '\0';
      CurrChar = '\0';
      NextChar = '\0';
    }
  }

  public struct TParsingText
  {
    private String FInput;
    private TCommentMethods FComments;
    private Char FLiteral;

    public TParsingPosition Position;

    private Boolean FBol;
    private Boolean FEof;

    private String  FGap;
    private String  FCommand;
    private String  FValue;

    public TCommentMethods Comments { get { return FComments; } }
    public Char Literal { get { return FLiteral; } }
    public Boolean Bol { get { return FBol; } }

    public Boolean Eof { get { return FEof; } }

    public String Gap { get { return FGap; } }

    public String Command { get { return FCommand; } }

    public String Value { get { return FValue; } }

    public TParsingText(String AText, TCommentMethods AComments, Char ALiteral)
    {
      FInput    = AText;
      FComments = AComments;
      FLiteral  = ALiteral;

      Position = new TParsingPosition();
      Position.Init();

      FBol = false;
      FEof = false;

      FGap     = "";
      FCommand = "";
      FValue   = "";

      MoveToNextChar();
    }

    public int Length { get { return FInput.Length; } }

  // Разбор текста
    public void MoveToNextChar()
    {
      Position.PriorChar = Position.CurrChar;

      Position.Index++;
      if (Position.Index < Length)
        Position.CurrChar = FInput[Position.Index];
      else
      {
        Position.CurrChar = '\0';
        FEof      = true;
      }

      if (Position.Index < Length - 1)
        Position.NextChar = FInput[Position.Index + 1];
      else
        Position.NextChar = '\0';
    }
  
    public void InternalSkipReturns()
    {
      if(Position.CurrChar == (Char)13 || Position.CurrChar == (Char)10)
      {
        MoveToNextChar();
        if((Position.CurrChar == (Char)13 || Position.CurrChar == (Char)10) && (Position.PriorChar != Position.CurrChar))
          MoveToNextChar();
      }
    }

    private void SkipText(ref int LPosition, Boolean AIncludeCurrent)
    {
      if(LPosition >= FInput.Length)
        return;

      int LWidth;
      if(Position.Index >= Length)
        LWidth = FInput.Length - LPosition;
      else 
        LWidth = Position.Index - LPosition;

      if (AIncludeCurrent) LWidth++;
      if(LWidth > 0)
        FGap = FGap + FInput.Substring(LPosition, LWidth);
      LPosition = Position.Index + 1;
    }

    public Boolean MoveNext()
    {
      int LPosition;
      int LLine = Position.Line;
      Boolean LWaitForCommand = false;
      Boolean LInLiteral = false;
      TCommentMethod LCurrComment;

      if (FEof)
        return false;

      FGap = "";
      LPosition = Position.Index;
      LCurrComment = TCommentMethod.None;

      for (;;) /*(FPosition <= FLength)*/
      {
        if(Position.CurrChar == (char)13) LLine++;

        if(LWaitForCommand && Position.CurrChar == '}')
        {
          LWaitForCommand = false;
          FCommand = FInput.Substring(LPosition, Position.Index - LPosition).TrimEnd();
          int LIndex = 0;
          int LLength = FCommand.Length;
          //Char[] LSpaces = new Char[3];

          for(; LIndex < LLength && !Spaces.Contains(FCommand[LIndex]); LIndex++);
          

          if(LIndex < LLength)
          {
            FValue   = FCommand.Substring(LIndex + 1);
            FCommand = FCommand.Substring(0, LIndex);
          }
          else
            FValue = "";

          FEof = (Position.Index >= Length);
          MoveToNextChar();

          return true;
        }
        else if(!LInLiteral && !LWaitForCommand && LCurrComment != TCommentMethod.None)
          switch (LCurrComment)
          {
            case TCommentMethod.Lattice:
            case TCommentMethod.DoubleMinus:
            case TCommentMethod.DoubleSlash:
              if (Position.CurrChar == (char)10 || Position.CurrChar == (char)13 || Position.CurrChar == (char)0)
                LCurrComment = TCommentMethod.None;
              MoveToNextChar();
              continue;

            case TCommentMethod.SlashRange:
              {
                if ((Position.CurrChar == '*') && (Position.NextChar == '/'))
                {
                  LCurrComment = TCommentMethod.None;
                  Position.Index++;
                }
                MoveToNextChar();
                continue;
              }

            case TCommentMethod.BracketRange:
              {
                if ((Position.CurrChar == '*') && (Position.NextChar == ')'))
                {
                  LCurrComment = TCommentMethod.None;
                  Position.Index++;
                }
                MoveToNextChar();
                continue;
              }

          case TCommentMethod.Braces:
            {
              if (Position.CurrChar == '}')
                LCurrComment = TCommentMethod.None;
              MoveToNextChar();
              continue;
            }
        }

        if(Position.CurrChar == (char)0)
        { 
          SkipText(ref LPosition, false);
          FCommand = "";
          FValue   = "";
          FEof   = true;
          Position.Line  = LLine;
          return true;
        }
        else if (!LWaitForCommand && Position.CurrChar == '{' && Position.NextChar == '$')
        {
          LCurrComment = TCommentMethod.Braces;
          LWaitForCommand = true;
          SkipText(ref LPosition, false);
          FBol = (Position.PriorChar == (Char)13 || Position.PriorChar == (Char)10);
          MoveToNextChar(); MoveToNextChar(); LPosition++;
          Position.Line = LLine;
          continue;
        }
        else if(!LInLiteral && !LWaitForCommand && LCurrComment == TCommentMethod.None)
          switch (Position.CurrChar)
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
              if (((TCommentMethods.DoubleMinus & FComments) != 0) && (Position.NextChar == '-'))
              {
                LCurrComment = TCommentMethod.DoubleMinus;
                Position.Index++;
                MoveToNextChar();
                continue;
              }
              else break;
            case '/':
              if (((TCommentMethods.DoubleSlash & FComments) != 0) && (Position.NextChar == '/'))
              {
                LCurrComment = TCommentMethod.DoubleSlash;
                Position.Index++;
                MoveToNextChar();
                continue;
              }
              else if (((TCommentMethods.SlashRange & FComments) != 0) && (Position.NextChar == '*'))
              {
                LCurrComment = TCommentMethod.SlashRange;
                Position.Index++;
                MoveToNextChar();
                continue;
              }
              else break;
            case '(':
              if (((TCommentMethods.BracketRange & FComments) != 0) && (Position.NextChar == '*'))
              {
                LCurrComment = TCommentMethod.BracketRange;
                Position.Index++;
                MoveToNextChar();
                continue;
              }
              else break;
          }

          if(!LInLiteral && Position.CurrChar == FLiteral)
            LInLiteral = true;
          else if(/*!LWaitForCommand &&*/ LInLiteral && Position.CurrChar == FLiteral)
            if(Position.NextChar == FLiteral)
              MoveToNextChar();
            else
              LInLiteral = false;

        MoveToNextChar();
      }
    }

    public String InternalDeepQuote(String AString, byte ADepth)
    {
      if(ADepth == 0 || String.IsNullOrWhiteSpace(AString))
        return AString;
      else
      {
        int Shl = 1 << ADepth;
        return AString.Replace(new String(FLiteral, 1), new String(FLiteral, Shl));
      }
    }

    public void Exception(String AInstruction)
    {
      throw new Exception("Ошибка препроцессорной компиляции в строке " + Position.Line.ToString() + ": " + AInstruction);
    }
  }

  
  public ScriptParser
  (
    SqlConnection   ASqlConnection,
    String          ATranslateScalar,
    String          AUseScalar
  )
  {
    if(ASqlConnection == null)
      throw new Exception("Не передан обязательный параметр <ASqlConnection>");

    FSqlConnection    = ASqlConnection;
    FTranslateScalar  = ATranslateScalar;
    FUseScalar        = AUseScalar;

    FOutput = new StringBuilder(1024);
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

  private Boolean InternalCallIF(ref TParsingText AInput, UDT.TParams AParams)
  {
    if(AInput.Bol) AInput.InternalSkipReturns();

    if(AInput.Command == "IF")
      return INT.TParams.EvaluateBoolean(AParams, AInput.Value, false);
    else
      return (AParams.Exists(AInput.Value) == (AInput.Command == "IFDEF"));
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
  public void CallIF
  (
ref TParsingText          AInput,
    UDT.TParams           AParams,

    TScriptParserWaitFor  AWaitFor        = TScriptParserWaitFor.None,
    Boolean               ASkipText       = false,
    Boolean               AParentSkipText = false,
    StringBuilder         AText           = null
  )
  {
    Boolean LSkipText = ASkipText;
    Boolean LElseIfCompleted = (AWaitFor != TScriptParserWaitFor.None && !ASkipText && !AParentSkipText);
    if(AText == null)
      AText = FOutput;

    while (AInput.MoveNext())
    {
      if(!LSkipText) AText.Append(AInput.Gap);

      if(!String.IsNullOrEmpty(AInput.Command))
      { 
        //SqlContext.Pipe.Send("Command: " + FCommand + ", FValue = " + FValue + ", AWaitFor = " + AWaitFor.ToString() + ", LSkipText = " + LSkipText.ToString() + ", LElseIfCompleted = " + LElseIfCompleted.ToString());

        if(AInput.Command == "IF" || AInput.Command == "IFDEF" || AInput.Command == "IFNDEF")
        {
          Boolean LIfTrue = InternalCallIF(ref AInput, AParams);

          CallIF
          (
            AInput          : ref AInput,
            AParams         : AParams,
            AWaitFor        : TScriptParserWaitFor.Else | TScriptParserWaitFor.End | TScriptParserWaitFor.EndIf,
            ASkipText       : LSkipText || (!LIfTrue),
            AParentSkipText : ASkipText
          );

          // SqlContext.Pipe.Send("IF: LElseFound = " + LElseFound.ToString());
        }

        else if(AInput.Command == "FOREACH")
        {
          TParsingPosition LPosition  = AInput.Position;

          InitSqlCommandOther();
          FSqlCommandOther.CommandText = DynamicSQL.FinalSQL(AInput.Value, AParams);

          DataTable LDataTable = null;

          try { LDataTable = GetDataTable(FSqlCommandOther); }
          catch (Exception E) { AInput.Exception("Ошибка исполнения запроса {" + FSqlCommandOther.CommandText + "}: " + E.Message); }

          foreach(DataRow LDataRow in LDataTable.Rows)
          {
            AInput.Position = LPosition;
            foreach(DataColumn LColumn in LDataTable.Columns)
              AParams.AddParam(LColumn.ColumnName, LDataRow[LColumn]);
            CallIF
            (
              AInput    : ref AInput,
              AParams   : AParams,
              AWaitFor  : TScriptParserWaitFor.End
            );
          }
          foreach(DataColumn LColumn in LDataTable.Columns)
            AParams.DeleteParam(LColumn.ColumnName);

          LDataTable.Clear();
        }

        else if(AInput.Command == "END" || AInput.Command == "ENDIF" || AInput.Command == "IFEND")
        {
          if(AInput.Bol) AInput.InternalSkipReturns();

          TScriptParserWaitFor LCommand = AInput.Command == "END" ? TScriptParserWaitFor.End : TScriptParserWaitFor.EndIf;
          if((AWaitFor & LCommand) == 0)
            AInput.Exception("Найден инструкция конца блока {$" + AInput.Command + "} без предшествующей ей инструкции открытия блока");

          return;
        }

        else if(AInput.Command == "ELSE")
        {
          if(AInput.Bol) AInput.InternalSkipReturns();

          if((AWaitFor & TScriptParserWaitFor.Else) == 0)
            AInput.Exception("Найдена инструкция {$ELSE} без предшествующей ей инструкции {$IF}");
  
          if(AInput.Value.Length > 0)
          {
            int LSpace = AInput.Value.IndexOfAny(Spaces); 
            String LCommand;
            String LValue;
            if(LSpace > 0)
            {
              LCommand = AInput.Value.Substring(0, LSpace);
              LValue   = AInput.Value.Substring(LSpace + 1);
            }
            else
            {
              LCommand = AInput.Value;
              LValue   = "";
            }

            if(!LElseIfCompleted && !AParentSkipText)
            { 
              if(LCommand == "IF" || LCommand == "IFDEF" || LCommand == "IFNDEF")
              {
                LElseIfCompleted = InternalCallIF(ref AInput, AParams);
                if(LElseIfCompleted)
                  LSkipText = false;
              }
              else
                AInput.Exception("Неизвестная инструкция {$ELSE " + LCommand + '}');
            }
            else if(LElseIfCompleted && !LSkipText)
            { 
              LSkipText = true;
            }
          }
          else
          { 
            AWaitFor ^= TScriptParserWaitFor.Else;
            LSkipText = LElseIfCompleted || AParentSkipText;
          }

          continue;
        }

        else if(AInput.Command == "I" || AInput.Command == "INCLUDE")
        {
          if(LSkipText) continue;

          byte LDepth;
          String LStringFValue = AInput.Value.Trim();
          InternalDeepQuoteLevel(ref LStringFValue, out LDepth);

          SqlString LValue = INT.TParams.AsNVarChar(AParams, LStringFValue);
          if(!LValue.IsNull)
            AText.Append(AInput.InternalDeepQuote(LValue.Value, LDepth));
        }

        else if(FTranslateScalar != null && (AInput.Command == "T" || AInput.Command == "TRANSLATE"))
        {
          if(LSkipText) continue;

          byte LDepth;
          String LStringFValue = AInput.Value.Trim();
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
          AText.Append(AInput.InternalDeepQuote(LValue.ToString(), LDepth));
        }

        else if(AInput.Command == "SELECT" || AInput.Command == "EXEC")
        {
          if(LSkipText) continue;

          byte LDepth;
          String LStringFValue = AInput.Value;
          InternalDeepQuoteLevel(ref LStringFValue, out LDepth);

          InitSqlCommandOther();
          FSqlCommandOther.CommandText = DynamicSQL.FinalSQL(AInput.Command + ' ' + LStringFValue, AParams);

          try
          { 
            Object LValue = FSqlCommandOther.ExecuteScalar();
            if(LValue != null && LValue != DBNull.Value)
              AText.Append(AInput.InternalDeepQuote(Convert.ToString(LValue), LDepth));
          }
          catch(Exception E)
          {
            AInput.Exception("Ошибка исполнения запроса {" + FSqlCommandOther.CommandText + "}: " + E.Message);
          }
        }

        else if(AInput.Command == "IMPORT")
        {
          if(LSkipText) continue;

          InitSqlCommandOther();
          FSqlCommandOther.CommandText = DynamicSQL.FinalSQL(AInput.Value, AParams);

          SqlDataReader LReader = null;

          try { LReader = FSqlCommandOther.ExecuteReader(); }
          catch(Exception E) { AInput.Exception("Ошибка исполнения запроса {" + FSqlCommandOther.CommandText + "}: " + E.Message); }

          if (!LReader.IsClosed)
          {
            if (LReader.Read())
            {
              Object[] LValues = new object[LReader.FieldCount];

              LReader.GetSqlValues(LValues);
              for (int i = LReader.FieldCount - 1; i >= 0; i--)
                AParams.AddParam(LReader.GetName(i), LValues[i]);
            }
            LReader.Close();
          }
        }

        else if(AInput.Command == "DEFINE")
        {
          if(LSkipText) continue;

          InitSqlCommandOther();
          FSqlCommandOther.CommandText = DynamicSQL.FinalSQL(AInput.Value, AParams);

          SqlDataReader LReader = null;

          try { LReader = FSqlCommandOther.ExecuteReader(); }
          catch(Exception E) { AInput.Exception("Ошибка исполнения запроса {" + FSqlCommandOther.CommandText + "}: " + E.Message); }

          if (!LReader.IsClosed)
          {
            while (LReader.Read() && !LReader.IsDBNull(0))
            {
              String SValues = LReader.GetString(0);
              foreach (String SValue in SValues.Split(new Char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                AParams.AddParam(SValue, (SqlBoolean)true);
            }
            LReader.Close();
          }
        }

        else if(AInput.Command == "SET" || AInput.Command == "APPEND")
        {
          if(LSkipText) continue;

          int LEqual = AInput.Value.IndexOf('=');
          if (LEqual < 0)
          {
            //Exception("Пропущен символ присвоения '=': " + FCommand + ' ' + FValue);
            Boolean LSet  = (AInput.Command == "SET");
            String  LName = AInput.Value;

            StringBuilder LText = new StringBuilder();
            CallIF
            (
              AInput  : ref AInput,
              AParams : AParams,
              AWaitFor: TScriptParserWaitFor.End,
              AText   : LText
            );
            if (LSet)
              AParams.AddParam(LName, new SqlString(LText.ToString()));
            else
            {
              SqlString FOldValue = AParams.AsSQLString(LName);
              if (FOldValue.IsNull)
                AParams.AddParam(LName, new SqlString(LText.ToString()));
              else
                AParams.AddParam(LName, new SqlString(FOldValue.Value + LText.ToString()));
            }
          }
          else
          {
            String LName = AInput.Value.Substring(0, LEqual).Trim();
            if (LName.Length == 0)
              AInput.Exception("Имя макроса должно быть непустым: " + AInput.Command + ' ' + AInput.Value);

            String LValue = AInput.Value.Substring(LEqual + 1, AInput.Value.Length - LEqual - 1);
            if (AInput.Command == "SET")
              AParams.AddParam(LName, new SqlString(LValue));
            else
            {
              SqlString FOldValue = AParams.AsSQLString(LName);
              if (FOldValue.IsNull)
                AParams.AddParam(LName, new SqlString(LValue));
              else
                AParams.AddParam(LName, new SqlString(FOldValue.Value + LValue));
            }
          }
          
          AInput.InternalSkipReturns();
        }

        else if(AInput.Command == "C" || AInput.Command == "COMMENT")
        {
          if(LSkipText) continue;

          AInput.InternalSkipReturns();
        }

        else if(FUseScalar != null && (AInput.Command == "U" || AInput.Command == "USE"))
        {
          if(LSkipText) continue;

          String LStringFValue = AInput.Value.Trim();

          if(FSqlCommandUseScalar == null)
          {
            FSqlCommandUseScalar = FSqlConnection.CreateCommand();
            FSqlCommandUseScalar.CommandType = CommandType.Text;
            FSqlCommandUseScalar.CommandText = FUseScalar;
            FSqlCommandUseScalar.Parameters.Add("@Unit", SqlDbType.NVarChar, 1000).Direction = ParameterDirection.Input;
          }

          int LIndex = LStringFValue.IndexOfAny(Spaces);
          FSqlCommandUseScalar.Parameters[0].Value = LIndex == -1 ? LStringFValue : LStringFValue.Substring(0, LIndex);
          Object LValue = FSqlCommandUseScalar.ExecuteScalar();
          if(LValue != null && LValue != DBNull.Value)
          {
            UDT.TParams LParams;
            // USE <UNIT> <EX>
            if (LIndex >= 0)
            {
              LParams = UDT.TParams.New();
              LParams.ContextConnection = FSqlConnection;

              String LParamsString = LStringFValue.Substring(LIndex + 1).TrimStart();
              String LConstantsString = null;
              if (LStringFValue[LIndex] == ' ')
              {
                LIndex = LParamsString.IndexOfAny(Returns);
                if (LIndex >= 0)
                {
                  LConstantsString = LParamsString.Substring(LIndex + 1).TrimStart();
                  LParamsString = LParamsString.Substring(0, LIndex);
                }
                //SqlContext.Pipe.Send("LParamsString = " + LParamsString);
                LParams.Load(AParams, LParamsString);
              }
              else
                LConstantsString = LParamsString;

              LParams.MergeParams(UDT.TParams.ParseString(LConstantsString));
            }
            else
              LParams = AParams;

            StringBuilder LText = new StringBuilder();
            TParsingText LParsingText = new TParsingText(AText: LValue.ToString(), AComments: AInput.Comments, ALiteral: AInput.Literal);
            CallIF
            (
              AInput  : ref LParsingText,
              AParams : LParams
            );
          }

          //AText.Append(AInput.InternalDeepQuote(LValue.ToString(), LDepth));
        }
        else if(AInput.Command == "EXCEPTION")
        {
          if(LSkipText) continue;

          AInput.Exception(String.IsNullOrWhiteSpace(AInput.Value) ? "Abstract error" : AInput.Value);
        }
        else
          AInput.Exception("Найдена неизвестная инструкция {$" + AInput.Command + "}");
      }
    }

    if((AWaitFor & TScriptParserWaitFor.End) != 0)
      AInput.Exception("Конец сценария достигнут, хотя ожидается инструкция {$END}");
  }
}

public class Compiler
{
  [SqlProcedure(Name = "Compile")]
  public static void Compile
  (
    String        AText,
    UDT.TParams   AParams,
    String        AComments,
    Char          ALiteral,
    String        ATranslateScalar,
    String        AUseScalar,
    out SqlChars  AResult
  )
  {
    if(String.IsNullOrWhiteSpace(AText))
    { 
      AResult = null;
      return;
    }

    if(ATranslateScalar != null && ATranslateScalar.IndexOf("@Text") == -1)
      throw new Exception("Скалярный запрос на межязыковой перевод должен содержать параметр \"@Text\"");

    if(AUseScalar != null && (AUseScalar.IndexOf("@Unit") == -1))
      throw new Exception("Скалярный запрос на вложенную вставку должен содержать параметр \"@Unit\"");

    ScriptParser.TParsingText LParsingText = new ScriptParser.TParsingText(AText, Pub.CommentMethodsParser(AComments), ALiteral);
    if(AParams == null)
      AParams = UDT.TParams.New();
    AParams.InitContextConnection();

    ScriptParser LScriptParser = new ScriptParser
                                 (
                                  AParams.ContextConnection,
                                  ATranslateScalar,
                                  AUseScalar
                                 );
    LScriptParser.CallIF(AParams: AParams, AInput: ref LParsingText);
    AResult = new SqlChars(LScriptParser.FOutput.ToString());
  }
}
