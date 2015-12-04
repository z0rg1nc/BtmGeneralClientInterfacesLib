#if (!BTM_LIFECYCLE_DEV && !BTM_LIFECYCLE_STAGE && !BTM_LIFECYCLE_PRODUCTION)
#error Lifecycle not set
#endif

#if ( (BTM_LIFECYCLE_DEV && BTM_LIFECYCLE_STAGE) || (BTM_LIFECYCLE_DEV && BTM_LIFECYCLE_PRODUCTION) || (BTM_LIFECYCLE_STAGE && BTM_LIFECYCLE_PRODUCTION))
#error Ambiguous lifecycle
#endif

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using BtmI2p.GeneralClientInterfaces.CdnProxyServer;
using BtmI2p.GeneralClientInterfaces.LifeCycleConstants;
using BtmI2p.LightCertificates.Lib;
using BtmI2p.MiscUtils;

namespace BtmI2p.GeneralClientInterfaces
{
    public enum ELifeCycle
    {
        Dev,
        Stage,
        Production
    }
    public static class ClientLifecycleEnvironment
    {
        public static List<ProxyServerClientInfo> ProxyServerClientInfos 
            => new List<ProxyServerClientInfo>()
        {
            new ProxyServerClientInfo()
            {
                I2PDestinations = ProxyServerDestinations,
                DestinationRestictions = new List<I2PHostClientDestinations>()
                {
                    new I2PHostClientDestinations()
                    {
                        ClientDestinationMask = new byte[] {0x00},
                        ClientDestinationMaskEqual = new byte[] {0x00}
                    }
                }
            }
        };
#if BTM_LIFECYCLE_DEV
        public static ELifeCycle LifeCycle => ELifeCycle.Dev;
        public static List<string> ProxyServerDestinations 
            => DevConstants.ProxyServerDestinations;
        public static string TitlesPrefix => "**** DEVENV **** ";
#elif BTM_LIFECYCLE_STAGE
        public static ELifeCycle LifeCycle => ELifeCycle.Stage;
        public static List<string> ProxyServerDestinations 
            => StageConstants.ProxyServerDestinations;
        public static string TitlesPrefix => "**** STAGEENV **** ";
#elif BTM_LIFECYCLE_PRODUCTION
        public static ELifeCycle LifeCycle => ELifeCycle.Production;
        public static List<string> ProxyServerDestinations 
            => ReleaseConstants.ProxyServerDestinations;
        public static string TitlesPrefix => "";
#endif
    }

    public static class CommonClientConstants
    {
        public const double EveryOperationDefaultCost = 0.0025;
        public const double RegistrationDefaultCost = 1.0;
        public static Version CurrentClientVersion => new Version(CurrentClientVersionString);
        public const string CurrentClientVersionString = "0.0.4.7620";

