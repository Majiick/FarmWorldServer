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
        public Database()
        {
            var cluster = new Cluster(new ClientConfiguration
            {
                Servers = new List<Uri> { new Uri("http://139.162.192.17") }
            });

            var authenticator = new PasswordAuthenticator("Ecoste", "tidux2284da06");
            cluster.Authenticate(authenticator);
            var bucket = cluster.OpenBucket("FarmWorld");
            var document = new Document<dynamic>
            {
                Id = "Hello",
                Content = new
                {
                    name = "Couchbase"
                }
            };

            var upsert = bucket.Upsert(document);
            if (upsert.Success)
            {
                var get = bucket.GetDocument<dynamic>(document.Id);
                document = get.Document;
                var msg = string.Format("{0} {1}!", document.Id, document.Content.name);
                Console.WriteLine(msg);
            }
        }
    }
}
