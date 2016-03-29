using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Diagnostics;
using System.Collections.Generic;
using Fiddler;
using System.IO;
using System.Net;
using System.IO.Compression;

namespace Cello
{
    namespace Utils
    {
        class DifferentialRequest
        {
            public RequestDetails requestDetials;
            public List<HTTPHeaderItem> headers;
            public List<HTTPHeaderItem> reduced_headers;
            public Dictionary<string, string> bodyParams;
            public Dictionary<string, string> reduced_bodyParams;
            public Dictionary<string, string> cookies;
            public Dictionary<string, string> reduced_cookies;
            public Dictionary<string, string> remaining_cookies;
            public string requestMethod;
            public int responseCode;
            public string requestBody;
            public Version protocolVersion;

            public DifferentialRequest(RequestDetails rqst_details)
            {
                this.requestDetials = rqst_details;
                headers = new List<HTTPHeaderItem>(requestDetials.headers);
                responseCode = rqst_details.responseCode;
                requestMethod = rqst_details.request_method;
                requestBody = rqst_details.request_body;
                protocolVersion = rqst_details.session.RequestHeaders.HTTPVersion.Contains("1.1")? HttpVersion.Version11 : HttpVersion.Version10;
                if (null != requestBody && ! "".Equals(requestBody))
                {
                    bodyParams = new Dictionary<string, string>();
                    foreach (string nameValuePairString in requestBody.Split('&'))
                    {
                        string[] nameAndValue = nameValuePairString.Split('=');
                        bodyParams.Add(nameAndValue[0], nameAndValue[1]);
                    }
                }
                if (null != rqst_details.cookies)
                {
                    cookies = new Dictionary<string, string>(rqst_details.cookies);
                }
            }

            public void Fire_differential_request(Form form)
            {
                Debug.Assert(null != requestDetials);

                // we just need to handle cookies and/or some headers in GET request
                if ("GET".Equals(requestMethod))
                {
                    Diff_GET(form);
                }
                // in a case of POST, we need to also handle body parameters, cookies and/or other headers
                else if ("POST".Equals(requestMethod))
                {
                    Diff_POST(form);
                }
                // and/or other request method to be implemented
                else
                {
                    //other request methods
                }
            }

