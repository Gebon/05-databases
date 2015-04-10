using System;
using System.Web.Http;

namespace Coordinator.Controllers
{
    public class ShardMappingController : ApiController
    {
        private int shardsCount;
        public ShardMappingController(IConfiguration configuration)
        {
            shardsCount = configuration.ShardCount;
        }

        public int Get(string id)
        {
            return Math.Abs(id.GetHashCode())%shardsCount;
        }
    }
}