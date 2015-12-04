using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BtmI2p.GeneralClientInterfaces.CdnProxyServer;
using BtmI2p.LightCertificates.Lib;
using BtmI2p.MiscUtil.Conversion;
using BtmI2p.MiscUtil.IO;
using BtmI2p.MiscUtils;
using BtmI2p.OneSideSignedJsonRpc;
using BtmI2p.SamHelper;
using NLog;
using Xunit;

namespace BtmI2p.GeneralClientInterfaces
{
    namespace WalletServer
    {
        public enum EWalletIdsTypeClient
        {
            Fees,
            AnonymousClient,
            Emission,
            Clients,
            ClientsReserved,
            SystemUsed
        }
        public static class WalletServerClientConstants
        {
            public static readonly Dictionary<EWalletIdsTypeClient,int>
                WalletClientGuidFirstZeroBytesCount = new Dictionary<EWalletIdsTypeClient, int>()
                {
                    { EWalletIdsTypeClient.Clients, 0 },
                    { EWalletIdsTypeClient.ClientsReserved, 4},
                    { EWalletIdsTypeClient.SystemUsed, 10 },
                    { EWalletIdsTypeClient.Emission, 11 },
                    { EWalletIdsTypeClient.AnonymousClient, 15 },
                    { EWalletIdsTypeClient.Fees, 16 }
                };
            /**/
            public static readonly Guid FeesWalletGuid
            = Guid.Empty;
            public readonly static Guid AnonymousWalletGuid =
                new Guid(
                    "00000000-0000-0000-0000-000000000001"
                );
            public static EWalletIdsTypeClient GetWalletIdType(Guid walletId)
            {
                if (walletId == FeesWalletGuid)
                    return EWalletIdsTypeClient.Fees;
                if (walletId == AnonymousWalletGuid)
                    return EWalletIdsTypeClient.AnonymousClient;
                /*******************/
                var emissionMask = Guid.Parse(
                    "FFFFFFFF-FFFF-FFFF-FFFF-FF0000000000"
                );
                var emissionMaskEqual = Guid.Parse(
                    "00000000-0000-0000-0000-0E0000000000"
                );
                if(walletId.CheckMask(emissionMask,emissionMaskEqual))
                    return EWalletIdsTypeClient.Emission;
                /*******************/
                var systemUsedMask = Guid.Parse(
                    "FFFFFFFF-FFFF-FFFF-FFFF-000000000000"
                );
                var systemUsedMaskEqual = Guid.Empty;
                if (walletId.CheckMask(systemUsedMask, systemUsedMaskEqual))
                    return EWalletIdsTypeClient.SystemUsed;
                /*******************/
                var clientsReservedMask = Guid.Parse(
                    "FFFFFFFF-0000-0000-0000-000000000000"
                );
                var clientsReservedMaskEqual = Guid.Empty;
                if (walletId.CheckMask(clientsReservedMask, clientsReservedMaskEqual))
                    return EWalletIdsTypeClient.ClientsReserved;
                /*******************/
                return EWalletIdsTypeClient.Clients;
            }
        }

        public static class WalletServerRestrictionsForClient
        {
            public static int MaxCommentBytesCount = 64256;
            public static long MaxTransferAmount = int.MaxValue;
            public static readonly int MaxSentReceiveTransferListSumCommentSize
                = (new ReliableSamHelperSettings()).MaxMesageLength - 50000;
            public static bool CheckSimpleTransferWalletTo(
                Guid walletToGuid
                )
            {
                var walletToType = WalletServerClientConstants.GetWalletIdType(
                    walletToGuid
                );
                if (
                    walletToType == EWalletIdsTypeClient.Clients 
                    || walletToType == EWalletIdsTypeClient.SystemUsed
                )
                    return true;
                return false;
            }
            public static int MaxTransferInfosInOneSimpleTransferRequest = 30;
        }
        public static class WalletServerTransferFeeHelper
        {
            public const long ConstFee = 1;
            public const decimal FeeRate = 0.0001m;
            public const decimal Fee1KbComment = 1;
            public static long GetFeePos(
                int transferCount,
                long totalTransferAmount,
                int totalCommentSize
            )
            {
                return ConstFee * transferCount
                    + (long)(FeeRate * totalTransferAmount)
                    + (long)(Fee1KbComment * Math.Floor(totalCommentSize / 1024.0m));
            }
        }
        public class SentTransactionInfo
        {
            public Guid WalletTo;
            public long TransactionAmount;
            public long OrderFee = 0;
            public byte[] Comment;
            public Guid CommentKeyGuid = Guid.Empty;
            public Guid TransactionGuid;
            public Guid RequestGuid;
            public DateTime TransactionSentTime;
            public bool AnonymousTransfer;
            public ETransferSignatureType SignatureType;
            public byte[] EncryptedSignature;
            /**/

