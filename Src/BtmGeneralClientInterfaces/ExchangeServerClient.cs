using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BtmI2p.GeneralClientInterfaces.CdnProxyServer;
using BtmI2p.LightCertificates.Lib;
using BtmI2p.MiscUtils;
using BtmI2p.Newtonsoft.Json;
using BtmI2p.OneSideSignedJsonRpc;
using Xunit;

namespace BtmI2p.GeneralClientInterfaces
{
    namespace ExchangeServer
    {
	    public static class ExchangeServerConstants
	    {
		    public const int MaxSecCodeLength = 20;
		    public const int MaxCurrencyCodeLength = 10;
			public const string FeeCurrencyCode = "BTM";
			public const decimal MaxAbsCurrencyValue = 1000000000000.0m;
	    }

	    public enum EExchangeGeneralErrCodes
	    {
		    WrongRequest = 100,
			AccountNotFound,
            SecurityNotFound,
			NotEnoughFunds,
            FeeAccountNotFound,
            FeeNotEnoughFunds
		}
        /**/
        public class ExchangeAccountClientInfo : ICheckable
        {
            public virtual Guid AccountGuid { get; set; }
            public virtual string CurrencyCode { get; set; }
            public virtual bool IsDefaultForTheCurrency { get; set; }
            public void CheckMe()
            {
                Assert.False(AccountGuid == Guid.Empty);
                Assert.False(string.IsNullOrWhiteSpace(CurrencyCode));
            }
        }
        /**/
        public class ExchangeCurrencyPairSecurityClientInfo : ICheckable
        {
            public virtual string SecCode { get; set; }
            public virtual string SecondCurrencyCode { get; set; }
            public void CheckMe()
            {
                Assert.False(string.IsNullOrWhiteSpace(SecCode));
                Assert.False(string.IsNullOrWhiteSpace(SecondCurrencyCode));
            }
        }
        /**/
        public class ExchangeCurrencyClientInfo : ICheckable
        {
            public virtual string Code { get; set; }
			public virtual string Description { get; set; }
			public virtual sbyte Scale { get; set; }
            /**/
			public virtual decimal MinDeposit { get; set; }
			public virtual decimal MaxDeposit { get; set; }
			public virtual decimal DepositInaccuracyConst { get; set; }
            /**/
			public virtual decimal NewDepositFeeConstBtmPos { get; set; }
            public virtual decimal DepositFeeConst { get; set; }
            public virtual decimal DepositFeePercent { get; set; }
            /**/
			public virtual decimal MinWithdrawPos { get; set; }
			public virtual decimal MaxWithdrawPos { get; set; }
            /**/
			public virtual decimal NewWithdrawFeeConstBtmPos { get; set; }
			public virtual decimal WithdrawFeeConstPos { get; set; }
			public virtual decimal WithdrawFeePercent { get; set; }
            /**/
            public void CheckMe()
            {
                Assert.False(string.IsNullOrWhiteSpace(Code));
                Assert.NotNull(Description);
                Assert.True(Scale >= 0);
				Assert.True(MinDeposit > 0);
				Assert.True(MaxDeposit >= MinDeposit);
				Assert.True(DepositInaccuracyConst >= 0);
				Assert.True(NewDepositFeeConstBtmPos >= 0);
                Assert.True(DepositFeeConst >= 0);
                Assert.True(DepositFeePercent >= 0 && DepositFeePercent <= 100);
				Assert.True(MinWithdrawPos > 0);
				Assert.True(MaxWithdrawPos >= MinWithdrawPos);
				Assert.True(NewWithdrawFeeConstBtmPos >= 0);
				Assert.True(WithdrawFeeConstPos >= 0);
				Assert.True(WithdrawFeePercent >= 0 && WithdrawFeePercent <= 100);
            }
        }

        public enum EExchangeSecurityType : sbyte
        {
            CurrencyPair
        }

        public enum EExchangeSecurityStatus : sbyte
        {
            Active,
            Suspended,
            Expired
        }

