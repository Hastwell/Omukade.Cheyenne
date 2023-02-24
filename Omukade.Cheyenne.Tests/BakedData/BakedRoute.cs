using Platform.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Route = Platform.Sdk.Route;

namespace Omukade.Cheyenne.Tests.BakedData
{
    internal class BakedRoute : Route
    {
        public Uri WebsocketUrl { get; set; }

        public string Region { get; set; }

        public string ServiceGroup { get; set; }


        public Uri ApiUrlBase { get; set; }
        public Uri ApiUrl(string api) => new Uri(ApiUrlBase, api);

        public void SelectWebsocketServicePort(string port)
        {
            
        }

        public void SetServiceGroup(string serviceGroup) => this.ServiceGroup = serviceGroup;
    }
}