            public void Diff_GET(Form form)
            {
                int cookies_final_count = 0;
                reduced_cookies = new Dictionary<string, string>();
                remaining_cookies = new Dictionary<string, string>();
                
                // for GET request, we only diff the cookies
                if (null != cookies)
                {
                    //===================================================================
                    HttpWebRequest replayRequest = ForgeGETRequest(form);
                    try
                    {
                        //(form as DifferentialForm).WriteLine(client.DownloadString(requestDetials.fullUrl));
                        //httpRequest.GetResponse().Close();
                        using (HttpWebResponse httpWebResponse = (HttpWebResponse)replayRequest.GetResponse())
                        using (Stream stream = httpWebResponse.GetResponseStream())
                        {
                            Stream responseStream = stream;
                            if (httpWebResponse.ContentEncoding.ToLower().Contains("gzip"))
                            {
                                responseStream = new GZipStream(stream, CompressionMode.Decompress);
                            }
                            else if (httpWebResponse.ContentEncoding.ToLower().Contains("deflate"))
                            {
                                responseStream = new DeflateStream(stream, CompressionMode.Decompress);
                            }

                            using (StreamReader responseReader = new StreamReader(responseStream))
                            {
                                string responseString = responseReader.ReadToEnd();
                                //responseString = System.Text.Encoding.UTF8.GetString(responseString);
                                //(form as DifferentialForm).WriteLine(responseString);
                                if (IsSameResponse(httpWebResponse, responseString, form))
                                {
                                    (form as DifferentialForm).WriteLine("can diff this GET request");
                                }
                                else
                                {
                                    (form as DifferentialForm).WriteLine("replay failed");
                                }
                            }
                        }
                    }
                    catch (WebException we)
                    {
                        MessageBox.Show(we.ToString());
                    }
                    //=====================================
                    while (cookies_final_count != cookies.Count)
                    {
                        KeyValuePair<string, string> cookie_name_value = cookies.Last();
                        cookies.Remove(cookies.Last().Key);

                        //HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(requestDetials.fullUrl);
                        HttpWebRequest httpRequest = ForgeGETRequest(form);

                        /*httpRequest.Method = "GET";

                        httpRequest.ProtocolVersion = requestDetials.session.RequestHeaders.HTTPVersion.Contains("1.1")? HttpVersion.Version11 : HttpVersion.Version10;

                        foreach (HTTPHeaderItem httpHeaderItem in headers)
                        {
                            // please refer to https://msdn.microsoft.com/en-us/library/system.net.httpwebrequest.headers(v=vs.110).aspx
                            // for more details
                            if ("Accept".Equals(httpHeaderItem.Name))
                            {
                                httpRequest.Accept = httpHeaderItem.Value;
                                continue;
                            }

                            if ("Connection".Equals(httpHeaderItem.Name))
                            {
                                //httpRequest.Connection = httpHeaderItem.Value;
                                httpRequest.KeepAlive = httpHeaderItem.Value.Contains("Keep-Alive") ? true : false;
                                continue;
                            }

                            if ("Content-Length".Equals(httpHeaderItem.Name))
                            {
                                // impossible to exist here in a GET request
                                continue;
                            }

                            if ("Content-Type".Equals(httpHeaderItem.Name))
                            {
                                httpRequest.ContentType = httpHeaderItem.Value;
                                continue;
                            }

                            if ("Expect".Equals(httpHeaderItem.Name))
                            {
                                httpRequest.Expect = httpHeaderItem.Value;
                                continue;
                            }

                            if ("Date".Equals(httpHeaderItem.Name))
                            {
                                // not implemented yet
                                //httpRequest.Date = 
                                continue;
                            }

                            if ("Host".Equals(httpHeaderItem.Name))
                            {
                                httpRequest.Host = httpHeaderItem.Value;
                                continue;
                            }

                            if ("If-Modified-Since".Equals(httpHeaderItem.Name))
                            {
                                // not implemented yet
                                //httpRequest.IfModifiedSince = 
                                continue;
                            }

                            if ("Range".Equals(httpHeaderItem.Name))
                            {
                                // not implemented yet
                                //httpRequest.AddRange = 
                                continue;
                            }

                            if ("Referer".Equals(httpHeaderItem.Name))
                            {
                                httpRequest.Referer = httpHeaderItem.Value;
                                continue;
                            }

                            if ("Transfer-Encoding".Equals(httpHeaderItem.Name))
                            {
                                httpRequest.TransferEncoding = httpHeaderItem.Value;
                                continue;
                            }

                            if ("User-Agent".Equals(httpHeaderItem.Name))
                            {
                                httpRequest.UserAgent = httpHeaderItem.Value;
                                continue;
                            }

                            if (!"Cookie".Equals(httpHeaderItem.Name))
                            {
                                httpRequest.Headers[httpHeaderItem.Name] = httpHeaderItem.Value;
                                (form as DifferentialForm).WriteLine("http header item: " + httpHeaderItem.Name + ": " + httpHeaderItem.Value);
                                continue;
                            }
                            else
                            {
                                (form as DifferentialForm).WriteLine("cookies: " + httpHeaderItem.Name + ": " + httpHeaderItem.Value);
                                continue;
                            }
                        }
                        int cookieIndex = 0;
                        foreach (KeyValuePair<string, string> cookie in cookies)
                        {
                            if (0 == cookieIndex)
                            {
                                httpRequest.Headers["Cookie"] = cookie.Key + "=" + cookie.Value;
                            }
                            else
                            {
                                httpRequest.Headers["Cookie"] += ";" + cookie.Key + "=" + cookie.Value;
                            }
                            cookieIndex++;
                        }

                        foreach (KeyValuePair<string, string> remaining_cookie in remaining_cookies)
                        {
                            if (0 == cookieIndex)
                            {
                                httpRequest.Headers["Cookie"] = remaining_cookie.Key + "=" + remaining_cookie.Value;
                            }
                            else
                            {
                                httpRequest.Headers["Cookie"] += ";" + remaining_cookie.Key + "=" + remaining_cookie.Value;
                            }
                            cookieIndex++;
                        }

                        if (null != httpRequest.Headers["Cookie"])
                        {
                            (form as DifferentialForm).WriteLine(httpRequest.Headers["Cookie"].ToString());
                        }
                        else
                        {
                            //(form as DifferentialForm).WriteLine("cookies null");
                        }*/
                        try
                        {
                            //(form as DifferentialForm).WriteLine(client.DownloadString(requestDetials.fullUrl));
                            //httpRequest.GetResponse().Close();
                            using (HttpWebResponse httpWebResponse = (HttpWebResponse)httpRequest.GetResponse())
                            using (Stream stream = httpWebResponse.GetResponseStream())
                            {
                                Stream responseStream = stream;
                                if (httpWebResponse.ContentEncoding.ToLower().Contains("gzip"))
                                {
                                    responseStream = new GZipStream(stream, CompressionMode.Decompress);
                                }
                                else if (httpWebResponse.ContentEncoding.ToLower().Contains("deflate"))
                                {
                                    responseStream = new DeflateStream(stream, CompressionMode.Decompress);
                                }

                                using (StreamReader responseReader = new StreamReader(responseStream))
                                {
                                    string responseString = responseReader.ReadToEnd();
                                    //responseString = System.Text.Encoding.UTF8.GetString(responseString);
                                    //(form as DifferentialForm).WriteLine(responseString);
                                    if (IsSameResponse(httpWebResponse, responseString, form))
                                    {
                                        continue;
                                    }
                                    else
                                    {
                                        remaining_cookies.Add(cookie_name_value.Key, cookie_name_value.Value);
                                        cookies_final_count++;
                                    }
                                }
                            }
                        }
                        catch (WebException we)
                        {
                            MessageBox.Show(we.ToString());
                        }
                        //ToDo: compare whether the response is different or not to determine whether the request is valid or not
                    }
                }
                else
                {
                    (form as DifferentialForm).WriteLine("no cookie in this GET request, omit this request");
                }
            }