        public class ExchangeSecurityClientInfo : ICheckable
        {
            public virtual string Code { get; set; } = "";
            public virtual EExchangeSecurityType SecurityType { get; set; } = EExchangeSecurityType.CurrencyPair;
            public virtual string ParentSecurityCode { get; set; } = "";
            public virtual EExchangeSecurityStatus Status { get; set; } = EExchangeSecurityStatus.Active;
            public virtual string Description { get; set; } = "";
            public virtual string BaseCurrencyCode { get; set; } = "";
            public virtual sbyte Scale { get; set; } = 0;
            public virtual decimal PriceStep { get; set; } = 0.0m;
            public virtual decimal Lot { get; set; } = 1.0m;
            public virtual DateTime Expiration { get; set; } = new DateTime(3000, 1, 1);
            public virtual decimal MinPrice { get; set; } = 0.0m;
            public virtual decimal MaxPrice { get; set; } = 0.0m;
            public void CheckMe()
            {
                Assert.False(string.IsNullOrWhiteSpace(Code));
                Assert.True(Enum.IsDefined(typeof(EExchangeSecurityType), SecurityType));
                Assert.NotNull(ParentSecurityCode);
                Assert.True(Enum.IsDefined(typeof(EExchangeSecurityStatus), Status));
                Assert.NotNull(Description);
                Assert.False(string.IsNullOrWhiteSpace(BaseCurrencyCode));
                Assert.True(Scale >= 0);
                Assert.True(PriceStep > 0.0m);
                Assert.True(Lot > 0.0m);
                Assert.True(MinPrice > 0.0m);
                Assert.True(MaxPrice > 0.0m);
                Assert.True(MinPrice % PriceStep == 0.0m);
                Assert.True(MaxPrice % PriceStep == 0.0m);
            }
        }
        public enum EExchangeOrderSide : sbyte
        {
            Buy,
            Sell
        }

        public enum EExchangeOrderStatus : sbyte
        {
            Active,
            Fulfilled,
            Cancelled
        }
        public class NewExchangeSecurityOrderRequest : ICheckable
        {
            public virtual EExchangeOrderSide Side { get; set; }
            public virtual decimal Price { get; set; }
            public virtual string SecurityCode { get; set; }
            public virtual long Qty { get; set; }
            /* The security base currency */
            public virtual Guid BaseAccountGuid { get; set; }
            /* The security account (second currency 
             * or repository if derivatives)*/
            public virtual Guid SecondAccountGuid { get; set; }
            public virtual Guid RequestGuid { get; set; }
            public void CheckMe()
            {
                Assert.True(Enum.IsDefined(typeof(EExchangeOrderSide), Side));
                Assert.True(Price > 0.0m);
                Assert.False(string.IsNullOrWhiteSpace(SecurityCode));
                Assert.True(Qty > 0);
                Assert.NotEqual(BaseAccountGuid,Guid.Empty);
                Assert.NotEqual(SecondAccountGuid, Guid.Empty);
                Assert.NotEqual(RequestGuid, Guid.Empty);
            }
        }
        /**/
        public class ExchangeSecurityOrderClientInfo : NewExchangeSecurityOrderRequest
        {
            public virtual Guid OrderGuid { get; set; }
            public virtual EExchangeOrderStatus Status { get; set; }
            public virtual DateTime SentDate { get; set; }
            public virtual long FilledQty { get; set; }
            public virtual Guid LockedFundsGuid { get; set; }
        }
        /**/

        public class ExchangeSecurityTradeClientInfo
        {
            public virtual Guid TradeGuid { get; set; }
            public virtual DateTime Date { get; set; }
            public virtual string SecCode { get; set; }
            public virtual long Qty { get; set; }
            /* Only one (yours) is not empty */
            public virtual Guid BidOrderGuid { get; set; }
            public virtual Guid AskOrderGuid { get; set; }
            public virtual decimal Price { get; set; }

            public EExchangeOrderSide GetTradeSide()
                => BidOrderGuid != Guid.Empty ? EExchangeOrderSide.Buy : EExchangeOrderSide.Sell;
        }

        /* Authenticated */
        public enum ERegisterNewAccountErrCodes
        {
            WrongCurrencyCode,
            MaxAccountCountExceed,
            WrongNewAccountGuid,
            NotEnoughFunds,
            AlreadyRegistered,
            AlreadyRegisteredOther
        }
		/* Depth of market */
        public class DomEntry
        {
            public EExchangeOrderSide Side { get; set; }
            public decimal Price { get; set; }
            public long Volume { get; set; }
        }
        /**/
        public class ExchangeAccountTotalBalanceInfo
        {
            public Guid AccountGuid { get; set; }
            public decimal CommittedBalance { get; set; }
            public decimal LockedFundsBalance { get; set; }
            public decimal TotalAvailableBalance { get; set; }
        }
        
