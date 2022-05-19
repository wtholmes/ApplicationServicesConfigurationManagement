using System.ComponentModel;

namespace AuthenticationServices
{
    public class CheckBoxViewModel
    {
        public int Id { get; set; }

        [DisplayName("Client Role")]
        public string OAuth2Role { get; set; }

        [DisplayName("Role Description")]
        public string OAuth2RoleDescription { get; set; }

        [DisplayName("Enabled")]
        public bool IsChecked { get; set; }
    }
}