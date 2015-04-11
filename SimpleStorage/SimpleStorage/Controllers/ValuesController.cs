using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using Client;
using Domain;
using SimpleStorage.Infrastructure;

namespace SimpleStorage.Controllers
{
    public static class Extensions
    {
        public static T Max<T, T1>(this IEnumerable<T> values, Func<T, T1> compareByFunc)
            where T1 : IComparable<T1>
        {
            var enumerable = values as IList<T> ?? values.ToList();
            if (enumerable.Count == 0)
                return default(T);
            var result = enumerable[0];
            var mappedResult = compareByFunc(result);
            foreach (var value in enumerable)
            {
                if (mappedResult.CompareTo(compareByFunc(value)) >= 0) continue;
                result = value;
                mappedResult = compareByFunc(result);
            }

            return result;
        }
    }
    public class ValuesController : ApiController
    {
        private readonly IConfiguration configuration;
        private readonly IStateRepository stateRepository;
        private readonly IStorage storage;
        private readonly int quorum;

        public ValuesController(IStorage storage, IStateRepository stateRepository, IConfiguration configuration)
        {
            this.storage = storage;
            this.stateRepository = stateRepository;
            this.configuration = configuration;
            quorum = (configuration.OtherReplicasPorts.Length + 1) / 2 + 1;
        }

        private void CheckState()
        {
            if (stateRepository.GetState() != State.Started)
                throw new HttpResponseException(HttpStatusCode.InternalServerError);
        }

        // GET api/values/5 
        public Value Get(string id)
        {
            Console.WriteLine("Get public");
            CheckState();
            var results = new List<Value> {storage.Get(id)};

            foreach (var internalClient in configuration.OtherReplicasPorts.Select(anotherShardPort => new InternalClient(String.Format("http://localhost:{0}/", anotherShardPort))))
            {
                try
                {
                    results.Add(internalClient.Get(id));
                }
                catch (HttpResponseException e)
                {
                    if (e.Response.StatusCode == HttpStatusCode.NotFound)
                        results.Add(null);
                }
                catch (Exception)
                {
                    continue;
                }
                if (results.Count == quorum)
                    break;
            }

            if (results.Count != quorum)
                throw new Exception(String.Format("Cant' read from {0} shards", quorum));

            var result = results.Where(x => x != null).Max(x => x.Revision);
            if (result == null)
                throw new HttpResponseException(HttpStatusCode.NotFound);
            return result;
        }

        // PUT api/values/5
        public void Put(string id, [FromBody] Value value)
        {
            CheckState();
            var count = 1;
            storage.Set(id, value);
            foreach (var internalClient in configuration.OtherReplicasPorts.Select(anotherShardPort => new InternalClient(String.Format("http://localhost:{0}/", anotherShardPort))))
            {
                try
                {
                    internalClient.Put(id, value);
                    count += 1;
                }
                catch (Exception)
                {
                    continue;
                }
                if (count == quorum)
                    return;
            }

            if (count != quorum)
                throw new Exception(String.Format("Can't write to {0} shards", quorum));
        }
    }
}