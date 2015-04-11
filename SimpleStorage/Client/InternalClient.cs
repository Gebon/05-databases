using System;
using System.Net.Http;
using Domain;

namespace Client
{
    public class InternalClient : ISimpleStorageClient
    {
        private readonly string endpoint;

        public InternalClient(string endpoint)
        {
            this.endpoint = endpoint;
        }

        public void Put(string id, Value value)
        {
            Console.WriteLine("In internal client");
            var putUri = endpoint + "api/admin/internalPut/" + id;
            using (var client = new HttpClient())
            using (var response = client.PutAsJsonAsync(putUri, value).Result)
                response.EnsureSuccessStatusCode();
        }

        public Value Get(string id)
        {
            var requestUri = endpoint + "api/admin/internalGet/" + id;
            using (var client = new HttpClient())
            using (var response = client.GetAsync(requestUri).Result)
            {
                response.EnsureSuccessStatusCode();
                return response.Content.ReadAsAsync<Value>().Result;
            }
        }
    }
}