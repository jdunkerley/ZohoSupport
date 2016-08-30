namespace ZohoSupportDownload
{
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Linq;

    public class ZohoRecord
    {
        private readonly List<Tuple<string, string>> _fieldsList;

        public ZohoRecord(string portal, Dictionary<string, string> serialisedDictionary)
        {
            this.Portal = portal;
            this.Department = serialisedDictionary[nameof(this.Department)];
            this.Module = (ZohoModule)Enum.Parse(typeof(ZohoModule), serialisedDictionary[nameof(this.Module)]);
            this.PrimaryId = long.Parse(serialisedDictionary[nameof(this.PrimaryId)]);

            this._fieldsList = new List<Tuple<string, string>>();

            foreach (var keyValuePair in serialisedDictionary)
            {
                switch (keyValuePair.Key)
                {
                    case nameof(this.Department):
                    case nameof(this.Module):
                    case nameof(this.PrimaryId):
                        break;
                    case nameof(this.Uri):
                        this.Uri = keyValuePair.Value;
                        break;
                    default:
                        this.AddField(keyValuePair.Key, keyValuePair.Value);
                        break;
                }
            }
        }

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

        public Dictionary<string, string> AsDictionary()
        {
            var zohoObject = new Dictionary<string, string>();
            foreach (var field in this.Fields)
            {
                var value = this[field];
                if (value != null)
                {
                    zohoObject[field] = value;
                }
            }

            return zohoObject;
        }

        public IEnumerable<string> Fields
        {
            get
            {
                return
                    new[] { nameof(this.Portal), nameof(this.Department), nameof(this.Module), nameof(this.PrimaryId), nameof(this.Uri) }
                    .Concat(this._fieldsList.Select(t => t.Item1))
                    .Distinct();
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