﻿// <auto-generated />

namespace OuterDriver
{
    using Newtonsoft.Json;

    internal class JsonValueContent
    {
        #region Constructors and Destructors

        public JsonValueContent(string sessionId, string id, string[] value)
        {
            this.SessionId = sessionId;
            this.Id = id;
            this.Value = value;
        }

        #endregion

        #region Public Properties

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("sessionId")]
        public string SessionId { get; set; }

        [JsonProperty("value")]
        public string[] Value { get; set; }

        #endregion

        #region Public Methods and Operators

        public string[] GetValue()
        {
            return this.Value;
        }

        #endregion
    }

    internal class JsonMovetoContent
    {
        #region Public Properties

        [JsonProperty("element")]
        public string Element { get; set; }

        [JsonProperty("sessionId")]
        public string SessionId { get; set; }

        [JsonProperty("xOffset")]
        public string XOffset { get; set; }

        [JsonProperty("yOffset")]
        public string YOffset { get; set; }

        #endregion
    }

    internal class JsonKeysContent
    {
        #region Public Properties

        [JsonProperty("sessionId")]
        public string SessionId { get; set; }

        [JsonProperty("value")]
        public string[] Value { get; set; }

        #endregion
    }

    internal class JsonResponse
    {
        #region Constructors and Destructors

        public JsonResponse(string sessionId, ResponseStatus responseCode, object value)
        {
            this.SessionId = sessionId;
            this.Status = responseCode;
            this.Value = value;
        }

        #endregion

        #region Public Properties

        [JsonProperty("sessionId")]
        public string SessionId { get; set; }

        [JsonProperty("status")]
        public ResponseStatus Status { get; set; }

        [JsonProperty("value")]
        public object Value { get; set; }

        #endregion
    }
}
