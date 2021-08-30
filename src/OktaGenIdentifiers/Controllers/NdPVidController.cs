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



namespace OktaGenIdentifiers.Controllers
{
    [Route("api/[controller]")]
        //[Authorize]
        [ApiController]
        public class ndPVidController : ControllerBase
        {
            /// <summary>
            ///   Get a unique id. Please "claim" it in okta ASAP
            /// </summary>
            [HttpPost]
            public ActionResult Post([FromHeader]string authorization)
            {
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.Console()
                    .CreateLogger();

                Log.Information("Hello from ndpvid controller");

                //Our output
                ContentResult result = new ContentResult();

                //Handle Authentication
                //Check if the header exists
                Log.Information(Secrets.get_secret.OktaPassphrase);
                if (string.IsNullOrEmpty(authorization)) {
                    result.StatusCode = 401;
                    result.Content = JsonConvert.SerializeObject(new OktaError("No Header Provided"));
                    Log.Warning(result.Content);
                    return result;
                }
                //We know the header exists, lets check if its what we expect
                //This facade is temporary. The value we want is called OktaAud to save time
                else if (!(authorization == Secrets.get_secret.OktaPassphrase)) {
                    Log.Information("Header does not match passphrase");
                    result.StatusCode = 401;
                    result.Content = JsonConvert.SerializeObject(new OktaError("Unauthorized"));
                    Log.Warning(result.Content);
                    return result;

                }
                //At this point we know we are authenticated

                //Get and set ids
                try {
                    Log.Information("Generating ndpvid");
                    var x = new GenPVid();
                    Log.Information("Created GenPVid object");
                    ndPVidObject ndpvid = new ndPVidObject(x.get_ndpvid().Result);
                    result.StatusCode = 200;
                    result.Content = JsonConvert.SerializeObject(ndpvid);
                    return result;

                }
                //If anything goes wrong, return an error
                catch(Exception err) {
                    Log.Error(err.Message);
                    Log.Error(JsonConvert.SerializeObject(err));
                    result.StatusCode = 400;
                    result.Content = JsonConvert.SerializeObject(new OktaError(err.Message));
                    return result;

                }
            }
        }
}
