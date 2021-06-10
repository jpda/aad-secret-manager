using System;
using Microsoft.Azure.Cosmos.Table;

namespace _425show.SecretManager
{
    public class CredentialEntity : TableEntity
    {
        public CredentialEntity() { }

        public string KeyId { get; set; }
        public string DisplayName { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public string Type { get; set; }
        public string Usage { get; set; }
        public string ParentAppObjectId { get; set; }
        public string ParentAppId { get; set; }
        public string ParentAppDisplayName { get; set; }
        public SecretType SecretType { get; set; }
        public static double DerivePartitionKey(DateTime endDateFromGraph)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            var diff = endDateFromGraph.Date - epoch; // .Date property gets us the date @ midnight
                                                      // we'll store this as the partition key, as most of our queries will drive from date
                                                      // e.g., give me all secrets expiring today
            var diffSeconds = diff.TotalSeconds;
            return diffSeconds;
        }
    }

    public enum SecretType
    {
        Certificate,
        SecretString
    }
}
