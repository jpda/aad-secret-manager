using System.Collections.Generic;
using System.Threading.Tasks;

namespace _425show.SecretManager
{
    public interface ICredentialStore
    {
        Task PersistCredentialMetadata(IEnumerable<CredentialEntity> credentials);
        Task<IEnumerable<CredentialEntity>> GetExpiringCredentials(string queryKey);
    }
}