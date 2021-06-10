using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace _425show.SecretManager
{
    public class AzureTableCredStore : ICredentialStore
    {
        private readonly ILogger _log;
        private readonly CloudTable _table;
        private readonly TableCredStoreConfiguration _config;

        public AzureTableCredStore(IOptions<TableCredStoreConfiguration> config, ILogger<AzureTableCredStore> logger)
        {
            _log = logger;
            _config = config.Value;
            _table = GetTableRef(); // ew
        }

        private CloudTable GetTableRef(string tableName = "CredStore")
        {
            var storage = CloudStorageAccount.Parse(_config.ConnectionString);
            var table = storage.CreateCloudTableClient().GetTableReference(tableName);
            table.CreateIfNotExists();
            return table;
        }

        public async Task<IEnumerable<CredentialEntity>> GetExpiringCredentials(string queryKey)
        {
            //var q = _table.CreateQuery<CredentialEntity>().Where();
            var q = new TableQuery<CredentialEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, queryKey));

            var results = new List<CredentialEntity>();
            TableContinuationToken token = null;
            do
            {
                var query = await _table.ExecuteQuerySegmentedAsync(q, token).ConfigureAwait(false);
                token = query.ContinuationToken;
                results.AddRange(query.Results);
            } while (token != null);
            return results;
        }

        public async Task PersistCredentialMetadata(IEnumerable<CredentialEntity> creds)
        {
            // group by partition key, then batch into 100 entity requests
            _log.LogInformation($"Grouping by partition key");
            var groups = creds.GroupBy(x => x.PartitionKey);
            foreach (var g in groups)
            {
                foreach (var batch in g.Batch(100))
                {
                    var batchOp = new TableBatchOperation();
                    foreach (var e in batch)
                    {
                        batchOp.Add(TableOperation.InsertOrReplace(e));
                    }
                    await _table.ExecuteBatchAsync(batchOp).ConfigureAwait(false);
                }
            }
        }
    }
}