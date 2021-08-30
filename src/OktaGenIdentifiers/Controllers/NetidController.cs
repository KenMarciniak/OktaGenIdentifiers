using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Serilog;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization;
using OktaGenIdentifiers.Models;


namespace OktaGenIdentifiers
{
    [Route("api/[controller]")]
    //[Authorize]
    [ApiController]
    public class netidController : ControllerBase
    {
        /// <summary>
        ///   Get a netid.
        /// </summary>
        [HttpPost]
        public ActionResult Post(RequestObject request, [FromHeader] string authorization)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .CreateLogger();

            ContentResult result = new ContentResult();

            //Handle Authentication
            //Check if the header exists
            if (string.IsNullOrEmpty(authorization))
            {
                result.StatusCode = 401;
                result.Content = JsonConvert.SerializeObject(new OktaError("No Header Provided"));
                Log.Warning(result.Content);
                return result;
            }
            //We know the header exists, lets check if its what we expect
            //This facade is temporary. The value we want is called OktaAud to save time
            else if (!(authorization == Secrets.get_secret.OktaPassphrase))
            {
                result.StatusCode = 401;
                result.Content = JsonConvert.SerializeObject(new OktaError("Unauthorized"));
                Log.Warning(result.Content);
                return result;

            }
            //At this point we know we are authenticated

            try
            {
                var x = new GenNetID();
                string username = "";

                //Try and build netid
                try
                {
                    username = x.get_netid(request.firstName, request.lastName, request.ndPrimaryAffiliation).Result;
                }
                //If we can't build the netid, leave the username var blank, but log
                catch (Exception err)
                {
                    Log.Warning("Unable to generate netid: {0}", err);
                }

                if (string.IsNullOrEmpty(username))
                {
                    string error_message = $"Unable to generate a netid for {request.firstName} {request.lastName}";
                    Log.Error(error_message);
                    result.StatusCode = 500;
                    result.Content = JsonConvert.SerializeObject(new OktaError(error_message));
                }
                else
                {
                    userNameObject userName = new userNameObject(username);
                    result.StatusCode = 200;
                    result.Content = JsonConvert.SerializeObject(userName);
                }
                return result;
            }
            //If anything goes wrong, return an error
            catch (Exception err)
            {
                Log.Error(err.Message);
                Log.Error(JsonConvert.SerializeObject(err));
                result.StatusCode = 400;
                result.Content = JsonConvert.SerializeObject(new OktaError(err.Message));
                return result;
            }
        }
    }
}
