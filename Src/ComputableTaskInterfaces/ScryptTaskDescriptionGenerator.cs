using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BtmI2p.MiscUtils;

namespace BtmI2p.ComputableTaskInterfaces.Client
{
    public static class ScryptTaskDescriptionGenerator
    {
        private class TaskParams
        {
            public int ByteCount;
            public byte[] SolutionEqual;
            public byte[] SolutionMask;
        }
        private static TaskParams GetTaskParamsFromBitCount(int bitCount)
        {
            int byteCount = (int)Math.Ceiling((decimal)bitCount / 8);
            if (byteCount < 1)
                throw new ArgumentOutOfRangeException("byteCount < 1");
            var solutionEqual = new byte[byteCount];
            MiscFuncs.GetRandomBytes(solutionEqual);
            var solutionMask = Enumerable.Range(0, byteCount).Select(x => (byte)0).ToArray();
            for (int i = 0; i < bitCount; i++)
            {
                int byteIndex = i / 8;
                solutionMask[byteIndex] |= (byte)(1 << 7 - ((i % 8)));
            }
            return new TaskParams()
            {
                ByteCount = byteCount,
                SolutionEqual = solutionEqual,
                SolutionMask = solutionMask
            };
        }
        public static long GetBalanceIncrease(decimal oneBitIncome, int bitCount)
        {
            return (long)Math.Ceiling((1 << bitCount) * oneBitIncome);
        }
        public static ComputableTaskDescription<ScryptTaskDescription>
            GetTaskDescriptionParams(
                long wishfulIncome,
                DateTime validUntil,
                decimal oneBitIncome,
                ScryptTaskDescription sourceTaskDescription
            )
        {
            if (wishfulIncome < oneBitIncome)
                throw new ArgumentOutOfRangeException(
                    MyNameof.GetLocalVarName(() => wishfulIncome)
                );
            int bitCount
                = (int)Math.Floor(
                    Math.Log(
                        (double)(wishfulIncome
                            / oneBitIncome),
                        2.0f
                    )
                );
            if (bitCount < 1)
                throw new ArgumentOutOfRangeException(
                    MyNameof.GetLocalVarName(() => bitCount)
                );
            long balanceIncrease = GetBalanceIncrease(oneBitIncome, bitCount);
            if (
                balanceIncrease < oneBitIncome
            )
                throw new ArgumentOutOfRangeException(
                    MyNameof.GetLocalVarName(() => balanceIncrease)
                );
            /////////////////////////////
            var result = new ComputableTaskDescription<ScryptTaskDescription>();
            result.CommonInfo.TaskType = (int)ETaskTypes.Scrypt;
            result.CommonInfo.BalanceGain = balanceIncrease;
            result.CommonInfo.TaskGuid = Guid.NewGuid();
            result.CommonInfo.ValidUntil = validUntil;
            /**/
            result.TaskDescription = MiscFuncs.GetDeepCopy(sourceTaskDescription);
            var resultTaskParams = GetTaskParamsFromBitCount(bitCount);
            result.TaskDescription.DkLen = resultTaskParams.ByteCount;
            result.TaskDescription.SolutionEqual = resultTaskParams.SolutionEqual;
            result.TaskDescription.SolutionMask = resultTaskParams.SolutionMask;
            MiscFuncs.GetRandomBytes(result.TaskDescription.Salt);
            /**/
            return result;
        }
    }
}
