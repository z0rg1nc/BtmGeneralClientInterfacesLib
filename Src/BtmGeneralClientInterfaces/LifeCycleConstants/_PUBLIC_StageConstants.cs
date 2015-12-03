using System.Collections.Generic;

namespace BtmI2p.GeneralClientInterfaces.LifeCycleConstants
{
#if BTM_LIFECYCLE_STAGE
    public static class StageConstants
    {
        public static List<string> ProxyServerDestinations 
            => ReleaseConstants.ProxyServerDestinations;
    }
#endif
}
