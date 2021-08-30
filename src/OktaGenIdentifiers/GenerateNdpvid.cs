using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using Serilog;
using Okta.Sdk;
using Okta.Sdk.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.Caching.Distributed;

using OktaGenIdentifiers;


namespace OktaGenIdentifiers
{
    public class GenPVid
    {
        //Constants
        public const string validchars = "bdfghjkmnqrstvwxz";
        public const string validdigits = "23456789";
        public const string ndpvidmask = "aaaadaad";
        public DynamoHelper cache;
        private OktaClient c;

        public GenPVid()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .CreateLogger();
        
            this.c = new OktaClient(new OktaClientConfiguration{
                OktaDomain = Secrets.get_secret.OktaUrl,
                Token = Secrets.get_secret.OktaApiToken,
                MaxRetries = 5,
                RequestTimeout = 60}
            );
            Log.Information("Okta client created");
            this.cache = new DynamoHelper();
        }

        /// <summary>
        ///   Get a unique ndpvid value
        /// </summary>
        public async Task<string> get_ndpvid()
        {
            var temp_ndpivd = gen_ndpvid();
            //Make new ones until find one that doesn't exist
            //Loop for checking okta
            while (true)
            {
                Log.Information(temp_ndpivd);
                //Loop for checking in-memory datastore
                while (true)
                {
                    //If we have a new unique id
                    if (this.cache.check("ndpvid", temp_ndpivd).Result)
                    {
                        //We found one not in the cache, lets check okta next
                        break;
                    }
                    else
                    {
                        temp_ndpivd = gen_ndpvid();
                    }
                }
                if (check_okta(temp_ndpivd).Result)
                {
                    bool success = this.cache.set("ndpvid", temp_ndpivd).Result;
                    if (success)
                    {
                        return temp_ndpivd;
                    }
                }
                temp_ndpivd = gen_ndpvid();
            }
        }


        /// <summary>
        ///   Generate a random ndpvid. Is not tested to be unique
        /// </summary>
        private string gen_ndpvid()
        {
            string ndpvid = String.Empty;
            Random rnd = new Random();
            int number = 10;

            //Our list of output
            string[] output = new string[number];

            ndpvid += "nd";

            var p = new Dictionary<Char, Action>
            {
                { 'a', () => ndpvid += validchars.OrderBy(x => rnd.Next()).First() },
                { 'd', () => ndpvid += validdigits.OrderBy(x => rnd.Next()).First() }
            };


            while (ndpvid.Length < ndpvidmask.Length) p[ndpvidmask[ndpvid.Length]]();
            Log.Information("ndpvid:",ndpvid);
            return ndpvid;
        }

        /// <summary>
        ///   Checks if value doesn't exist in okta
        ///   True: value is free to use
        ///   False: value is taken
        /// </summary>
        public async Task<bool> check_okta(string possibly_unique)
        {
            try
            {
                var users = await this.c.Users.ListUsers(search: $"profile.ndPVid eq \"{possibly_unique}\"").ToArray();
                if (users.Count() == 0)
                {
                    return true;
                }
                else
                {
                    Log.Information("Found a taken value, {id}", possibly_unique);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Error("ERROR: {0}", ex);
                return false;
            }


        }
    }
}
