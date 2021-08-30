using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Serilog;

/*
 *	Use this code snippet in your app.
 *	If you need more information about configurations or implementing the sample code, visit the AWS docs:
 *	https://aws.amazon.com/developers/getting-started/net/
 *
 *	Make sure to include the following packages in your code.
 *
 *	using System;
 *	using System.IO;
 *
 *	using Amazon;
 *	using Amazon.SecretsManager;
 *	using Amazon.SecretsManager.Model;
 *
 */

/*
 * AWSSDK.SecretsManager version="3.3.0" targetFramework="net45"
 */
namespace OktaGenIdentifiers
{
    public class Secrets
    {
        public static SecretsBundle get_secret;

        /// <summary>
        ///   For when you know aws secrets manager can't be called
        /// </summary>
        public Secrets()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .CreateLogger();

            Log.Information("Pulling Environment vars");

            //So The Oktasdk seems to complain when we don't have an environment variable named "HOME"
            //I'm not really sure why, but this fixes it for now
            Environment.SetEnvironmentVariable("HOME", "~/");

            //Pull secrets
            get_secret = GetSecretWithEnvs();
        }

        /// <summary>
        ///   Tries to get secrets from aws secrets manager, then falls back to env variables
        /// </summary>
        public Secrets(string service)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .CreateLogger();

            //So The Oktasdk seems to complain when we don't have an environment variable named "HOME"
            //I'm not really sure why, but this fixes it for now
            Environment.SetEnvironmentVariable("HOME", "~/");

