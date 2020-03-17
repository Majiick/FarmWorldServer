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
            Console.WriteLine("Connecting to database.");
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

        public void AddToUserInventory(ItemSchema.ItemDBSchema item)
        {
            var queryRequest = new QueryRequest()
                .Statement("SELECT meta(`FarmWorld`).id, * FROM `FarmWorld` WHERE userName=$1 AND uniqueName=$2")
                .AddPositionalParameter(item.userName)
                .AddPositionalParameter(item.uniqueName);
            var result = _bucket.Query<dynamic>(queryRequest);
            if (!result.Success)
            {
                throw new Exception(String.Format("AddToUserInventory initial queryRequest failed: {0}", result.Status));
            }
            if (result.Rows.Count > 1)
            {
                throw new Exception(String.Format("AddToUserInventory initial query returned more than 1 row for uuesr {0} item {1}", item.userName, item.uniqueName));
            }

            if (result.Rows.Count == 0)  // TODO: Potential race here if 2 items are added at same time on an item that did not exist before.
            {
                var idResult = _bucket.Increment("UserItemInventoryCounter");
                if (!idResult.Success)
                {
                    Console.WriteLine("Failed to get next increment for UserItemInventoryCounter.");
                    return;
                }

                var document = new Document<dynamic>
                {
                    Id = "item" + idResult.Value.ToString(),
                    Content = item
                };
                var upsert = _bucket.Upsert(document);
                if (!upsert.Success)
                {
                    throw new Exception(String.Format("Upserting item failed for user {0} and item {1} and quantity {2}", item.userName, item.uniqueName, item.quantity));
                }
            }
            else
            {
                var row = result.Rows[0];
                string id = row.GetValue("id");

                var lockResult = _bucket.GetAndLock<dynamic>(id, 1); // TODO: Implement wait and retry.
                if (!lockResult.Success)
                {
                    if (lockResult.Status == Couchbase.IO.ResponseStatus.Locked)
                    {
                        Console.WriteLine(String.Format("Item with id {0} already DB locked for user {1}.", id, item.userName));
                    }
                    else if (lockResult.Status == Couchbase.IO.ResponseStatus.KeyNotFound)
                    {
                        throw new Exception(String.Format("Locking item failed for user {0} and item {1} and quantity {2}", item.userName, item.uniqueName, item.quantity));
                    }
                    else
                    {
                        throw new NotImplementedException(String.Format("Lock operation threw status {0} which is not handled on id {1}.", lockResult.Status, id));
                    }
                }

                Newtonsoft.Json.Linq.JObject obj = lockResult.Value;
                obj["quantity"] = item.quantity + obj.Value<int>("quantity");

                IOperationResult replaceResult = _bucket.Replace(id, obj, lockResult.Cas);  // This replaces the object and releases the DB lock.
                if (!replaceResult.Success)
                {
                    throw new Exception(String.Format("Replace on item id '{0}' did not work: {1}.", id, replaceResult.Status));
                }
            }
        }

        public List<ItemSchema.ItemDBSchema> GetUserInventory(string userName)
        {
            var queryRequest = new QueryRequest()
                .Statement("SELECT meta(`FarmWorld`).id, * FROM FarmWorld WHERE userName=$1 AND uniqueName IS NOT MISSING;")
                .AddPositionalParameter(userName)
                .ScanConsistency(ScanConsistency.RequestPlus);
            var result = _bucket.Query<dynamic>(queryRequest);
            if (!result.Success)
            {
                Console.WriteLine(String.Format("Getting items for user {0} failed with error {1}.", userName, result.Errors));
                return null;
            }

            List<ItemSchema.ItemDBSchema> ret = new List<ItemSchema.ItemDBSchema>(result.Rows.Count);
            foreach (var row in result.Rows)
            {
                Newtonsoft.Json.Linq.JObject itemJson = row.GetValue("FarmWorld");
                string id = row.GetValue("id");

                ItemSchema.ItemDBSchema item = new ItemSchema.ItemDBSchema();
                item.id = id;
                item.quantity = itemJson.Value<int>("quantity");
                item.uniqueName = itemJson.Value<string>("uniqueName");
                item.userName = itemJson.Value<string>("userName");
                ret.Add(item);
            }

            return ret;
        }

        public List<ObjectSchema.IObject> GetAllLockedObjects()
        {
            Console.WriteLine("Getting all locked objects.");
            var queryRequest = new QueryRequest()
                .Statement("SELECT meta(`FarmWorld`).id, * FROM `FarmWorld` WHERE locked=true");
            var result = _bucket.Query<dynamic>(queryRequest);
            if (!result.Success)
            {
                throw new Exception(String.Format("Getting all locked objects failed: {0}", result.Status));
            }

            List<ObjectSchema.IObject> ret = new List<ObjectSchema.IObject>(result.Rows.Count);
            foreach (var row in result.Rows)
            {
                Newtonsoft.Json.Linq.JObject IObjectJson = row.GetValue("FarmWorld");
                string id = row.GetValue("id");

                ret.Add(ObjectSchema.ObjectTypes.ConstructObject(id, IObjectJson));
            }

            return ret;
        }

        public void UnlockAllObjects()
        {
            Console.WriteLine("Unlocking all objects");
            var queryRequest = new QueryRequest()
                .Statement("UPDATE `FarmWorld` SET locked=false WHERE locked=true");
            var result = _bucket.Query<dynamic>(queryRequest);
            if (!result.Success)
            {
                throw new Exception(String.Format("Unlocking all objects failed: {0}", result.Status));
            }
        }

        public bool Lock(string id, string lockedBy)
        {
            var lockResult = _bucket.GetAndLock<dynamic>(id, 1);
            if (!lockResult.Success)
            {
                if (lockResult.Status == Couchbase.IO.ResponseStatus.Locked)
                {
                    Console.WriteLine(String.Format("Object with id {0} already DB locked.", lockResult.Status.ToString()));
                } else if (lockResult.Status == Couchbase.IO.ResponseStatus.KeyNotFound)
                {
                    Console.WriteLine(String.Format("Object with id {0} already locked.", lockResult.Status.ToString()));
                } else
                {
                    throw new NotImplementedException(String.Format("Lock operation threw status {0} which is not handled on id {1}.", lockResult.Status, id));
                }

                return false;
            }
            Newtonsoft.Json.Linq.JObject obj = lockResult.Value;
            if (obj.Value<bool>("locked") == true)
            {
                Console.WriteLine(String.Format("Object with id {0} is already locked by {1}.", id, obj.Value<string>("lockedBy")));
                IOperationResult unlockResult = _bucket.Unlock(id, lockResult.Cas);
                if (!unlockResult.Success)
                {
                    throw new Exception("unlockResult here should always be a success.");
                }
                return false;
            }

            obj["locked"] = true;
            obj["lockedBy"] = lockedBy;
            obj["lockStartTime"] = DateTimeOffset.Now.ToUnixTimeSeconds();
            IOperationResult replaceResult = _bucket.Replace(id, obj, lockResult.Cas);  // This replaces the object and releases the DB lock.
            if (!replaceResult.Success)
            {
                throw new Exception(String.Format("Lock on object id '{0}' did not work: {1}.", id, replaceResult.Status));
            }

            return true;
        }

        public bool Unlock(string id)
        {
            var lockResult = _bucket.GetAndLock<dynamic>(id, 1);
            if (!lockResult.Success)
            {
                if (lockResult.Status == Couchbase.IO.ResponseStatus.Locked)
                {
                    Console.WriteLine(String.Format("Object with id {0} already DB locked.", lockResult.Status.ToString()));
                }
                else if (lockResult.Status == Couchbase.IO.ResponseStatus.KeyNotFound)
                {
                    Console.WriteLine(String.Format("Object with id {0} already locked.", lockResult.Status.ToString()));
                }
                else
                {
                    throw new NotImplementedException(String.Format("Lock operation threw status {0} which is not handled on id {1}.", lockResult.Status, id));
                }

                return false;
            }
            Newtonsoft.Json.Linq.JObject obj = lockResult.Value;
            if (obj.Value<bool>("locked") == false)
            {
                Console.WriteLine(String.Format("Object with id {0} was not locked.", id, obj.Value<string>("lockedBy")));
                return false;
            }

            obj["locked"] = false;
            obj["lockedBy"] = "";
            obj["lockStartTime"] = 0;
            IOperationResult replaceResult = _bucket.Replace(id, obj, lockResult.Cas);  // This replaces the object and releases the DB lock.
            if (!replaceResult.Success)
            {
                throw new Exception(String.Format("Unlock on object id '{0}' did not work: {1}.", id, replaceResult.Status));
            }


            return true;
        }

        // Returns id of written object.
        public string Write(ObjectSchema.IObject obj)  // TODO: Make async.
        {
            var idResult = _bucket.Increment("InGameObjectCounter");
            if (!idResult.Success)
            {
                Console.WriteLine("Failed to get next increment for InGameObjectCounter.");
                return "";
            }

            string id = idResult.Value.ToString();

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
