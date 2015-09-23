using System;
using System.Data.SqlTypes;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace Sql.UDF.SOAPRequest
{
    public partial class SOAP
    {
        /// <summary>
        /// Отправка произвольного SOAP запроса к WEB сервису и получение результата
        /// </summary>
        /// <param name="uri">Адрес сервиса</param>
        /// /// <param name="header">XML заголовка</param>
        /// <param name="body">XML тело запроса</param>
        [Microsoft.SqlServer.Server.SqlProcedure]
        public static void SOAPRequest(SqlString uri, ref SqlXml header, ref SqlXml body)
        {
            SqlString soapRequest = string.Empty;
            SqlString soapResponse;
            soapRequest += "<soap:Envelope ";
            soapRequest += "xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' ";
            soapRequest += "xmlns:xsd='http://www.w3.org/2001/XMLSchema' ";
            soapRequest += "xmlns:soap='http://schemas.xmlsoap.org/soap/envelope/' ";
            soapRequest += "xmlns:encodingStyle='http://schemas.xmlsoap.org/soap/encoding' >";

            // Добавляем залоловок, если он передан
            if (!header.IsNull && header.Value != string.Empty)
                soapRequest += "<soap:Header>" + header.Value.ToString() + "</soap:Header>";

            soapRequest += "<soap:Body>";
            soapRequest += body.Value.ToString();
            soapRequest += "</soap:Body></soap:Envelope>";

            var req = WebRequest.Create(uri.ToString());
            
            req.Headers.Add("SOAPAction", "\"\"");
            req.ContentType = "text/xml;charset=\"utf-8\"";
            req.Method = "POST";

            // Отправляем запрос
            using (var stm = req.GetRequestStream())
            using (var stmw = new StreamWriter(stm))
                stmw.Write(soapRequest);

            // Получаем ответ
            using (var webResponse = req.GetResponse())
            using (var responseStream = new StreamReader(webResponse.GetResponseStream(), Encoding.UTF8))
                soapResponse = responseStream.ReadToEnd();

            // Удаляем неописанные namespace из ответа (ответы WEB сервисов не всегда правильны :-( )
            soapResponse = Regex.Replace(soapResponse.Value, "SOAP-ENV:", "", RegexOptions.IgnoreCase);
            soapResponse = Regex.Replace(soapResponse.Value, "SOAP:", "", RegexOptions.IgnoreCase);

            // Возвращаем данные из <Header>
            using (var r = XmlReader.Create(new StringReader(soapResponse.Value)))
                header = r.ReadToFollowing("Header") ? new SqlXml(XmlReader.Create(new StringReader(r.ReadInnerXml()))) : SqlXml.Null;
            
            // Возвращаем данные из <Body>
            using (var r = XmlReader.Create(new StringReader(soapResponse.Value)))
                body = r.ReadToFollowing("Body") ? new SqlXml(XmlReader.Create(new StringReader(r.ReadInnerXml()))) : SqlXml.Null;

        }

    };
}

