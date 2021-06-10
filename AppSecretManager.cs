using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;

namespace _425show.SecretManager
{
    public class AppSecretManager
    {
        private readonly GraphServiceClient _graphClient;
        private readonly ILogger _log;
        private ICredentialStore _credStore;

        public AppSecretManager(ILogger<AppSecretManager> logger, GraphServiceClient graphClient, ICredentialStore credStore)
        {
            _log = logger;
            _graphClient = graphClient;
            _credStore = credStore;
        }

        public async Task<IEnumerable<CredentialEntity>> GetCredentialMetadata()
        {
            _log.LogInformation("Querying Graph for apps");
            // https://graph.microsoft.com/v1.0/applications?$count=true&$select=id,appDisplayName,keyCredentials&$filter=keyCredential/any(x:x/endDateTime lt '2021-12-31')
            var apps = await _graphClient.Applications
                .Request()
                .Top(10) // force max page size to reduce the number of requests
                .Select("id,keyCredentials,passwordCredentials,appDisplayName")
                .GetAsync()
                .ConfigureAwait(false);

            var creds = new List<CredentialEntity>();
            var appIterator = PageIterator<Application>.CreatePageIterator(_graphClient, apps, app =>
            {
                var keyCreds = app.KeyCredentials.Select(k => new CredentialEntity()
                {
                    PartitionKey = CredentialEntity.DerivePartitionKey(k.EndDateTime.Value.UtcDateTime).ToString(),
                    RowKey = k.KeyId.ToString(),
                    ParentAppDisplayName = app.DisplayName,
                    ParentAppObjectId = app.Id,
                    ParentAppId = app.AppId,
                    DisplayName = k.DisplayName,
                    EndDateTime = k.EndDateTime.Value.UtcDateTime,
                    StartDateTime = k.StartDateTime.Value.UtcDateTime,
                    KeyId = k.KeyId.ToString(),
                    Type = k.Type,
                    Usage = k.Usage,
                    SecretType = SecretType.Certificate
                }).ToList(); //prevent multitple evaluations

                creds.AddRange(keyCreds);

                var secretCreds = app.PasswordCredentials.Select(p => new CredentialEntity()
                {
                    PartitionKey = CredentialEntity.DerivePartitionKey(p.EndDateTime.Value.UtcDateTime).ToString(),
                    // todo: is keyid valid for pw creds? I don't think so
                    RowKey = p.KeyId.ToString(),
                    ParentAppDisplayName = app.DisplayName,
                    ParentAppObjectId = app.Id,
                    ParentAppId = app.AppId,
                    DisplayName = p.DisplayName,
                    EndDateTime = p.EndDateTime.Value.UtcDateTime,
                    StartDateTime = p.StartDateTime.Value.UtcDateTime,
                    KeyId = p.KeyId.ToString(),
                    SecretType = SecretType.SecretString
                }).ToList(); //prevent multitple evaluations

                creds.AddRange(secretCreds);

                return true;
            });

            await appIterator.IterateAsync().ConfigureAwait(false);
            _log.LogInformation($"Got {creds.Count} credentials from Graph");
            return creds;
        }

        public async Task PersistCredentialMetadata(IEnumerable<CredentialEntity> creds)
        {
            await _credStore.PersistCredentialMetadata(creds).ConfigureAwait(false);
        }

        public async Task<IEnumerable<CredentialEntity>> GetExpiringCredentials(string key)
        {
            return await _credStore.GetExpiringCredentials(key).ConfigureAwait(false);
        }
    }
}