using Platform.Sdk;
using Platform.Sdk.Models.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Route = Platform.Sdk.Route;

namespace Omukade.Cheyenne.Tests.BakedData
{
    internal class BakedStage : Stage
    {
        public bool BypassValidation { get; set; }

        public bool SupportsRouting { get; set; }

        public Route GlobalRoute { get; set; }

        public string name { get; set; }

        public Route RouteForRegion(string region)
        {
            throw new NotImplementedException();
        }

        public Route RouteForResponse(QueryRouteResponse routeResponse)
        {
            throw new NotImplementedException();
        }
    }
}
