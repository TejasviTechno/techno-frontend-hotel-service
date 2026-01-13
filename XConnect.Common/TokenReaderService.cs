using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using XConnect.CommonUtilities.Interface;
using XConnect.Models;

namespace XConnect.CommonUtilities
{
    public class TokenReaderService : ITokenReaderService
    {
        public TokenDetails getTokenDetails(string token)
        {
            TokenDetails _token = new TokenDetails();
            try
            {
                // Split the JWT into its three parts
                string[] jwtParts = token.Split('.');

                if (jwtParts.Length == 3)
                {
                    // Decode the payload (second part)
                    string payload = jwtParts[1];
                    string decodedPayload = Base64UrlDecode(payload);
                    string decodedPayloadJson = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(decodedPayload));
                    if (!string.IsNullOrEmpty(decodedPayloadJson))
                    {
                        JsonObject jToken = System.Text.Json.JsonSerializer.Deserialize<JsonObject>(decodedPayloadJson);
                        #region Not Used
                        //_token.agentId = Convert.ToInt32(jToken["agentId"]?.ToString());
                        //_token.subAgentId = 0;
                        //_token.subUserId = Convert.ToInt32(jToken["subUserId"]?.ToString());

                        //if (Convert.ToInt32(jToken["userId"].ToString()) != _token.agentId && Convert.ToInt32(jToken["userId"].ToString()) != _token.subUserId)
                        //{
                        //    _token.subAgentId = Convert.ToInt32(jToken["userId"]?.ToString());
                        //}
                        //_token.userTypeId = Convert.ToInt32(jToken["userTypeId"]?.ToString());

                        //_token.creditLimitCurrencyId = Convert.ToInt32(jToken["creditlimitCurrencyId"]?.ToString());
                        //_token.creditLimitCurrencyName = Convert.ToString(jToken["creditlimitCurrencyCode"]?.ToString());
                        //_token.mapCategoryId = Convert.ToInt32(jToken["rateCategoryId"]?.ToString());
                        //_token.agentCode = Convert.ToString(jToken["agentCode"]?.ToString());
                        //if (Guid.TryParse(jToken["tenantId"]?.ToString(), out var tenantGuid))
                        //{
                        //    _token.tenantId = tenantGuid;
                        //}
                        #endregion
                        #region New Code
                        _token.userType = Convert.ToString(jToken["userType"]?.ToString());
                        _token.subAgentId = 0;
                        _token.parentId = 0;
                        switch (_token.userType.ToLower())
                        {
                            case "agent": //Agent
                                _token.agentId = Convert.ToInt32(jToken["agentId"]?.ToString());
                                _token.parentId = 0;
                                break;
                            case "subagent": //SubAgent
                                _token.agentId = Convert.ToInt32(jToken["userId"]?.ToString());
                                _token.parentId = Convert.ToInt32(jToken["agentId"]?.ToString());
                                break;
                            case "subuser": //SubUser
                                _token.agentId = Convert.ToInt32(jToken["agentId"]?.ToString());
                                _token.subUserId = Convert.ToInt32(jToken["subUserId"]?.ToString());
                                break;
                        }

                        _token.creditLimitCurrencyId = Convert.ToInt32(jToken["creditlimitCurrencyId"]?.ToString());
                        _token.creditLimitCurrencyName = Convert.ToString(jToken["creditlimitCurrencyCode"]?.ToString());
                        _token.mapCategoryId = Convert.ToInt32(jToken["rateCategoryId"]?.ToString());
                        _token.agentCode = Convert.ToString(jToken["agentCode"]?.ToString());
                        if (Guid.TryParse(jToken["tenantId"]?.ToString(), out var tenantGuid))
                        {
                            _token.tenantId = tenantGuid;
                        }
                        _token.siteCurrency = "USD";
                        #endregion
                    }
                }
            }
            catch (Exception ex)
            {
                _token = null;
            }
            return _token;
        }
        public string Base64UrlDecode(string base64Url)
        {
            // Replace Base64Url characters with Base64 characters
            string base64 = base64Url.Replace('-', '+').Replace('_', '/');

            // Add padding if necessary
            int padding = base64.Length % 4;
            if (padding > 0)
            {
                base64 = base64.PadRight(base64.Length + (4 - padding), '=');
            }

            return base64;
        }

    }
}
