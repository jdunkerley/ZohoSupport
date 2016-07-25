namespace ZohoSupportDownload
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class Program
    {
        static void Main(string[] args)
        {
            var zohoClient = new ZohoApi { ApiKey = args[0], Portal = args[1], Department = args[2] };

            var module = (ZohoModule)Enum.Parse(typeof(ZohoModule), args[3]);

            var content = zohoClient.GetRecords(module, true, 1, $"{module}({args[4]})");

            var fields = new List<string>
                             {
                                 nameof(ZohoRecord.Department),
                                 nameof(ZohoRecord.Module),
                                 nameof(ZohoRecord.Id),
                                 nameof(ZohoRecord.Uri)
                             };
            fields.AddRange(content.SelectMany(r => r.Fields).Distinct());

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
        }
    }
}
