using System.Net;

namespace SimpleStorage
{
    public interface IConfiguration
    {
        int CurrentNodePort { get; }
        int[] OtherShardsPorts { get; }
        int[] OtherReplicasPorts { get; }
        bool IsMaster { get; }
        IPEndPoint MasterEndpoint { get; }
    }
}