            try
            {
                var s = GetSecret(service);
                if (s != null)
                {
                    get_secret = s;
                }
            }
            catch (Exception e)
            {
                Log.Warning(e.Message);
                Log.Warning("Unable to pull from secrets manager");

                get_secret = GetSecretWithEnvs();

                //Check if we could get everything from environment variables
                if (!get_secret.validate())
                {
                    Log.Error("Could not load Env variables. Please set them in aws/.env");
                }
            }

        }

        /// <summary>
        ///   If we are unable to read aws, try reading env variables
        /// </summary>
        public static SecretsBundle GetSecretWithEnvs()
        {
            //Load .env file (if exists)
            try
            {
                DotNetEnv.Env.Load();
            }
            catch
            {
                Log.Information(".env file doesn't exist");
            }
            return new SecretsBundle(
                                     Environment.GetEnvironmentVariable("OKTA_URL"),
                                     Environment.GetEnvironmentVariable("OKTA_AUD"),
                                     Environment.GetEnvironmentVariable("OKTA_PASSPHRASE"),
                                     Environment.GetEnvironmentVariable("OKTA_API_TOKEN"),
                                     Environment.GetEnvironmentVariable("SENTRY_URL"),
                                     Environment.GetEnvironmentVariable("DYNAMO_TABLE"),
                                     Environment.GetEnvironmentVariable("DYNAMO_NAME"),
                                     Environment.GetEnvironmentVariable("ENVIRONMENT"));
        }

        /// <summary>
        ///   Talks to amazon and gets the secret
        /// </summary>
        public static SecretsBundle GetSecret(string service)
        {
            Log.Information("Getting secret " + service);

            if (string.IsNullOrWhiteSpace(service))
            {
                throw new ArgumentException("No aws secret key provided");
            }

            string secretName = service;
            string region = "us-east-1";
            string secret = "";

            MemoryStream memoryStream = new MemoryStream();

            IAmazonSecretsManager client = new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(region));

            GetSecretValueRequest request = new GetSecretValueRequest();
            request.SecretId = secretName;

            request.VersionStage = "AWSCURRENT"; // VersionStage defaults to AWSCURRENT if unspecified.

            GetSecretValueResponse response = null;

            // In this sample we only handle the specific exceptions for the 'GetSecretValue' API.
            // See https://docs.aws.amazon.com/secretsmanager/latest/apireference/API_GetSecretValue.html
            // We rethrow the exception by default.

            try
            {
                response = client.GetSecretValueAsync(request).Result;
            }
            catch (DecryptionFailureException e)
            {
                // Secrets Manager can't decrypt the protected secret text using the provided KMS key.
                // Deal with the exception here, and/or rethrow at your discretion.
                Log.Error("Decryption Failure");
                Log.Error(e.ToString());
                throw e;
            }
            catch (InternalServiceErrorException e)
            {
                // An error occurred on the server side.
                // Deal with the exception here, and/or rethrow at your discretion.
                Log.Error("Internal Service Error");
                Log.Error(e.ToString());
                throw e;
            }
            catch (InvalidParameterException e)
            {
                // You provided an invalid value for a parameter.
                // Deal with the exception here, and/or rethrow at your discretion
                Log.Error("Invalid Parameter");
                Log.Error(e.ToString());
                throw e;
            }
            catch (InvalidRequestException e)
            {
                // You provided a parameter value that is not valid for the current state of the resource.
                // Deal with the exception here, and/or rethrow at your discretion.
                Log.Error("Invalid Request");
                Log.Error(e.ToString());
                throw e;
            }
            catch (ResourceNotFoundException e)
            {
                // We can't find the resource that you asked for.
                // Deal with the exception here, and/or rethrow at your discretion.
                Log.Error("Resource Not Found");
                Log.Error(e.ToString());
                throw e;
            }
            catch (System.AggregateException ae)
            {
                // More than one of the above exceptions were triggered.
                // Deal with the exception here, and/or rethrow at your discretion.
                Log.Error("Aggregate Exception");
                Log.Error(ae.ToString());
                throw ae;
            }

            // Decrypts secret using the associated KMS CMK.
            // Depending on whether the secret is a string or binary, one of these fields will be populated.
            if (response.SecretString != null)
            {
                secret = response.SecretString;
            }
            //This shouldn't be a path that ever happens, but it "came with" from aws's sample
            else
            {
                memoryStream = response.SecretBinary;
                StreamReader reader = new StreamReader(memoryStream);
                string decodedBinarySecret = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(reader.ReadToEnd()));
            }

            return new SecretsBundle(secret);

        }
    }

    /// <summary>
    ///   Class to store all the secrets
    /// </summary>
    public class SecretsBundle
    {

        public string DynamoName { get; private set; }
        public string DynamoTable { get; private set; }
        public string OktaPassphrase { get; private set; }
        public string OktaApiToken { get; private set; }
        public string OktaUrl { get; private set; }
        public string OktaAud { get; private set; }
        public string SentryUrl { get; private set; }
        public string Environment { get; private set; }




        public SecretsBundle(string json)
        {
            JObject j = JObject.Parse(json);
            OktaUrl = (string)j["OktaUrl"];
            OktaAud = (string)j["OktaAud"];
            OktaPassphrase = (string)j["OktaPassphrase"];
            OktaApiToken = (string)j["OktaApiToken"];
            SentryUrl = (string)j["SentryUrl"];
            DynamoTable = (string)j["DynamoTable"];
            DynamoName = (string)j["DynamoName"];
            Environment = (string)j["Environment"];
        }
        public SecretsBundle(string okta_url,
                             string okta_aud,
                             string okta_passphrase,
                             string okta_api_token,
                             string sentry_url,
                             string dynamo_table,
                             string dynamo_name,
                             string environment)
        {

            OktaUrl = okta_url;
            OktaAud = okta_aud;
            OktaPassphrase = okta_passphrase;
            OktaApiToken = okta_api_token;
            SentryUrl = sentry_url;
            DynamoTable = dynamo_table;
            DynamoName = dynamo_name;
            Environment = environment;
        }

        /// <summary>
        ///   Check if any values are not filled
        /// </summary>
        public bool validate()
        {
            try
            {
                if (
                    string.IsNullOrWhiteSpace(this.DynamoName) ||
                    string.IsNullOrWhiteSpace(this.DynamoTable) ||
                    string.IsNullOrWhiteSpace(this.OktaPassphrase) ||
                    string.IsNullOrWhiteSpace(this.OktaApiToken) ||
                    string.IsNullOrWhiteSpace(this.OktaUrl) ||
                    string.IsNullOrWhiteSpace(this.OktaAud) ||
                    string.IsNullOrWhiteSpace(this.SentryUrl) ||
                    string.IsNullOrWhiteSpace(this.Environment)
                    )
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}