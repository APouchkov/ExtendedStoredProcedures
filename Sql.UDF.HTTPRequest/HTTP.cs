using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using System.IO;
using System.Net;
using System.Security.Cryptography;

public partial class HTTP
{
  struct HTTPResponseRow
  {
    public Int16	Status;
    public String	Header;
    public String	Body;
  }

  public static void FillHTTPResponseRow(object row, out SqlInt16 Status, out SqlString Header, out SqlString Body)
  {
    Status  = ((HTTPResponseRow)row).Status;
    Header	= ((HTTPResponseRow)row).Header;
    Body	  = ((HTTPResponseRow)row).Body;
  }

	[
		SqlFunction
		(
			FillRowMethodName = "FillHTTPResponseRow",
			DataAccess        = DataAccessKind.None,
			TableDefinition   = "[Status] smallint, [Header] nvarchar(max), [Body] nvarchar(max)", 
			IsDeterministic   = false
		)
	]
	public static IEnumerable HTTPRequestFunction
  (
    String AUrl,
    String AMethod
  )
	{
    Int16   RStatus;
    String  RHeader;
    String  RBody;

    HTTPRequestMethod
    (
      AUrl: AUrl, AMethod: AMethod, 
      AKeepAlive: false, AHeader: null, AParams: null, AProxy: null, AContentType: null, AUserAgent: null, ACookies: null,
      ACheckSum: false,
      RStatus: out RStatus, RHeader: out RHeader, RBody: out RBody
    );

		HTTPResponseRow LRow;
    LRow.Status = RStatus;
    LRow.Header = RHeader;
    LRow.Body   = RBody;

		List<HTTPResponseRow> LRows = new List<HTTPResponseRow>();
    LRows.Add(LRow);

    return LRows;
  }

	[
		SqlFunction
		(
			FillRowMethodName = "FillHTTPResponseRow",
			DataAccess        = DataAccessKind.None,
			TableDefinition   = "[Status] smallint, [Header] nvarchar(max), [CheckSum] nvarchar(4000)", 
			IsDeterministic   = false
		)
	]
	public static IEnumerable HTTPRequestCheckSumFunction(String AUrl, String AMethod)
	{
    Int16   RStatus;
    String  RHeader;
    String  RBody;

    HTTPRequestMethod
    (
      AUrl: AUrl, AMethod: AMethod, 
      AKeepAlive: false, AHeader: null, AParams: null, AProxy: null, AContentType: null, AUserAgent: null, ACookies: null,
      ACheckSum: true,
      RStatus: out RStatus, RHeader: out RHeader, RBody: out RBody
    );

		HTTPResponseRow LRow;
    LRow.Status = RStatus;
    LRow.Header = RHeader;
    LRow.Body   = RBody;

		List<HTTPResponseRow> LRows = new List<HTTPResponseRow>();
    LRows.Add(LRow);

    return LRows;
  }

	[SqlMethod(DataAccess = DataAccessKind.None,	IsDeterministic = false)]
	public static void HTTPRequestMethod
  (
    String  AUrl,
    String  AMethod,

    Boolean AKeepAlive,
    String  AContentType,
    String  AUserAgent,
    String  ACookies,
    String  AHeader,

    String  AParams,
    String  AProxy,

    Boolean ACheckSum,

    out Int16   RStatus,
    out String  RHeader,
    out String  RBody
  )
	{
		try
		{
			HttpWebRequest Request = (HttpWebRequest)WebRequest.Create(AUrl);

      Request.KeepAlive             = AKeepAlive;
			Request.UseDefaultCredentials = true;
			Request.Method                = AMethod;

      Request.ContentType = AContentType ?? "application/x-www-form-urlencoded"; 
      Request.Accept = "*/*";

      if(!String.IsNullOrEmpty(AUserAgent))
        Request.UserAgent = AUserAgent;

      if(!String.IsNullOrEmpty(AHeader))
        foreach(String LHeaderLine in AHeader.Split(new Char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
          Request.Headers.Add(LHeaderLine);

      if(!String.IsNullOrEmpty(ACookies))
      {
        Request.CookieContainer = new CookieContainer();
        Request.CookieContainer.SetCookies(Request.RequestUri, ACookies);
      }

      if (!String.IsNullOrEmpty(AParams) && AMethod != "GET" && AMethod != "HEAD")
      {
          byte[] LData = Encoding.ASCII.GetBytes(AParams);
          Request.ContentLength = LData.Length;
          using (Stream LStream = Request.GetRequestStream())
          {
            LStream.Write(LData, 0, LData.Length);
          }
      }

			if (!String.IsNullOrEmpty(AProxy))
			{
				WebProxy Proxy = new WebProxy(AProxy);
				Proxy.BypassProxyOnLocal    = true;
				Proxy.UseDefaultCredentials = true;

				Request.Proxy = Proxy;
			}

      using (HttpWebResponse Response = Request.GetResponse() as HttpWebResponse)
      {

        RStatus = 0;
        RHeader = Response.Headers.ToString();

        if (ACheckSum)
        {
          using (SHA1 sha1Encrypter = new SHA1CryptoServiceProvider())
          {
            RBody = BitConverter.ToString(sha1Encrypter.ComputeHash(Response.GetResponseStream()));
          }
        }
        else
        {
          StreamReader LText;
          if (Response.CharacterSet.Length > 0)
          {
            Encoding LEncoding = Encoding.GetEncoding(Response.CharacterSet);
            LText = new StreamReader(Response.GetResponseStream(), LEncoding);
          }
          else
            LText = new StreamReader(Response.GetResponseStream());
          RBody = LText.ReadToEnd();
          LText.Close();
        }
      }
		}
		catch (Exception LException)
		{
			RStatus = 1;
			RHeader = LException.Message;
			RBody   = null;
		}
	}
};
