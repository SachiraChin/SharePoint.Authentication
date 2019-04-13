using System.Collections.Generic;

namespace SharePoint.Authentication.Owin.Models
{
    internal class AADOpenIdConfig
    {
        public string authorization_endpoint { get; set; }
        public string token_endpoint { get; set; }
        public List<string> token_endpoint_auth_methods_supported { get; set; }
        public string jwks_uri { get; set; }
        public List<string> response_modes_supported { get; set; }
        public List<string> subject_types_supported { get; set; }
        public List<string> id_token_signing_alg_values_supported { get; set; }
        public bool http_logout_supported { get; set; }
        public bool frontchannel_logout_supported { get; set; }
        public string end_session_endpoint { get; set; }
        public List<string> response_types_supported { get; set; }
        public List<string> scopes_supported { get; set; }
        public string issuer { get; set; }
        public List<string> claims_supported { get; set; }
        public bool microsoft_multi_refresh_token { get; set; }
        public string check_session_iframe { get; set; }
        public string userinfo_endpoint { get; set; }
        public object tenant_region_scope { get; set; }
        public string cloud_instance_name { get; set; }
        public string cloud_graph_host_name { get; set; }
        public string msgraph_host { get; set; }
        public string rbac_url { get; set; }
    }
}