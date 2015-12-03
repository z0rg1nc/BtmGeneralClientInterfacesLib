using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using BtmI2p.BasicAuthHttpJsonRpc.Client;
using BtmI2p.MiscUtil.Conversion;
using BtmI2p.MiscUtil.IO;
using BtmI2p.MiscUtils;
using Xunit;

namespace BtmI2p.ExternalAppsLocalApi
{
    public class WalletApiBaseRequest : ICheckable
    {
        public WalletApiBaseRequest()
        {
            RequestSentUtcTimeTicks = DateTime.UtcNow.Ticks;
        }

        public Guid BaseWalletGuid;
        public long RequestSentUtcTimeTicks;
        public virtual void CheckMe()
        {
        }
    }
    /**/
    public class SendTransferRequest : WalletApiBaseRequest
    {
        public Guid RequestGuid = Guid.NewGuid();
        public Guid WalletToGuid;
        public long Amount;
        public bool ForceAnonymousTransfer = false;
        public byte[] CommentBytes = new byte[0];
        public byte[] HmacAuthCode = new byte[32];
        /**/
        public static byte[] GetHmacAuthCode(
            SendTransferRequest request,
            byte[] hmacKey
        )
        {
            Assert.NotNull(request);
            Assert.NotNull(hmacKey);
            Assert.Equal(hmacKey.Length,64);
            using (var ms = new MemoryStream())
            {
                var converter = new LittleEndianBitConverter();
                using (var littleStream = new EndianBinaryWriter(converter, ms))
                {
                    littleStream.Write(request.RequestGuid.ToByteArray());
                    littleStream.Write(request.RequestSentUtcTimeTicks);
                    littleStream.Write(request.BaseWalletGuid.ToByteArray());
                    littleStream.Write(request.WalletToGuid.ToByteArray());
                    littleStream.Write(request.Amount);
                    littleStream.Write(request.ForceAnonymousTransfer);
                    littleStream.Write(request.CommentBytes);
                }
                return new HMACSHA256(hmacKey).ComputeHash(ms.ToArray());
            }
        }

        public override void CheckMe()
        {
            Assert.InRange(
                Amount,
                1,
                int.MaxValue
            );
            Assert.NotNull(CommentBytes);
            Assert.True(CommentBytes.Length <= 64000);
            Assert.NotNull(HmacAuthCode);
            Assert.Equal(HmacAuthCode.Length,32);
        }
    }
    /**/
    public partial interface IWalletLocalJsonRpcApi
    {
        // EGeneralWalletLocalApiErrorCodes20151004
        [Obsolete]
        Task<ResultOrError<bool>> IsWalletConnected(
            WalletApiBaseRequest request
        );

        // EGeneralWalletLocalApiErrorCodes20151004
        [Obsolete]
        Task<ResultOrError<long>> GetBalance(
            WalletApiBaseRequest request
        );

        // ESendTransferWalletLocalApiErrCodes20151004, EGeneralWalletLocalApiErrorCodes20151004
        [Obsolete]
        Task<ResultOrError<VoidResult>> SendTransfer(
            SendTransferRequest request
        );
    }
}
