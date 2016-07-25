namespace ZohoSupportDownload
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.SqlServer.Server;

    using Newtonsoft.Json.Linq;

    using RestSharp;

    public class ZohoApi
    {
        /// <summary>
        /// Factory Method To Create RestClient
        /// </summary>
        internal static Func<string, IRestClient> createClient = url => new RestClient(url);

        private readonly string _baseUrl;

        public ZohoApi(string zohoApi = "https://support.zoho.com/")
        {
            this._baseUrl = zohoApi;
        }

        /// <summary>
        /// Zoho Support API Auth Token
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// Portal of Zoho Support
        /// </summary>
        public string Portal { get; set; }

        /// <summary>
        /// Department of Zoho Support
        /// </summary>
        public string Department { get; set; }

        public ICollection<ZohoRecord> GetRecords(ZohoModule module, bool allRecords = false, int fromIndex = 1, string fields = "")
        {
            var outputArray = new List<ZohoRecord>();
            var client = createClient(this._baseUrl);

            bool doRequest = true;
            while (doRequest)
            {

                var request = new RestRequest("api/json/{module}/getrecords", Method.GET);
                request.AddParameter("portal", this.Portal);
                request.AddParameter("department", this.Department);
                request.AddParameter("authtoken", this.ApiKey);
                request.AddParameter("fromindex", fromIndex.ToString());
                request.AddParameter("toindex", "200");
                if (!string.IsNullOrWhiteSpace(fields))
                {
                    request.AddParameter("selectfields", fields);
                }
                request.AddUrlSegment("module", module.ToString().ToLowerInvariant());

                IRestResponse response = client.Execute(request);
                var json = JObject.Parse(response.Content);

                var recordArray = this.ParseJsonToRecords(module, json).ToArray();
                outputArray.AddRange(recordArray);

                // Loop Round
                if (allRecords && recordArray.Length == 200)
                {
                    fromIndex += 200;
                }
                else
                {
                    doRequest = false;
                }
            }

            return outputArray;
        }

        private IEnumerable<ZohoRecord> ParseJsonToRecords(ZohoModule module, JObject json)
        {
            string idField = module.ToString().ToUpperInvariant().TrimEnd('S') + "ID";

            var jsonArray = json["response"]["result"][module.ToString()]["row"] as JArray;
            foreach (var jsonRow in jsonArray)
            {
                var list = new List<string>();

                var fields = new Dictionary<string, string>();
                foreach (var jsonField in jsonRow["fl"] as JArray)
                {
                    string val = jsonField["val"].Value<string>();
                    list.Add(val);

                    string content = jsonField["content"]?.Value<string>();
                    if (content == "null")
                    {
                        content = null;
                    }

                    fields[val] = content;
                }

                var id = long.Parse(readValue(fields, idField));
                var record = new ZohoRecord(this.Portal, this.Department, module, id, "https://support.zoho.com" + readValue(fields, "URI"));
                foreach (var field in list.Where(f => f != "URI"))
                {
                    record.AddField(field, fields[field]);
                }

                yield return record;
            }
        }

        private static string readValue(Dictionary<string, string> fields, string field)
        {
            string content;
            if (fields.TryGetValue(field, out content))
            {
                return content;
            }

            return null;
        }
    }
}