using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using System.Text;
using Serilog;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.DynamoDBv2.DocumentModel;

using Amazon.Runtime;

namespace OktaGenIdentifiers
{

    public class DynamoHelper
    {

        public AmazonDynamoDBClient client;
        public Table table;

        public DynamoHelper()
        {
            client = new AmazonDynamoDBClient(RegionEndpoint.USEast1);
            table = Table.LoadTable(client, Secrets.get_secret.DynamoTable);
        }

        /// <summary>
        ///   Set a value in the cache
        /// </summary>
        public async Task<bool> set(string DynamoName, string str, bool blacklisted = false)
        {
            //Check if value is already in okta
            var item = await table.GetItemAsync(DynamoName + "|" + str);
            if (item != null)
            {
                return false;
            }

            var x = new Document();
            x["uid"] = DynamoName + "|" + str;
            //set to expire one day from today.
            //Might not be removed for up to 72 hours and one second:
            //48 hours for aws delay,
            //24 hours for offset,
            //1 second for Ceiling
            if (!blacklisted)
            {
                x["expire_at"] = Math.Ceiling(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds + 86400).ToString();
            }

            var a = await table.PutItemAsync(x);
            return true;
        }

        /// <summary>
        ///   Checks if a value exists
        ///   Returns True if value is free
        ///   Returns False if value is taken
        /// </summary>
        public async Task<bool> check(string DynamoName, string str)
        {
            Log.Debug(DynamoName + "|" + str);
            var item = await table.GetItemAsync(DynamoName + "|" + str);
            //Check that these are not null
            if (item == null)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        ///   Get a value from the cache
        /// </summary>
        public async Task<string> get(string DynamoName, string str, bool getExpireAtTime = false)
        {
            var item = await table.GetItemAsync(DynamoName + "|" + str);
            try
            {
                return item["uid"].ToString().Split('|')[1];
            }
            catch
            {
                return "";
            }
        }


        /// <summary>
        ///   Delete an entry. Just for testing
        /// </summary>
        public void delete(string DynamoName, string str)
        {
            table.DeleteItemAsync(DynamoName + "|" + str);
        }
    }
}
