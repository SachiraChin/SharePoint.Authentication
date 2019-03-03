namespace SharePoint.Authentication
{
    public class ACSTokenHelper : TokenHelper
    {
        private readonly ACSAuthenticationParameters _authenticationParameters;

        public ACSTokenHelper(ACSAuthenticationParameters authenticationParameters) : base(authenticationParameters)
        {
            _authenticationParameters = authenticationParameters;
        }
    }
}