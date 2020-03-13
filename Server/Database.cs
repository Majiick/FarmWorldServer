using Couchbase;
using Couchbase.Authentication;
using Couchbase.Configuration.Client;
using Couchbase.N1QL;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server
{
    class Database
    {
        public readonly string ServerURI = "http://139.162.192.17";
        Couchbase.Core.IBucket _bucket;

        public Database()
        {
            var cluster = new Cluster(new ClientConfiguration
            {
                Servers = new List<Uri> { new Uri(ServerURI) }
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
                Console.WriteLine(String.Format("Failed to write object: {0}", obj.ToString()));  // TODO: Throw exception.
                return "";
            }

            return id;
        }

        public T Read<T>(string id) where T : ObjectSchema.IFromJson<T>, ObjectSchema.IObject, new()
        {
            var get = _bucket.GetDocument<dynamic>(id);
            if (!get.Success)
            {
                throw new KeyNotFoundException(String.Format("Failed to retrieve document: {0}", id));
            }
           
            var document = get.Document;
            T obj = new T();
            obj.FromJson(document.Content, ref obj);
            obj.id = document.Id;
            return obj;
        }

        /* ReadAllObjects returns all of the objects of a particular type.
         * 
         * Example of one row of data returned for "SELECT  meta(`FarmWorld`).id, * FROM `FarmWorld` WHERE type = 'MINEABLE'"
         *  {
              "FarmWorld": {
                "id": "",
                "type": "MINEABLE",
                "mineableType": "ROCK",
                "size": "MEDIUM",
                "x": 38.02586,
                "y": -2.3841858E-07,
                "z": 16.635824,
                "rot_x": 0.0,
                "rot_y": 0.0,
                "rot_z": 0.0,
                "rot_w": 0.0
              },
              "id": "MINEABLE1"
            }
         */
        public List<T> ReadAllObjects<T>(ObjectSchema.ObjectTypes.IObjectType type) where T : ObjectSchema.IObject, ObjectSchema.IFromJson<T>, new() // TODO: Make Async
        {
            var queryRequest = new QueryRequest()
                .Statement("SELECT  meta(`FarmWorld`).id, * FROM `FarmWorld` WHERE type = $1")
                .AddPositionalParameter(type.Value);
            var result = _bucket.Query<dynamic>(queryRequest);
            if (!result.Success)
            {
                Console.WriteLine(String.Format("Getting all objects of type {0} failed with error {1}.", type.Value, result.Errors));
                return null;
            }

            List<T> ret = new List<T>(result.Rows.Count);
            foreach (var row in result.Rows)
            {
                Newtonsoft.Json.Linq.JObject IObject = row.GetValue("FarmWorld");
                string id = row.GetValue("id");

                T obj = new T();
                obj.FromJson(IObject, ref obj);
                obj.id = id;
                ret.Add(obj);
            }

            return ret;
        }
    }
}
