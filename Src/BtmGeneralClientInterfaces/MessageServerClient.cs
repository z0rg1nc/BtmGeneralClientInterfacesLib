using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using BtmI2p.AesHelper;
using BtmI2p.GeneralClientInterfaces.CdnProxyServer;
using BtmI2p.LightCertificates.Lib;
using BtmI2p.MiscUtil.Conversion;
using BtmI2p.MiscUtil.IO;
using BtmI2p.MiscUtils;
using BtmI2p.OneSideSignedJsonRpc;
using BtmI2p.SamHelper;
using Xunit;

namespace BtmI2p.GeneralClientInterfaces
{
    namespace MessageServer
    {
        public static class MessageServerClientLimitations
        {
            public static readonly int MaxMessageSize = 64000;
            public static readonly TimeSpan MaxMessageLifetime 
                = TimeSpan.FromDays(180.0);
            public static readonly decimal MaxUnauthorizedIncomeMessageFee 
                = 1000.0m;
            public static readonly int MaxSentReceiveMessageListSumSize
                = (new ReliableSamHelperSettings()).MaxMesageLength - 50000;
        }
        public enum EMessageGeneralErrCodes
        {
            WrongRequest = 100,
            UserNotFound,
            MessageKeyNotExist
        }
        public static class MessageServerClientConstants
        {
            public static readonly Dictionary<MessageClientGuidType, int>
                MessageClientGuidFirstZeroBytesCount
                    = new Dictionary<MessageClientGuidType, int>()
                    {
                        { MessageClientGuidType.Clients, 0 },
                        { MessageClientGuidType.ClientsReserved, 4 },
                        { MessageClientGuidType.SystemUsed, 10 }
                    };
            public static MessageClientGuidType GetMessageClientGuidType(Guid userId)
            {
                var systemUsedMask
                    = Guid.Parse("FFFFFFFF-FFFF-FFFF-FFFF-000000000000");
                var systemUsedMaskEqual = Guid.Empty;
                if (
                    userId.CheckMask(
                        systemUsedMask,
                        systemUsedMaskEqual
                    )
                )
                    return MessageClientGuidType.SystemUsed;
                /*******************/
                var clientsReservedMask
                    = Guid.Parse("FFFFFFFF-0000-0000-0000-000000000000");
                var clientsReservedMaskEqual = Guid.Empty;
                if (
                    userId.CheckMask(
                        clientsReservedMask,
                        clientsReservedMaskEqual
                    )
                )
                    return MessageClientGuidType.ClientsReserved;
                /*******************/
                return MessageClientGuidType.Clients;
            }
        }
        // used for cert id verification also
        public enum MessageClientGuidType
        {
            SystemUsed,
            ClientsReserved,
            Clients
        }

        
        public static class MessageServerMessageFeeHelper
        {
            public const decimal ConstFee = 0.01m;
            public const decimal Fee1Kb = 0.1m;
            public const decimal ConstFee1Hour = 0.01m;
            public const decimal Fee1Hour = 0.1m;

