using System.Collections.Generic;

namespace BtmI2p.GeneralClientInterfaces.LifeCycleConstants
{
#if BTM_LIFECYCLE_DEV
    public static class DevConstants
    {
        public static List<string> ProxyServerDestinations 
            => ReleaseConstants.ProxyServerDestinations;
    }
#endif
}
