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
            public string requestMethod;
            public int responseCode;
            public string requestBody;

            public DifferentialRequest(RequestDetails rqst_details)
            {
                this.requestDetials = rqst_details;
                headers = new List<HTTPHeaderItem>(requestDetials.headers);
                responseCode = rqst_details.responseCode;
                requestMethod = rqst_details.request_method;
                requestBody = rqst_details.request_body;
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

                if ("GET".Equals(requestMethod))
                {
                    Diff_GET(form);
                }
                else if ("POST".Equals(requestMethod))
                {

                }
            }

            public void Diff_GET(Form form)
            {
                int cookies_final_count = 0;
                reduced_cookies = new Dictionary<string, string>();
                if (null != cookies)
                {
                    while (cookies_final_count != cookies.Count)
                    {
                        KeyValuePair<string, string> cookie_name_value = cookies.Last();
                        cookies.Remove(cookies.Last().Key);
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
                                //httpRequest.IfModifiedSince = 
                                continue;
                            }

                            if ("Range".Equals(httpHeaderItem.Name))
                            {
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
                        foreach (KeyValuePair<string, string> cookie in cookies)
                        {
                            httpRequest.Headers["Cookie"] = cookie.Key + "=" + cookie.Value + "; ";
                            (form as DifferentialForm).WriteLine("cookies: " + cookie.Key + ": " + cookie.Value);
                        }
                        try
                        {
                            //(form as DifferentialForm).WriteLine(client.DownloadString(requestDetials.fullUrl));
                            httpRequest.GetResponse().Close();
                        }
                        catch (WebException we)
                        {
                            MessageBox.Show(we.ToString());
                        }
                        //ToDo: compare whether the response is different or not to determine whether the request is valid or not
                    }
                }
            }
        }
    }
}
