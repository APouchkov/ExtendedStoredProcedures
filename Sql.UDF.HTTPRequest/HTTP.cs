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
			DataAccess = DataAccessKind.None,
			TableDefinition = "[Status] smallint, [Header] nvarchar(max), [Body] nvarchar(max)", 
			IsDeterministic = true
		)
	]
	public static IEnumerable HTTPRequest(String AUrl, String AMethod, String AProxy)
	{
    return InternalHTTPRequest(AUrl, AMethod, AProxy);
  }

	[
		SqlFunction
		(
			FillRowMethodName = "FillHTTPResponseRow",
			DataAccess = DataAccessKind.None,
			TableDefinition = "[Status] smallint, [Header] nvarchar(max), [CheckSum] nvarchar(4000)", 
			IsDeterministic = true
		)
	]
	public static IEnumerable HTTPRequestCheckSum(String AUrl, String AMethod, String AProxy)
	{
    return InternalHTTPRequest(AUrl, AMethod, AProxy, true);
  }

	public static IEnumerable InternalHTTPRequest(String AUrl, String AMethod, String AProxy, Boolean ACheckSum = false)
	{
		HTTPResponseRow row;
		List<HTTPResponseRow> rows = new List<HTTPResponseRow>();

		try
		{
			WebRequest Request = WebRequest.Create(AUrl);

			Request.UseDefaultCredentials = true;
			Request.Method                = AMethod;

			if (!String.IsNullOrEmpty(AProxy))
			{
				WebProxy Proxy = new WebProxy(AProxy);
				Proxy.BypassProxyOnLocal    = true;
				Proxy.UseDefaultCredentials = true;

				Request.Proxy = Proxy;
			}

      HttpWebResponse Response = Request.GetResponse() as HttpWebResponse;
      row.Status = 0;
			row.Header = Response.Headers.ToString();

      if(ACheckSum)
      {
        SHA1 sha1Encrypter = new SHA1CryptoServiceProvider();
        row.Body = BitConverter.ToString(sha1Encrypter.ComputeHash(Response.GetResponseStream()));
      }
      else
      { 
        StreamReader LText;
        if(Response.CharacterSet.Length > 0)
        {
          Encoding LEncoding = Encoding.GetEncoding(Response.CharacterSet);
          LText = new StreamReader(Response.GetResponseStream(), LEncoding);
        }
        else
          LText = new StreamReader(Response.GetResponseStream());
			  row.Body = LText.ReadToEnd();
      }

			rows.Add(row);
		}
		catch (Exception LException)
		{
			row.Status = 1;
			row.Header = LException.Message;
			row.Body   = null;
			rows.Add(row);
		}
		return rows;
	}
};
