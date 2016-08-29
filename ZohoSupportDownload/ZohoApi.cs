namespace ZohoSupportDownload
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;

    using Newtonsoft.Json.Linq;

    using RestSharp;

    public class ZohoApi
    {
        public static Action<string> LogMessage = delegate { };

        /// <summary>
        /// Factory Method To Create RestClient
        /// </summary>
        internal static Func<string, IRestClient> CreateClient = url => new RestClient(url);

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

        public ZohoRecord GetRecord(ZohoModule module, long primaryId, string fields = "")
        {
            var request = this.CreateRequest(module, "api/json/{module}/getrecordsbyid", fields);
            request.AddParameter("id", primaryId.ToString());

            var recordArray = this.MakeRequest(module, request);
            return recordArray?.FirstOrDefault();
        }

        public IEnumerable<ZohoRecord> GetRecords(ZohoModule module, bool allRecords = false, int fromIndex = 1, string fields = "")
        {
            Console.Write($"{this.Department} {module} ");
            bool doRequest = true;
            while (doRequest)
            {
                Console.Write(".");

                var request = this.CreateRequest(module, "api/json/{module}/getrecords", fields);
                request.AddParameter("fromindex", fromIndex.ToString());
                request.AddParameter("toindex", (fromIndex + 199).ToString());

                var recordArray = this.MakeRequest(module, request);
                if (recordArray != null)
                {
                    foreach (var zohoRecord in recordArray)
                    {
                        yield return zohoRecord;
                    }
                }

                // Loop Round
                if (allRecords && (recordArray?.Length ?? 0) == 200)
                {
                    fromIndex += 200;
                }
                else
                {
                    doRequest = false;
                }

            }

            Console.WriteLine();
        }

        private IRestRequest CreateRequest(ZohoModule module, string urlPath, string fields)
        {
            var request = new RestRequest(urlPath, Method.GET);

            request.AddParameter("portal", this.Portal);
            request.AddParameter("department", this.Department);
            request.AddParameter("authtoken", this.ApiKey);
            request.AddUrlSegment("module", module.ToString().ToLowerInvariant());

            if (!string.IsNullOrWhiteSpace(fields))
            {
                request.AddParameter("selectfields", fields == "all" ? "all" : $"{module}({fields})");
            }

            return request;
        }

        private ZohoRecord[] MakeRequest(ZohoModule module, IRestRequest request)
        {
            var client = CreateClient(this._baseUrl);

            IRestResponse response = null;

            int attempt = 0;
            while (attempt < 3 && (response?.StatusCode != HttpStatusCode.OK))
            {
                attempt++;
                response = client.Execute(request);
                LogMessage(response.ResponseUri + " : " + response.StatusCode);
            }

            if (response?.StatusCode != HttpStatusCode.OK)
            {
                return null;
            }

            try
            {
                var json = JObject.Parse(response.Content);

                var recordArray = this.ParseJsonToRecords(module, json).ToArray();
                return recordArray;
            }
            catch (Exception ex)
            {
                LogMessage("Error reading JSON: " + ex.Message + ex.StackTrace);
                return null;
            }
        }

        private IEnumerable<ZohoRecord> ParseJsonToRecords(ZohoModule module, JObject json)
        {
            string idField = module.IdField();

            var jsonArray = json["response"]["result"][module == ZohoModule.Requests ? "Cases" : module.ToString()]["row"] as JArray;
            if (jsonArray == null)
            {
                yield break;
            }

            foreach (var jsonRow in jsonArray)
            {
                var list = new List<string>();

                var fields = new Dictionary<string, string>();

                var jsonFields = jsonRow["fl"] as JArray;
                if (jsonFields != null)
                {
                    foreach (var jsonField in jsonFields)
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
                }

                var id = long.Parse(ReadValue(fields, idField));
                var uri = ReadValue(fields, "URI");
                if (uri != null)
                {
                    uri = $"https://support.zoho.com{uri}";
                }

                var record = new ZohoRecord(this.Portal, this.Department, module, id, uri);
                foreach (var field in list.Where(f => f != "URI"))
                {
                    record.AddField(field, fields[field]);
                }

                yield return record;
            }
        }

        private static string ReadValue(Dictionary<string, string> fields, string field)
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