            public void Diff_POST(Form form)
            {

            }

            public HttpWebRequest ForgeGETRequest(Form form)
            {
                HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(requestDetials.fullUrl);

                httpRequest.Method = "GET";

                httpRequest.ProtocolVersion = requestDetials.session.RequestHeaders.HTTPVersion.Contains("1.1")? HttpVersion.Version11 : HttpVersion.Version10;

                foreach (HTTPHeaderItem httpHeaderItem in headers)
                {
                    // please refer to https://msdn.microsoft.com/en-us/library/system.net.httpwebrequest.headers(v=vs.110).aspx
                    // for more details
                    if ("Accept".Equals(httpHeaderItem.Name))
                    {
                        httpRequest.Accept = httpHeaderItem.Value;
                        continue;
                    }

                    if ("Connection".Equals(httpHeaderItem.Name))
                    {
                        //httpRequest.Connection = httpHeaderItem.Value;
                        httpRequest.KeepAlive = httpHeaderItem.Value.Contains("Keep-Alive") ? true : false;
                        continue;
                    }

                    if ("Content-Length".Equals(httpHeaderItem.Name))
                    {
                        // impossible to exist here in a GET request
                        continue;
                    }

                    if ("Content-Type".Equals(httpHeaderItem.Name))
                    {
                        httpRequest.ContentType = httpHeaderItem.Value;
                        continue;
                    }

                    if ("Expect".Equals(httpHeaderItem.Name))
                    {
                        httpRequest.Expect = httpHeaderItem.Value;
                        continue;
                    }

                    if ("Date".Equals(httpHeaderItem.Name))
                    {
                        // not implemented yet
                        //httpRequest.Date = 
                        continue;
                    }

                    if ("Host".Equals(httpHeaderItem.Name))
                    {
                        httpRequest.Host = httpHeaderItem.Value;
                        continue;
                    }

                    if ("If-Modified-Since".Equals(httpHeaderItem.Name))
                    {
                        // not implemented yet
                        //httpRequest.IfModifiedSince = 
                        continue;
                    }

                    if ("Range".Equals(httpHeaderItem.Name))
                    {
                        // not implemented yet
                        //httpRequest.AddRange = 
                        continue;
                    }

                    if ("Referer".Equals(httpHeaderItem.Name))
                    {
                        httpRequest.Referer = httpHeaderItem.Value;
                        continue;
                    }

                    if ("Transfer-Encoding".Equals(httpHeaderItem.Name))
                    {
                        httpRequest.TransferEncoding = httpHeaderItem.Value;
                        continue;
                    }

                    if ("User-Agent".Equals(httpHeaderItem.Name))
                    {
                        httpRequest.UserAgent = httpHeaderItem.Value;
                        continue;
                    }

                    if (!"Cookie".Equals(httpHeaderItem.Name))
                    {
                        httpRequest.Headers[httpHeaderItem.Name] = httpHeaderItem.Value;
                        (form as DifferentialForm).WriteLine("http header item: " + httpHeaderItem.Name + ": " + httpHeaderItem.Value);
                        continue;
                    }
                    else
                    {
                        (form as DifferentialForm).WriteLine("cookies: " + httpHeaderItem.Name + ": " + httpHeaderItem.Value);
                        continue;
                    }
                }
                int cookieIndex = 0;
                foreach (KeyValuePair<string, string> cookie in cookies)
                {
                    if (0 == cookieIndex)
                    {
                        httpRequest.Headers["Cookie"] = cookie.Key + "=" + cookie.Value;
                    }
                    else
                    {
                        httpRequest.Headers["Cookie"] += ";" + cookie.Key + "=" + cookie.Value;
                    }
                    cookieIndex++;
                }

                foreach (KeyValuePair<string, string> remaining_cookie in remaining_cookies)
                {
                    if (0 == cookieIndex)
                    {
                        httpRequest.Headers["Cookie"] = remaining_cookie.Key + "=" + remaining_cookie.Value;
                    }
                    else
                    {
                        httpRequest.Headers["Cookie"] += ";" + remaining_cookie.Key + "=" + remaining_cookie.Value;
                    }
                    cookieIndex++;
                }

                if (null != httpRequest.Headers["Cookie"])
                {
                    (form as DifferentialForm).WriteLine(httpRequest.Headers["Cookie"].ToString());
                }
                else
                {
                    //(form as DifferentialForm).WriteLine("cookies null");
                }

                return httpRequest;
            }

