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

        // Returns id of written object.
        public string Write(string baseId, ObjectSchema.IObject obj)  // TODO: Make async.
        {
            var idResult = _bucket.Increment(baseId);
            if (!idResult.Success)
            {
                Console.WriteLine(String.Format("Failed to get next increment for baseId {0}.", baseId));
                return "";
            }

            string id = baseId + idResult.Value.ToString();

            var document = new Document<dynamic>
            {
                Id = id,
                Content = obj
            };
            var upsert = _bucket.Upsert(document);
            if (!upsert.Success)
            {
                Console.WriteLine(String.Format("Failed to write object: {0}", obj.ToString()));
                return "";
            }

            return id;
        }

        public T Read<T>(string id) where T : ObjectSchema.IFromJson<T>, new()
        {
            var get = _bucket.GetDocument<dynamic>(id);
            if (!get.Success)
            {
                Console.Write(String.Format("Failed to retrieve object: {0}", id));
                return default(T);  // TODO: This should raise an Exception
            }
           
            var document = get.Document;
            T obj = new T();
            obj.FromJson(document.Content, ref obj);
            return obj;
        }
    }
}