            public TransferToInfo InfoForSignature()
            {
                return new TransferToInfo()
                {
                    SignatureType = SignatureType,
                    EncryptedSignature = EncryptedSignature,
                    WalletToGuid = WalletTo,
                    TransferAmount = TransactionAmount,
                    TransferGuid = TransactionGuid,
                    AnonymousTransfer = AnonymousTransfer,
                    Comment = Comment,
                    CommentKeyGuid = CommentKeyGuid
                };
            }
        }
        public class GetSentTransfersOrderRequest : ICheckable
        {
            public DateTime DateTimeFrom = DateTime.MinValue;
            public DateTime DateTimeTo = DateTime.MaxValue;
            public Guid WalletTo = Guid.Empty;
            public Guid LastKnownTransactionGuid = Guid.Empty;
            public int LimitCount = 100;
            public List<Guid> IncludeTransferGuidOnlyList = new List<Guid>(); 
            public void CheckMe()
            {
                Assert.True(DateTimeTo >= DateTimeFrom);
                Assert.InRange(
                    LimitCount,
                    1,
                    200
                );
                Assert.NotNull(IncludeTransferGuidOnlyList);
                Assert.Equal(
                    IncludeTransferGuidOnlyList,
                    IncludeTransferGuidOnlyList.Distinct()
                );
                Assert.InRange(
                    IncludeTransferGuidOnlyList.Count,
                    0,
                    100
                );
            }
        }
        public class GenNewCommentKeyOrderRequest
        {
            public WalletCommentKeyClientInfo KeyInfo;
            public Guid RequestGuid = Guid.NewGuid();
            public void CheckMe(
                Guid transferAndCommentKeyGuidMask,
                Guid transferAndCommentKeyGuidMaskEqual,
                int expectedEncryptedKeyLength,
                Guid walletFromGuid,
                int expectedSignatureLength
            )
            {
                Assert.NotEqual(RequestGuid,Guid.Empty);
                Assert.NotNull(KeyInfo);
                Assert.NotEqual(KeyInfo.CommentKeyGuid,Guid.Empty);
                Assert.True(
                    KeyInfo.CommentKeyGuid.CheckMask(
                        transferAndCommentKeyGuidMask,
                        transferAndCommentKeyGuidMaskEqual
                    )
                );
                Assert.Equal(KeyInfo.WalletFrom,walletFromGuid);
                Assert.NotEqual(KeyInfo.WalletTo,Guid.Empty);
                Assert.NotNull(KeyInfo.KeyEncryptedFrom);
                Assert.Equal(KeyInfo.KeyEncryptedFrom.Length,expectedEncryptedKeyLength);
                Assert.NotNull(KeyInfo.KeyEncryptedTo);
                Assert.Equal(KeyInfo.KeyEncryptedTo.Length,expectedEncryptedKeyLength);
                var nowTime = DateTime.UtcNow;
                Assert.Equal(
                    KeyInfo.IssuedTime,
                    MiscFuncs.RoundDateTimeToSeconds(KeyInfo.IssuedTime)
                );
                Assert.InRange(
                    KeyInfo.IssuedTime,
                    nowTime.Subtract(TimeSpan.FromMinutes(5)),
                    nowTime.AddMinutes(5.0)
                );
                Assert.Equal(
                    KeyInfo.ValidUntilTime,
                    MiscFuncs.RoundDateTimeToSeconds(KeyInfo.ValidUntilTime)
                );
                Assert.True(KeyInfo.IssuedTime < KeyInfo.ValidUntilTime);
                Assert.True(KeyInfo.ValidUntilTime <= nowTime.AddMonths(1));
                Assert.Equal(KeyInfo.KeyAlg,EWalletCommentKeyAlg.Aes256);
                if (KeyInfo.AnonymousKey)
                {
                    Assert.Equal(KeyInfo.SignatureType, EWalletCommentKeySignatureType.None);
                    Assert.Null(KeyInfo.Signature);
                }
                else
                {
                    Assert.Equal(
                        KeyInfo.SignatureType, 
                        EWalletCommentKeySignatureType.Type20150929
                    );
                    Assert.NotNull(KeyInfo.Signature);
                    Assert.Equal(
                        KeyInfo.Signature.SignerCertificateId,
                        KeyInfo.WalletFrom
                    );
                    Assert.NotNull(KeyInfo.Signature.SignatureBytes);
                    Assert.Equal(
                        KeyInfo.Signature.SignatureBytes.Length, 
                        expectedSignatureLength
                    );
                }
            }
        }
        
