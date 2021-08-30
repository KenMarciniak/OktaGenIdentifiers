using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Caching.Redis;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Serilog;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization;
using OktaGenIdentifiers.Models;



namespace OktaGenIdentifiers
{
    [Route("api/")]
    //[Authorize]
    [ApiController]
    public class AcGenController : ControllerBase
    {
        /// <summary>
        ///   Get a unique id. Please "claim" it in okta ASAP
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> Post([FromBody] RequestObject request, [FromHeader] string authorization)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .CreateLogger();

            //Our output
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

            //Get and set ids
            try
            {

                var pvid = new GenPVid();
                var net = new GenNetID();
                var uuid = new GenUUID();
                var tnuuid = uuid.get_uuid();
                var tndpvid = pvid.get_ndpvid();
                var tnetid = net.get_netid(request.firstName, request.lastName, request.ndPrimaryAffiliation);
                //ndUniqueIDObject uuid = new ndUniqueIDObject(UUID.get_uuid());

                //Wait for everything to finish
                var ndpvid = await tndpvid;
                var netid = await tnetid;
                var nduuid = await tnuuid;

                var full_object = new ResponseObject(new ndPVidObject(ndpvid),
                                                 new ndUniqueIDObject(nduuid),
                                                 new userNameObject(netid));


                result.StatusCode = 200;
                result.Content = JsonConvert.SerializeObject(full_object);
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