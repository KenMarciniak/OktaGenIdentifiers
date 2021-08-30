using System.Collections.Generic;

namespace OktaGenIdentifiers.Models
{
    /// <summary>
    /// The combined response of all uids we take care of. Primarily for the SCIM agent to call
    /// </summary>
    public class ResponseObject
    {
        public string ndPVid { get; set; }
        public string ndGuid { get; set; }
        public string ndUniqueID { get; set; }
        public string userName { get; set; }
        public string email { get; set; }

        /// <summary>
        ///   Combines the three functions we have and fuses their output.
        ///   TODO: I am not sure if this is the most "csharp-y" way of doing things. possibly refactor?
        /// </summary>
        public ResponseObject(ndPVidObject ndpvid, ndUniqueIDObject nduniqueid, userNameObject username)
        {
            ndPVid = ndpvid.ndPVid;
            ndGuid = ndpvid.ndGuid;
            ndUniqueID = nduniqueid.ndUniqueID;
            userName = username.userName;
            email = username.email;
        }
    }

    /// <summary>
    ///   What we return when just generating a new ndPVid/ndGuid set
    /// </summary>
    public class ndPVidObject
    {
        public string ndPVid { get; set; }
        public string ndGuid { get; set; }

        public ndPVidObject(string ndpvid)
        {
            ndPVid = ndpvid;
            ndGuid = "nd.edu." + ndpvid;
        }
    }

    /// <summary>
    ///   What we return when just generating a new ndUniqueID
    /// </summary>
    public class ndUniqueIDObject
    {
        public string ndUniqueID { get; set; }

        public ndUniqueIDObject(string nduniqueid)
        {
            ndUniqueID = nduniqueid;
        }
    }

    /// <summary>
    ///   What we return when just generating a new userName/email set
    /// </summary>
    public class userNameObject
    {
        public string userName { get; set; }
        public string email { get; set; }

        public userNameObject(string username)
        {
            string environment = Secrets.get_secret.Environment;
            if (environment.ToLower() == "preview")
            {
                userName = username + "@guat.nd.edu";
            }
            else
            {
                userName = username + "@nd.edu";
            }
            email = userName;
        }
    }

}