        /**/
        public enum EExchangeClientEventTypes : sbyte
        {
            SecurityListChanged, // (int)0
            CurrencyListChanged, // (int)0
            AccountNewTransfer, // (Guid)accountGuid
            AccountLockedFundsAdded, // (MutableTuple) accountGuid, lockedFundsGuid
            AccountLockedFundsModified, // (MutableTuple) accountGuid, lockedFundsGuid
            AccountLockedFundsReleased, // (MutableTuple) accountGuid, lockedFundsGuid
            AccountListChanged, // (int)0
            NewTrade, //(int)0
            NewOrder, //(int)0
            OrderChanged, //(Guid)orderGuid
			SecurityCurrencyPairListChanged, // (int)0
			NewDeposit, //(int)0
			DepositChanged, //(MutableTuple) accountGuid, depositGuid
			NewWithdraw, //(int)0
			WithdrawChanged, //(MutableTuple) accountGuid, withdrawGuid
		}
        public class ExchangeClientEventSerialized
        {
            public static Type GetEventArgsType(EExchangeClientEventTypes eventType)
            {
                switch (eventType)
                {
                    case EExchangeClientEventTypes.SecurityListChanged:
                    case EExchangeClientEventTypes.AccountListChanged:
                    case EExchangeClientEventTypes.CurrencyListChanged:
                    case EExchangeClientEventTypes.NewTrade:
                    case EExchangeClientEventTypes.NewOrder:
					case EExchangeClientEventTypes.NewDeposit:
					case EExchangeClientEventTypes.NewWithdraw:
						return typeof (int);
                    case EExchangeClientEventTypes.AccountNewTransfer:
                    case EExchangeClientEventTypes.OrderChanged:
                        return typeof (Guid);
                    case EExchangeClientEventTypes.AccountLockedFundsAdded:
                    case EExchangeClientEventTypes.AccountLockedFundsModified:
                    case EExchangeClientEventTypes.AccountLockedFundsReleased:
					case EExchangeClientEventTypes.DepositChanged:
					case EExchangeClientEventTypes.WithdrawChanged:
						return typeof (MutableTuple<Guid, Guid>);
                    default:
                        throw new ArgumentOutOfRangeException(
                            MyNameof.GetLocalVarName(() => eventType));
                }
            }

            public virtual Guid EventGuid { get; set; }
            public virtual EExchangeClientEventTypes EventType { get; set; }
            public virtual string SerializedEventArgs { get; set; }
            public virtual DateTime RaisedDateTime { get; set; }
        }

	    public class GetNewBaseRequest
	    {
			public DateTime StartTime { get; set; }
			public int MaxBufferCount { get; set; }

		    public void CheckMe()
		    {
				Assert.InRange(
					StartTime,
					new DateTime(2000, 1, 1),
					new DateTime(3000, 1, 1)
				);
				Assert.InRange(MaxBufferCount, 1, 100);
			}
	    }

		public class ExchangeSubscribeClientEventsRequest : GetNewBaseRequest, ICheckable
		{
            public ExchangeSubscribeClientEventsRequest()
            {
                MaxBufferCount = 100;
                MaxBufferSeconds = 3;
#if DEBUG
                TimeoutSeconds = 5;
#else
                TimeoutSeconds = 60*3;
#endif
            }
			
            public Guid LastKnownEventGuid { get; set; }
            public int MaxBufferSeconds { get; set; }
            public int TimeoutSeconds { get; set; }
            public new void CheckMe()
            {
				base.CheckMe();
                Assert.InRange(MaxBufferSeconds, 0, 30);
                Assert.InRange(TimeoutSeconds, 0, 60*5); // 5 minutes
            }
        }
        /**/
        public enum EExchangeAccountTransferType : sbyte
        {
            ExternalReplenishment,
            ExternalWithdrawal,
            TradeReceived,
            TradeSent,
            Fee,
            InitBalance,
            Revocation
        }
        /**/
        public class ExchangeAccountTranferClientInfo
        {
            public virtual Guid TransferGuid { get; set; }
            public virtual Guid AccountGuid { get; set; }
            public virtual decimal Value { get; set; }
            public virtual EExchangeAccountTransferType TransferType { get; set; }
            public virtual string Note { get; set; }
            public virtual DateTime SentDateTime { get; set; }
        }
        public class GetAccountTransfersRequest : GetNewBaseRequest, ICheckable
		{
            public Guid AccountGuid { get; set; }
            public Guid LastKnownTransferGuid { get; set; }
            public new void CheckMe()
            {
                Assert.NotEqual(AccountGuid, Guid.Empty);
                base.CheckMe();
            }
        }
        /**/
        public class ExchangeAccountLockedFundsClientInfo
        {
            public virtual Guid LockedFundsGuid { get; set; }
            public virtual Guid AccountGuid { get; set; }
            public virtual decimal Value { get; set; }
            public virtual DateTime LockDate { get; set; }
            public virtual bool IsActive { get; set; }
            public virtual string Note { get; set; }
        }
        public class GetActiveAccountLockedFundsRequest : ICheckable
		{
            public Guid AccountGuid { get; set; }
            public Guid LastKnownLockedFundsGuid { get; set; }
            public int MaxBufferCount { get; set; }
            public void CheckMe()
            {
                Assert.NotEqual(AccountGuid, Guid.Empty);
                Assert.InRange(MaxBufferCount, 1, 100);
            }
        }
        /**/

