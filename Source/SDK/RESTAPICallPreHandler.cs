using System;
using System.Collections.Generic;
using System.Text;

namespace PayPal.Api
{
    /// <summary>
    /// RESTApiCallPreHandler requires a configuration system to function properly. Pass
    /// a config Dictionary for dynamic configuration.
    /// </summary>
    public class RESTAPICallPreHandler : IAPICallPreHandler
    {
        /// <summary>
        /// Dynamic configuration map
        /// </summary>
        private Dictionary<string, string> config;

        /// <summary>
        /// Optional headers map
        /// </summary>
        private Dictionary<string, string> headersMap;

        /// <summary>
        /// Gets or sets the SDK version information
        /// </summary>
        public SDKVersion SdkVersion { get; set; }

        /// <summary>
        ///  Gets and sets the Authorization Token
        /// </summary>
        public string AuthorizationToken { get; set; }

        /// <summary>
        /// Gets and sets the Idempotency Request Id
        /// </summary>
        public string RequestId { get; set; }

        /// <summary>
        /// Payload
        /// </summary>
        public string Payload { get; set; }

        /// <summary>
        /// RESTAPICallPreHandler taking dynamic configuration Dictionary
        /// </summary>
        /// <param name="config">Dictionary for dynamic configuration</param>
        public RESTAPICallPreHandler(Dictionary<string, string> config)
        {
            this.config = ConfigManager.GetConfigWithDefaults(config);
        }

        /// <summary>
        /// RESTAPICallPreHandler taking dynamic configuration Dictionary and HTTP Headers Dictionary
        /// </summary>
        /// <param name="config">Dictionary for dynamic configuration</param>
        /// <param name="headersMap">Dictionary for HTTP Headers</param>
        public RESTAPICallPreHandler(Dictionary<string, string> config, Dictionary<string, string> headersMap)
        {
            this.config = ConfigManager.GetConfigWithDefaults(config);
            this.headersMap = (headersMap == null) ? new Dictionary<string, string>() : headersMap;
        }

        public Dictionary<string, string> GetHeaderMap()
        {
            Dictionary<string, string> headers = new Dictionary<string, string>();

            /*
		     * The implementation is PayPal specific. The Authorization header is
		     * formed for OAuth or Basic, for OAuth system the authorization token
		     * passed as a parameter is used in creation of HTTP header, for Basic
		     * Authorization the ClientID and ClientSecret passed as parameters are
		     * used after a Base64 encoding.
		     */
            if (!string.IsNullOrEmpty(AuthorizationToken))
            {
                headers.Add(BaseConstants.AuthorizationHeader, AuthorizationToken);
            }
            else if (!string.IsNullOrEmpty(GetClientID()) && !string.IsNullOrEmpty(GetClientSecret()))
            {
                headers.Add(BaseConstants.AuthorizationHeader, "Basic " + EncodeToBase64(GetClientID(), GetClientSecret()));
            }

            /*
             * Appends request Id which is used by PayPal API service for
		     * Idempotency
             */
            if (!string.IsNullOrEmpty(RequestId))
            {
                headers.Add(BaseConstants.PayPalRequestIdHeader, RequestId);
            }

            // Add User-Agent header for tracking in PayPal system
            Dictionary<string, string> userAgentMap = FormUserAgentHeader();
            if (userAgentMap != null && userAgentMap.Count > 0)
            {
                foreach (KeyValuePair<string, string> entry in userAgentMap)
                {
                    headers.Add(entry.Key, entry.Value);
                }
            }

            // Add any custom headers
            if (headersMap != null && headersMap.Count > 0)
            {
                foreach (KeyValuePair<string, string> entry in headersMap)
                {
                    headers.Add(entry.Key, entry.Value);
                }
            }
            return headers;
        }

        public string GetPayload()
        {
            return this.Payload;
        }

        public string GetEndpoint()
        {
            string endpoint = null;
            if (config.ContainsKey(BaseConstants.EndpointConfig))
            {
                endpoint = config[BaseConstants.EndpointConfig];
            }
            else if (config.ContainsKey(BaseConstants.ApplicationModeConfig))
            {
                switch (config[BaseConstants.ApplicationModeConfig])
                {
                    case BaseConstants.LiveMode:
                        endpoint = BaseConstants.RESTLiveEndpoint;
                        break;
                    case BaseConstants.SandboxMode:
                        endpoint = BaseConstants.RESTSandboxEndpoint;
                        break;
                }
            }
            if (!endpoint.EndsWith("/"))
            {
                endpoint += "/";
            }
            return endpoint;
        }

        public PayPal.Authentication.ICredential GetCredential()
        {
            return null;
        }

        /// <summary>
        /// Override this method to customize User-Agent header value
        /// </summary>
        /// <returns>User-Agent header value string</returns>
        protected Dictionary<string, string> FormUserAgentHeader()
        {
            return UserAgentHeader.GetHeader();
        }

        private String GetClientID()
        {
            return this.config.ContainsKey(BaseConstants.ClientId) ? this.config[BaseConstants.ClientId] : null;
        }

        private String GetClientSecret()
        {
            return this.config.ContainsKey(BaseConstants.ClientSecret) ? this.config[BaseConstants.ClientSecret] : null;
        }

        private String EncodeToBase64(string clientID, string clientSecret)
        {
            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(clientID + ":" + clientSecret);
                string base64ClientID = Convert.ToBase64String(bytes);
                return base64ClientID;
            }
            catch (System.Exception ex)
            {
                throw new PayPalException(ex.Message, ex);
            }
        }

    }
}
