using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OAuth2_UserIdentity.Models
{
    public class UserStateValue
    {
        public int UserStateValueID { get; set; }
        public string userObjId { get; set; }
        public string stateGuid { get; set; }
    }
}