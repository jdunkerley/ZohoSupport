namespace ZohoSupportDownload
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

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

        public ICollection<ZohoRecord> GetRecords(ZohoModule module, bool allRecords = false, int fromIndex = 1, string fields = "", int primaryId = -1)
        {
            var outputArray = new List<ZohoRecord>();
            var client = CreateClient(this._baseUrl);

            bool doRequest = true;
            while (doRequest)
            {
                Console.Write(".");

                string urlPath = (primaryId != -1 ? "api/json/{module}/getrecordsbyid" : "api/json/{module}/getrecords");
                var request = new RestRequest(urlPath, Method.GET);

                request.AddParameter("portal", this.Portal);
                request.AddParameter("department", this.Department);
                request.AddParameter("authtoken", this.ApiKey);

                if (primaryId != -1)
                {
                    request.AddParameter("id", primaryId.ToString());
                }

                request.AddParameter("fromindex", fromIndex.ToString());
                request.AddParameter("toindex", (fromIndex + 199).ToString());

                if (!string.IsNullOrWhiteSpace(fields))
                {
                    request.AddParameter("selectfields", fields);
                }
                request.AddUrlSegment("module", module.ToString().ToLowerInvariant());

                IRestResponse response = client.Execute(request);

                LogMessage(response.ResponseUri + " : " + response.StatusCode);

                try
                {
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
                catch (Exception ex)
                {
                    LogMessage("Error reading JSON: " + ex.Message + ex.StackTrace);
                    doRequest = false;
                }

            }

            Console.WriteLine();
            return outputArray;
        }

        private IEnumerable<ZohoRecord> ParseJsonToRecords(ZohoModule module, JObject json)
        {
            string idField;
            switch (module)
            {
                case ZohoModule.Requests:
                    idField = "CASEID";
                    break;
                case ZohoModule.TimeEntry:
                    idField = "TIME_ENTRY_ID";
                    break;
                case ZohoModule.Tasks:
                    idField = "ACTIVITYID";
                    break;
                default:
                    idField = module.ToString().ToUpperInvariant().TrimEnd('S') + "ID";
                    break;
            }

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