        public class GetWalletCertRequest : ICheckable
        {
            public List<Guid> WalletCertGuidList = new List<Guid>();
            public void CheckMe()
            {
                Assert.NotNull(WalletCertGuidList);
                Assert.InRange(WalletCertGuidList.Count,1,20);
                Assert.Equal(WalletCertGuidList,WalletCertGuidList.Distinct());
            }
        }

        public enum ETransferSignatureType : sbyte
        {
            None,
            /* TransferGuid . round2sec(SentTime) . anonymous(always false) . walletFrom . walletTo
            . amount . sha256(commentBody) . commentKeyGuid . signatureType */
            Type20150929
        }

        public enum EProcessSimpleTransferErrCodes
        {
            NoErrors,
            WalletToNotExist,
            NotEnoughFunds,
            CommentKeyIsNotRegistered,
            WrongCommentKey,
            ExpiredCommentKey,
            CommentSizeTooBig,
            WrongTransferAmount,
            TransferAmountLessThanWalletToRequires,
            AnonymousKeyWithNotAnonymousTransfer,
            NotAnonymousKeyWithAnonymousTransfer,
            TransferGuidAlreadyRegistered,
            ExpectedFeeMoreThanRequestMaxFee
        }
        public class TransferToInfo
        {
            public bool AnonymousTransfer = false;
            public Guid WalletToGuid;
            public long TransferAmount;
            public byte[] Comment = new byte[0];
            public Guid CommentKeyGuid = Guid.Empty;
            public Guid TransferGuid;
            public ETransferSignatureType SignatureType = ETransferSignatureType.None;
            public byte[] EncryptedSignature = new byte[0];
            /**/
            public byte[] GetDataToSign(Guid walletFromGuid, DateTime sentTime)
            {
                Assert.False(AnonymousTransfer);
                Assert.Equal(SignatureType,ETransferSignatureType.Type20150929);
                if (SignatureType == ETransferSignatureType.Type20150929)
                {
                    using (var ms = new MemoryStream())
                    {
                        using (var sha256 = new SHA256Managed())
                        {
                            var converter = new LittleEndianBitConverter();
                            using (
                                var writer = new EndianBinaryWriter(
                                    converter,
                                    ms
                                )
                            )
                            {
                                writer.Write(TransferGuid.ToByteArray());
                                writer.Write(MiscFuncs.RoundDateTimeToSeconds(sentTime).Ticks);
                                writer.Write(AnonymousTransfer);
                                writer.Write(walletFromGuid.ToByteArray());
                                writer.Write(WalletToGuid.ToByteArray());
                                writer.Write(TransferAmount);
                                writer.Write(sha256.ComputeHash(Comment));
                                writer.Write(CommentKeyGuid.ToByteArray());
                                writer.Write((sbyte)SignatureType);
                            }
                            var result = sha256.ComputeHash(ms.ToArray());
                            return result;
                        }
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }
                /**/
            }

            private static readonly Logger _log = LogManager.GetCurrentClassLogger();
            /**/
            public void CheckMe(
                Guid transferAndCommentKeyGuidMask,
                Guid transferAndCommentKeyGuidMaskEqual
            )
            {
                Assert.NotEqual(WalletToGuid,Guid.Empty);
                Assert.InRange(
                    TransferAmount,
                    1,
                    WalletServerRestrictionsForClient.MaxTransferAmount
                );
                Assert.NotNull(Comment);
                Assert.InRange(
                    Comment.Length,
                    0, 
                    WalletServerRestrictionsForClient.MaxCommentBytesCount
                );
                Assert.True(
                    TransferGuid.CheckMask(
                        transferAndCommentKeyGuidMask,
                        transferAndCommentKeyGuidMaskEqual
                    )
                );
                if (AnonymousTransfer || CommentKeyGuid == Guid.Empty)
                {
                    Assert.Equal(SignatureType, ETransferSignatureType.None);
                    Assert.NotNull(EncryptedSignature);
                    Assert.Empty(EncryptedSignature);
                }
                else
                {
                    Assert.Equal(SignatureType, ETransferSignatureType.Type20150929);
                    Assert.NotNull(EncryptedSignature);
                    Assert.Equal(EncryptedSignature.Length, 80);
                }
            }
        }
        public class SimpleTransferOrderRequest
        {
            public Guid RequestGuid = MiscFuncs.GenGuidWithFirstBytes(0);
            public List<TransferToInfo> TransferToInfos;
            public DateTime SentTime;
            public long MaxFee = 0;
            public void CheckMe(
                Guid transferAndCommentKeyGuidMask,
                Guid transferAndCommentKeyGuidMaskEqual
            )
            {
                Assert.NotEqual(RequestGuid, Guid.Empty);
                Assert.NotNull(TransferToInfos);
                Assert.InRange(TransferToInfos.Count,1, WalletServerRestrictionsForClient
                        .MaxTransferInfosInOneSimpleTransferRequest);
                Assert.Equal(
                    TransferToInfos.Select(_ => _.TransferGuid),
                    TransferToInfos.Select(_ => _.TransferGuid).Distinct()
                );
                foreach (var transferToInfo in TransferToInfos)
                {
                    Assert.NotNull(transferToInfo);
                    transferToInfo.CheckMe(
                        transferAndCommentKeyGuidMask,
                        transferAndCommentKeyGuidMaskEqual
                    );
                }
                var nowTime = DateTime.UtcNow;
                Assert.Equal(
                    SentTime,
                    MiscFuncs.RoundDateTimeToSeconds(SentTime)
                );
                Assert.InRange(
                    SentTime,
                    nowTime.Subtract(TimeSpan.FromMinutes(5.0)),
                    nowTime.AddMinutes(5)
                );
                Assert.InRange(
                    MaxFee,
                    0,
                    int.MaxValue
                );
            }
        }
        // return new wallet balance
        public class SuccessSimpleTransferResult
        {
            public long Fee;
        }
        public class WalletClientSettingsOnServer
        {
            public long MinIncomeTransferBalance = 1;
        }
        public class RegisterWalletOrderRequest
        {
            public LightCertificate PublicWalletCert;
            public LightCertificate PublicMasterWalletCert;
            public WalletClientSettingsOnServer WalletSettingsOnServer;
            public DateTime SentTime;
            public Guid RequestGuid = Guid.NewGuid();
        }
        public class IncomingTransactionInfo
        {
            public Guid WalletFrom;
            public long TransactionAmount;
            public byte[] Comment;
            public Guid CommentKeyId = Guid.Empty;
            public Guid TransactionGuid;
            public DateTime TransactionSentTime;
            public bool AnonymousTransfer;
            public ETransferSignatureType SignatureType;
            public byte[] EncryptedSignature;
            /**/
            public TransferToInfo ForSignatureInfo(Guid walletTo)
            {
                return new TransferToInfo()
                {
                    SignatureType = SignatureType,
                    EncryptedSignature = EncryptedSignature,
                    WalletToGuid = walletTo,
                    AnonymousTransfer = AnonymousTransfer,
                    TransferAmount = TransactionAmount,
                    Comment = Comment,
                    TransferGuid = TransactionGuid,
                    CommentKeyGuid = CommentKeyId
                };
            }
            /**/

