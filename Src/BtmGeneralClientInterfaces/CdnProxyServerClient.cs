using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BtmI2p.ComputableTaskInterfaces.Client;
using BtmI2p.JsonRpcHelpers.Client;
using BtmI2p.JsonRpcSamI2pClient;
using BtmI2p.LightCertificates.Lib;
using BtmI2p.MiscUtils;
using BtmI2p.MyFileManager;

namespace BtmI2p.GeneralClientInterfaces
{
    namespace CdnProxyServer
    {
        /**/
        public class ProxyServerClientInfo
        {
            //public LightCertificate PublicCert;
            public List<I2PHostClientDestinations> DestinationRestictions;
            public List<string> I2PDestinations;
        }
        /***********************/
        public class ProxyNotEnoughFundsException : Exception
        {
            public const int RpcRethrowableExceptionErrorCode = 1000000;

            public static void ProcessRpcRethrowableException(
                RpcRethrowableException rpcExc
            )
            {
                if(rpcExc.ErrorData.ErrorCode == RpcRethrowableExceptionErrorCode)
                    throw new ProxyNotEnoughFundsException();
            }
        }
        /***********************/
        public class RpcMethodBalanceFeeInfo
        {
            public decimal BalanceCost; // balance > 0
            public decimal MaxEndServerExecutionFee;
        }
        public class RpcMethodInfoWithFees
        {
            public JsonRpcServerMethodInfo JsonRpcMethodInfo;
            public RpcMethodBalanceFeeInfo BalanceFeeInfo;
        }
        /***********************/
        public class RpcMethodTimeoutFeeInfo
        {
            public decimal TimeoutCostSeconds; // balance <= 0
            public decimal OperationTimeSeconds;
        }
        public class RpcMethodInfoWithFeesAndTimeouts 
        {
            public RpcMethodInfoWithFees MethodInfoWithFees;
            public RpcMethodTimeoutFeeInfo TimeoutFeeInfo;
        }
        /***********************/
        public class ProxyServerInfo
        {
            public List<MiningTaskInfo> MiningTaskInfos;
            public decimal MaxBalance;
            public List<RpcMethodInfoWithFeesAndTimeouts> MethodFees;
            public decimal TimeoutMaxSumSeconds;
            public long MinMiningTaskBalanceGain, MaxMiningTaskBalanceGain;
            public Guid WalletGuid = Guid.Empty; //to fill balance
            public string ClientVersionString 
                = CommonClientConstants.CurrentClientVersionString;
        }
        /***********************/
        public class ResultFeeInfo
        {
            public decimal ProxyFee;
            public decimal EndServerCheckFee;
            public decimal EndServerExecutionFee;
        }
        public class ProcessPacketResult
        {
            public ResultFeeInfo ResultFees;
            public byte[] BytesResult;
        }
        /***********************/
        public class MiningTaskInfo
        {
            public int TaskType;
        }
        /***********************/

        public class NewVersionArchiveIdentity
        {
            public byte[] NewVersionArchiveSha256Hash;
            public string NewVersionString;
            public int ClientAppPackageType = (int)EClientAppPackageType.Desktop;
        }
        /**/
        public class UpdateClientPackageResult
        {
            public string OldVersionString = null;
            public bool DiffsOnly = false;
            public FileManagerDataInfo PackageDataInfo;
            public SignedData<NewVersionArchiveIdentity> SignedIdentity;
        }
        /***********************/

        public class PingRequest
        {
            public string ClientVersion 
                = CommonClientConstants.CurrentClientVersionString;
        }

        public class PingResponse
        {
            public bool NewVersionAvailable = false;
            public DateTime NowTimeUtc = DateTime.UtcNow;
            public decimal Balance = 0.0m;
        }
        /***********************/
        public enum EPassTaskSolutionProxyErrCodes
        {
            TaskAlreadySolved,
            MaxBalanceExceed
        }

        public enum EGetUpdateClientPackageChunkErrCodes
        {
            PermisionNotGranted,
            ChunkNotAvailable
        }
        
        public enum EClientAppPackageType
        {
            Desktop
        }
        public class GetUpdateClientPackageInfoRequest : ICheckable
        {
            public string OldVersionString = string.Empty;
            public bool DiffsOnly = false;
            public byte[] OldVersionArchiveDataHash = new byte[0];
            public int ClientAppPackageType = (int) EClientAppPackageType.Desktop;
            public void CheckMe()
            {
                if(string.IsNullOrWhiteSpace(OldVersionString))
                    throw new ArgumentNullException(
                        this.MyNameOfProperty(e => e.OldVersionString));
                Version t1;
                if(!Version.TryParse(OldVersionString, out t1))
                    throw new ArgumentOutOfRangeException(
                        this.MyNameOfProperty(e => e.OldVersionString));
                if(OldVersionArchiveDataHash == null)
                    throw new ArgumentNullException(
                        this.MyNameOfProperty(e => e.OldVersionArchiveDataHash));
                if(OldVersionArchiveDataHash.Length != 32)
                    throw new ArgumentOutOfRangeException(
                        this.MyNameOfProperty(e => e.OldVersionArchiveDataHash));
                if(ClientAppPackageType != (int) EClientAppPackageType.Desktop)
                    throw new ArgumentOutOfRangeException(
                        this.MyNameOfProperty(e => e.ClientAppPackageType));
            }
        }

        public static class ProxyServerBalanceCosts
        {
            public const double EveryOperationDefaultCost
                = CommonClientConstants.EveryOperationDefaultCost;
        }