            public static decimal GetFee(
                int messageSize,
                TimeSpan saveForTimeSpan,
                bool unauthorizedUser = false,
                decimal unauthorizedUserFee = 0.0m
            )
            {
                var result = ConstFee
                    + (ConstFee1Hour + (Fee1Kb * messageSize / 1024.0m))
                        *(Math.Abs((decimal)saveForTimeSpan.TotalHours))*Fee1Hour;
                if (unauthorizedUser)
                    result += unauthorizedUserFee;
                return result;
            }
        }
        public class GetOtherUserPubCertCommandRequest : ICheckable
        {
            public List<Guid> OtherUserGuidList;
            public void CheckMe()
            {
                Assert.NotNull(OtherUserGuidList);
                Assert.InRange(OtherUserGuidList.Count,0,20);
                Assert.Equal(OtherUserGuidList,OtherUserGuidList.Distinct());
            }
        }
        public class SentMessageInfo
        {
            public Guid MessageGuid;
            public Guid UserToGuid;
            public int MessageType;
            public byte[] MessageBody;
            public Guid MessageKeyGuid = Guid.Empty;
            public DateTime MessageSentTime;
            public DateTime MessageSaveUntil;
            public EMessageSignatureType SignatureType;
            public byte[] EncryptedSignature;
            /**/
            public bool CheckSignature(Guid userFrom, AesKeyIvPair pair)
            {
                if (SignatureType == EMessageSignatureType.None)
                    return false;
                else
                {
                    Assert.NotNull(pair);
                    Assert.NotNull(EncryptedSignature);
                    return EncryptedSignature.SequenceEqual(
                        pair.EncryptData(
                            new SendMessageCommandRequest()
                            {
                                UserTo = UserToGuid,
                                SignatureType = SignatureType,
                                MessageBody = MessageBody,
                                MessageGuid = MessageGuid,
                                MessageKeyGuid = MessageKeyGuid,
                                MessageType = MessageType,
                                SentDateTime = MessageSentTime,
                                SaveUntil = MessageSaveUntil
                            }.GetDataForSignature(userFrom)
                        )
                    );
                }
            }
        }
        public class GetSentMessagesCommandRequest : ICheckable
        {
            public DateTime DateTimeFrom = DateTime.MinValue;
            public DateTime DateTimeTo = DateTime.MaxValue;
            public Guid UserTo = Guid.Empty;
            public Guid LastKnownMessageGuid = Guid.Empty;
            public int BufferCount = 100;
            public int MessageTypeFilter = -1; //All
            public void CheckMe()
            {
                Assert.InRange(
                    BufferCount,
                    1,
                    100
                );
                Assert.True(DateTimeTo >= DateTimeFrom);
            }
        }
        public class IncomingMessageInfo
        {
            public Guid MessageGuid;
            public Guid UserFromGuid;
            public int MessageType;
            public byte[] MessageBody;
            public Guid MessageKeyGuid = Guid.Empty;
            public DateTime MessageSentTime;
            public DateTime MessageSaveUntil;
            public EMessageSignatureType SignatureType;
            public byte[] EncryptedSignature;
            /**/

            public bool CheckSignature(Guid userToGuid, AesKeyIvPair pair)
            {
                if (SignatureType == EMessageSignatureType.None)
                    return false;
                else
                {
                    Assert.NotNull(pair);
                    Assert.NotNull(EncryptedSignature);
                    return EncryptedSignature.SequenceEqual(
                        pair.EncryptData(
                            new SendMessageCommandRequest()
                            {
                                UserTo = userToGuid,
                                SignatureType = SignatureType,
                                MessageBody = MessageBody,
                                MessageGuid = MessageGuid,
                                MessageKeyGuid = MessageKeyGuid,
                                MessageType = MessageType,
                                SentDateTime = MessageSentTime,
                                SaveUntil = MessageSaveUntil
                            }.GetDataForSignature(UserFromGuid)
                        )
                    );
                }
            }
        }
        public class GetIncomeMessageCommandRequest : ICheckable
        {
            public int BufferCount = 30;
            public Guid LastKnownMessageGuid = Guid.Empty;
            public Guid UserFrom = Guid.Empty;
            public int MessageTypeFilter = -1; //All
            public DateTime SentTimeFrom = DateTime.MinValue;
            public DateTime SentTimeTo = DateTime.MaxValue; //Always for online
            public void CheckMe()
            {
                Assert.InRange(
                    BufferCount,
                    1,
                    100
                );
                Assert.True(SentTimeTo >= SentTimeFrom);
            }
        }
        public enum EUserMessageType
        {
            Utf8Text
        }