        public static LightCertificate UpdatesSignatureCertificate 
            = Encoding.UTF8.GetString(
                Convert.FromBase64String(
                "ew0KICAiQWRkaXRpb25hbERhdGEiOiAiIiwNCiAgIk5hbWUiOiAiUHJveHlTZXJ2ZXIxRm9yQ2xpZW50IiwNCiAgIlNpZ25hdHVyZXMiOiBbDQogICAgew0KICAgICAgIlNpZ25lckNlcnRpZmljYXRlSWQiOiAiYjA0YjdmN2EtOTA5Ny00ODkyLTlkZTMtMDA1NDc4ZGIyMjI2IiwNCiAgICAgICJTaWduYXR1cmVCeXRlcyI6ICJxWUY0TzJ1ZlBUV2Q2Tk80ZWdFYlppcjYxam81U0VtYzd6bEtYeUg2b3ZyQy80V1ZKZUNBcVUzVlJ4bUdMcDl2ZlluQWJhK1QwVjdPS0dYRUdIK2tCRUZKMHNQMGhmczloYTl0N1J3MUhJQWxDc3pLM3Q0dUZyN25vREJDczYydGpaSG9OVTVaUWl3d3pBSGl1TGRZZlFyNGd4Z0diSDU5ZG5DeVp3eXNzNHFiSjdZZGc5K1p5MGY3T3FLbVFrVWN0OHNhWmIremFMT0lBT2NEVHNnYnZvNHpiY0lUZ1lzL0VyUGd6c0FiUEc3YUlGRUJEZjVQY2RVYzFrTzBySmg2WUFVMGFLVC9MUGhHQVJrd3FZQ3pxVWx0SGp0akxoZUFHeWVKa1VjZ1o2OEg1ZHdMdTFISm5GNmo3a2dFNGFsMzRweEJaeVMrVm9jRmJkemJmOWlINFE9PSINCiAgICB9DQogIF0sDQogICJTaWduVHlwZSI6IDEsDQogICJTaWduS2V5U2l6ZSI6IDIwNDgsDQogICJQdWJsaWNTaWduUGFyYW1ldGVycyI6ICJBd0FBQUFFQUFRQUJBQURSc2VMemtzY0R2ZlpEZzQxVW5id0c3aktLcWF4VXkvdE9sZStnM3kwM1QvUU5RQ3hyZGdsQS9CcENPZTJhZkdlK0EzOGZMdXVyZjJpTW0yZG5RVVhWS2FLVW83L0FyNzE4ZVN0emRJNkF1cVREQ3ZGUzZWbXYyYmpsN2JpQm84ZHBmeHNNLzVnSGVrQ0U0MlZ6V2lSS3dLTTV0M2R3QmpsblA3UUordHlHZk05Y3dyRGdvMVdHQmJia3ZCN0NaZmgrZzJ6V3pnRFgvY2I2NWhwVFczNGdBOVBZdDRBU1I5bWNJNGpJQmVHZHJNOVFwNUhURWFMTzM3aHgyUVBJM3paZUY5K1ZEaWxkRTFFZi95andPREpzNUp0NmRZVzVOeDQvQU5wcSt2MlFhWkk0UklidUZVV1lHcWl1VjgvOXRPZy9rMnIvT1pPMWtGQzVJdzVJazhZRiIsDQogICJQcml2YXRlU2lnblBhcmFtZXRlcnMiOiBudWxsLA0KICAiRW5jcnlwdFR5cGUiOiAxLA0KICAiRW5jcnlwdEtleVNpemUiOiAyMDQ4LA0KICAiUHVibGljRW5jcnlwdFBhcmFtZXRlcnMiOiAiQXdBQUFBRUFBUUFCQUFDVDg2QkJBUlU2cnhQOGxOZlJzbm4rTC9HK3RwbEEzeGpZa25nYlU3b0lRcnlkUmkzTTBDeVA1Lzg2VEtZeUpJYVhLdUJ5T21YN0Y4U21mbSsyVGlaYVY1T01oL0xoM3BJbVFITmZZYjgrbjJteWp3QnpVVXlMbjA0YjRBeWpvM1Q3ei9PUkZDNE0vNit5bGorUG1XZGlXN2JQMjUrYkFQclMwb2ZWaE5lV2xOVnhWaStvckhwUlRkMzlvTFZVK2lIdjBPcXlNUWJuMlRVbDhnQjNMTkluRUFPeEp5cHlEZUpOY0lodmhMTEZTVXpBTWFUMDB6TEhRc2lVN1pMRHpYR0ZzT0FQMXdvVHhhYlNRUUVldk1lS0hJbWJIeFJ6Zks4c1ZjVkxoV1V1ZXh3TXhXN2thdm9BU2dVK0NPTVFDSEMxNExBdVQ4Qm9IM1B5M2tEK081KzkiLA0KICAiUHJpdmF0ZUVuY3J5cHRQYXJhbWV0ZXJzIjogbnVsbCwNCiAgIk9ubHlQdWJsaWMiOiB0cnVlLA0KICAiSWQiOiAiYjA0YjdmN2EtOTA5Ny00ODkyLTlkZTMtMDA1NDc4ZGIyMjI2Ig0KfQ=="
                )
            ).ParseJsonToType<LightCertificate>();

        public static List<ProxyServerClientInfo> ProxyServerClientInfos
            = ClientLifecycleEnvironment.ProxyServerClientInfos;
    }

    public interface IGetRelativeTime
    {
        Task<DateTime> GetRelativeTime(DateTime time);
        Task<DateTime> GetNowTime();
    }

    public class GetRelativeTimeImpl : IGetRelativeTime
    {
        private readonly Func<DateTime, Task<DateTime>> _getRelativeTimeFunc;
        private readonly Func<Task<DateTime>> _getNowTimeFunc;
        public GetRelativeTimeImpl(
            Func<DateTime, Task<DateTime>> getRelativeTimeFunc,
            Func<Task<DateTime>> getNowTimeFunc
        )
        {
            _getNowTimeFunc = getNowTimeFunc;
            _getRelativeTimeFunc = getRelativeTimeFunc;
        }

        public async Task<DateTime> GetRelativeTime(DateTime time)
        {
            return await _getRelativeTimeFunc(time).ConfigureAwait(false);
        }

        public async Task<DateTime> GetNowTime()
        {
            return await _getNowTimeFunc().ConfigureAwait(false);
        }
    }

    public static class LightCertificateRestrictions
    {
        public static bool IsValid(
            LightCertificate lightCertificate
        )
        {
            return 
                lightCertificate.EncryptType
                    == (int) ELightCertificateSignType.Rsa
                && lightCertificate.SignType
                    == (int) ELightCertificateSignType.Rsa
                && lightCertificate.EncryptKeySize
                    == 2048
                && lightCertificate.SignKeySize
                    == 2048;
        }
    }
    public interface IGetAuthInfo
    {
        [BalanceCost(CommonClientConstants.EveryOperationDefaultCost, 0.0)]
        Task<byte[]> GetAuthData(Guid clientGuid);
    }
    public interface IAuthMe
    {
        [BalanceCost(CommonClientConstants.EveryOperationDefaultCost, 0.0)]
        Task<bool> AuthMe(Guid clientGuid, byte[] authDataFromClient);
    }
    public class ServerAddressForClientInfo
    {
        public Guid ServerGuid;
        //public LightCertificate PublicCertificate;
        public List<RpcMethodInfoWithFees> EndReceiverMethodInfos
            = new List<RpcMethodInfoWithFees>();
    }
    public interface IPingable
    {
        [BalanceCost(CommonClientConstants.EveryOperationDefaultCost, 0.0)]
        Task<int> Ping(int a);
    }
}