        public enum EOldVersionHandling
        {
            DelayAndThrowTimeout
        }
        public class VersionCompatibilityRequest : ICheckable
        {
            public string CurrentClientVersion 
                = CommonClientConstants.CurrentClientVersionString;
            public int OldVersionHandling 
                = (int)EOldVersionHandling.DelayAndThrowTimeout;

            public void CheckMe()
            {
                var tryParseVersion = new Version(CurrentClientVersion);
                var handling = (EOldVersionHandling) OldVersionHandling;
            }
        }

        public interface IFromClientToCdnProxy
        {
            [JsonSamOperationContract(false, false)]
            [BalanceCost(ProxyServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            [TimeoutCost(1.0, 30.0)]
            Task<PingResponse> Ping(PingRequest infoFromClient);

            [JsonSamOperationContract(true, true)]
            [BalanceCost(ProxyServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            [TimeoutCost(1.0, 30.0)]
            Task<ComputableTaskSerializedDescription> GenNewTaskDescryption(
                int taskType, 
                long wishfulIncome
            );

            [JsonSamOperationContract(true, true)]
            [BalanceCost(ProxyServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            [TimeoutCost(1.0, 30.0)]
            Task<long> PassTaskSolution(
                int taskType, 
                ComputableTaskSerializedSolution taskSolution
            );

            [JsonSamOperationContract(true, true)]
            [BalanceCost(ProxyServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            [TimeoutCost(1.0, 30.0)]
            Task<decimal> GetCurrentBalance();

            [Obsolete("Use ProcessPacketCheckVersion instead",true)]
            [JsonSamOperationContract(true, true)]
            [BalanceCost(ProxyServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<ProcessPacketResult> ProcessPacket(
                Guid endReceiverId,
                byte[] packetData
            );

            //Timeout if new version
            [JsonSamOperationContract(true, true)]
            [BalanceCost(ProxyServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<ProcessPacketResult> ProcessPacketCheckVersion(
                Guid endReceiverId,
                byte[] packetData,
                VersionCompatibilityRequest request
            );

            [JsonSamOperationContract(true, true)]
            [BalanceCost(ProxyServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            [TimeoutCost(1.0f, 30.0)]
            Task<ProxyServerInfo> GetProxyServerInfo();

            [Obsolete("Use GetEndReceiverFeesCheckVersion instead", true)]
            [JsonSamOperationContract(true, true)]
            [BalanceCost(ProxyServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<List<RpcMethodInfoWithFees>> GetEndReceiverFees(Guid endReceiverId);

            //Timeout if new version
            [JsonSamOperationContract(true, true)]
            [BalanceCost(ProxyServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<List<RpcMethodInfoWithFees>> GetEndReceiverFeesCheckVersion(
                Guid endReceiverId,
                VersionCompatibilityRequest request
            );
            
            [Obsolete("Use GetLookupEndReceiverIdCheckVersion instead", true)]
            [JsonSamOperationContract(true, true)]
            [BalanceCost(ProxyServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<ServerAddressForClientInfo> GetLookupEndReceiverId();

            //Timeout if new version
            [JsonSamOperationContract(true, true)]
            [BalanceCost(ProxyServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<ServerAddressForClientInfo> GetLookupEndReceiverIdCheckVersion(
                VersionCompatibilityRequest request);
            
            [JsonSamOperationContract(true, true)]
            [BalanceCost(ProxyServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<UpdateClientPackageResult> GetUpdateClientPackageInfo(
                GetUpdateClientPackageInfoRequest request
            );

            [JsonSamOperationContract(true, true)]
            [BalanceCost(ProxyServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<FileManagerDataChunk> GetUpdateClientPackageChunk(
                Guid fileGuid,
                int chunkNum
            );
        }
        public class I2PHostClientDestinations
        {
            public byte[] ClientDestinationMask;
            public byte[] ClientDestinationMaskEqual;
        }
        /**/

        [AttributeUsage(AttributeTargets.Method)]
        public class BalanceCostExecutionFromTypeAttribute : Attribute
        {
            public BalanceCostExecutionFromTypeAttribute(
                double balanceCost,
                Type interfaceExecutionCostType
            )
            {
                BalanceCost = (decimal)balanceCost;
                InterfaceCostType = interfaceExecutionCostType;
            }
            public decimal BalanceCost { get; set; }
            public Type InterfaceCostType { get; set; }
        }

        [AttributeUsage(AttributeTargets.Method)]
        public class BalanceCostAttribute : System.Attribute
        {
            public BalanceCostAttribute(
                double balanceCost,
                double maxEndServerExecutionFee
            )
            {
                BalanceFeeInfo = new RpcMethodBalanceFeeInfo()
                {
                    BalanceCost = (decimal)balanceCost,
                    MaxEndServerExecutionFee = (decimal)maxEndServerExecutionFee
                };
            }
            public RpcMethodBalanceFeeInfo BalanceFeeInfo { get; set; }
        }

        [AttributeUsage(AttributeTargets.Method)]
        public class TimeoutCostAttribute : System.Attribute
        {
            public TimeoutCostAttribute(
                double timeoutCostSeconds,
                double operationTime
            )
            {
                TimeoutFeeInfo = new RpcMethodTimeoutFeeInfo()
                {
                    TimeoutCostSeconds = (decimal)timeoutCostSeconds,
                    OperationTimeSeconds = (decimal)operationTime
                };
            }
            public RpcMethodTimeoutFeeInfo TimeoutFeeInfo { get; set; }
        }
    }
}
