using System;
using System.Threading.Tasks;
using BtmI2p.GeneralClientInterfaces.CdnProxyServer;

namespace BtmI2p.GeneralClientInterfaces
{
    namespace LookupServer
    {
        public static class LookupServerBalanceCosts
        {
            public const double EveryOperationDefaultCost 
                = CommonClientConstants.EveryOperationDefaultCost;
        }
        public interface IFromClientToLookup
        {
            [BalanceCost(LookupServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<ServerAddressForClientInfo> GetUserServerAddress(
                Guid userGuid
            );
            [BalanceCost(LookupServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<ServerAddressForClientInfo> GetWalletServerAddress(
                Guid walletGuid
            );
            [BalanceCost(LookupServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<ServerAddressForClientInfo> GetMiningServerAddress(
                Guid miningClientGuid
            );
            [BalanceCost(LookupServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<ServerAddressForClientInfo> GetExchangeServerAddress(
                Guid exchangeClientGuid
            );
        }
    }
}
