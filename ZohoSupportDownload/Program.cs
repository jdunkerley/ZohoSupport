namespace ZohoSupportDownload
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Fclp;

    using Newtonsoft.Json;

    public class Program
    {
        private static string _portal;

        private static string _department;

        private static string _apiKey;

        private static ZohoModule _module;

        private static long? _id;

        private static string _fields;

        private static Mode _output;

        private static FluentCommandLineParser CreateParser()
        {
            var p = new FluentCommandLineParser();

            p.Setup<string>('p', "portal").Callback(s => _portal = s).Required().WithDescription("Zoho Portal for the API");
            p.Setup<string>('d', "department").Callback(s => _department = s).Required().WithDescription("Zoho Department to use for API");
            p.Setup<string>('a', "apikey").Callback(s => _apiKey = s).Required().WithDescription("API Key for the Zoho API");
            p.Setup<ZohoModule>('m', "module").Callback(m => _module = m).Required().WithDescription("Module to retrieve data from.");

            p.Setup<long>('i', "id").Callback(i => _id = i).WithDescription("Optional ID to retrieve data for single record.");

            p.Setup<string>('f', "fields").Callback(s => _fields = s).SetDefault("all").WithDescription("Optional list of fields to retrieve (all by default).");
            p.Setup<Mode>('o', "output").SetDefault(Mode.CSV).Callback(s => _output = s).WithDescription("Optional output format (CSV by default).");
            p.SetupHelp("?", "help").Callback(t => Console.WriteLine(t));

            return p;
        }

        static int Main(string[] args)
        {
            var result = CreateParser().Parse(args);
            if (result.HasErrors)
            {
                Console.WriteLine(result.ErrorText);
                return -1;
            }

            // Set Up Logging
            using (var fileName = new StreamWriter($"{_department} {_module} ZohoApi.log", true) { AutoFlush = true })
            {
                ZohoApi.LogMessage = s => { fileName.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {s}"); };

                // Set Up API
                var zohoClient = new ZohoApi { ApiKey = _apiKey, Portal = _portal, Department = _department };

                ZohoApi.LogMessage($"Downloading from {_module} : fields {_fields}" + (_id.HasValue ? $"for id {_id}" : string.Empty));

                ZohoRecord[] current;
                switch (_output)
                {
                    case Mode.JSONRewrite:
                    case Mode.JSONAppend:
                    case Mode.CSVRewrite:
                        current = LoadFromJSON($"{_department} {_module}.json");
                        break;
                    case Mode.JSONMerge:
                    case Mode.CSVMerge:
                        var records = new List<ZohoRecord>();
                        foreach (var file in Directory.GetFiles(Directory.GetCurrentDirectory(), $"* {_module}.json"))
                        {
                            records.AddRange(LoadFromJSON(file));
                        }
                        current = records.ToArray();
                        break;
                    default:
                        current = new ZohoRecord[0];
                        break;
                }

                IEnumerable<ZohoRecord> content;
                switch (_output)
                {
                    case Mode.CSV:
                    case Mode.JSON:
                    case Mode.JSONAppend:
                        content = (!_id.HasValue
                                       ? zohoClient.GetRecords(_module, true, current.Length + 1, _fields)
                                       : new[] { zohoClient.GetRecord(_module, _id.Value, _fields) });
                        break;
                    default:
                        content = new ZohoRecord[0];
                        break;
                }

                content = current.Concat(content);

                // Generate Returned Field List
                switch (_output)
                {
                    case Mode.JSON:
                    case Mode.JSONAppend:
                    case Mode.JSONRewrite:
                        SaveToJson($"{_department} {_module}.json", content);
                        break;
                    case Mode.JSONMerge:
                        SaveToJson($"{_module}.json", content);
                        break;
                    case Mode.CSV:
                    case Mode.CSVRewrite:
                        SaveToCSV($"{_department} {_module}.csv", content);
                        break;
                    case Mode.CSVMerge:
                        SaveToCSV($"{_module}.csv", content);
                        break;
                }


                fileName.Close();
                ZohoApi.LogMessage = delegate { };
            }
            return 0;
        }

        private static void SaveToCSV(string filename, IEnumerable<ZohoRecord> content)
        {
            var list = content.ToList();

            ZohoApi.LogMessage("Creating Field List...");
            var fields = new List<string>();
            fields.AddRange(list.SelectMany(r => r.Fields).Distinct());

            // Save To CSV
            ZohoApi.LogMessage($"Writing CSV {filename} ...");
            using (var wrt = new StreamWriter(filename))
            {
                wrt.WriteLine(string.Join(",", fields));
                foreach (var zohoRecord in list)
                {
                    bool first = true;
                    foreach (var field in fields)
                    {
                        if (!first)
                        {
                            wrt.Write(",");
                        }

                        var val = zohoRecord[field];
                        if (val != null)
                        {
                            if (val.Contains(",") || val.Contains("\r") || val.Contains("\n") || val.Contains("\""))
                            {
                                wrt.Write("\"" + val.Replace("\"", "\"\"") + "\"");
                            }
                            else
                            {
                                wrt.Write(val);
                            }
                        }

                        first = false;
                    }
                    wrt.WriteLine();
                }
                wrt.Close();
            }
        }

        private static ZohoRecord[] LoadFromJSON(string fileName)
        {
            if (!File.Exists(fileName))
            {
                return new ZohoRecord[0];
            }


            ZohoApi.LogMessage($"Reading JSON {fileName} ...");

            var output = new List<ZohoRecord>();

            using (var streamReader = new StreamReader(fileName))
            {
                var jsonString = streamReader.ReadToEnd();
                var jsonParsed = JsonConvert.DeserializeObject<Dictionary<string, string>[]>(jsonString);

                foreach (var dictionary in jsonParsed)
                {
                    output.Add(new ZohoRecord(_portal, dictionary));
                }
            }

            return output.ToArray();
        }

        private static void SaveToJson(string fileName, IEnumerable<ZohoRecord> content)
        {
            // Save To JSON
            ZohoApi.LogMessage($"Writing JSON {fileName} ...");
            using (var wrt = new StreamWriter(fileName))
            {
                wrt.AutoFlush = true;

                wrt.WriteLine("[");
                foreach (var zohoRecord in content)
                {
                    wrt.WriteLine(JsonConvert.SerializeObject(zohoRecord.AsDictionary()) + ",");
                }
                wrt.WriteLine("]");
                wrt.Close();
            }
        }

        public enum Mode
        {
            CSV,
            JSON,
            JSONAppend,
            CSVRewrite,
            JSONRewrite,
            JSONMerge,
            CSVMerge
        }
    }
}
