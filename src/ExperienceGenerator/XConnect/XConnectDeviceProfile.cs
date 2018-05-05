using Sitecore.XConnect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xConnectDataGenerator.XConnect
{
    public class XConnectDeviceProfile
    {
        public static DeviceProfile CreateDeviceProfile(Contact contact, Interaction interaction)
        {
            DeviceProfile deviceProfile = new DeviceProfile(Guid.NewGuid());
            deviceProfile.LastKnownContact = contact;
            interaction.DeviceProfile = deviceProfile;

            return deviceProfile;
        }
    }
}