        public class GetAccountLockedFundsStatusRequest : ICheckable
        {
            public Guid AccountGuid { get; set; }
            public List<Guid> LockedFundsGuidList { get; set; }
            public void CheckMe()
            {
                Assert.NotEqual(AccountGuid, Guid.Empty);
                Assert.NotNull(LockedFundsGuidList);
                Assert.InRange(LockedFundsGuidList.Count, 1, 100);
                Assert.Equal(LockedFundsGuidList, LockedFundsGuidList.Distinct());
            }
        }
        /**/
        public class GetNewTradesRequest : GetNewBaseRequest, ICheckable
		{
            public Guid LastKnownTradeGuid { get; set; }
        }
        /**/
        public class GetAllActiveNewOtherOrdersRequest : ICheckable
        {
            public Guid LastKnownOrderGuid { get; set; }
            public int MaxBufferCount { get; set; }
            public DateTime StartTime { get; set; }
            public void CheckMe()
            {
                Assert.InRange(MaxBufferCount, 1, 100);
                Assert.InRange(StartTime,new DateTime(2000,1,1), new DateTime(3000,1,1));
            }
        }
        /**/
        public class GetOrderInfosRequest : ICheckable
        {
            public List<Guid> OrderGuidList { get; set; } 
            public void CheckMe()
            {
                Assert.NotNull(OrderGuidList);
                Assert.InRange(OrderGuidList.Count, 1, 100);
                Assert.Equal(OrderGuidList, OrderGuidList.Distinct());
            }
        }
		/*Deposit withdraw*/
		public enum EExchangeDepositStatus : sbyte
		{
			Created,
			PaymentDetailsReceived,
			PaymentReceived,
			Expired,
			Error,
			SentToPaymentService
		}

	    public class ExchangeDepositClientInfo
	    {
			public virtual Guid DepositGuid { get; set; }
			public virtual Guid AccountGuid { get; set; }
			public virtual Guid RequestGuid { get; set; }
            /**/
			public virtual decimal ExpectedValuePos { get; set; }
			public virtual decimal EstimatedPaymentSystemFeeNeg { get; set; }
            public virtual decimal EstimatedFeeNeg { get; set; }
            /**/
            public virtual decimal FinalPaymentSystemFeeNeg { get; set; }
            public virtual decimal FinalFeeNeg { get; set; }
            /**/
			public virtual EExchangeDepositStatus Status { get; set; }
			public virtual string StatusComment { get; set; }
			public virtual DateTime CreatedDate { get; set; }
			public virtual DateTime ValidUntil { get; set; }
			public virtual string PaymentDetailsSerialized { get; set; }
			public virtual string CurrencyCode { get; set; }
			public virtual decimal FinalPaidTotalValuePos { get; set; }
		}

		public enum EExchangeWithdrawStatus
		{
			Created,
			InQueue,
			Processing,
			Complete,
			Error
		}

	    public class ExchangeWithdrawClientInfo
	    {
			public virtual Guid WithdrawGuid { get; set; }
			public virtual Guid AccountGuid { get; set; }
			public virtual Guid RequestGuid { get; set; }
            /**/
			public virtual decimal ValueNeg { get; set; }
			public virtual decimal EstimatedFeeNeg { get; set; }
            public virtual decimal EstimatedPaymentSystemFeeNeg { get; set; }
            /**/
            public virtual decimal FinalFeeNeg { get; set; }
            public virtual decimal FinalPaymentSystemFeeNeg { get; set; }
            /**/
			public virtual EExchangeWithdrawStatus Status { get; set; }
			public virtual string StatusComment { get; set; }
			public virtual DateTime CreatedDate { get; set; }
			public virtual string PaymentDetailsSerialized { get; set; }
			public virtual string CurrencyCode { get; set; }
            public virtual Guid LockedFundsGuid { get; set; }
        }
		public class GetAllActiveNewOtherDepositsRequest : GetNewBaseRequest, ICheckable
		{
			public Guid LastKnownDepositGuid { get; set; }
		}
		public class GetAllActiveNewOtherWithdrawRequest : GetNewBaseRequest, ICheckable
		{
			public Guid LastKnownWithdrawGuid { get; set; }
		}

	    public class CreateNewDepositRequest : ICheckable
	    {
			public Guid AccountGuid { get; set; }
			public decimal ValuePos { get; set; }
			public Guid RequestGuid { get; set; }
		    public void CheckMe()
		    {
				Assert.NotEqual(AccountGuid, Guid.Empty);
				Assert.True(ValuePos > 0);
				Assert.True(Math.Abs(ValuePos) <= ExchangeServerConstants.MaxAbsCurrencyValue);
				Assert.NotEqual(RequestGuid, Guid.Empty);
		    }
	    }

