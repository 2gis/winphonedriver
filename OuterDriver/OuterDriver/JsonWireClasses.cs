using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterDriver
{
    public class JsonValueContent
    {
        public String sessionId;
        public String id { get; set; }
        public String[] value { get; set; }

        public JsonValueContent(String sessionId, String id, String[] value)
        {
            this.sessionId = sessionId;
            this.id = id;
            this.value = value;
        }

        public String[] GetValue()
        {
            return this.value;
        }
    }

    public class JsonResponse
    {
        public String sessionId { get; set; }
        public ResponseStatus status { get; set; }
        public Object value { get; set; }

        public JsonResponse(String sessionId, ResponseStatus responseCode, Object value)
        {
            this.sessionId = sessionId;
            this.status = responseCode;
            this.value = value;
        }
    }
}
