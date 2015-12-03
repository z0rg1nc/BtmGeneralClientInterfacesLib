using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BtmI2p.ComputableTaskInterfaces.Client;
using BtmI2p.GeneralClientInterfaces.CdnProxyServer;
using BtmI2p.LightCertificates.Lib;
using BtmI2p.MiscUtils;
using BtmI2p.OneSideSignedJsonRpc;

namespace BtmI2p.GeneralClientInterfaces
{
    namespace MiningServer
    {
        public enum EGeneralMiningErrCodes
        {
            WrongArgs = 100,
            MaxBalanceExceeded,
            WrongTaskType
        }
        /**/
        public enum EPassTaskSolutionErrCodes
        {
            TaskAlreadySolved,
            TaskNotExist,
            TaskExpired,
            WrongSolution
        }
        /**/

        public enum EGenNewTaskDescriptionErrCodes
        {
            
        }
        public interface IAuthenticatedFromClientToMining
        {
            [BalanceCost(MiningServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<ComputableTaskSerializedDescription> GenNewTaskDescription(
                int taskType,
                long wishfulIncome
            );
            [BalanceCost(MiningServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<long> PassTaskSolution(
                int taskType,
                ComputableTaskSerializedSolution taskSolution
            );
            [BalanceCost(MiningServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<long> GetCurrentBalance();
        }
        /**/
        public enum ETransferFundsToWalletErrCodes
        {
            NotEnoughFunds,
            WrongWalletGuidType,
            WalletToNotExist,
            AlreadyProcessed
        }
        public interface ISignedFromClientToMining
        {
            [BalanceCost(MiningServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task TransferFundsToWallet(
                Guid walletGuid, 
                long amount,
                Guid miningTransferGuid
            );
        }
        /**/
        public enum ERegisterMiningClientCertErrCodes
        {
            WrongRequestTime,
            AlreadyRegisteredOther,
            AlreadyRegistered,
            WrongCert,
            WrongCertGuid
        }
        public class RegisterMiningClientCertRequest
        {
            public LightCertificate PublicMiningClientCert;
            public DateTime SentTime;
            public Guid RequestGuid = Guid.NewGuid();
        }
        public class MiningServerInfoForClient : ICheckable
        {
            public List<MiningTaskInfo> MiningTaskInfos;
            public long MaxBalance;
            public long MinMiningTaskBalanceGain, MaxMiningTaskBalanceGain;
            public void CheckMe()
            {
                if(MiningTaskInfos == null)
                    throw new ArgumentNullException();
                if(
                    MaxBalance <= 0
                    || MinMiningTaskBalanceGain <= 0
                    || MaxMiningTaskBalanceGain <= 0
                    )
                    throw new Exception("<0");
                if(MinMiningTaskBalanceGain > MaxMiningTaskBalanceGain)
                    throw new Exception(
                        "MinTaskBalance > MaxTaskBalance"
                    );
                if(MiningTaskInfos.Count == 0)
                    throw new Exception("MiningTaskInfos.Count == 0");
            }
        }
        public interface IFromClientToMining
            : IGetAuthInfo, IAuthMe
        {
            // ISignedFromClientToMining
            [BalanceCostExecutionFromType(
                MiningServerBalanceCosts.EveryOperationDefaultCost,
                typeof(ISignedFromClientToMining)
            )]
            Task<byte[]> ProcessSignedRequestPacket(
                SignedData<OneSideSignedRequestPacket> signedRequestPacket
            );
            // SignedFromClientToMining fees
            [BalanceCost(MiningServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<List<RpcMethodInfoWithFees>> GetSignedMethodInfos();

            // IAuthenticatedFromClientToMining
            [BalanceCostExecutionFromType(
                MiningServerBalanceCosts.EveryOperationDefaultCost,
                typeof(IAuthenticatedFromClientToMining)
            )]
            Task<byte[]> ProcessAuthenticatedPacket(
                byte[] packet, Guid userId
            );
            // AuthenticatedFromClientToMining fees
            [BalanceCost(MiningServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<List<RpcMethodInfoWithFees>> GetAuthenticatedMethodInfos();

            [BalanceCost(MiningServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<MiningServerInfoForClient> GetMiningServerInfo();
            /**/
            [BalanceCost(
                MiningServerBalanceCosts.EveryOperationDefaultCost, 
                MiningServerBalanceCosts.RegistrationMiningClientCost
            )]
            Task RegisterMiningClientCert(
                SignedData<RegisterMiningClientCertRequest> request
            );

            [BalanceCost(MiningServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<bool> IsMiningClientCertRegistered(Guid miningClientId);

            [BalanceCost(MiningServerBalanceCosts.EveryOperationDefaultCost, 0.0)]
            Task<Guid> GenMiningClientGuidForRegistration();
        }
        public static class MiningServerBalanceCosts
        {
            public const double EveryOperationDefaultCost 
                = CommonClientConstants.EveryOperationDefaultCost;
            public const double RegistrationMiningClientCost
				= CommonClientConstants.RegistrationDefaultCost;
        }
    }
}