            public static IncomingTransactionInfo FromTransferTo(
                TransferToInfo successTransferToInfo,
                Guid walletFrom,
                DateTime sentTime
            )
            {
                Assert.NotNull(successTransferToInfo);
                return new IncomingTransactionInfo()
                {
                    WalletFrom =
                        successTransferToInfo.AnonymousTransfer
                            ? WalletServerClientConstants.AnonymousWalletGuid
                            : walletFrom,
                    TransactionAmount = successTransferToInfo.TransferAmount,
                    TransactionGuid = successTransferToInfo.TransferGuid,
                    Comment = successTransferToInfo.Comment,
                    CommentKeyId = successTransferToInfo.CommentKeyGuid,
                    TransactionSentTime = sentTime,
                    AnonymousTransfer = successTransferToInfo.AnonymousTransfer,
                    SignatureType =
                        successTransferToInfo.AnonymousTransfer
                            ? ETransferSignatureType.None 
                            : successTransferToInfo.SignatureType,
                    EncryptedSignature =
                        successTransferToInfo.AnonymousTransfer
                            ? new byte[0] 
                            : successTransferToInfo.EncryptedSignature
                };
            }
        }
        public class SubscribeIncomingTransactionsOrderRequest : ICheckable
        {
            public int BufferCount = 100;
            public Guid LastKnownTransactionGuid = Guid.Empty;
            public DateTime SentTimeFrom = DateTime.MinValue;
            public DateTime SentTimeTo = DateTime.MaxValue; //Always for online
            public List<Guid> IncludeTransferGuidOnlyList = new List<Guid>();
            public void CheckMe()
            {
                Assert.InRange(
                    BufferCount,
                    1,
                    100
                );
                Assert.True(SentTimeTo >= SentTimeFrom);
                Assert.NotNull(IncludeTransferGuidOnlyList);
                Assert.InRange(
                    IncludeTransferGuidOnlyList.Count,
                    0,
                    100
                );
                Assert.Equal(
                    IncludeTransferGuidOnlyList,
                    IncludeTransferGuidOnlyList.Distinct()
                );
            }
        }
        public class CheckSimpleTransferWasProcessedRequest
        {
            public Guid OrderRequestId;
        }
        public class CheckSimpleTransferWasProcessedResponse
        {
            public bool WasProcessed = false;
            public List<Guid> RelatedTransferGuidList = new List<Guid>();
        }
        public enum EWalletCommentKeyAlg : sbyte
        {
            Aes256
        }
        public enum EWalletCommentKeySignatureType : sbyte
        {
            None,
            /* 
            CommentKeyGuid . AnonymousKey(must be always true) . KeyAlg . WalletFrom . WalletTo
            . sha256(DecryptedKey) . IssuedTime (round2sec) . ValidUntilTime (rount2sec) . SignatureType
            */
            Type20150929
        }
        public class WalletCommentKeyClientInfo
        {
            public Guid CommentKeyGuid { get; set; }
            public bool AnonymousKey { get; set; }
            public EWalletCommentKeyAlg KeyAlg { get; set; }
            public Guid WalletFrom { get; set; }
            public Guid WalletTo { get; set; }
            public byte[] KeyEncryptedFrom { get; set; }
            public byte[] KeyEncryptedTo { get; set; }
            public DateTime IssuedTime { get; set; }
            public DateTime ValidUntilTime { get; set; }
            public EWalletCommentKeySignatureType SignatureType { get; set; }
            public LightCertificateSignature Signature { get; set; }
            /**/
            public byte[] GetDataForSignature(byte[] decryptedKey)
            {
                Assert.False(AnonymousKey);
                Assert.Equal(SignatureType,EWalletCommentKeySignatureType.Type20150929);
                if (SignatureType == EWalletCommentKeySignatureType.Type20150929)
                {
                    using (var ms = new MemoryStream())
                    {
                        using (var sha256 = new SHA256Managed())
                        {
                            var converter = new LittleEndianBitConverter();
                            using (
                                var writer = new EndianBinaryWriter(
                                    converter,
                                    ms
                                    )
                                )
                            {
                                writer.Write(CommentKeyGuid.ToByteArray());
                                writer.Write(AnonymousKey);
                                writer.Write((sbyte)KeyAlg);
                                writer.Write(WalletFrom.ToByteArray());
                                writer.Write(WalletTo.ToByteArray());
                                writer.Write(sha256.ComputeHash(decryptedKey));
                                writer.Write(MiscFuncs.RoundDateTimeToSeconds(IssuedTime).Ticks);
                                writer.Write(MiscFuncs.RoundDateTimeToSeconds(ValidUntilTime).Ticks);
                                writer.Write((sbyte)SignatureType);
                            }
                            return sha256.ComputeHash(ms.ToArray());
                        }
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }
        /**/
        public enum EWalletGeneralErrCodes
        {
            NoErrors = 100,
            WrongRequest,
            WalletGuidForbidden,
            OtherWalletGuidForbidden,
            // current wallet server doesn't process packets from this wallet guid range
            WrongServerWalletGuidRange, 
            OtherWalletGuidNotExist,
            AlreadyRegisteredByOtherWallet,
            WalletNotRegistered,
            OtherWalletNotRegistered
        }
        /**/
        public enum EGenerateNewCommentKeyErrCodes
        {
            KeyExistOther
        }
        public interface ISignedFromClientToWallet
        {
            [BalanceCost(WalletServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<SuccessSimpleTransferResult> ProcessSimpleTransfer(
                SimpleTransferOrderRequest request
            );
        }

        public class WalletServerInfoForClient
        {
            public Guid TransferAndCommentKeyGuidMask;
            public Guid TransferAndCommentKeyGuidMaskEqual;
        }
        /**/
        public enum EWalletClientEventTypes : sbyte
        {
            NewSentTransfer, // (MutableTuple) walletToGuid, transferGuid, transferSentDateTime
            NewReceivedTransfer // (MutableTuple) walletFromGuid, transferGuid, transferSentDateTime
        }

        public class WalletClientEventSerialized
        {
            public static Type GetEventArgsType(EWalletClientEventTypes eventType)
            {
                switch (eventType)
                {
                    case EWalletClientEventTypes.NewSentTransfer:
                    case EWalletClientEventTypes.NewReceivedTransfer:
                        return typeof(MutableTuple<Guid, Guid, DateTime>);
                    default:
                        throw new ArgumentOutOfRangeException(
                            MyNameof.GetLocalVarName(() => eventType));
                }
            }
            public virtual Guid EventGuid { get; set; }
            public virtual EWalletClientEventTypes EventType { get; set; }
            public virtual string SerializedEventArgs { get; set; }
            public virtual DateTime RaisedDateTime { get; set; }
        }

        public class WalletSubscribeClientEventsRequest : ICheckable
        {
            public DateTime StartTime { get; set; }
            public Guid LastKnownEventGuid { get; set; }
            public int MaxBufferCount { get; set; } = 100;
            public int MaxBufferSeconds { get; set; } = 3;
            public int TimeoutSeconds { get; set; } 
                = ClientLifecycleEnvironment.LifeCycle == ELifeCycle.Dev
                    ? 5
                    : 180;
            public void CheckMe()
            {
                Assert.InRange(
                    StartTime,
                    new DateTime(2000, 1, 1),
                    new DateTime(3000, 1, 1)
                );
                Assert.InRange(MaxBufferCount, 1, 100);
                Assert.InRange(MaxBufferSeconds, 0, 30);
                Assert.InRange(TimeoutSeconds, 0, 60 * 5); // 5 minutes
            }
        }

        /**/
        public interface IAuthenticatedFromClientToWallet
        {
            [BalanceCost(WalletServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<List<WalletClientEventSerialized>> SubscribeClientEvents(
                WalletSubscribeClientEventsRequest request
            );

            [BalanceCost(WalletServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<WalletServerInfoForClient> GetServerInfo();

            [BalanceCost(WalletServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<WalletClientSettingsOnServer> GetMySettings();

            [BalanceCost(WalletServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task UpdateMySettings(WalletClientSettingsOnServer settingsOnServer);
            /**/
            [BalanceCost(WalletServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task GenerateNewCommentKey(
                GenNewCommentKeyOrderRequest request
            );

            [BalanceCost(WalletServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<List<WalletCommentKeyClientInfo>> GetCommentOutKeyList(
                List<Guid> commentGuidList
            );

            [BalanceCost(WalletServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<List<WalletCommentKeyClientInfo>> GetCommentInKeyList(
                List<Guid> commentGuidList
            );

            [BalanceCost(WalletServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<List<WalletCommentKeyClientInfo>> GetValidCommentOutKeyList(
                Guid walletTo
            ); 
            /**/
            [BalanceCost(WalletServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<List<SentTransactionInfo>> GetSentTransferInfo(
                GetSentTransfersOrderRequest request
            );

            [BalanceCost(WalletServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<List<LightCertificate>> GetOtherWalletCert(
                GetWalletCertRequest request
            );

            [BalanceCost(WalletServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<long> GetWalletBalance();

            [BalanceCost(WalletServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<List<IncomingTransactionInfo>> SubscribeIncomeTransactions(
                SubscribeIncomingTransactionsOrderRequest request
            );

            [BalanceCost(WalletServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<CheckSimpleTransferWasProcessedResponse> WasSimpleTransferRequestProcessed(
                CheckSimpleTransferWasProcessedRequest request
            );
        }

        public enum ERegisterWalletErrCodes
        {
            AlreadyRegistered,
            AlreadyRegisteredOther,
            WrongCert,
            WrongCertSignature,
            WrongRequestSentTime,
            WrongCertGuid
        }
        public interface IFromClientToWallet : IGetAuthInfo, IAuthMe, IPingable
        {
            // ISignedFromClientToWallet
            [BalanceCostExecutionFromType(
                WalletServerBalanceCosts.EveryOperationDefaultCost,
                typeof(ISignedFromClientToWallet)
            )]
            Task<byte[]> ProcessSignedRequestPacket(
                SignedData<OneSideSignedRequestPacket> signedRequestPacket
            );
            // SignedFromClientToWallet fees
            [BalanceCost(WalletServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<List<RpcMethodInfoWithFees>> GetSignedMethodInfos();

            // IAuthenticatedFromClientToWallet
            [BalanceCostExecutionFromType(
                WalletServerBalanceCosts.EveryOperationDefaultCost,
                typeof(IAuthenticatedFromClientToWallet)
            )]
            Task<byte[]> ProcessAuthenticatedPacket(
                byte[] packet, Guid walletId
            );
            // AuthenticatedFromClientToWallet fees
            [BalanceCost(WalletServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<List<RpcMethodInfoWithFees>> GetAuthenticatedMethodInfos();

            [BalanceCost(WalletServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<bool> IsWalletRegistered(Guid walletId);

            [BalanceCost(
                WalletServerBalanceCosts.EveryOperationDefaultCost, 
                WalletServerBalanceCosts.RegistrationWalletCost
            )]
            Task RegisterWallet(
                SignedData<RegisterWalletOrderRequest> signedRequest
            );

            [BalanceCost(WalletServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<bool> IsWalletGuidValidForRegistration(Guid walletGuid);
        }
        public static class WalletServerBalanceCosts
        {
            public const double EveryOperationDefaultCost 
                = CommonClientConstants.EveryOperationDefaultCost;
            public const double RegistrationWalletCost 
                = CommonClientConstants.RegistrationDefaultCost;
        }
        public class BitmoneyInvoiceData : ICheckable
        {
            public Guid WalletTo
                = Guid.Empty;
            public byte[] CommentBytes
                = Encoding.UTF8.GetBytes("");
            public long TransferAmount = 0;
            public bool ForceAnonymousTransfer = false;
            public void CheckMe()
            {
				if (CommentBytes == null)
                    throw new ArgumentNullException(
                        this.MyNameOfProperty(e => e.CommentBytes));
                if (
                    CommentBytes.Length + 32 
                        > WalletServerRestrictionsForClient.MaxCommentBytesCount
                )
                    throw new ArgumentOutOfRangeException(
                        this.MyNameOfProperty(e => e.CommentBytes) + "."
                            + MyNameof.GetLocalVarName(() => CommentBytes.Length)
                    );
                if (
                    TransferAmount <= 0
                    || TransferAmount
                        > WalletServerRestrictionsForClient.MaxTransferAmount
                )
                    throw new ArgumentOutOfRangeException(
                        this.MyNameOfProperty(e => e.TransferAmount));
            }
        }
    }
}
