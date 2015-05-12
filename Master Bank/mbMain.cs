using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Reflection;
using System.Xml;
using System.Data.SqlTypes;
using Sockets;
using System.IO;
using Microsoft.SqlServer.Server;
using System.Windows.Forms;

public class T
{
    private const String fnFlag = "FLAG";
    private const String fnSign = "SIGNATURE";
    private const String fnVersion = "VERSION";
    private const String HeaderId = "MBDP";    
    private const int PROTOCOL_VERSION = 200;
    private const String NameSep = ":";
    private const Char Sep = (char)0x1C;
    
    private struct NameVal
    {
        public string Name;
        public string Value;
    };

    private static string BinToStr(ref byte[] b, int count)
    {
        string s = "";
        for (int i = 0; i < count; i++)
        {
            s += (char)b[i];
        }        
        byte[] c = new byte[b.Length - count];
        Array.Copy(b, count++, c, 0, c.Length);
        b = c;         
        return s;
    }

    private static int BinToInt(ref byte[] b)
    {
        int i = 0;
        i = b[0] + b[1] * 16 + b[2] * 256 + b[3] * 4096;

        byte[] c = new byte[b.Length - 4];
        if (b.Length > 4)
        {
            Array.Copy(b, 4, c, 0, c.Length);
        }
        b = c;
        return i;
    }   
    
    private static void StrToBin(ref byte[] b, string s)
    {        
        int L =  b.Length;
        Array.Resize(ref b, L + s.Length);
        for (int i = 0; i < s.Length; i++)
        {
            b[L + i] = (byte)s[i];
        }
    }

    private static void IntToBin(ref byte[] b, int n)
    {        
        string s = String.Format("{0:X}", n);
        if (s.Length % 2 == 1)
        {
            s = "0" + s;
        }
        int k = b.Length;
        Array.Resize(ref b, b.Length + 4);
        for (int i = s.Length; i > 0; i -= 2)
        {
            b[k] = Convert.ToByte(Convert.ToUInt32(s.Substring(i - 2, 2), 16));
            k++;
        }        
    }

    private static string ValByName(NameVal[] nv, string Name)
    {
        for(int i = 0; i < nv.Length; i ++)
        {
            if (nv[i].Name == Name) 
            {
                return nv[i].Value;
            }
        }
        return "";
    }

    private static void CheckFieldName(string s)
    {
        bool Result = s.Length == 3;
        if (Result)
        {
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                Result = !((c < '0') || (c > '9'));
                if (!Result) 
                {
                    break;
                }
            }
        }
        
        if (!Result)
        {
            throw new Exception(String.Format("Неверное имя поля '{0}'", s));
        }
    }

    private static void CheckFieldValue(string s)
    {
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (!(((c >= ' ') && (c <= '~')) || ((c >= 'А') && (c <= 'п')) || ((c >= 'р') && (c <= 'ё'))))
            {
                throw new Exception(String.Format("Неверное значение '{0}'", s));
            }
        }
    }

    [SqlFunction(FillRowMethodName = "ListRow")]
    public static IEnumerable xp_MasterBank_SendCmd(SqlString Host, int Port, SqlXml Data)
    {
        XmlDocument xd = new XmlDocument();        
        xd.LoadXml("<root>" + Data.Value + "</root>");       
        XmlNodeList items = xd.GetElementsByTagName("row");
        NameVal[] nv = new NameVal[items.Count + 1];
        
        for (int i = 0; i < items.Count; i++)
        {
            nv[i].Name = items[i].Attributes[0].Value;
            CheckFieldName(nv[i].Name);
            nv[i].Value = items[i].Attributes[1].Value;
        };

        byte[] Msg = new byte[0];        
        StrToBin(ref Msg, HeaderId);
        IntToBin(ref Msg, PROTOCOL_VERSION);        
        
        string sPack = "";
        for (int i = 0; i < nv.Length; i++)
        {
            sPack += nv[i].Name + NameSep + nv[i].Value + Sep;
        };
        IntToBin(ref Msg, sPack.Length);
                
        string sFL = ValByName(nv, fnFlag);
        if (sFL == "") 
        { 
            sFL = "1" ;
        };
        IntToBin(ref Msg, int.Parse(sFL));
        StrToBin(ref Msg, sPack);

        Sock.CreateSocket(Host.ToString(), Port, false);
        
        Sock.SendDataToServer(Msg);

        byte[] OutMsg = Sock.GetDataFromServer();        
        
        nv = new NameVal[3];

        nv[0].Name = fnSign;        
        nv[0].Value = BinToStr(ref OutMsg, HeaderId.Length);
        
        nv[1].Name = fnVersion;
        nv[1].Value = BinToInt(ref OutMsg).ToString();
        int Size = BinToInt(ref OutMsg);        
        
        nv[2].Name = fnFlag;
        nv[2].Value = BinToInt(ref OutMsg).ToString();

        while (Size > OutMsg.Length)
        {
            byte[] tmp = Sock.GetDataFromServer();
            Array.Resize(ref OutMsg, OutMsg.Length + tmp.Length);
            tmp.CopyTo(OutMsg, OutMsg.Length - tmp.Length);
        }

        string Answer = BinToStr(ref OutMsg, Size);
        String sVal, sName;
        int idx, k;

        while (Answer.Length > 0)
        {
            idx = Answer.IndexOf(Sep);
            k = 0;
            if (idx < 0)
            {
                k++;
                idx = Answer.Length;
            }
            sName = Answer.Substring(0, idx);
            if (idx > Answer.Length)
            {
                idx = Answer.Length - 1;
            }
            Answer = Answer.Substring(idx + 1 - k);

            idx = sName.IndexOf(NameSep);
            sVal = sName.Substring(idx + 1);
            sName = sName.Trim().Substring(0, idx);
            while (sName.Length < 3)
            {
                sName = "0" + sName;
            }

            Array.Resize(ref nv, nv.Length + 1);
            CheckFieldName(sName);
            CheckFieldValue(sVal);
            nv[nv.Length - 1].Name = sName;
            nv[nv.Length - 1].Value = sVal;
        }
        return nv;        
    }
    
    public static void ListRow(Object nv, out string Id, out string Value)
    {        
        NameVal NV = (NameVal)nv;
        Id = NV.Name;
        Value = NV.Value;
    }
}