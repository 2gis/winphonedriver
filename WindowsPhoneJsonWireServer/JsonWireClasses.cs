using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsPhoneJsonWireServer
{
    public class FindElementObject
    {
        [JsonProperty("using")]
        public String usingMethod { get; set; }
        public String sessionId { get; set; }
        public String value { get; set; }

        public String getValue()
        {
            return value;
        }

    }

    public class JsonKeysContent
    {
        public String sessionId { get; set; }
        public String[] value { get; set; }

        public String[] GetValue()
        {
            return this.value;
        }
    }

    public class WebElement
    {
        public String ELEMENT { get; set; }

        public WebElement(String element)
        {
            this.ELEMENT = element;
        }
    }

    public class JsonResponse
    {
        public String sessionId = "awesomeSessionId";
        public int status { get; set; }
        public object value { get; set; }

        public JsonResponse(int responseCode, object value)
        {
            this.status = responseCode;
            this.value = value;
        }
    }
}
