(******************************************************************************)
(*  xpReport - foGlobal                                                       *)
(*  Автор: Турянский Александр                                                *)
(*  Версия: 3.5 от 27.03.2008                                                 *)
(******************************************************************************)
unit foGlobal;

interface

type
  TGenders = (gndMale, gndFemale, gndNeutral);

function GetCurrentModuleName: String;
function GetTempDir: String;
function GetMemSize: Integer;

function  GetRusEnding(n: Integer; o: Array of String): String;
function  Month_r(AMonth: Byte): String;
function  GetStrInt(n: Integer; Gender: TGenders = gndMale): String;
function  GetStrFloat(f: Extended; Gender: TGenders = gndMale; const W: String = ''; const eSingle: String = ''; const eSome: String = ''; const eMulti: String = ''): String;
function  GetStrDate(Date: TDateTime; isNice: Boolean = False): String;

implementation

uses
  Windows, SysUtils, DateUtils, psAPI;

const
  Decimals1: array [0..39] of string = ('ноль','один','два','три','четыре','пять','шесть','семь','восемь','девять','десять','одиннадцать','двенадцать','тринадцать','четырнадцать','пятнадцать','шестнадцать','семнадцать','восемнадцать','девятнадцать','двадцать','тридцать','сорок','пятьдесят','шестьдесят','семьдесят','восемьдесят','девяносто','сто','двести','триста','четыреста','пятьсот','шестьсот','семьсот','восемьсот','девятьсот','тысяч','миллион','миллиард');
  Decimals2: array [1..2] of string = ('одна','две');
  Decimals3: array [1..2] of string = ('одно','два');
  Decimals4: array [1..6] of string = ('десят','сот','тысячн','десятитысячн','стотысячн','миллионн');
  DecEnds  : array [0..1] of string = ('ая','ых');
  Months:  array [1..12] of string = ('Январь', 'Февраль', 'Март', 'Апрель', 'Май', 'Июнь', 'Июль', 'Август', 'Сентябрь', 'Октябрь', 'Ноябрь', 'Декабрь');

function GetCurrentModuleName: String;
var
  Buffer: array[0..1023] of Char;
begin
  SetString(Result, Buffer, GetModuleFileName(SysInit.HInstance, Buffer, SizeOf(Buffer)));
end;

function GetTempDir: String;
var
  Buffer: array[0..1023] of Char;
begin
  SetString(Result, Buffer, GetTempPath(SizeOf(Buffer), Buffer));
end;

function GetMemSize: Integer;
var
  ASize: Integer;
  PMC: PPROCESS_MEMORY_COUNTERS;
begin
   ASize := SizeOf(_PROCESS_MEMORY_COUNTERS);
   GetMem(PMC, ASize);
   try
     PMC^.cb := ASize;
     if GetProcessMemoryInfo(GetCurrentProcess(), PMC, ASize) then
       Result := PMC^.WorkingSetSize
     else
       Result := -1;
   finally
     FreeMem(PMC);
   end;
end;

function GetRusEnding(n: Integer; o: Array of String): String;
var
  d,e: Integer;
begin
  d := n mod 100;
  e := d mod 10;
  if Length(o)>2 then Result := o[2] else Result := o[1];
  if (d<10) or (d>20) then begin
    if e = 1 then Result := o[0]
      else if (e>1) and (e<5) then Result := o[1]
  end
end;

function Month_r(AMonth: Byte): String;
begin
  Result := Months[AMonth];
  if AMonth in [3,8] then
    Result := Result + 'а'
  else
    Result[Length(Result)] := 'я';
end;

function GetStrInt(n: Integer; Gender: TGenders = gndMale): String;
const
  _dec_Des      = 18;
  _dec_Sot      = 27;
  _dec_Tis      = 37;
  _dec_Million  = 38;
  _dec_Milliard = 39;
var
  nn: Integer;
  s: String;
begin
  if n<=0 then begin
    Result:=Decimals1[0];
    CharUpperBuff(@Result[1],1)
  end else begin
      Result := '';
        nn := n mod 1000;
        if nn>=100 then begin Result := Decimals1[_dec_Sot+(nn div 100)]; nn := nn mod 100 end;
        if nn>20 then begin Result := Result + ' ' + Decimals1[_dec_Des+(nn div 10)]; nn := nn mod 10 end;
        if nn>2 then Result := Result + ' ' + Decimals1[nn]
        else if nn>0 then begin
          case Gender of
            gndMale: Result := Result + ' ' + Decimals1[nn];
            gndFemale: Result := Result + ' ' + Decimals2[nn];
            gndNeutral: Result := Result + ' ' + Decimals3[nn];
          end;
        end;
        Result := TrimLeft(Result);

      n := n div 1000;
      nn := n mod 1000;
      if nn>0 then begin
        s := GetStrInt(nn,gndFemale) + ' ' + Decimals1[_dec_Tis] + GetRusEnding(nn,['а','и','']);
        Result := s + ' ' + Result;
      end;

      n := n div 1000;
      nn := n mod 1000;
      if nn>0 then begin
        s := GetStrInt(nn) + ' ' + Decimals1[_dec_Million] + GetRusEnding(nn,['','а','ов']);
        Result := s + ' ' + Result;
      end;

      n := n div 1000;
      nn := n mod 1000;
      if nn>0 then begin
        s := GetStrInt(nn) + ' ' + Decimals1[_dec_Milliard] + GetRusEnding(nn,['','а','ов']);
        Result := s + ' ' + Result;
      end;

      Result := Trim(Result);
      CharUpperBuff(@Result[1],1);
   end;
end;

function  GetStrFloat(f: Extended; Gender: TGenders = gndMale; const W: String = ''; const eSingle: String = ''; const eSome: String = ''; const eMulti: String = ''): String;
var
  i,j: Integer;
begin
  i := Trunc(f);
  f := f - i; j := 0;

  while (j<=5) and (f-Trunc(f)<>0) do begin Inc(j); f := f * 10.0 end;
  if j>0 then begin
    Result := GetStrInt(i,gndFemale) + ' цел' + GetRusEnding(i,DecEnds) + ' ';
    i := Round(f); while (i>0) and ((i mod 10)=0) do begin i := i div 10; Dec(j) end;
    Result := Result + IntToStr(i) + ' ' + Decimals4[j] + GetRusEnding(i,DecEnds);
  end else Result := GetStrInt(i,Gender);
  if W<>'' then Result := Result + ' ' + W + GetRusEnding(i,[eSingle,eSome,eMulti]);
end;

function GetStrDate(Date: TDateTime; isNice: Boolean = False): String;
begin
  Result := IntToStr(DayOf(Date));
  if isNice then Result := #171 + Result + #187;
  Result := Result + ' ' + AnsiLowerCase(Month_r(MonthOf(Date)));
  Result := Result + ' ' + IntToStr(YearOf(Date)) + ' г.'
end;

end.

