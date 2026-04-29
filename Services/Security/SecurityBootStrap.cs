using System.Security.Cryptography;

namespace ProtocolWorkbench.Core.Services.Security
{
    public class SecurityBootstrap : ISecurityBootstrap
    {
        private readonly ISecuritySessionState _state;

        public SecurityBootstrap(ISecuritySessionState state)
        {
            _state = state;
        }

        public async Task InitializeAsync()
        {
            await _state.LoadAsync();

            if (_state.SpEcdh is null)
            {
                _state.SpEcdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
                _state.SpPublicB64 = Convert.ToBase64String(
                    _state.SpEcdh.ExportSubjectPublicKeyInfo());

                await _state.SaveAsync();
            }
        }
    }
}
