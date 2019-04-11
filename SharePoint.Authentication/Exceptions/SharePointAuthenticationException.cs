using System;

namespace SharePoint.Authentication.Exceptions
{
    public class SharePointAuthenticationException : Exception
    {
        public SharePointAuthenticationException()
        {
        }

        public SharePointAuthenticationException(string message) : base(message)
        {
        }
        
        public SharePointAuthenticationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
