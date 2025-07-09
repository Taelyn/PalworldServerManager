using Palworld.RESTSharp;

namespace PalworldServerManager
{
    internal class RestAPI
    {
        internal static PalworldRESTSharpClient CreatePalworldClient()
        {
            return new PalworldRESTSharpClient("http://localhost:8212", Properties.Settings.Default.AdminPassword);
        }
    }
}
