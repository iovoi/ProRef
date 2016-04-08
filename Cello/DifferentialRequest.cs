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
using HtmlDiff;

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
            public Dictionary<string, string> remaining_bodyParam;
            public Dictionary<string, string> cookies;
            //public Dictionary<string, string> reduced_cookies;
            public Dictionary<string, string> remaining_cookies;
            public string url;
            public string urlWithoutParam;
            public Dictionary<string, string> urlParam; // this field will not be null, check count to determine whether null
            public Dictionary<string, string> remaining_urlParam;
            public string requestMethod;
            public int responseCode;
            public string requestBody;
            public Version protocolVersion;
            public string responseBodyString;
            public Dictionary<string, string> originResponseCookies;
            public DifferentialForm form;

            public DifferentialRequest(RequestDetails rqst_details, DifferentialForm diff_form)
            {
                this.requestDetials = rqst_details;
                headers = new List<HTTPHeaderItem>(requestDetials.headers);
                responseCode = rqst_details.responseCode;
                requestMethod = rqst_details.request_method;
                requestBody = rqst_details.request_body;
                protocolVersion = rqst_details.session.RequestHeaders.HTTPVersion.Contains("1.1")? HttpVersion.Version11 : HttpVersion.Version10;
                url = rqst_details.fullUrl;
                if (rqst_details.fullUrl.Contains('?'))
                {
                    urlParam = new Dictionary<string, string>();
                    string[] urlStringArray = rqst_details.fullUrl.Split(new string[] {"?"}, StringSplitOptions.RemoveEmptyEntries);
                    urlWithoutParam = urlStringArray[0];
                    string[] paramsInUrl = urlStringArray[1].Split('&');
                    foreach (string param in paramsInUrl)
                    {
                        string[] paramPair = param.Split('=');
                        Debug.Assert(paramPair.Length == 2);
                        urlParam.Add(paramPair[0], paramPair[1]);
                    }
                }
                else
                {
                    urlWithoutParam = string.Empty;
                    urlParam = new Dictionary<string, string>();
                }

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
                responseBodyString = rqst_details.session.GetResponseBodyAsString();
                form = diff_form;
                originResponseCookies = new Dictionary<string, string>();
                foreach (HTTPHeaderItem headerItem in rqst_details.session.oResponse.headers)
                {
                    if ("Set-Cookie".Equals(headerItem.Name))
                    {
                        //string[] headerValues = headerItem.Value.Split(';');
                        string cookieNameValue = headerItem.Value.Split(';')[0];
                        form.WriteLine("cookieValue: " + cookieNameValue);
                        if (cookieNameValue.Contains('='))
                        {
                            string[] cookieNameValueStringArray = cookieNameValue.Split('=');
                            originResponseCookies.Add(cookieNameValueStringArray[0], cookieNameValueStringArray[1]);
                            //(form as DifferentialForm).WriteLine("true cookie value: " + cookieNameValueStringArray[0] + "=" + cookieNameValueStringArray[1]);
                        }
                    }
                }

                //while (true) { }
            }

            public void Fire_differential_request()
            {
                Debug.Assert(null != requestDetials);

                //(form as DifferentialForm).WriteLine("response:==================\r\n" + requestDetials.session.GetResponseBodyAsString());

                // we just need to handle cookies and/or some headers in GET request
                // currently we only handled cookies
                if ("GET".Equals(requestMethod))
                {
                    Diff_GET();
                }
                // in a case of POST, we need to also handle body parameters, cookies and/or other headers
                else if ("POST".Equals(requestMethod))
                {
                    Diff_POST();
                }
                // and/or other request method to be implemented
                else
                {
                    //other request methods
                }
            }

            public void Diff_GET()
            {
                int cookies_final_count = 0;
                //reduced_cookies = new Dictionary<string, string>();
                remaining_cookies = new Dictionary<string, string>();
                remaining_urlParam = new Dictionary<string, string>();
                
                // first we replay the request first to see if it can be replayed
                //===================================================================
                HttpWebRequest replayRequest = ForgeGETRequest();
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
                            if (IsSameResponse(httpWebResponse, responseString))
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
                // for GET request, we only diff the cookies
                if (null != cookies && cookies.Count > 0)
                {
                    //while (cookies_final_count != cookies.Count)
                    while (cookies.Count > 0)
                    {
                        KeyValuePair<string, string> cookie_name_value = cookies.Last();
                        cookies.Remove(cookies.Last().Key);

                        //HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(requestDetials.fullUrl);
                        HttpWebRequest httpRequest = ForgeGETRequest();

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
                                    if (IsSameResponse(httpWebResponse, responseString))
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
                            form.WriteLine("remaining cookie count: " + remaining_cookies.Count);
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
                    (form as DifferentialForm).WriteLine("no cookie in this GET request, omit cookies");
                }

                // if this request have url parameters, we also handle this situation
                if (null != urlParam && urlParam.Count > 0)
                {
                    while (urlParam.Count > 0)
                    {
                        KeyValuePair<string, string> urlParam_name_value = urlParam.Last();
                        urlParam.Remove(urlParam.Last().Key);
                        HttpWebRequest httpRequest = ForgeGETRequest();
                        try
                        {
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
                                    if (IsSameResponse(httpWebResponse, responseString))
                                    {
                                        continue;
                                    }
                                    else
                                    {
                                        remaining_urlParam.Add(urlParam_name_value.Key, urlParam_name_value.Value);
                                    }
                                }
                            }

                            form.WriteLine("remaining url param count: " + remaining_urlParam.Count);
                        }
                        catch (WebException we)
                        {
                            MessageBox.Show(we.ToString());
                        }
                    }
                }
                else
                {
                    form.WriteLine("no url parameters in the GET request, omit url paramters");
                }
            }

            public void Diff_POST()
            {
                int cookies_final_count = 0;
                int bodyParam_final_count = 0;
                remaining_cookies = new Dictionary<string, string>();
                remaining_bodyParam = new Dictionary<string, string>();
                remaining_urlParam = new Dictionary<string, string>();
                
                // first we replay the request to see if it can be replayed
                HttpWebRequest replayRequest = ForgePOSTRequest();

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
                            if (IsSameResponse(httpWebResponse, responseString))
                            {
                                (form as DifferentialForm).WriteLine("can diff this POST request");
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

                // for POST request, we only diff cookies and body parameters
                // we handle body parameter first
                if (null != bodyParams && bodyParams.Count > 0)
                {
                    while (bodyParams.Count > 0)
                    {
                        KeyValuePair<string, string> paramBody_name_value = bodyParams.Last();
                        bodyParams.Remove(bodyParams.Last().Key);

                        HttpWebRequest httpRequest = ForgePOSTRequest();

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
                                    if (IsSameResponse(httpWebResponse, responseString))
                                    {
                                        continue;
                                    }
                                    else
                                    {
                                        remaining_bodyParam.Add(paramBody_name_value.Key, paramBody_name_value.Value);
                                        bodyParam_final_count++;
                                    }
                                }
                            }

                            form.WriteLine("remaining body param count: " + remaining_bodyParam.Count);
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
                    form.WriteLine("no cookie in this POST request, omit the body parameters");
                }

                // after the body parameters, we handle cookies
                if (null != cookies && cookies.Count > 0)
                {
                    while (cookies.Count > 0)
                    {
                        KeyValuePair<string, string> cookie_name_value = cookies.Last();
                        cookies.Remove(cookies.Last().Key);

                        //HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(requestDetials.fullUrl);
                        HttpWebRequest httpRequest = ForgePOSTRequest();

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
                                    if (IsSameResponse(httpWebResponse, responseString))
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

                            form.WriteLine("remaining cookies count: " + remaining_cookies.Count);
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
                    form.WriteLine("no cookie in this POST request");
                }

                // if there is url paramters, we handle them here
                if (null != urlParam && urlParam.Count > 0)
                {
                    while (urlParam.Count > 0)
                    {
                        KeyValuePair<string, string> urlParam_name_value = urlParam.Last();
                        urlParam.Remove(urlParam.Last().Key);
                        HttpWebRequest httpRequest = ForgePOSTRequest();
                        try
                        {
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
                                    if (IsSameResponse(httpWebResponse, responseString))
                                    {
                                        continue;
                                    }
                                    else
                                    {
                                        remaining_urlParam.Add(urlParam_name_value.Key, urlParam_name_value.Value);
                                    }
                                }
                            }

                            form.WriteLine("remaining url param count: " + remaining_urlParam.Count);
                        }
                        catch (WebException we)
                        {
                            MessageBox.Show(we.ToString());
                        }
                    }
                }
                else
                {
                    form.WriteLine("no url parameters in this POST request, omit url paramters");
                }
            }

            public HttpWebRequest ForgePOSTRequest()
            {
                HttpWebRequest httpRequest = null;
                if (!requestDetials.fullUrl.Contains('?'))
                {
                    httpRequest = (HttpWebRequest)WebRequest.Create(requestDetials.fullUrl);
                }
                else
                {
                    string url2request = urlWithoutParam;
                    int urlParamIndex = 0;

                    if (null != urlParam)
                    {
                        foreach (KeyValuePair<string, string> paramPair in urlParam)
                        {
                            if (0 == urlParamIndex)
                            {
                                url2request += "?" + paramPair.Key + "=" + paramPair.Value;
                            }
                            else
                            {
                                url2request += "&" + paramPair.Key + "=" + paramPair.Value;
                            }
                            urlParamIndex++;
                        }
                    }

                    if (null != remaining_urlParam)
                    {
                        foreach (KeyValuePair<string, string> remaining_paramPair in remaining_urlParam)
                        {
                            if (0 == urlParamIndex)
                            {
                                url2request += "?" + remaining_paramPair.Key + "=" + remaining_paramPair.Value;
                            }
                            else
                            {
                                url2request += "&" + remaining_paramPair.Key + "=" + remaining_paramPair.Value;
                            }
                            urlParamIndex++;
                        }
                    }
                    httpRequest = (HttpWebRequest)WebRequest.Create(url2request);
                }

                httpRequest.AllowAutoRedirect = false;

                httpRequest.Method = "POST";

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
                        //(form as DifferentialForm).WriteLine("http header item: " + httpHeaderItem.Name + ": " + httpHeaderItem.Value);
                        continue;
                    }
                    else
                    {
                        //(form as DifferentialForm).WriteLine("cookies: " + httpHeaderItem.Name + ": " + httpHeaderItem.Value);
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

                /*if (null != httpRequest.Headers["Cookie"])
                {
                    //(form as DifferentialForm).WriteLine(httpRequest.Headers["Cookie"].ToString());
                }
                else
                {
                    //(form as DifferentialForm).WriteLine("cookies null");
                }*/

                string newRequestBody = null;
                int paramIndex = 0;
                foreach (KeyValuePair<string, string> param in bodyParams)
                {
                    if (0 == paramIndex)
                    {
                        newRequestBody = param.Key + "=" + param.Value;
                    }
                    else
                    {
                        newRequestBody += "&" + param.Key + "=" + param.Value;
                    }
                    paramIndex++;
                }

                foreach (KeyValuePair<string, string> p in remaining_bodyParam)
                {
                    if (0 == paramIndex)
                    {
                        newRequestBody = p.Key + "=" + p.Value;
                    }
                    else
                    {
                        newRequestBody += "&" + p.Key + "=" + p.Value;
                    }
                    paramIndex++;
                }

                if (null != requestBody && null != newRequestBody)
                {
                    byte[] requestBodyByteArray = System.Text.Encoding.Default.GetBytes(newRequestBody);
                    if (null != requestBodyByteArray)
                    {
                        httpRequest.ContentLength = requestBodyByteArray.Length;
                    }
                    else
                    {
                        httpRequest.ContentLength = 0;
                    }
                    using (Stream requestStream = httpRequest.GetRequestStream())
                    {
                        requestStream.Write(requestBodyByteArray, 0, requestBodyByteArray.Length);
                    }
                }
                else
                {
                    httpRequest.ContentLength = 0;
                }

                return httpRequest;
            }

            public HttpWebRequest ForgeGETRequest()
            {
                HttpWebRequest httpRequest = null;
                if (!requestDetials.fullUrl.Contains('?'))
                {
                    httpRequest = (HttpWebRequest)WebRequest.Create(requestDetials.fullUrl);
                }
                else
                {
                    string url2request = urlWithoutParam;
                    int urlParamIndex = 0;

                    if (null != urlParam)
                    {
                        foreach (KeyValuePair<string, string> paramPair in urlParam)
                        {
                            if (0 == urlParamIndex)
                            {
                                url2request += "?" + paramPair.Key + "=" + paramPair.Value;
                            }
                            else
                            {
                                url2request += "&" + paramPair.Key + "=" + paramPair.Value;
                            }
                            urlParamIndex++;
                        }
                    }

                    if (null != remaining_urlParam)
                    {
                        foreach (KeyValuePair<string, string> remaining_paramPair in remaining_urlParam)
                        {
                            if (0 == urlParamIndex)
                            {
                                url2request += "?" + remaining_paramPair.Key + "=" + remaining_paramPair.Value;
                            }
                            else
                            {
                                url2request += "&" + remaining_paramPair.Key + "=" + remaining_paramPair.Value;
                            }
                            urlParamIndex++;
                        }
                    }
                    httpRequest = (HttpWebRequest)WebRequest.Create(url2request);
                }

                httpRequest.AllowAutoRedirect = false;

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
                        //(form as DifferentialForm).WriteLine("http header item: " + httpHeaderItem.Name + ": " + httpHeaderItem.Value);
                        continue;
                    }
                    else
                    {
                        //(form as DifferentialForm).WriteLine("cookies: " + httpHeaderItem.Name + ": " + httpHeaderItem.Value);
                        continue;
                    }
                }
                int cookieIndex = 0;
                if (null != cookies)
                {
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
                }

                if (null != remaining_cookies)
                {
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
                }

                if (null != httpRequest.Headers["Cookie"])
                {
                    //(form as DifferentialForm).WriteLine(httpRequest.Headers["Cookie"].ToString());
                }
                else
                {
                    //(form as DifferentialForm).WriteLine("cookies null");
                }

                return httpRequest;
            }

            public bool IsSameResponse(HttpWebResponse httpWebResponse, string htmlString)
            {
                if (((int)httpWebResponse.StatusCode) != this.responseCode)
                {
                    //MessageBox.Show("response code changed");
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
                            //(form as DifferentialForm).WriteLine("headerValue: " + headerValue);
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

                /*foreach (KeyValuePair<string, string> cookiePair in originResponseCookies)
                {
                    string value;
                    if (responseCookiesDict.TryGetValue(cookiePair.Key, out value))
                    {
                        continue;
                    }
                    else
                    {
                        return false;
                    }
                }*/

                // if we delete a cookie (i.e. X) and later the response html string is the same and
                // there is a header set-cookie to set that cookie (i.e. X), we consider that cookie
                // is useless.
                // if we delete a cookie (i.e. X) and later the response html string is different then
                // we consider that the cookie is critical no matter there is a header set-cookie to X or not
                // but if the set-cookie X header occur with the meaningful value and html string is different 
                // we consider there is a vulnerability (not yet implemented)

                bool cookieCompareResult = false;
                foreach (KeyValuePair<string, string> cookiePair in responseCookiesDict)
                {
                    string value;
                    if (originResponseCookies.TryGetValue(cookiePair.Key, out value))
                    {
                        cookieCompareResult = true;
                        continue;
                    }
                    else
                    {
                        //return false;
                        cookieCompareResult = false;
                        break;
                    }
                }

                // compare response string if it exists
                bool htmlStringCompareResult = true; // default to true, because if there is no response html, then it should be the same
                if ((null != htmlString && htmlString.Length > 0) && (null != responseBodyString && responseBodyString.Length > 0))
                {
                    HtmlDiff.HtmlDiff diffHelper = new HtmlDiff.HtmlDiff(requestDetials.session.GetResponseBodyAsString(), htmlString);
                    string diffOutput = diffHelper.Build();
                    double diffLength = 0;
                    foreach (Match m in diffHelper._matches)
                    {
                        diffLength += m.Size;
                        //(form as DifferentialForm).WriteLine("Match: " + m.StartInOld + " " + m.StartInNew + " " + m.Size);
                    }

                    if (!IsWithinThreshold(responseBodyString.Length, htmlString.Length))
                    {
                        using (System.IO.StreamWriter file =
                                            new System.IO.StreamWriter("D:\\data\\workspace\\VisualStudioProject\\Cello\\output\\response_original_"
                                                + DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + ".html"))
                        {
                            file.WriteLine(responseBodyString);
                        }

                        using (System.IO.StreamWriter file =
                                            new System.IO.StreamWriter("D:\\data\\workspace\\VisualStudioProject\\Cello\\output\\response_test_"
                                                + DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + ".html"))
                        {
                            file.WriteLine(htmlString);
                        }
                        //return false;
                        htmlStringCompareResult = false;
                    }
                    else
                    {
                        htmlStringCompareResult = true;
                    }
                    //(form as DifferentialForm).WriteLine(responseBodyString);
                    //(form as DifferentialForm).WriteLine("originalStringLength: " + responseBodyString.Length);
                    //(form as DifferentialForm).WriteLine("responseStringLength: " + htmlString.Length);

                    //(form as DifferentialForm).WriteLine("diffOutput:");
                    //(form as DifferentialForm).WriteLine(diffOutput);
                    //(form as DifferentialForm).WriteLine(requestDetials.session.GetResponseBodyAsString());
                }
                else
                {
                    //MessageBox.Show("response not match, either original response doesn't have response html or the test response doesn't have response html");
                }

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

                //return true;

                int htmlStringLength = 0;
                int responseStringLength = 0;
                if (null != htmlString)
                {
                    htmlStringLength = htmlString.Length;
                }
                else
                {
                    htmlStringLength = 0;
                }

                if (null != responseBodyString)
                {
                    responseStringLength = responseBodyString.Length;
                }
                else
                {
                    responseStringLength = 0;
                }

                if ((0 == htmlStringLength && 0 != responseStringLength) || (0 != htmlStringLength && 0 == responseStringLength))
                {
                    return false;
                }
                else
                {
                    if (htmlStringCompareResult)
                    {
                        return true;
                    }
                    else if (!htmlStringCompareResult && cookieCompareResult)
                    {
                        return false;
                    }
                    else if (!htmlStringCompareResult && !cookieCompareResult)
                    {
                        MessageBox.Show("a possible vulnerability found");
                        return false;
                    }
                    else
                    {
                        throw new Exception("compare html diff error");
                    }
                }
            }

            public bool IsWithinThreshold(int originLength, int newLength)
            {
                // we use the percentage of different to determine whether they are the same response or not
                // if the difference percentage is less than 1%, we consider them as same response body string
                // differece percentage = | originLength - matches length | / originLength
                
                // we use 0.01 as the threshold
                double threshold = 0.01;
                return (Math.Abs(originLength - newLength) / (double)originLength) < threshold;
            }
            
            // ToDo:
            // 1. improve the same response algorithm; 2. implement the url parameter differentiation; 3. think how to use Match.size;
        }
    }
}
