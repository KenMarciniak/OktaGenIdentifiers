using System;

namespace OktaGenIdentifiers.Models
{
    public class RequestObject
    {
        public string ndPVid { get; set; }
        public string ndGuid { get; set; }
        public string ndUniqueID { get; set; }
        public string userName { get; set; }
        public string email { get; set; }
        public string userType { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string ndPrimaryAffiliation { get; set; }
    }
}