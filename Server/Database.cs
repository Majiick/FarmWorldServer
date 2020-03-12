using Couchbase;
using Couchbase.Authentication;
using Couchbase.Configuration.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server
{
    class Database
    {
        Couchbase.Core.IBucket _bucket;
        public Database()
        {
            var cluster = new Cluster(new ClientConfiguration
            {
                Servers = new List<Uri> { new Uri("http://139.162.192.17") }
            });

            var authenticator = new PasswordAuthenticator("Ecoste", "tidux2284da06");
            cluster.Authenticate(authenticator);
            _bucket = cluster.OpenBucket("FarmWorld");


            var document = new Document<dynamic>
            {
                Id = "Hello",
                Content = new
                {
                    name = "Couchbase"
                }
            };

            var upsert = _bucket.Upsert(document);
        }

        public void Write(string baseId, string json)
        {
            var idResult = _bucket.Increment(baseId);
            if (!idResult.Success)
            {
                Console.WriteLine(String.Format("Failed to get next increment for baseId {0}.", baseId));
                return;
            }
            string id = baseId + idResult.Value.ToString();

            var document = new Document<dynamic>
            {
                Id = id,
                Content = json
            };
            var upsert = _bucket.Upsert(document);
            if (!upsert.Success)
            {
                Console.WriteLine(String.Format("Failed to write json: {0}", json));
            }
        }

        public void Read(string id)
        {
            var get = _bucket.GetDocument<dynamic>(id);
            var document = get.Document;

            var msg = string.Format("{0}: {1}", document.Id, document.Content);
            Console.WriteLine(msg);
        }
    }
}