        public enum EMessageSignatureType : sbyte
        {
            None,
            /* 
                MessageGuid . UserFromGuid . UserToGuid . MessageType . Sha256(MessageBody) 
                . MessageSentTime (ticks, round to seconds) . MessageKeyGuid 
                . MessageSaveUntil (ticks, round to seconds) . SignatureType
            */
            Type20150924
        }
        public class SendMessageCommandRequest
        {
            public byte[] GetDataForSignature(
                Guid userFromGuid
            )
            {
                if (SignatureType == EMessageSignatureType.Type20150924)
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
                                writer.Write(MessageGuid.ToByteArray());
                                writer.Write(userFromGuid.ToByteArray());
                                writer.Write(UserTo.ToByteArray());
                                writer.Write(MessageType);
                                writer.Write(sha256.ComputeHash(MessageBody));
                                writer.Write(MiscFuncs.RoundDateTimeToSeconds(SentDateTime).Ticks);
                                writer.Write(MessageKeyGuid.ToByteArray());
                                writer.Write(MiscFuncs.RoundDateTimeToSeconds(SaveUntil).Ticks);
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

            public SendMessageCommandRequest()
            {
            }

            public SendMessageCommandRequest(DateTime nowTime, TimeSpan keepForTs)
            {
                SentDateTime = MiscFuncs.RoundDateTimeToSeconds(nowTime);
                SaveUntil = MiscFuncs.RoundDateTimeToSeconds(nowTime + keepForTs);
            }

            public Guid RequestGuid = MiscFuncs.GenGuidWithFirstBytes(0);
            public Guid MessageGuid;
            public Guid UserTo;
            public byte[] MessageBody;
            public Guid MessageKeyGuid = Guid.Empty;
            public int MessageType;
            public DateTime SentDateTime;
            public DateTime SaveUntil;
	        public decimal MaxMessageFee = 0.0m;
            public EMessageSignatureType SignatureType = EMessageSignatureType.None;
            public byte[] EncryptedSignature;
            public void CheckMe(
                Guid messageAndMessageKeyGuidMask,
                Guid messageAndMessageKeyGuidMaskEqual,
                EKeyAlgType keyAlgType
            )
            {
                var nowTimeUtc = DateTime.UtcNow;
                Assert.NotEqual(RequestGuid,Guid.Empty);
                Assert.NotEqual(MessageGuid,Guid.Empty);
                Assert.True(
                    MessageGuid.CheckMask(
                        messageAndMessageKeyGuidMask,
                        messageAndMessageKeyGuidMaskEqual
                    )
                );
                Assert.NotEqual(UserTo,Guid.Empty);
                Assert.NotNull(MessageBody);
                Assert.False(MessageBody.Length > MessageServerClientLimitations.MaxMessageSize);
                Assert.True(MaxMessageFee >= 0);
                Assert.Equal(
                    SentDateTime,
                    MiscFuncs.RoundDateTimeToSeconds(SentDateTime)
                );
                Assert.InRange(
                    SentDateTime,
                    nowTimeUtc.Subtract(TimeSpan.FromMinutes(5.0)),
                    nowTimeUtc.AddMinutes(5.0)
                );
                Assert.Equal(
                    SaveUntil,
                    MiscFuncs.RoundDateTimeToSeconds(SaveUntil)
                );
                Assert.True(SaveUntil > SentDateTime);
                Assert.True(
                    SaveUntil - SentDateTime
                    <= MessageServerClientLimitations.MaxMessageLifetime
                );
                if (MessageKeyGuid == Guid.Empty)
                {
                    Assert.Equal(SignatureType, EMessageSignatureType.None);
                    Assert.NotNull(EncryptedSignature);
                    Assert.Empty(EncryptedSignature);
                }
                else
                {
                    Assert.Equal(SignatureType,EMessageSignatureType.Type20150924);
                    Assert.NotNull(EncryptedSignature);
                    Assert.Equal(EncryptedSignature.Length,80);
                }
            }
        }

        public class SendMessageCommandResponse
        {
            public decimal MessageFee;
            public Guid MessageGuid;
        }
        public class MessageClientSettingsOnServerClientInfo : ICheckable
        {
            public virtual Guid AuthGuid { get; set; } = Guid.NewGuid();
            public virtual decimal UnauthorizedIncomeMessageFee { get; set; } = 100.0m;
            public virtual bool AutoRenewAuthGuid { get; set; } = true;
            public void CheckMe()
            {
                if (
                    UnauthorizedIncomeMessageFee <= 0.0m
                    || UnauthorizedIncomeMessageFee
                        > MessageServerClientLimitations.MaxUnauthorizedIncomeMessageFee
                )
                {
                    throw new ArgumentOutOfRangeException(
                        this.MyNameOfProperty(
                            e => e.UnauthorizedIncomeMessageFee
                        )
                    );
                }
            }
        }
        public class RegisterUserRequest
        {
            public LightCertificate PublicUserCert;
            public LightCertificate PublicMasterUserCert;
            public MessageClientSettingsOnServerClientInfo ClientSettingsOnServer;
            public DateTime SentTime;
            public Guid RequestGuid = Guid.NewGuid();
        }

