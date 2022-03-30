using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AuthenticationServices
{
    public class CheckBoxViewModel
    {
        public int Id { get; set; }

        public string OAuth2Role { get; set; }

        public bool IsChecked { get; set; }
    }
}