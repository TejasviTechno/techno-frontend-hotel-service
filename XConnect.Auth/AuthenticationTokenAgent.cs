using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using UserContextMiddleWare.Services;
using XConnect.Auth.Interface;
using XConnect.CommonUtilities;
using XConnect.CommonUtilities.Interface;
using XConnect.DAL.Interface;
using XConnect.ErrorLogHandler.Interface;
using XConnect.Models;




namespace Xconnect.Auth
{
    public class AuthenticationTokenAgent : IAuthenticationTokenAgent
    {
        public const string _salt = "rz8LuOtFBXphj9WQfvFh";
        static Regex MobileCheck = new Regex(@"android|(android|bb\d+|meego).+mobile|avantgo|bada\/|blackberry|blazer|compal|elaine|fennec|hiptop|iemobile|ip(hone|od|ad)|iris|kindle|lge |maemo|midp|mmp|mobile.+firefox|netfront|opera m(ob|in)i|palm( os)?|phone|p(ixi|re)\/|plucker|pocket|psp|series(4|6)0|symbian|treo|up\.(browser|link)|vodafone|wap|windows (ce|phone)|xda|xiino", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);
        static Regex MobileVersionCheck = new Regex(@"1207|6310|6590|3gso|4thp|50[1-6]i|770s|802s|a wa|abac|ac(er|oo|s\-)|ai(ko|rn)|al(av|ca|co)|amoi|an(ex|ny|yw)|aptu|ar(ch|go)|as(te|us)|attw|au(di|\-m|r |s )|avan|be(ck|ll|nq)|bi(lb|rd)|bl(ac|az)|br(e|v)w|bumb|bw\-(n|u)|c55\/|capi|ccwa|cdm\-|cell|chtm|cldc|cmd\-|co(mp|nd)|craw|da(it|ll|ng)|dbte|dc\-s|devi|dica|dmob|do(c|p)o|ds(12|\-d)|el(49|ai)|em(l2|ul)|er(ic|k0)|esl8|ez([4-7]0|os|wa|ze)|fetc|fly(\-|_)|g1 u|g560|gene|gf\-5|g\-mo|go(\.w|od)|gr(ad|un)|haie|hcit|hd\-(m|p|t)|hei\-|hi(pt|ta)|hp( i|ip)|hs\-c|ht(c(\-| |_|a|g|p|s|t)|tp)|hu(aw|tc)|i\-(20|go|ma)|i230|iac( |\-|\/)|ibro|idea|ig01|ikom|im1k|inno|ipaq|iris|ja(t|v)a|jbro|jemu|jigs|kddi|keji|kgt( |\/)|klon|kpt |kwc\-|kyo(c|k)|le(no|xi)|lg( g|\/(k|l|u)|50|54|\-[a-w])|libw|lynx|m1\-w|m3ga|m50\/|ma(te|ui|xo)|mc(01|21|ca)|m\-cr|me(rc|ri)|mi(o8|oa|ts)|mmef|mo(01|02|bi|de|do|t(\-| |o|v)|zz)|mt(50|p1|v )|mwbp|mywa|n10[0-2]|n20[2-3]|n30(0|2)|n50(0|2|5)|n7(0(0|1)|10)|ne((c|m)\-|on|tf|wf|wg|wt)|nok(6|i)|nzph|o2im|op(ti|wv)|oran|owg1|p800|pan(a|d|t)|pdxg|pg(13|\-([1-8]|c))|phil|pire|pl(ay|uc)|pn\-2|po(ck|rt|se)|prox|psio|pt\-g|qa\-a|qc(07|12|21|32|60|\-[2-7]|i\-)|qtek|r380|r600|raks|rim9|ro(ve|zo)|s55\/|sa(ge|ma|mm|ms|ny|va)|sc(01|h\-|oo|p\-)|sdk\/|se(c(\-|0|1)|47|mc|nd|ri)|sgh\-|shar|sie(\-|m)|sk\-0|sl(45|id)|sm(al|ar|b3|it|t5)|so(ft|ny)|sp(01|h\-|v\-|v )|sy(01|mb)|t2(18|50)|t6(00|10|18)|ta(gt|lk)|tcl\-|tdg\-|tel(i|m)|tim\-|t\-mo|to(pl|sh)|ts(70|m\-|m3|m5)|tx\-9|up(\.b|g1|si)|utst|v400|v750|veri|vi(rg|te)|vk(40|5[0-3]|\-v)|vm40|voda|vulc|vx(52|53|60|61|70|80|81|83|85|98)|w3c(\-| )|webc|whit|wi(g |nc|nw)|wmlb|wonu|x700|yas\-|your|zeto|zte\-", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

        private readonly IDLConnection _dlConnection;
        private readonly IMemoryCache _cache;
        private readonly IException _exception;
        private readonly KeySettings _keySettings;
        private readonly ICommonMethod _commonMethod;
        private readonly ITokenReaderService _jwt;

        private readonly IUserContextAccessor _userContextAccessor;

        public AuthenticationTokenAgent(IDLConnection dlConnection, IMemoryCache cache, IException exception, IOptions<KeySettings> keySettings, ICommonMethod commonMethod, ITokenReaderService jwt, IUserContextAccessor userContextAccessor)
        {
            _dlConnection = dlConnection;
            _cache = cache;
            _exception = exception;
            _keySettings = keySettings.Value;
            _commonMethod = commonMethod;
            _jwt = jwt;
            _userContextAccessor = userContextAccessor;
        }

        public async Task<AuthData> Authenticate(TokenData TokenData, CommonRequest data, string IP = "", string IUserAgent = "", bool IsRefereshCache = false)
        {
            AuthData authData = new AuthData();
            try
            {
                TokenData = await GetDataFromToken(data.Token, data.IsMobile, data.IsSearch, IP, IUserAgent, IsRefereshCache);
                authData.tokenData = TokenData;
                authData.commonRequest = data;

                return authData;
            }
            catch (Exception ex)
            {
                await _exception.Error(ex);
            }
            return authData;
        }

        
        public async Task<TokenData> GetDataFromTokenOLD(string token, int ismobile, int issearch = 0, string IP = "", string IUserAgent = "", bool IsRefereshCache = false)
        {
            TokenData data = new TokenData();

            try
            {
                bool isbase64 = token.IsBase64String();

                #region new token
                if (isbase64)
                {
                    byte[] pass = Encoding.ASCII.GetBytes(_salt);
                    string code3 = null;
                    code3 = Decrypt(token, pass);
                    //string[] parts = code3.Split(new char[] { ':' });
                    string[] parts = ":2:1:0:Agent:0:TECH:5:USD:2000:1".Split(new char[] { ':' });

                    if (parts.Length > 0)
                    {
                        int IsMobileDevice = Convert.ToInt32(fBrowserIsMobile(IUserAgent));
                        IPWhitelistRQ RQ = new IPWhitelistRQ();
                        RQ.IPsList = new List<IPWhitelist>();
                        int RateCategoryId = 0;
                        
                        IPWhitelistRQ listips = await WhiteListAgentIps(Convert.ToInt32(parts[Convert.ToInt32(_MainToken.AgentId)]), RQ, token, issearch, IsRefereshCache);
                        //if (IsMobileDevice == 1 || IP == "203.122.18.162" || IP == "203.122.10.18" || IP == "203.122.10.19" || IP == "103.19.248.44" || IP == "49.248.19.101" || IP == "::1" || IP== "52.210.41.136")// || (IsMobileDevice == 0 && IP == "103.19.248.157"))
                        if (ismobile != 1)
                            ismobile = listips.mainconfig.ismobile;
                        if (listips.IPsList.Count > 0 || ismobile == 1 || IP == "::1")

                        {
                            List<IPWhitelist> ips = listips.IPsList.Where(X => X.IP == IP).ToList();
                            if (ips.Count > 0 || ismobile == 1 || IP == "::1")

                            {
                                string Url = parts[Convert.ToInt32(_MainToken.URL)];
                                int agentid = Convert.ToInt32(parts[Convert.ToInt32(_MainToken.AgentId)]);
                                // = listips.IPsList[0].MapCategoryID;// Convert.ToInt32(parts[Convert.ToInt32(_MainToken.MapCategoryId)]);
                                int parentid = Convert.ToInt32(parts[Convert.ToInt32(_MainToken.ParentId)]);
                                string Type = parts[Convert.ToInt32(_MainToken.UserType)];
                                string subuser = Convert.ToString(parts[Convert.ToInt32(_MainToken.subuserid)]);
                                data.AgentCode = Convert.ToString(parts[Convert.ToInt32(_MainToken.AgentCode)]);
                                data.CreditLimitCurrency = Convert.ToInt32(parts[Convert.ToInt32(_MainToken.CreditLimitCurrency)]);
                                data.CreditLimitCurrencyName = Convert.ToString(parts[Convert.ToInt32(_MainToken.CreditLimitCurrencyName)]);
                                data.ApiTimeOut = Convert.ToInt32(parts[Convert.ToInt32(_MainToken.ApiTimeOut)]); ;
                                data.CompanyId = Convert.ToInt32(parts[Convert.ToInt32(_MainToken.CompanyId)]); ;
                                data.dtROE = listips.mainconfig.dtROE;
                                data.SpeedCategoryId = listips.mainconfig.SpeedCategoryId;
                                data.CitycodeOverpass = listips.mainconfig.CitycodeOverpass;//@HID //160525
                                //DataRow[] dr = listips.mainconfig.dtROE.Select("currencyshortname='" + data.CreditLimitCurrencyName + "'");
                                DtRoeNew dtroe = listips.mainconfig.dtROENew.Where(x => x.currencyCode == data.CreditLimitCurrencyName).First();
                                //CurrencyNames apiCurrecny = await SetCurrencySession(data.CreditLimitCurrencyName, dr);
                                CurrencyNames apiCurrecny = await SetCurrencySessionNew(data.CreditLimitCurrencyName, dtroe);  //051125
                                data.apiCurrency = apiCurrecny;
                                //data.IsAutoVoucher = listips.IPsList[0].IsAutoVoucher;
                                if (Convert.ToString(subuser) == null)
                                {
                                    subuser = "0";
                                }

                                data.SubUserId = Convert.ToInt32(subuser);
                                data.AgentId = listips.RateCategoryId == 0 ? 0 : agentid;
                                {
                                    if (token == "K3XpQoNauwTpD7qJ7dsplEM2npRZM3T97hVdY1P1s4m0CeKKJjRMFgI3UHfmruk+ri9OtNEtzeJTaFmiIQx3Sg==")
                                    {
                                        data.AgentId = 0;
                                        return data;
                                    }
                                }
                                data.RateCategoryId = listips.RateCategoryId;
                                data.URL = Url;
                                data.Type = Type;
                                data.ParentId = parentid;
                                if (issearch == 1)
                                {
                                    data.IsSearchAllow = listips.mainconfig.IsSearchAllow;
                                    data.IsSearchmail = listips.mainconfig.IsSearchMail;
                                }
                                data.SiteCurrency = "USD"; //need to change once sp modify and return in 3rd table
                            }
                        }
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {

                await _exception.Error(ex);
            }

            return data;
        }

        public async Task<TokenData> GetDataFromToken(string token, int ismobile, int issearch = 0, string IP = "", string IUserAgent = "", bool IsRefereshCache = false)
        {
            TokenData data = new TokenData();

            try
            {
                

                //TokenDetails _token = _jwt.getTokenDetails(token);
                TokenDetails _token = getTokenDetailsFromuserContext(_userContextAccessor);


                #region new token
                int IsMobileDevice = Convert.ToInt32(fBrowserIsMobile(IUserAgent));
                IPWhitelistRQ RQ = new IPWhitelistRQ();
                RQ.IPsList = new List<IPWhitelist>();
                int RateCategoryId = 0;

                IPWhitelistRQ listips = await WhiteListAgentIps(Convert.ToInt32(_token.agentId), RQ, Convert.ToString(_token.tenantId), issearch, IsRefereshCache);
                //if (IsMobileDevice == 1 || IP == "203.122.18.162" || IP == "203.122.10.18" || IP == "203.122.10.19" || IP == "103.19.248.44" || IP == "49.248.19.101" || IP == "::1" || IP== "52.210.41.136")// || (IsMobileDevice == 0 && IP == "103.19.248.157"))
                if (ismobile != 1)
                    ismobile = listips.mainconfig.ismobile;
                if (listips.IPsList.Count > 0 || ismobile == 1 || IP == "::1")

                {
                    List<IPWhitelist> ips = listips.IPsList.Where(X => X.IP == IP).ToList();
                    if (ips.Count > 0 || ismobile == 1 || IP == "::1")
                    {
                        //string Url = parts[Convert.ToInt32(_MainToken.URL)];
                        int agentid = Convert.ToInt32(_token.agentId);
                        // = listips.IPsList[0].MapCategoryID;// Convert.ToInt32(parts[Convert.ToInt32(_MainToken.MapCategoryId)]);
                        int parentid = Convert.ToInt32(_token.parentId);
                        data.Type = Convert.ToString(_token.userType);
                        data.ParentId = parentid;//string Type =  "Agent"; // parts[Convert.ToInt32(_MainToken.UserType)];
                        //string Type = _token.UserType.ToString();
                        string subuser = Convert.ToString(_token.subUserId);
                        data.AgentCode = Convert.ToString(_token.agentCode);
                        data.CreditLimitCurrency = Convert.ToInt32(_token.creditLimitCurrencyId);
                        data.CreditLimitCurrencyName = Convert.ToString(_token.creditLimitCurrencyName);
                        data.ApiTimeOut = Convert.ToInt32(2000); ;
                        //data.CompanyId = Convert.ToInt32(parts[Convert.ToInt32(_MainToken.CompanyId)]); 
                        data.TanentId = Convert.ToString(_token.tenantId);
                        data.dtROE = listips.mainconfig.dtROE;
                        data.dtROENew = listips.mainconfig.dtROENew; //051125
                        data.SpeedCategoryId = listips.mainconfig.SpeedCategoryId;
                        data.CitycodeOverpass = listips.mainconfig.CitycodeOverpass;//@HID //160525
                                                                                    //DataRow[] dr = listips.mainconfig.dtROE.Select("currencyshortname='" + data.CreditLimitCurrencyName + "'");
                        DtRoeNew dtroe = listips.mainconfig.dtROENew.Where(x => x.currencyCode.Trim() == data.CreditLimitCurrencyName).First();
                        //CurrencyNames apiCurrecny = await SetCurrencySession(data.CreditLimitCurrencyName, dr);
                        CurrencyNames apiCurrecny = await SetCurrencySessionNew(data.CreditLimitCurrencyName, dtroe);  //051125
                        data.apiCurrency = apiCurrecny;
                        //data.IsAutoVoucher = listips.IPsList[0].IsAutoVoucher;
                        if (Convert.ToString(subuser) == null)
                        {
                            subuser = "0";
                        }

                        data.SubUserId = Convert.ToInt32(subuser);
                        data.AgentId = listips.RateCategoryId == 0 ? 0 : agentid;
                        #region Not Used
                        //{
                        //    if (token == "K3XpQoNauwTpD7qJ7dsplEM2npRZM3T97hVdY1P1s4m0CeKKJjRMFgI3UHfmruk+ri9OtNEtzeJTaFmiIQx3Sg==")
                        //    {
                        //        data.AgentId = 0;
                        //        return data;
                        //    }
                        //}
                        #endregion
                        data.RateCategoryId = listips.RateCategoryId;
                        //data.URL = Url;
                       
                        if (issearch == 1)
                        {
                            data.IsSearchAllow = listips.mainconfig.IsSearchAllow;
                            data.IsSearchmail = listips.mainconfig.IsSearchMail;
                        }
                        data.SiteCurrency = Convert.ToString(_token.siteCurrency); //need to change once sp modify and return in 3rd table
                        data.CanBookWithinCancellation = _token.CanBookWithinCancellation; //Added on 10-JAN-26
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {

                await _exception.Error(ex);
            }

            return data;
        }
        public async Task<CurrencyNames> SetCurrencySessionNew(string currencyname, DtRoeNew dtRoe) //051125
        {
            try
            {
                CurrencyNames objCur = new CurrencyNames();

                if (dtRoe != null)
                {
                    objCur.BuyingCurrencyFactor = Convert.ToDecimal(dtRoe.buyingROE);
                    objCur.SellingCurrencyFactor = Convert.ToDecimal(dtRoe.sellingROE);
                    objCur.CurrencyMarkup = 1;// Convert.ToDecimal(dr[0]["MarkupPercentage"]);
                    objCur.SellingCurrencyMarkup = 1;// Convert.ToDecimal(dr[0]["SellingMarkupPercentage"]);
                    objCur.CurrencyShortname = Convert.ToString(dtRoe.currencyCode);
                    objCur.CurrencyCode = Convert.ToString(dtRoe.currencyId);
                    objCur.CurrencySymbol = ""; // Convert.ToString(dr[0]["CurrencySymbol"]);
                }
                else
                {
                    objCur.BuyingCurrencyFactor = 1;
                    objCur.SellingCurrencyFactor = 1;
                    objCur.CurrencyMarkup = 0;
                    objCur.SellingCurrencyMarkup = 0;
                    objCur.CurrencyShortname = currencyname;
                    objCur.CurrencySymbol = "";

                    try
                    {
                        //Common.getException_v1("currencyname:" + currencyname, "SetCurrencySession", dr.Length.ToString(), "XMLOUT", "", "");
                    }
                    catch (Exception ex)
                    {
                        //Common.getException_v1(ex.ToString(), "SetCurrencySession", Convert.ToString(ex.GetType().Name), "XMLOUT", "", "");
                    }
                }
                return objCur;
            }
            catch (Exception ex)
            {
                await _exception.Error(ex);
            }
            return null;
        }

        public TokenDetails getTokenDetailsFromuserContext(IUserContextAccessor _userContextAccessor) //Added on 10-Jan-26
        {
            TokenDetails _token = new TokenDetails();
            try
            {
                if (_userContextAccessor != null)
                {
                    #region New Code
                    _token.userType = Convert.ToString(_userContextAccessor.DecodedJwt.UserType);
                    _token.subAgentId = 0;
                    _token.parentId = 0;
                    switch (_token.userType.ToLower())
                    {
                        case "agent": //Agent
                            _token.agentId = Convert.ToInt32(_userContextAccessor.DecodedJwt.AgentId);
                            _token.parentId = 0;
                            break;
                        case "subagent": //SubAgent
                            _token.agentId = Convert.ToInt32(_userContextAccessor.DecodedJwt.UserId);
                            _token.parentId = Convert.ToInt32(_userContextAccessor.DecodedJwt.AgentId);
                            break;
                        case "subuser": //SubUser
                            _token.agentId = Convert.ToInt32(_userContextAccessor.DecodedJwt.AgentId);
                            _token.subUserId = Convert.ToInt32(_userContextAccessor.DecodedJwt.SubUserId);
                            break;
                    }

                    _token.creditLimitCurrencyId = Convert.ToInt32(_userContextAccessor.DecodedJwt.CreditLimitCurrencyId);
                    _token.creditLimitCurrencyName = "USD"; // Convert.ToString(jToken["creditlimitCurrencyCode"]?.ToString());
                    _token.mapCategoryId = Convert.ToInt32(_userContextAccessor.DecodedJwt.RateCategoryId);
                    _token.agentCode = Convert.ToString(_userContextAccessor.DecodedJwt.AgentCode);

                    //if (Guid.TryParse(jToken["tenantId"]?.ToString(), out var tenantGuid))
                    {
                        //_token.tenantId = tenantGuid;
                        _token.tenantId = _userContextAccessor.DecodedJwt.TenantId;
                    }
                    _token.siteCurrency = "USD";

                    if(_userContextAccessor.DecodedJwt.PermissionType != null && _userContextAccessor.DecodedJwt.PermissionType.Count > 0)
                    {
                        _token.CanBookWithinCancellation = _userContextAccessor.DecodedJwt.PermissionType.Any(p => p.Equals("Allow booking within cancellation", StringComparison.OrdinalIgnoreCase));
                    }

                    #endregion
                }
            }
            catch (Exception ex)
            {
                _token = null;
            }
            return _token;
        }

        public  bool fBrowserIsMobile(string HTTP_USER_AGENT)
        {
            if (HTTP_USER_AGENT != null)
            {
                var u = HTTP_USER_AGENT.ToString();
                //Common.LogException("IsMobileDevice=" + u);
                if (u.Length < 4)
                    return false;

                if (MobileCheck.IsMatch(u) || MobileVersionCheck.IsMatch(u.Substring(0, 4)))
                    return true;
            }
            return false;
        }

        public async Task<IPWhitelistRQ> WhiteListAgentIps(int AgentId, IPWhitelistRQ list, string TenantId, int IsSearch = 0, bool IsRefereshCache = false)
        {
            try
            {
                IPWhitelistRQ iPWhitelistRQ = new IPWhitelistRQ();
                ConfigMain mainconfig = new ConfigMain();

                for (int i = 0; i < list.IPsList.Count; i++)
                {
                    list.IPsList[i].AgentID = AgentId;
                }
                DataSet ds = new DataSet();

                string keydata = "WhiteListAgentIps_" + Convert.ToString(AgentId);

            

                if (!_cache.TryGetValue(keydata,out ds) || IsRefereshCache)
                {
                    
                    SqlParameter[] para = new SqlParameter[] {
                    new SqlParameter("@agentid",AgentId),
                    new SqlParameter("@TenantId",TenantId),
                    new SqlParameter("@IsSearch",IsSearch),
                    new SqlParameter("@IsLive",Convert.ToInt32(_keySettings.IsLive))
                };

                    ds = await _dlConnection.GetDataSet_Limit("GetWhiteListIP", para, 2);
                    if (ds.Tables[0].Rows.Count > 0)
                    {
                        // Set cache with expiration (example: 12 hours)
                        var cacheEntryOptions = new MemoryCacheEntryOptions()
                            .SetAbsoluteExpiration(DateTime.Now.AddHours(1));  // Set cache to expire in 12 hours

                        _cache.Set(keydata, ds, cacheEntryOptions);
                    }
                }
                //string json = JsonConvert.SerializeObject(ds);
                //ds = JsonConvert.DeserializeObject<DataSet>(json);

                List<IPWhitelist> lst = HtmlExtensions.ToList<IPWhitelist>(ds.Tables[0]);
                if (ds.Tables[1].Rows.Count > 0)
                {
                    iPWhitelistRQ.RateCategoryId = Convert.ToInt32(ds.Tables[1].Rows[0]["MapCategoryID"]);
                    mainconfig.ismobile = Convert.ToInt32(ds.Tables[1].Rows[0]["IsMobile"]);
                    mainconfig.SpeedCategoryId = Convert.ToInt32(ds.Tables[1].Rows[0]["SpeedCategory"]);
                    mainconfig.IsSearchAllow = Convert.ToInt32(ds.Tables[1].Rows[0]["IsAllow"]);
                    mainconfig.IsSearchMail = Convert.ToInt32(ds.Tables[1].Rows[0]["SearchMail"]);
                    mainconfig.CitycodeOverpass = Convert.ToInt32(ds.Tables[1].Rows[0]["CitycodeOverpass"]);//@HID
                }
                iPWhitelistRQ.IPsList = lst;

                #region Call API for ROE
                string keydataRoE = "ROE";
                ApiResponseNew<DtRoeNew> dtRoeNew;
                if (!_cache.TryGetValue(keydataRoE, out dtRoeNew) || IsRefereshCache)
                {
                    dtRoeNew = await _commonMethod.SendRequestAsync<ApiResponseNew<DtRoeNew>>("http://172.10.1.154:8000/api", "Currency/ROE", HttpMethod.Get);

                    if (dtRoeNew != null && dtRoeNew.result != null && dtRoeNew.statusCode == 200 ) 
                    {
                        var cacheEntryOptions = new MemoryCacheEntryOptions()
                            .SetAbsoluteExpiration(DateTime.Now.AddHours(1));  // Set cache to expire in 12 hours

                        _cache.Set(keydataRoE, dtRoeNew, cacheEntryOptions);
                    }


                }
                if (dtRoeNew?.result != null)
                {
                    // Deserialize into your strong type
                    mainconfig.dtROENew = dtRoeNew?.result;
                }

                #endregion
                //if (ds.Tables[2].Rows.Count > 0)
                //    mainconfig.dtROE = ds.Tables[2];

                iPWhitelistRQ.mainconfig = mainconfig;

                return iPWhitelistRQ;
            }
            catch (Exception ex)
            {
                await _exception.Error(ex);
                return null;
            }
        }

        public  async Task<CurrencyNames> SetCurrencySession(string currencyname, DataRow[] dr)
        {
            try
            {
                CurrencyNames objCur = new CurrencyNames();

                if (dr != null && dr.Length > 0)
                {
                    objCur.BuyingCurrencyFactor = Convert.ToDecimal(dr[0]["AmtToExch"]);
                    objCur.SellingCurrencyFactor = Convert.ToDecimal(dr[0]["SellingAmtToExch"]);
                    objCur.CurrencyMarkup = Convert.ToDecimal(dr[0]["MarkupPercentage"]);
                    objCur.SellingCurrencyMarkup = Convert.ToDecimal(dr[0]["SellingMarkupPercentage"]);
                    objCur.CurrencyShortname = Convert.ToString(dr[0]["currencyshortname"]);
                    objCur.CurrencyCode = Convert.ToString(dr[0]["CurrencyId"]);
                    objCur.CurrencySymbol = Convert.ToString(dr[0]["CurrencySymbol"]);
                }
                else
                {
                    objCur.BuyingCurrencyFactor = 1;
                    objCur.SellingCurrencyFactor = 1;
                    objCur.CurrencyMarkup = 0;
                    objCur.SellingCurrencyMarkup = 0;
                    objCur.CurrencyShortname = currencyname;
                    objCur.CurrencySymbol = "";

                    try
                    {
                        //Common.getException_v1("currencyname:" + currencyname, "SetCurrencySession", dr.Length.ToString(), "XMLOUT", "", "");
                    }
                    catch (Exception ex)
                    {
                        //Common.getException_v1(ex.ToString(), "SetCurrencySession", Convert.ToString(ex.GetType().Name), "XMLOUT", "", "");
                    }
                }
                return objCur;
            }
            catch (Exception ex)
            {
                await _exception.Error(ex);
            }
            return null;
        }
        public enum _MainToken
        {
            URL = 0,//0
            AgentId = 1,//1
            MapCategoryId = 2,//2
            ParentId = 3,//3
            UserType = 4,//4
            subuserid = 5,//5
            AgentCode = 6,//6
            CreditLimitCurrency = 7,//7
            CreditLimitCurrencyName = 8,//8
            ApiTimeOut = 9,//9
            CompanyId = 10//10


        }

        public enum UserTypeId //30-Dec-25
        {
            Agent = 1,
            SubAgent = 2, // alias
            SubUser = 4
        }

        public  string Decrypt(string decryptedText, byte[] passwordBytes)
        {
            byte[] bytesToBeDecrypted = Convert.FromBase64String(decryptedText);

            // Hash the password with SHA256
            passwordBytes = SHA256.Create().ComputeHash(passwordBytes);

            byte[] decryptedBytes = AES_Decrypt(bytesToBeDecrypted, passwordBytes);

            // Getting the size of salt
            int saltSize = GetSaltSize(passwordBytes);

            // Removing salt bytes, retrieving original bytes
            byte[] originalBytes = new byte[decryptedBytes.Length - saltSize];
            for (int i = saltSize; i < decryptedBytes.Length; i++)
            {
                originalBytes[i - saltSize] = decryptedBytes[i];
            }

            return Encoding.UTF8.GetString(originalBytes);
        }

        public  byte[] AES_Decrypt(byte[] bytesToBeDecrypted, byte[] passwordBytes)
        {
            byte[] decryptedBytes = null;
            // Set your salt here to meet your flavor:
            byte[] saltBytes = passwordBytes;
            // Example:
            //saltBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

            using (MemoryStream ms = new MemoryStream())
            {
                using (RijndaelManaged AES = new RijndaelManaged())
                {
                    AES.KeySize = 256;
                    AES.BlockSize = 128;

                    var key = new Rfc2898DeriveBytes(passwordBytes, saltBytes, 1000);
                    AES.Key = key.GetBytes(AES.KeySize / 8);
                    AES.IV = key.GetBytes(AES.BlockSize / 8);

                    AES.Mode = CipherMode.CBC;

                    using (CryptoStream cs = new CryptoStream(ms, AES.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(bytesToBeDecrypted, 0, bytesToBeDecrypted.Length);
                        cs.Close();
                    }
                    decryptedBytes = ms.ToArray();
                }
            }

            return decryptedBytes;
        }

        public  int GetSaltSize(byte[] passwordBytes)
        {
            var key = new Rfc2898DeriveBytes(passwordBytes, passwordBytes, 1000);
            byte[] ba = key.GetBytes(2);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ba.Length; i++)
            {
                sb.Append(Convert.ToInt32(ba[i]).ToString());
            }
            int saltSize = 0;
            string s = sb.ToString();
            foreach (char c in s)
            {
                int intc = Convert.ToInt32(c.ToString());
                saltSize = saltSize + intc;
            }

            return saltSize;
        }
    }
}