            public bool IsSameResponse(HttpWebResponse httpWebResponse, string htmlString, Form form)
            {
                if (((int)httpWebResponse.StatusCode) != this.responseCode)
                {
                    return false;
                }
                
                if (httpWebResponse.ProtocolVersion != protocolVersion)
                {
                    return false;
                }

                // can only get one instance of Set-Cookie header from response
                // so we are switching to other solution below
                //foreach (string header in httpWebResponse.Headers)
                //{
                    // content-length is out of our consideration because
                    // response content-length might be different everytime
                    // also not consider two specific headers: 
                    // content-encoding and transfer-encoding
                    // for simplicity we only consider set-cookie headers,
                    // response status code and body string if it exists
                    //if (!"Content-Encoding".Equals(header) && !"Transfer-Encoding".Equals(header))
                    //{
                    //    string headerValue = httpWebResponse.GetResponseHeader(header);
                    //}
                    //(form as DifferentialForm).WriteLine("Headers: " + header);
                    //if ("Set-Cookie".Equals(header))
                    //{
                    //    string headerValue = httpWebResponse.GetResponseHeader(header);
                    //    string cookieValue = headerValue.Split(';')[0];
                    //    (form as DifferentialForm).WriteLine("cookieValue: " + cookieValue);
                    //}
                //}

                Dictionary<string, string> responseCookiesDict = new Dictionary<string,string>();
                foreach (string header in httpWebResponse.Headers)
                {
                    // content-length is out of our consideration because
                    // response content-length might be different everytime
                    // also not consider two specific headers: 
                    // content-encoding and transfer-encoding
                    // for simplicity we only consider set-cookie headers,
                    // response status code and body string if it exists
                    //if (!"Content-Encoding".Equals(header) && !"Transfer-Encoding".Equals(header))
                    //{
                    //    string headerValue = httpWebResponse.GetResponseHeader(header);
                    //}
                    //(form as DifferentialForm).WriteLine("Headers: " + header);
                    if ("Set-Cookie".Equals(header))
                    {
                        string[] headerValues = httpWebResponse.Headers.GetValues(header);
                        //string[] cookieValue = new string[]; 
                        foreach (string headerValue in headerValues)
                        {
                            (form as DifferentialForm).WriteLine("headerValue: " + headerValue);
                            string headerKeyValuePairString = headerValue.Split(';')[0];
                            if (headerKeyValuePairString.Contains('='))
                            {
                                string[] cookieNameValueStringArray = headerKeyValuePairString.Split('=');
                                responseCookiesDict.Add(cookieNameValueStringArray[0], cookieNameValueStringArray[1]);
                                //(form as DifferentialForm).WriteLine("true cookie value: " + cookieNameValueStringArray[0] + "=" + cookieNameValueStringArray[1]);
                            }
                        }
                    }
                }

                foreach (KeyValuePair<string, string> responseCookies in responseCookiesDict)
                {
                    // no sure if the cookie values are all the same
                    // we give up comparing the cookie value
                    // and give up finding the same cookie name from response of session object
                }

                // compare response string if it exists


                // not working code snippet
                //if (null == httpWebResponse.Cookies)
                //{
                //    (form as DifferentialForm).WriteLine("response cookie null");
                //}
                //else
                //{
                //    (form as DifferentialForm).WriteLine("response cookie size: " + httpWebResponse.Cookies.Count.ToString());
                //    (form as DifferentialForm).WriteLine("response cookie not null");
                //}
                //foreach (Cookie responseCookie in httpWebResponse.Cookies)
                //{
                //    string cookieName = responseCookie.Name;
                //    string cookieValue = responseCookie.Value;
                //    (form as DifferentialForm).WriteLine("response cookies:");
                //    (form as DifferentialForm).WriteLine("cookie: " + cookieName + "=" + cookieValue);
                //}
                return true;
            }
        }
    }
}
