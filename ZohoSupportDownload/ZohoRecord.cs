namespace ZohoSupportDownload
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class ZohoRecord
    {
        private readonly List<Tuple<string, string>> _fieldsList;

        public ZohoRecord(string portal, string department, ZohoModule module, long primaryId, string uri)
        {
            this.Portal = portal;
            this.Department = department;
            this.Module = module;
            this.PrimaryId = primaryId;
            this.Uri = uri;

            this._fieldsList = new List<Tuple<string, string>>();
        }

        public string Portal { get; }

        public string Department { get; }

        public ZohoModule Module { get; }

        public long PrimaryId { get; }

        public string Uri { get; }

        public void AddField(string name, string value)
        {
            this._fieldsList.RemoveAll(f => f.Item1 == name);
            this._fieldsList.Add(Tuple.Create(name, value));
        }

        public IEnumerable<string> Fields
        {
            get
            {
                return this._fieldsList.Select(t => t.Item1);
            }
        }

        public string this[string name]
        {
            get
            {
                if (name == nameof(this.Portal))
                {
                    return this.Portal;
                }
                if (name == nameof(this.Department))
                {
                    return this.Department;
                }
                if (name == nameof(this.Module))
                {
                    return this.Module.ToString();
                }
                if (name == nameof(this.PrimaryId))
                {
                    return this.PrimaryId.ToString();
                }
                if (name == nameof(this.Uri))
                {
                    return this.Uri;
                }

                return this._fieldsList
                    .Where(t => t.Item1 == name)
                    .Select(t => t.Item2)
                    .FirstOrDefault();
            }
        }
    }
}