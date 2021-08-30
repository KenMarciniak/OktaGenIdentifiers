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
using System.Text.RegularExpressions;
using System.Globalization;
using OktaGenIdentifiers;
using OktaGenIdentifiers.Models;
using Diacritics.Extensions;

namespace OktaGenIdentifiers
{
    public class GenNetID
    {
        public DynamoHelper cache;
        private OktaClient c;

        public GenNetID()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .CreateLogger();

            this.c = new OktaClient(new OktaClientConfiguration
            {
                OktaDomain = Secrets.get_secret.OktaUrl,
                Token = Secrets.get_secret.OktaApiToken,
                MaxRetries = 5,
                RequestTimeout = 60
            });

            this.cache = new DynamoHelper();

        }

        /// <summary>
        ///   Get a unique netid value
        /// </summary>
        public async Task<string> get_netid(String first_name, String last_name, String affiliation)
        {
            const int MAX_NETID_CHAR = 8;
            bool isBot = false;
            var random = new Random();
            var chars = "abcdef023456789";

            if (affiliation == "bot")
            {
                if (last_name != "BOT")
                {
                    Log.Error($"Bot accounts must have BOT as last name: {first_name},{last_name}");
                    return null;
                }
                isBot = true;
            }

            if (!isBot) //don't process bot names
            {
                first_name = ProcessName(first_name);
                last_name = ProcessName(last_name);
            }

            if (string.IsNullOrEmpty(last_name))
            {
                Log.Error($"No last name supplied: {first_name},{last_name}");
                return null;
            }
            //Start with concatenating first initial of first name with
            //last name and truncating to eight characters.  If first name
            //is empty, then just use last_name
            String netid = "";
            if (string.IsNullOrEmpty(first_name))
            {
                Log.Information($"No first name supplied for {last_name}.");
                netid = last_name;
            }
            else
            {
                if (isBot)
                {
                    var randomhex = new string(chars.Select(c => chars[random.Next(chars.Length)]).Take(6).ToArray());
                    netid = "b_" + randomhex; //bot NetID
                }
                else netid = first_name.Substring(0, 1) + last_name; //Non bot NetID
            }
            if (netid.Length > MAX_NETID_CHAR)
                netid = netid.Substring(0, MAX_NETID_CHAR);

            string original_netid = netid;

            //Loop for checking against existing netids
            for (int i = 0; i < 10000; i++)
            {
                //After the first iteration, then increment netid
                //and adjust length if necessary
                if (i > 0)
                {
                    if (isBot)
                    {
                        //if that bot netid is taken, generate a new random one
                        var randomhex = new string(chars.Select(c => chars[random.Next(chars.Length)]).Take(6).ToArray());
                        netid = "b_" + randomhex; //bot NetID
                    }
                    else
                    {
                        string suffix = i.ToString();

                        //Don't use numbers containing zeroes or ones since they look
                        //too similar to lowercase L's and uppercase o's
                        if (suffix.Contains('1') || suffix.Contains('0'))
                            continue;
                        //remove any changes we made to the netids
                        netid = original_netid;

                        //Make sure the netid + suffix are less than 8 chars for non bots
                        if (netid.Length + suffix.Length > MAX_NETID_CHAR)
                            netid = netid.Substring(0, MAX_NETID_CHAR - suffix.Length) + suffix;
                        else
                            netid = netid + suffix;
                    }

                }

                //If the netid does not exist in DB then check Okta
                if (this.cache.check("netid", netid).Result)
                {
                    //If netid does not exist in Okta, then set in DB and return
                    if (check_okta(netid).Result)
                    {
                        bool success = this.cache.set("netid", netid).Result;
                        if (success)
                        {
                            return netid;
                        }
                    }

                }
            }

            return null;
        }

        /// <summary>
        ///   Checks if value doesn't exist in okta
        ///   True: value is free to use
        ///   False: value is taken
        /// </summary>
        public async Task<bool> check_okta(string login)
        {
            try
            {
                var users = await this.c.Users.ListUsers(search: $"profile.login sw \"{login}@\"").ToArray();
                if (users.Count() == 0)
                {
                    return true;
                }
                else
                {
                    Log.Information("Found a taken value, {id}", login);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Error("ERROR: {0}", ex);
                return false;
            }


        }

        /// <summary>
        ///   Remove accents, umlauts, and other glyphs from string.
        ///   Return ascii string
        /// </summary>
        public static string ProcessName(string text)
        {
            string rd = text.RemoveDiacritics();
            string lower = rd.ToLower(CultureInfo.CurrentCulture);
            string rdclean = Regex.Replace(lower, @"[^a-z]+", "");

            return rdclean;
        }
    }
}