using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Graph;
using _425show.Msal.Extensions;

namespace _425show.SecretManager
{
    public class MsalBuilderCredentialAuthenticationProvider : IAuthenticationProvider
    {
        private readonly MsalBuilder _msalBuilder;
        public MsalBuilderCredentialAuthenticationProvider(MsalBuilder builder)
        {
            _msalBuilder = builder;
        }

        public async Task AuthenticateRequestAsync(HttpRequestMessage request)
        {
            var msal = _msalBuilder.Build();
            var tokenRequest = await msal.AcquireTokenForClient(new[] { "https://graph.microsoft.com/.default" }).ExecuteAsync().ConfigureAwait(false);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenRequest.AccessToken);
        }
    }
}