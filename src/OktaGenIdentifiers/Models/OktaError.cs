using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OktaGenIdentifiers.Models
{

    /// <summary>
    ///   The error type we will send to okta
    /// </summary>
    public class OktaError {
        public Dictionary<string, string> error{ get; set; }

        /// <summary>
        ///   Initialize an error with a message
        /// </summary>
        public OktaError(string error_message) {
            this.error = new Dictionary<string, string>();
            this.error.Add("errorSummary", error_message);
        }
    }
}