	    public enum ECreateNewWithdrawErrCodes
	    {
		    NotEnoughFunds
	    }
        /**/
        public enum EExchangePaymentDetailsEntryType : sbyte
        {
            StringType,
            GuidType,
            DecimalType,
            IntType,
            LongType,
            ByteArrayType
        }
        public class ExchangePaymentDetailsEntry : ICheckable
        {
            public string EntryKey { get; set; } = "";
            public string Description { get; set; } = "";
            public EExchangePaymentDetailsEntryType EntryType { get; set; } 
                = EExchangePaymentDetailsEntryType.StringType;
            public string SerializedValue { get; set; } = "".WriteObjectToJson();
            public bool Enabled = true;
            /**/
            public void CheckMe()
            {
                Assert.False(string.IsNullOrWhiteSpace(EntryKey));
                Assert.NotNull(Description);
                Assert.True(
                    Enum.IsDefined(
                        typeof(EExchangePaymentDetailsEntryType),
                        EntryType
                    )
                );
                Assert.NotNull(SerializedValue);
                switch (EntryType)
                {
                    case EExchangePaymentDetailsEntryType.StringType:
                    {
                        var v = StringValue;
                    }
                        break;
                    case EExchangePaymentDetailsEntryType.DecimalType:
                    {
                        var v = DecimalValue;
                    }
                        break;
                    case EExchangePaymentDetailsEntryType.IntType:
                    {
                        var v = IntValue;
                    }
                        break;
                    case EExchangePaymentDetailsEntryType.LongType:
                    {
                        var v = LongValue;
                    }
                        break;
                    case EExchangePaymentDetailsEntryType.GuidType:
                    {
                        var v = GuidValue;
                    }
                        break;
                    case EExchangePaymentDetailsEntryType.ByteArrayType:
                    {
                        var v = ByteArrayValue;
                    }
                        break;
                }
            }
            [JsonIgnore]
            public string StringValue {
                get
                {
                    Assert.NotNull(SerializedValue);
                    Assert.Equal(EntryType, EExchangePaymentDetailsEntryType.StringType);
                    return SerializedValue.ParseJsonToType<string>();
                }
                set
                {
                    Assert.Equal(EntryType,EExchangePaymentDetailsEntryType.StringType);
                    SerializedValue = value.WriteObjectToJson();
                }
            }
            [JsonIgnore]
            public Guid GuidValue
            {
                get
                {
                    Assert.NotNull(SerializedValue);
                    Assert.Equal(EntryType,EExchangePaymentDetailsEntryType.GuidType);
                    return SerializedValue.ParseJsonToType<Guid>();
                }
                set
                {
                    Assert.Equal(EntryType,EExchangePaymentDetailsEntryType.GuidType);
                    SerializedValue = value.WriteObjectToJson();
                }
            }
            [JsonIgnore]
            public decimal DecimalValue {
                get
                {
                    Assert.NotNull(SerializedValue);
                    Assert.Equal(EntryType,EExchangePaymentDetailsEntryType.DecimalType);
                    return SerializedValue.ParseJsonToType<decimal>();
                }
                set
                {
                    Assert.Equal(EntryType,EExchangePaymentDetailsEntryType.DecimalType);
                    SerializedValue = value.WriteObjectToJson();
                }
            }
            [JsonIgnore]
            public int IntValue {
                get
                {
                    Assert.NotNull(SerializedValue);
                    Assert.Equal(EntryType,EExchangePaymentDetailsEntryType.IntType);
                    return SerializedValue.ParseJsonToType<int>();
                }
                set
                {
                    Assert.Equal(EntryType,EExchangePaymentDetailsEntryType.IntType);
                    SerializedValue = value.WriteObjectToJson();
                }
            }
            [JsonIgnore]
            public long LongValue {
                get
                {
                    Assert.NotNull(SerializedValue);
                    Assert.Equal(EntryType,EExchangePaymentDetailsEntryType.LongType);
                    return SerializedValue.ParseJsonToType<long>();
                }
                set
                {
                    Assert.Equal(EntryType,EExchangePaymentDetailsEntryType.LongType);
                    SerializedValue = value.WriteObjectToJson();
                }
            }
            [JsonIgnore]
            public byte[] ByteArrayValue{
                get
                {
                    Assert.NotNull(SerializedValue);
                    Assert.Equal(EntryType,EExchangePaymentDetailsEntryType.ByteArrayType);
                    return SerializedValue.ParseJsonToType<byte[]>();
                }
                set
                {
                    Assert.Equal(EntryType,EExchangePaymentDetailsEntryType.ByteArrayType);
                    SerializedValue = value.WriteObjectToJson();
                }
            }
        }
        public class ExchangePaymentDetails : ICheckable
        {
            public string Description { get; set; }
                = "";
            public List<ExchangePaymentDetailsEntry> PaymentDetailsEntries { get; set; } 
                = new List<ExchangePaymentDetailsEntry>();

            public void CheckMe()
            {
                Assert.NotNull(Description);
                Assert.NotNull(PaymentDetailsEntries);
                foreach (var entry in PaymentDetailsEntries)
                {
                    Assert.NotNull(entry);
                    entry.CheckMe();
                }
                var entryKeys = PaymentDetailsEntries.Select(_ => _.EntryKey).ToList();
                Assert.Equal(entryKeys,entryKeys.Distinct());
            }

