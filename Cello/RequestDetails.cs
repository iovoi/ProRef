using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fiddler;

namespace Cello
{
    namespace Utils
    {
        class RequestDetails
        {
            public Session session = null;
            public string fullUrl;
            public string request_method;
            public List<HTTPHeaderItem> headers;
            public int header_count = 0;
            public string request_body;
            public int responseCode;
            public Dictionary<string, string> cookies;

            // ToDo:
            //create header list

            public RequestDetails(Session s)
            {
                this.session = new Session(new SessionData(s));
                this.fullUrl = session.fullUrl;
                this.request_method = session.RequestMethod;
                this.responseCode = s.responseCode;
                headers = new List<HTTPHeaderItem>();
                foreach (HTTPHeaderItem httpHeaderItem in session.RequestHeaders)
                {
                    header_count++;
                    headers.Add(httpHeaderItem);
                    if ("Cookie".Equals(httpHeaderItem.Name))
                    {
                        cookies = new Dictionary<string,string>();
                        string cookiesValue = httpHeaderItem.Value;
                        foreach (string nameValuePair in cookiesValue.Split(';'))
                        {
                            string[] nameValue = nameValuePair.Split('=');
                            cookies.Add(nameValue[0], nameValue[1]);
                        }
                    }
                }
                request_body = System.Text.Encoding.Default.GetString(session.RequestBody);
            }
        }
    }
}