        public enum EMessageKeySignature : sbyte
        {
            None,
            /* MessageKeyGuid . UserFromGuid . UserToGuid . sha256(DecryptedKey) . IssuedTime(ticks) 
            . ValidUntil(ticks) . KeyAlgType . KeySignatureType
            */
            Type20150923
        }
        public enum EKeyAlgType : sbyte
        {
            Aes256
        }
        public class MessageKeyClientInfo
        {
            public Guid MessageKeyGuid { get; set; }
            public Guid UserFromGuid { get; set; }
            public Guid UserToGuid { get; set; }
            public byte[] KeyEncryptedFrom { get; set; }
            public byte[] KeyEncryptedTo { get; set; }
            public DateTime IssuedTime { get; set; }
            public DateTime ValidUntil { get; set; }
            public EKeyAlgType KeyAlgType { get; set; } = EKeyAlgType.Aes256;
            public EMessageKeySignature KeySignatureType { get; set; } = EMessageKeySignature.Type20150923;
            public LightCertificateSignature KeySignature { get; set; } = null;
            /**/

            public bool CheckInKeySignature(
                LightCertificate userFromCert,
                LightCertificate userToCert,
                byte[] userToCertBytes
                )
            {
                Assert.NotNull(userFromCert);
                Assert.NotNull(userToCert);
                Assert.NotNull(userToCertBytes);
                if (KeySignatureType == EMessageKeySignature.None)
                    return false;
                else
                {
                    Assert.NotNull(KeySignature);
                    Assert.Equal(KeySignature.SignerCertificateId, userFromCert.Id);
                    var decryptedKey = userToCert.DecryptData(KeyEncryptedTo, userToCertBytes);
                    var dataForSigning = GetByteArrayForSigning(decryptedKey);
                    return userFromCert.VerifyData(
                        new SignedData()
                        {
                            Data = dataForSigning,
                            Signature = KeySignature
                        }
                    );
                }
            }

            /**/
            public bool CheckOutKeySignature(
                LightCertificate userFromCert, 
                byte[] userFromCertBytes
            )
            {
                Assert.NotNull(userFromCert);
                Assert.NotNull(userFromCertBytes);
                if (KeySignatureType == EMessageKeySignature.None)
                    return false;
                else
                {
                    Assert.NotNull(KeySignature);
                    Assert.Equal(KeySignature.SignerCertificateId, userFromCert.Id);
                    var decryptedKey = userFromCert.DecryptData(KeyEncryptedFrom, userFromCertBytes);
                    var dataForSigning = GetByteArrayForSigning(decryptedKey);
                    return userFromCert.VerifyData(
                        new SignedData()
                        {
                            Data = dataForSigning,
                            Signature = KeySignature
                        }
                    );
                }
            }

            /**/
            public byte[] GetByteArrayForSigning(
                byte[] decryptedKey
            )
            {
                using (var ms = new MemoryStream())
                {
                    if (KeySignatureType == EMessageKeySignature.Type20150923)
                    {
                        var littleConverter = new LittleEndianBitConverter();
                        using (var sha256 = new SHA256Managed())
                        {
                            using (
                                var writer = new EndianBinaryWriter(
                                    littleConverter,
                                    ms
                                )
                            )
                            {
                                writer.Write(MessageKeyGuid.ToByteArray());
                                writer.Write(UserFromGuid.ToByteArray());
                                writer.Write(UserToGuid.ToByteArray());
                                writer.Write(sha256.ComputeHash(decryptedKey));
                                writer.Write(MiscFuncs.RoundDateTimeToSeconds(IssuedTime).Ticks);
                                writer.Write(MiscFuncs.RoundDateTimeToSeconds(ValidUntil).Ticks);
                                writer.Write((sbyte)KeyAlgType);
                                writer.Write((sbyte)KeySignatureType);
                            }
                            return sha256.ComputeHash(ms.ToArray());
                        }
                        
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
            }
        }
        public class GenNewMessageKeyCommandRequest
        {
            public MessageKeyClientInfo KeyInfo;
            public Guid RequestGuid = Guid.NewGuid();
            public void CheckMe(
                Guid messageAndMessageKeyGuidMask,
                Guid messageAndMessageKeyGuidMaskEqual,
                int expectedEncryptedKeyLength,
                Guid userFromGuid,
                int expectedSignatureLength
            )
            {
                Assert.NotEqual(RequestGuid,Guid.Empty);
                Assert.NotNull(KeyInfo);
                Assert.NotEqual(KeyInfo.MessageKeyGuid,Guid.Empty);
                Assert.True(
                    KeyInfo.MessageKeyGuid.CheckMask(
                        messageAndMessageKeyGuidMask,
                        messageAndMessageKeyGuidMaskEqual
                    )
                );
                Assert.Equal(KeyInfo.UserFromGuid,userFromGuid);
                Assert.NotEqual(KeyInfo.UserToGuid,Guid.Empty);
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
                    KeyInfo.ValidUntil,
                    MiscFuncs.RoundDateTimeToSeconds(KeyInfo.ValidUntil)
                );
                Assert.True(KeyInfo.IssuedTime < KeyInfo.ValidUntil);
                Assert.True(KeyInfo.ValidUntil <= nowTime.AddMonths(1));
                Assert.Equal(KeyInfo.KeyAlgType,EKeyAlgType.Aes256);
                Assert.Equal(KeyInfo.KeySignatureType,EMessageKeySignature.Type20150923);
                Assert.NotNull(KeyInfo.KeySignature);
                Assert.Equal(
                    KeyInfo.KeySignature.SignerCertificateId,
                    KeyInfo.UserFromGuid
                );
                Assert.NotNull(KeyInfo.KeySignature.SignatureBytes);
                Assert.Equal(KeyInfo.KeySignature.SignatureBytes.Length, expectedSignatureLength);
            }
        }
        /**/
        public enum ESaveNewMessageKeyErrCodes
        {
            KeyAlreadyExist,
            KeyAlreadyExistOtherUser
        }
        public interface ISignedFromClientToMessage
        {
            
        }
        public enum ESendMessageErrCodes
        {
            KeyIsNotRegistered,
            ExpiredKey,
            UserToNotExist,
            NotEnoughFunds,
            WrongSentTime,
            WrongMessageFee,
            WrongMessageRequest,
            AlreadyProcessedOther
        }

