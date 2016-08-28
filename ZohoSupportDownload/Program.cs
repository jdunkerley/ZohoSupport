namespace ZohoSupportDownload
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public class Program
    {
        static void Main(string[] args)
        {
            var fileName = new StreamWriter("zohoapi.log", true) { AutoFlush = true };
            ZohoApi.LogMessage = s => { fileName.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {s}"); };

            var zohoClient = new ZohoApi { ApiKey = args[0], Portal = args[1], Department = args[2] };

            var module = (ZohoModule)Enum.Parse(typeof(ZohoModule), args[3]);

            int primaryId;
            if (args.Length < 6 || !int.TryParse(args[5], out primaryId))
            {
                primaryId = -1;
            }

            string fieldList = "";
            if (args.Length >= 5)
            {
                fieldList = args[4] == "all" ? "all" : $"{module}({args[4]})";
            }

            ZohoApi.LogMessage($"Downloading from {module} : {primaryId} - fields {fieldList}");
            var content = zohoClient.GetRecords(module, args.Length < 6 && primaryId == -1, 1, fieldList, primaryId);

            // Generate Returned Field List
            ZohoApi.LogMessage("Creating Field List...");
            var fields = new List<string>
                             {
                                 nameof(ZohoRecord.Department),
                                 nameof(ZohoRecord.Module),
                                 nameof(ZohoRecord.PrimaryId),
                                 nameof(ZohoRecord.Uri)
                             };
            fields.AddRange(content.SelectMany(r => r.Fields).Distinct());

            // Save To CSV
            ZohoApi.LogMessage($"Writing CSV {args[2]} {module}.csv ...");
            var wrt = new System.IO.StreamWriter($"{args[2]} {module}.csv");
            wrt.WriteLine(string.Join(",", fields));
            foreach (var zohoRecord in content)
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
                            wrt.Write("\"" + val.Replace("\"","\"\"") + "\"");
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

            fileName.Close();
        }
    }
}