            public static bool EntriesStructureEqual(
                ExchangePaymentDetails a, 
                ExchangePaymentDetails b
            )
            {
                try
                {
                    Assert.NotNull(a);
                    a.CheckMe();
                    Assert.NotNull(b);
                    b.CheckMe();
                    Assert.Equal(
                        a.PaymentDetailsEntries.Count,
                        b.PaymentDetailsEntries.Count
                        );
                    var count = a.PaymentDetailsEntries.Count;
                    for (int i = 0; i < count; i++)
                    {
                        var entryA = a.PaymentDetailsEntries[i];
                        var entryB = b.PaymentDetailsEntries[i];
                        Assert.Equal(entryA.EntryKey, entryB.EntryKey);
                        Assert.Equal(entryA.EntryType, entryB.EntryType);
                        Assert.Equal(entryA.Enabled, entryB.Enabled);
                    }
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }
        /*
            EntryType EntryKey
            BTC payment details:
                StringType ADDRESS_B58 "Bitcoin address" ""
                DecimalType VALUE "Bitcoin amount, 8 digit scale" 0.0m
                DecimalType ALLOWED_INACCURACY
            BTM payment details:
                GuidType WALLET_TO
                LongType AMOUNT
                StringType COMMENT_STRING
                StringType INVOICE_SERIALIZED
        */
        public class CreateNewWithdrawRequest : ICheckable
	    {
			public Guid AccountGuid { get; set; }
			public decimal ValueNeg { get; set; }
			public Guid RequestGuid { get; set; }
			public ExchangePaymentDetails PaymentDetails { get; set; }
		    public void CheckMe()
		    {
				Assert.NotEqual(AccountGuid,Guid.Empty);
				Assert.True(ValueNeg < 0.0m);
				Assert.True(Math.Abs(ValueNeg) <= ExchangeServerConstants.MaxAbsCurrencyValue);
				Assert.NotEqual(RequestGuid, Guid.Empty);
				Assert.NotNull(PaymentDetails);
                PaymentDetails.CheckMe();
		    }
	    }

	    public class GetDepositInfosRequest : ICheckable
	    {
			public List<Guid> DepositGuidList { get; set; }
		    public void CheckMe()
		    {
				Assert.NotNull(DepositGuidList);
				Assert.InRange(DepositGuidList.Count,1,100);
				Assert.Equal(DepositGuidList,DepositGuidList.Distinct());
		    }
	    }

	    public class GetWithdrawInfosRequest : ICheckable
	    {
			public List<Guid> WithdrawGuidList { get; set; }
			public void CheckMe()
		    {
				Assert.NotNull(WithdrawGuidList);
				Assert.InRange(WithdrawGuidList.Count, 1, 100);
				Assert.Equal(WithdrawGuidList, WithdrawGuidList.Distinct());
			}
	    }

        public class ProlongDepositOnePeriodRequest : ICheckable
        {
            public Guid RequestGuid { get; set; }
            public Guid AccountGuid { get; set; }
            public Guid DepositGuid { get; set; }
            public void CheckMe()
            {
                Assert.NotEqual(RequestGuid,Guid.Empty);
                Assert.NotEqual(AccountGuid,Guid.Empty);
                Assert.NotEqual(DepositGuid,Guid.Empty);
            }
        }

        public enum EProlongDepositOnePeriodErrCodes
        {
            DepositNotFound,
            WrongDepositStatus,
            MaxProlongationCountExceeded
        }
        public interface IAuthenticatedFromClientToExchange
        {
            [BalanceCost(ExchangeServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<List<ExchangeAccountClientInfo>> GetAllAccounts();

            [BalanceCost(ExchangeServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<Guid> GetNewAccountGuidForRegistration();

            [BalanceCost(ExchangeServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task RegisterNewAccount(ExchangeAccountClientInfo accountInfo);

            [BalanceCost(ExchangeServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<decimal> GetAccountTotalAvailableBalance(Guid accountGuid);

            [BalanceCost(ExchangeServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task MakeAccountDefault(Guid accountGuid);

            [BalanceCost(ExchangeServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<List<ExchangeAccountTotalBalanceInfo>> GetAllAccountBalances();

            [BalanceCost(ExchangeServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<List<ExchangeClientEventSerialized>> SubscribeClientEvents(
                ExchangeSubscribeClientEventsRequest request
            );

            [BalanceCost(ExchangeServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<List<ExchangeAccountTranferClientInfo>> GetAccountTransfers(
                GetAccountTransfersRequest request
            );

            [BalanceCost(ExchangeServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<List<ExchangeAccountLockedFundsClientInfo>> GetNewActiveAccountLockedFunds(
                GetActiveAccountLockedFundsRequest request
            );

            [BalanceCost(ExchangeServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<List<ExchangeAccountLockedFundsClientInfo>> GetAccountLockedFundsInfos(
                GetAccountLockedFundsStatusRequest request
            );

            [BalanceCost(ExchangeServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<List<ExchangeSecurityTradeClientInfo>> GetNewTrades(
                GetNewTradesRequest request
            );

            [BalanceCost(ExchangeServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<List<ExchangeSecurityOrderClientInfo>> GetAllActiveNewOtherOrders(
                GetAllActiveNewOtherOrdersRequest request
            );

            [BalanceCost(ExchangeServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<List<ExchangeSecurityOrderClientInfo>> GetOrderInfos(
                GetOrderInfosRequest request
            );
			/**/
            [BalanceCost(ExchangeServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
			Task<List<ExchangeDepositClientInfo>> GetNewDeposits(
				GetAllActiveNewOtherDepositsRequest request
			);

			[BalanceCost(ExchangeServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
			Task<List<ExchangeDepositClientInfo>> GetDepositInfos(
				GetDepositInfosRequest request);

			/// <summary>
			/// 
			/// </summary>
			/// <param name="request"></param>
			/// <returns>Deposit Guid</returns>
			[BalanceCost(ExchangeServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
			Task<Guid> CreateNewDeposit(CreateNewDepositRequest request);
			/**/
			[BalanceCost(ExchangeServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
			Task<List<ExchangeWithdrawClientInfo>> GetNewWithdraws(
				GetAllActiveNewOtherWithdrawRequest request
			);

			[BalanceCost(ExchangeServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
			Task<List<ExchangeWithdrawClientInfo>> GetWithdrawInfos(
				GetWithdrawInfosRequest request);

			/// <summary>
			/// 
			/// </summary>
			/// <param name="request"></param>
			/// <returns>Withdraw Guid</returns>
			[BalanceCost(ExchangeServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
			Task<Guid> CreateNewWithdraw(CreateNewWithdrawRequest request);

            [BalanceCost(ExchangeServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task ProlongDepositOnePeriod(ProlongDepositOnePeriodRequest request);
        }
		/**/
		public enum EAddNewOrderErrCodes
        {
            SecurityNotFound,
            WrongPrice,
            PriceTooLow,
            PriceTooHigh,
            SecurityNotSupported,
            BaseAccountNotFound,
            SecondAccountNotFound,
            WrongBaseAccountCurrency,
            WrongSecondAccountCurrency,
            FeeAccountNotFound,
            FeeAccountNotEnoughFunds,
            NotEnoughFunds,
        }
        public enum ECancelOrderErrCodes
        {
            SecurityNotFound,
            OrderNotFound,
            WrongStatusFulfilled,
            WrongStatusCancelled,
        }
        /* Signed */
        public interface ISignedFromClientToExchange
        {
            [BalanceCost(ExchangeServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<Guid> AddNewOrder(NewExchangeSecurityOrderRequest request);

            [BalanceCost(ExchangeServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task CancelOrder(Guid orderGuid);
        }
        /**/
        public enum ERegisterExchangeClientCertErrCodes
        {
            WrongRequestTime,
            AlreadyRegisteredOther,
            AlreadyRegistered,
            WrongCert,
            WrongCertGuid
        }
        public class RegisterExchangeClientCertRequest
        {
            public LightCertificate PublicExchangeClientCert;
            public DateTime SentTime;
            public Guid RequestGuid = Guid.NewGuid();
        }
        /**/
        public class GetDomListRequest : ICheckable
        {
            public List<string> SecCodeList { get; set; }
            public void CheckMe()
            {
                Assert.NotNull(SecCodeList);
                Assert.NotEmpty(SecCodeList);
                Assert.InRange(SecCodeList.Count,1,100);
                Assert.Equal(SecCodeList,SecCodeList.Distinct());
	            foreach (var secCode in SecCodeList)
	            {
		            Assert.InRange(secCode.Length,1,ExchangeServerConstants.MaxSecCodeLength);
	            }
            }
        }
		/**/
		public enum EExchangeFeeTypes
        {
            RegisterNewWalletFix,
            Order1LotFix,
            Trade1LotFix,
			Trade1LotPercent
        }
        /**/
        public enum EExchangeChartTimeframe : sbyte
        {
            M1,
            M5,
            M15,
            M30,
            H1,
            H4,
            D1,
            W1,
            Mn1
        }

        public class ExchangeChartCandleClientInfo
        {
            public virtual string SecurityCode { get; set; }
            public virtual DateTime StartTime { get; set; }
            public virtual EExchangeChartTimeframe Timeframe { get; set; }
            public virtual decimal OpenValue { get; set; }
            public virtual decimal CloseValue { get; set; }
            public virtual decimal HighValue { get; set; }
            public virtual decimal LowValue { get; set; }
            public virtual long TotalVolumeLots { get; set; }
            public virtual long LastChangedNum { get; set; }
        }

        public class GetLastNCandlesRequest : ICheckable
        {
            public EExchangeChartTimeframe Timeframe;
            public string SecCode;
            public int N;
            public void CheckMe()
            {
                Assert.True(Enum.IsDefined(typeof(EExchangeChartTimeframe),Timeframe));
                Assert.False(string.IsNullOrWhiteSpace(SecCode));
                Assert.InRange(SecCode.Length,1,ExchangeServerConstants.MaxSecCodeLength);
                Assert.InRange(N,1,200);
            }
        }

        public class GetChangedChartCandlesRequest : ICheckable
        {
            public int MaxCount = 100;
            // SecCode, Timeframe, LastChangedNum
            public List<MutableTuple<string, EExchangeChartTimeframe, long>> KnownCandleInfos
                = new List<MutableTuple<string, EExchangeChartTimeframe, long>>();

            public void CheckMe()
            {
                Assert.InRange(MaxCount,1,100);
                Assert.NotNull(KnownCandleInfos);
                Assert.InRange(KnownCandleInfos.Count,1,100);
                foreach (var candleInfo in KnownCandleInfos)
                {
                    Assert.NotNull(candleInfo);
                    Assert.InRange(candleInfo.Item1.Length, 1, ExchangeServerConstants.MaxSecCodeLength);
                    Assert.True(Enum.IsDefined(typeof(EExchangeChartTimeframe),candleInfo.Item2));
                }
                var toCompare = KnownCandleInfos.Select(_ => new { _.Item1, _.Item2 }).ToList();
                Assert.Equal(
                    toCompare,
                    toCompare.Distinct(
                        MiscFuncs.SerializeComparer.CreateInstanceFromEnumerable(
                            toCompare
                        )
                    )
                );
            }
        }
        /**/
        public interface IFromClientToExchange
            : IGetAuthInfo, IAuthMe
        {
            // ISignedFromClientToExchange
            [BalanceCostExecutionFromType(
                ExchangeServerBalanceCosts.EveryOperationDefaultCost,
                typeof(ISignedFromClientToExchange)
            )]
            Task<byte[]> ProcessSignedRequestPacket(
                SignedData<OneSideSignedRequestPacket> signedRequestPacket
            );
            // SignedFromClientToExchange fees
            [BalanceCost(ExchangeServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<List<RpcMethodInfoWithFees>> GetSignedMethodInfos();

            //IAuthenticatedFromClientToExchange
            [BalanceCostExecutionFromType(
                ExchangeServerBalanceCosts.EveryOperationDefaultCost,
                typeof(IAuthenticatedFromClientToExchange)
            )]
            Task<byte[]> ProcessAuthenticatedPacket(
                byte[] packet, Guid userId
            );
            // AuthenticatedFromClientToExchange fees
            [BalanceCost(ExchangeServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<List<RpcMethodInfoWithFees>> GetAuthenticatedMethodInfos();

            /**/
            [BalanceCost(
                ExchangeServerBalanceCosts.EveryOperationDefaultCost,
                ExchangeServerBalanceCosts.RegistrationExchangeClientCost
            )]
            Task RegisterExchangeClientCert(
                SignedData<RegisterExchangeClientCertRequest> request
            );

            [BalanceCost(ExchangeServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<bool> IsExchangeClientCertRegistered(Guid exchangeClientId);

            [BalanceCost(ExchangeServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<Guid> GenExchangeClientGuidForRegistration();

            [BalanceCost(ExchangeServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<List<ExchangeCurrencyClientInfo>> GetCurrencyList();

            [BalanceCost(ExchangeServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<List<ExchangeSecurityClientInfo>> GetSecurityList();

            //Sorted by price asc
            [BalanceCost(ExchangeServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<List<MutableTuple<string,List<DomEntry>>>> GetDomList(
                GetDomListRequest request
			);

	        [BalanceCost(ExchangeServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
	        Task<List<ExchangeCurrencyPairSecurityClientInfo>> GetCurrencyPairSecurityList();

			[BalanceCost(ExchangeServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
	        Task<List<MutableTuple<EExchangeFeeTypes, decimal>>> GetFees();

            [BalanceCost(ExchangeServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<List<ExchangeChartCandleClientInfo>> GetLastNChartCandles(
                GetLastNCandlesRequest request
            );

            [BalanceCost(ExchangeServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<List<ExchangeChartCandleClientInfo>> GetChangedChartCandles(
                GetChangedChartCandlesRequest request
            );

            [BalanceCost(ExchangeServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<ExchangePaymentDetails> GetEmptyWithdrawPaymentDetails(
                string currencyCode
            );
        }

        public static class ExchangeServerBalanceCosts
        {
            public const double EveryOperationDefaultCost
                = CommonClientConstants.EveryOperationDefaultCost;
            public const double RegistrationExchangeClientCost
				= CommonClientConstants.RegistrationDefaultCost;
        }
    }
}