        public enum ESaveMySettingsErrCodes
        {
            WrongSettings
        }
        
        public enum EAuthMeWriteToUserErrCodes
        {
            WrongAuthGuid
        }
        
        public class MessageServerInfoForClient
        {
            public Guid WalletGuid;
            public Guid MessageAndMessageKeyGuidMask;
            public Guid MessageAndMessageKeyGuidMaskEqual;
        }
        /**/
        public enum EMessageClientEventTypes : sbyte
        {
            NewSentMessage, // (MutableTuple) userToGuid, messageGuid, sentDateTime
            NewReceivedMessage // (MutableTuple) userFromGuid, messageGuid, sentDateTime
        }
        public class MessageClientEventSerialized
        {
            public static Type GetEventArgsType(EMessageClientEventTypes eventType)
            {
                switch (eventType)
                {
                    case EMessageClientEventTypes.NewSentMessage:
                    case EMessageClientEventTypes.NewReceivedMessage:
                        return typeof(MutableTuple<Guid, Guid, DateTime>);
                    default:
                        throw new ArgumentOutOfRangeException(
                            MyNameof.GetLocalVarName(() => eventType));
                }
            }
            public virtual Guid EventGuid { get; set; }
            public virtual EMessageClientEventTypes EventType { get; set; }
            public virtual string SerializedEventArgs { get; set; }
            public virtual DateTime RaisedDateTime { get; set; }
        }

        public class MessageSubscribeClientEventsRequest : ICheckable
        {
            public MessageSubscribeClientEventsRequest()
            {
#if DEBUG
                TimeoutSeconds = 5;
#else
                TimeoutSeconds = 180;
#endif
            }

