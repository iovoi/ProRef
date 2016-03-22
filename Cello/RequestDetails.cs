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

            // ToDo:
            //create header list

            public RequestDetails(Session s)
            {
                this.session = new Session(new SessionData(s));
                this.fullUrl = session.fullUrl;
                this.request_method = session.RequestMethod;
                headers = new List<HTTPHeaderItem>();
                foreach (HTTPHeaderItem httpHeaderItem in session.RequestHeaders)
                {
                    header_count++;
                    headers.Add(httpHeaderItem);
                }
                request_body = System.Text.Encoding.Default.GetString(session.RequestBody);
            }
        }
    }
}
