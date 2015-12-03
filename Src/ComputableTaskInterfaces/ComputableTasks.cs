using System;

namespace BtmI2p.ComputableTaskInterfaces.Client
{
    public enum ETaskTypes
    {
        Scrypt
    }
    public class ComputableTaskCommonInfo
    {
        public Guid TaskGuid = Guid.NewGuid();
        public DateTime ValidUntil = DateTime.MaxValue;
        public int TaskType = 0;
        public long BalanceGain = 0;
    }

    public class ComputableTaskDescription<T1>
    {
        public ComputableTaskCommonInfo CommonInfo 
            = new ComputableTaskCommonInfo();
        public T1 TaskDescription;
    }

    public class ComputableTaskSerializedDescription
    {
        public ComputableTaskCommonInfo CommonInfo 
            = new ComputableTaskCommonInfo();
        public string TaskDescriptionSerialized;
    }
    public class ScryptTaskDescription
    {
        public int PassSaltSize;
        public int N, R, P, DkLen;
        public byte[] Salt;
        public byte[] SolutionMask;
        public byte[] SolutionEqual;
    }

    public class ComputableTaskSolution<T1>
    {
        public ComputableTaskCommonInfo CommonInfo 
            = new ComputableTaskCommonInfo();
        public T1 TaskSolution;
    }

    public class ComputableTaskSerializedSolution
    {
        public ComputableTaskCommonInfo CommonInfo
            = new ComputableTaskCommonInfo();
        public string TaskSolutionSerialized;
    }
    public class ScryptTaskSolution
    {
        public byte[] SolutionPass;
    }
}