            public DateTime StartTime { get; set; }
            public Guid LastKnownEventGuid { get; set; }
            public int MaxBufferCount { get; set; } = 100;
            public int MaxBufferSeconds { get; set; } = 3;
            public int TimeoutSeconds { get; set; }
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
        public interface IAuthenticatedFromClientToMessage
        {
            [BalanceCost(MessageServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<List<MessageClientEventSerialized>> SubscribeClientEvents(
                MessageSubscribeClientEventsRequest request
            );

            [BalanceCost(MessageServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task SaveNewMessageKey(
                GenNewMessageKeyCommandRequest request
            );
            [BalanceCost(MessageServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<List<LightCertificate>> GetOtherUserPubCertList(
                GetOtherUserPubCertCommandRequest request
            );
            /**/
            [BalanceCost(MessageServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<SendMessageCommandResponse> SendMessage(SendMessageCommandRequest request);

            [BalanceCost(MessageServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<List<SentMessageInfo>> GetSentMessages(
                GetSentMessagesCommandRequest request
            );

            [BalanceCost(MessageServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<List<IncomingMessageInfo>> GetIncomingMessages(
                GetIncomeMessageCommandRequest request
            );
            /**/
            [BalanceCost(MessageServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<List<MessageKeyClientInfo>> GetMessageInKeyList(
                List<Guid> messageKeyGuidList
            );

            [BalanceCost(MessageServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<List<MessageKeyClientInfo>> GetMessageOutKeyList(
                List<Guid> messageKeyGuidList
            );

            [BalanceCost(MessageServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<List<MessageKeyClientInfo>> GetValidMessageOutKeyList(
                Guid userTo
            ); 
            /**/
            [BalanceCost(MessageServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<decimal> GetBalance();
            /**/
            [BalanceCost(MessageServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<MessageClientSettingsOnServerClientInfo> GetMySettings();

            [BalanceCost(MessageServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task SaveMySettings(MessageClientSettingsOnServerClientInfo settings);

            [BalanceCost(MessageServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<List<MessageClientSettingsOnServerClientInfo>> GetOtherUserSettings(
                List<Guid> otherUserGuids
            );
            /**/
            [BalanceCost(MessageServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task AmOnline();

            [BalanceCost(MessageServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<List<Guid>> GetOnlineUsers(List<Guid> testedUserGuids);
            /**/
            [BalanceCost(MessageServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task GrantMeWriteToUser(Guid userTo, Guid userToAuthGuid);

            [BalanceCost(MessageServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task GrantUserWriteToMe(Guid userGuid);

            [BalanceCost(MessageServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task RevokeUserPermissionWriteToMe(Guid userGuid);

            [BalanceCost(MessageServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<List<Guid>> GetIGrantedWriteToUserList(
                List<Guid> testedUserGuids
            );

            [BalanceCost(MessageServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<List<Guid>> GetGrantedWriteToMeUserList(
                List<Guid> testedUserGuids
            );

            [BalanceCost(MessageServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<MessageServerInfoForClient> GetServerInfo();
        }

        public enum ERegisterMessageClientErrCodes
        {
            RegisteredAlready,
            RegisteredAlreadyOther,
            WrongRequestSentTime,
            WrongCert,
            WrongCertSignature,
            WrongCertGuid
        }
        public interface IFromClientToMessage 
            : IGetAuthInfo, IAuthMe
        {
            // ISignedFromClientToUser
            [BalanceCostExecutionFromType(
                MessageServerBalanceCosts.EveryOperationDefaultCost,
                typeof(ISignedFromClientToMessage)
            )]
            Task<byte[]> ProcessSignedRequestPacket(
                SignedData<OneSideSignedRequestPacket> signedRequestPacket
            );
            // SignedFromClientToUser fees
            [BalanceCost(MessageServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<List<RpcMethodInfoWithFees>> GetSignedMethodInfos();

            // IAuthenticatedFromClientToUser
            [BalanceCostExecutionFromType(
                MessageServerBalanceCosts.EveryOperationDefaultCost,
                typeof(IAuthenticatedFromClientToMessage)
            )]
            Task<byte[]> ProcessAuthenticatedPacket(
                byte[] packet, Guid userId
                );
            // AuthenticatedFromClientToUser fees
            [BalanceCost(MessageServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<List<RpcMethodInfoWithFees>> GetAuthenticatedMethodInfos();
            /**/
            [BalanceCost(
                MessageServerBalanceCosts.EveryOperationDefaultCost, 
                MessageServerBalanceCosts.RegistrationMessageClientCost
            )]
            Task RegisterMessageClient(
                SignedData<RegisterUserRequest> request
            );

            [BalanceCost(MessageServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<bool> IsUserCertRegistered(
                Guid userId
            );

            [BalanceCost(MessageServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<bool> IsUserGuidValidForRegistration(Guid userGuid);
        }

        public static class MessageServerBalanceCosts
        {
            public const double EveryOperationDefaultCost 
                = CommonClientConstants.EveryOperationDefaultCost;
            public const double RegistrationMessageClientCost 
                = CommonClientConstants.RegistrationDefaultCost;
        }
    }
}
