using System;
using System.Threading.Tasks;
using BtmI2p.BasicAuthHttpJsonRpc.Client;
using BtmI2p.GeneralClientInterfaces.WalletServer;
using BtmI2p.MiscUtils;

namespace BtmI2p.ExternalAppsLocalApi
{
    public class ProxyApiBaseRequest : ICheckable
    {
        public ProxyApiBaseRequest()
        {
            RequestSentUtcTimeTicks = DateTime.UtcNow.Ticks;
        }
        public long RequestSentUtcTimeTicks;
        public virtual void CheckMe()
        {
        }
    }
    /**/

    public class GetInvoiceDataRequest : ProxyApiBaseRequest
    {
        public long Amount;
        public override void CheckMe()
        {
            base.CheckMe();
            if (Amount < 0)
                throw new ArgumentOutOfRangeException(
                    MyNameof.GetLocalVarName(() => Amount));
        }
    }
    /**/
    public partial interface IProxyLocalJsonRpcApi
    {
        //EGeneralProxyLocalApiErrorCodes20151004
        [Obsolete]
        Task<ResultOrError<bool>> IsProxyServerConnected(ProxyApiBaseRequest request);

        //EGeneralProxyLocalApiErrorCodes20151004
        [Obsolete]
        Task<ResultOrError<decimal>> GetProxyServerBalance(ProxyApiBaseRequest request);

        //EGeneralProxyLocalApiErrorCodes20151004
        [Obsolete]
        Task<ResultOrError<BitmoneyInvoiceData>> GetInvoiceDataForReplenishment(
            GetInvoiceDataRequest request);

        //EGeneralProxyLocalApiErrorCodes20151004
        [Obsolete]
        Task<ResultOrError<bool>> IsNewAppVersionAvailable(ProxyApiBaseRequest request);
    }
}
