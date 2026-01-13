using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using XConnect.CommonUtilities.Interface;
using XConnect.DAL.Interface;
using XConnect.ErrorLogHandler.Interface;
using XConnect.Models;
using static Azure.Core.HttpHeader;
using static XConnect.Models.ErrorLog;

namespace XConnect.CommonUtilities
{
    public class VCNRepo : IVCN
    {
        private readonly IException _exception;
        private readonly KeySettings _keySettings;
        private readonly IDLConnection _dlConnection;
        public static string Developerkey = "-;=)wUZ4!QbRA.AYrq}=KVs4y}MY)(mm"; // Added on 16-MAY-2024


        public VCNRepo(IException exception, IOptions<KeySettings> keySettings, IDLConnection dlConnection)
        {
            _exception = exception;
            _keySettings = keySettings.Value;
            _dlConnection = dlConnection;
        }

        public void GetApisoServiceDatatoReloadVCNDebitNonconfirmedbookings(string ReferenceNo, int BookingId, int BookingDetailId)
        {

            try
            {

                SqlParameter[] para = new SqlParameter[] {
                     new SqlParameter("@ReferenceNo",ReferenceNo),
                    new SqlParameter("@BookingId",BookingId),
                    new SqlParameter("@BookingDetailId",BookingDetailId)
                };

                DataTable dtAPISOData = _dlConnection.GetDataTable("GetAPISO_mail_Services_Final_Autoreload_bookingnonconfirmed", para);
                if (dtAPISOData != null && dtAPISOData.Rows.Count > 0)
                {
                    for (int i = 0; i < dtAPISOData.Rows.Count; i++)
                    {
                        string _vcnName = Convert.ToString(dtAPISOData.Rows[i]["vcnName"]);
                        decimal ROEcalculated = getrateofexchange(Convert.ToInt32(dtAPISOData.Rows[i]["vcCurrencyid"]), Convert.ToDateTime(dtAPISOData.Rows[i]["CreatedDate"]));
                        String utcdatetime = DateTime.UtcNow.ToString();
                        decimal finalamt = 0;
                        decimal calculatedvcamount = 0;
                        finalamt = (Convert.ToDecimal(dtAPISOData.Rows[i]["loadedtotalamount"]));
                        string totalamount = Convert.ToString(finalamt);
                        totalamount = Convert.ToString(Math.Round(Convert.ToDecimal(totalamount), 6));
                        if (_vcnName.ToLower() == "triplink")
                        {
                            #region "Triplink"
                            cardPera _pera = new cardPera();
                            _pera.vcnBookingId = Convert.ToInt32(dtAPISOData.Rows[0]["apisoBookingId"]);
                            _pera.amount = 0;
                            _pera.minAuthAmount = 0;
                            _pera.maxAuthAmount = 0;
                            var jsonstring = Newtonsoft.Json.JsonConvert.SerializeObject(_pera);
                            string _response = getWebResponse(jsonstring, "updateCard");
                            if (!string.IsNullOrEmpty(_response))
                            {
                                DataSet dsResponse = GetDataSetFromResponse(_response);
                                if (dsResponse != null && dsResponse.Tables.Contains("result"))
                                {
                                    if (dsResponse.Tables["result"].Rows.Count > 0)
                                    {
                                        if (Convert.ToString(dsResponse.Tables["result"].Rows[0]["returnCode"]) == "000000")
                                        {
                                            SaveVCNresponse(jsonstring, "success", Convert.ToString(dtAPISOData.Rows[i]["PUB_TOK"]), Convert.ToString(dtAPISOData.Rows[i]["CreatedvcnId"]), Convert.ToInt32(dtAPISOData.Rows[i]["apisoBookingId"]), totalamount, "updateCard" + Convert.ToInt32(dtAPISOData.Rows[0]["apisoBookingId"]), "LOAD_UNLOADcancellednonconfirmed", 0, "", "", "", 0, "D", totalamount, ROEcalculated, 0);
                                            SaveVCNresponse("ActualPriceofVCN", "ActualPriceofVCN", "", "", Convert.ToInt32(dtAPISOData.Rows[i]["apisoBookingId"]), "0", "ActualPriceofVCN", "ActualPriceofVCN", 0, "", "", "", 0, "", "", 0, Convert.ToDecimal(0));
                                        }
                                    }
                                }
                            }
                            #endregion
                        }
                        else if (_vcnName.ToLower() == "apiso")
                        {
                            #region "APISO"
                            #region load immididate

                            /////created vc in inactive state with 1 USD now top up remaning amount
                            string GetAccessTokenLOAD_UNLOAD = Getaccesstokenstring(Convert.ToString(dtAPISOData.Rows[i]["UniqueId"]));
                            //check current and real amount 


                            //end

                            //AMOUNT_CCY//need to add proper currency Id
                            RootObjectLOADUNLOADImmediate obj = new RootObjectLOADUNLOADImmediate();


                            obj.MethodName = "LOAD_UNLOAD";

                            LOADUNLOADImmediate Input = new LOADUNLOADImmediate();
                            Input.AMOUNT = "";
                            Input.AMOUNT_CCY = Convert.ToString(dtAPISOData.Rows[i]["Topupcurrency_id"]);
                            Input.INPUT_ID = Convert.ToString(Convert.ToString(dtAPISOData.Rows[i]["PUB_TOK"]));
                            Input.INPUT_TYPE = "T";
                            Input.SOURCE = "Web Portal";
                            Input.TXN_TYPE = "F"; // D for debit //F means  full debit

                            obj.Input = Input;

                            string jsonStringLOAD_UNLOAD = Newtonsoft.Json.JsonConvert.SerializeObject(obj);
                            string pwsIdLOAD_UNLOAD = Convert.ToString(dtAPISOData.Rows[i]["UniqueId"]);
                            string dataLOAD_UNLOAD = SendRequest("LOAD_UNLOAD", jsonStringLOAD_UNLOAD, GetAccessTokenLOAD_UNLOAD, utcdatetime, pwsIdLOAD_UNLOAD);


                            RootObjectloadunloadresponse loadunloadresponse = Newtonsoft.Json.JsonConvert.DeserializeObject<RootObjectloadunloadresponse>(dataLOAD_UNLOAD);
                            if (Convert.ToInt32(loadunloadresponse.Data.TXN_ID) > 0)
                            {
                                SaveVCNresponse(jsonStringLOAD_UNLOAD, dataLOAD_UNLOAD, Convert.ToString(dtAPISOData.Rows[i]["PUB_TOK"]), Convert.ToString(dtAPISOData.Rows[i]["CreatedvcnId"]), Convert.ToInt32(dtAPISOData.Rows[i]["apisoBookingId"]), totalamount, "LOAD_UNLOADcancellednonconfirmed", "LOAD_UNLOADcancellednonconfirmed", 0, "", "", "", 0, "D", totalamount, ROEcalculated, 0);

                                SaveVCNresponse("ActualPriceofVCN", "ActualPriceofVCN", "", "", Convert.ToInt32(dtAPISOData.Rows[i]["apisoBookingId"]), "0", "ActualPriceofVCN", "ActualPriceofVCN", 0,
                           "", "", "", 0, "", "", 0, Convert.ToDecimal(0)
                           );

                            }
                            #endregion
                            #endregion
                        }
                    }
                }
            }
            catch (WebException ex)
            {
                //ExceptionManager.LogException(ex);
            }
        }
        public void GetApisoServiceDatatoCreateVCN(string ReferenceNo, int BookingId, int BookingDetailId, ref string Pan, ref string CVV, ref string Expiry, bool IsPruvo)
        {
            int apisobookingidlog = 0;
            try
            {
                DataTable dtAPISOData = GetAPISO_mail_Services_Final(ReferenceNo, BookingId, BookingDetailId, IsPruvo);
                if (dtAPISOData != null && dtAPISOData.Rows.Count > 0)
                {
                    for (int i = 0; i < dtAPISOData.Rows.Count; i++)
                    {
                        string _vcnName = Convert.ToString(dtAPISOData.Rows[i]["vcnName"]);
                        apisobookingidlog = Convert.ToInt32(dtAPISOData.Rows[i]["apisoBookingId"]);
                        decimal ROETOUSD = 1;
                        decimal ROEUSDTOVCN = 1;
                        decimal ROEcalculated = getrateofexchange(Convert.ToInt32(dtAPISOData.Rows[i]["vcCurrencyid"]), Convert.ToDateTime(dtAPISOData.Rows[i]["CreatedDate"]));
                        int iscurrentusdprice = 0;
                        int isfullamt = 0;
                        String utcdatetime = DateTime.UtcNow.ToString();
                        if (Convert.ToString(dtAPISOData.Rows[i]["vcCurrencyid"]) == Convert.ToString(_keySettings.UsdCurrencyId))
                        {
                            iscurrentusdprice = 1;
                        }

                        int isactive = 0;
                        string totalamount = "0";
                        string ACTIVE_DATE = "";
                        string AutoReloaddate = "";
                        decimal calculatedvcamount = 0;
                        decimal minAuthAmount = 0;
                        if (Convert.ToInt32(dtAPISOData.Rows[i]["VCnactivtionflag"]) == 1)
                        {
                            isactive = 1;
                            ACTIVE_DATE = "";
                            isfullamt = 1;
                            if (iscurrentusdprice == 0)
                            {
                                decimal ROE = getrateofexchange(Convert.ToInt32(dtAPISOData.Rows[i]["vcCurrencyid"]), Convert.ToDateTime(dtAPISOData.Rows[i]["CreatedDate"]));
                                minAuthAmount = 1 / ROE;
                            }
                            else
                            {
                                minAuthAmount = 1;
                            }
                        }
                        else if (Convert.ToInt32(dtAPISOData.Rows[i]["VCnactivtionflag"]) == 3 && Convert.ToInt32(dtAPISOData.Rows[i]["isautoload"]) == 1)
                        {
                            isactive = 1;
                            if (iscurrentusdprice == 0)
                            {

                                decimal ROE = getrateofexchange(Convert.ToInt32(dtAPISOData.Rows[i]["vcCurrencyid"]), Convert.ToDateTime(dtAPISOData.Rows[i]["CreatedDate"]));
                                calculatedvcamount = 1 / ROE;
                                totalamount = Convert.ToString(calculatedvcamount); //convert here amount to eq 1 usd every currency
                                ROEUSDTOVCN = ROE;
                            }
                            else
                            {
                                totalamount = "1";
                            }
                            minAuthAmount = Convert.ToDecimal(totalamount);
                        }
                        else if (Convert.ToInt32(dtAPISOData.Rows[i]["VCnactivtionflag"]) == 2)
                        {
                            DateTime activedate = Convert.ToDateTime(dtAPISOData.Rows[i]["issueDate"]);
                            ACTIVE_DATE = Convert.ToString(activedate);
                            isfullamt = 1;
                            if (iscurrentusdprice == 0)
                            {
                                decimal ROE = getrateofexchange(Convert.ToInt32(dtAPISOData.Rows[i]["vcCurrencyid"]), Convert.ToDateTime(dtAPISOData.Rows[i]["CreatedDate"]));
                                minAuthAmount = 1 / ROE;
                            }
                            else
                            {
                                minAuthAmount = 1;
                            }
                        }
                        else if (Convert.ToInt32(dtAPISOData.Rows[i]["VCnactivtionflag"]) == 4 && Convert.ToInt32(dtAPISOData.Rows[i]["isautoload"]) == 1)
                        {
                            DateTime activedate = Convert.ToDateTime(dtAPISOData.Rows[i]["issueDate"]);
                            ACTIVE_DATE = Convert.ToString(activedate);
                            if (iscurrentusdprice == 0)
                            {
                                decimal ROE = getrateofexchange(Convert.ToInt32(dtAPISOData.Rows[i]["vcCurrencyid"]), Convert.ToDateTime(dtAPISOData.Rows[i]["CreatedDate"]));
                                calculatedvcamount = 1 / ROE;
                                totalamount = Convert.ToString(calculatedvcamount); //convert here amount to eq 1 usd every currency
                                ROEUSDTOVCN = ROE;
                            }
                            else
                            {
                                totalamount = "1";
                            }
                            minAuthAmount = Convert.ToDecimal(totalamount);
                        }
                        else
                        {

                        }
                        if (totalamount == "0" && Convert.ToInt32(dtAPISOData.Rows[i]["isautoload"]) == 0)
                        {

                            calculatedvcamount = Convert.ToDecimal(dtAPISOData.Rows[i]["BookingAmount"]);
                            decimal ROE = 1;
                            decimal ROESupptoUSD = 1;
                            //safer side need to add patch in currency of card is exists and proper then continue
                            if (Convert.ToInt32(dtAPISOData.Rows[i]["vcCurrencyid"]) == Convert.ToInt32(dtAPISOData.Rows[i]["Currency"]))
                            {
                                ROE = 1;
                                ROESupptoUSD = 1;
                                calculatedvcamount = calculatedvcamount * ROE;
                            }
                            else
                            {
                                ROESupptoUSD = getrateofexchange(Convert.ToInt32(dtAPISOData.Rows[i]["Currency"]), Convert.ToDateTime(dtAPISOData.Rows[i]["CreatedDate"]));
                                calculatedvcamount = calculatedvcamount * ROESupptoUSD;

                                ROE = getrateofexchange(Convert.ToInt32(dtAPISOData.Rows[i]["vcCurrencyid"]), Convert.ToDateTime(dtAPISOData.Rows[i]["CreatedDate"]));
                                calculatedvcamount = calculatedvcamount / ROE;

                                ROETOUSD = ROESupptoUSD;
                                ROEUSDTOVCN = ROE;
                            }
                            totalamount = Convert.ToString(calculatedvcamount);
                        }
                        totalamount = Convert.ToString(Math.Round(Convert.ToDecimal(totalamount), 2));

                        if (_vcnName.ToLower() == "triplink")
                        {
                            #region "TripLink"
                            VCNRepo.cardPera _pera = new VCNRepo.cardPera();
                            _pera.vcnBookingId = apisobookingidlog;
                            _pera.amount = Convert.ToDecimal(totalamount);
                            _pera.minAuthAmount = minAuthAmount;
                            _pera.maxAuthAmount = Convert.ToDecimal(totalamount);
                            var jsonstring = Newtonsoft.Json.JsonConvert.SerializeObject(_pera);

                            string _response = getWebResponse(jsonstring, "createCard");
                            int _status = 0;
                            string _cardLogId = "";
                            if (!string.IsNullOrEmpty(_response))
                            {
                                DataSet dsResponse = GetDataSetFromResponse(_response);
                                if (dsResponse != null && dsResponse.Tables.Contains("result"))
                                {
                                    if (dsResponse.Tables["result"].Rows.Count > 0)
                                    {
                                        if (Convert.ToString(dsResponse.Tables["result"].Rows[0]["returnCode"]) == "000000")
                                        {
                                            _status = 1;
                                            _cardLogId = Convert.ToString(dsResponse.Tables["result"].Rows[0]["cardLogId"]);
                                            string _cardTransactionId = Convert.ToString(dsResponse.Tables["result"].Rows[0]["cardsId"]);
                                            Pan = Convert.ToString(dsResponse.Tables["result"].Rows[0]["cardNum"]);
                                            CVV = Convert.ToString(dsResponse.Tables["result"].Rows[0]["cvv2"]);
                                            Expiry = Convert.ToString(dsResponse.Tables["result"].Rows[0]["cardExpirationDate"]);
                                            int Bookingstatus = Convert.ToInt32(dtAPISOData.Rows[i]["Bookingstatus"]);
                                            SaveVCNresponse(jsonstring, "success", _cardLogId, _cardTransactionId, Convert.ToInt32(dtAPISOData.Rows[i]["apisoBookingId"]), totalamount, "CREATE_VCN" + _cardLogId, "CREATE_VCN", isfullamt,
                                                "", "", "", Bookingstatus, "", "", ROEcalculated, 0);

                                            try
                                            {
                                                SaveROEAPISO(Convert.ToInt32(dtAPISOData.Rows[i]["apisoBookingId"]), ROETOUSD, ROEUSDTOVCN, "CreateVCN");
                                            }
                                            catch (System.Exception ex)
                                            {

                                            }


                                            SaveVCNresponse("ActualPriceofVCN", "ActualPriceofVCN", "", "", apisobookingidlog, "0", "ActualPriceofVCN", "ActualPriceofVCN", 0,
                                              "", "", "", 0, "", "", 0, Convert.ToDecimal(totalamount));
                                        }
                                    }
                                }
                            }
                            if (_status == 0)
                            {
                                SaveVCNresponse(jsonstring, _response, "", "", apisobookingidlog, "0", "CREATE_VCNeroor" + _cardLogId, "CREATE_VCNeroor", 0,
                               "", "", "", 0, "", "", ROEcalculated, 0
                               );
                                continue;
                            }
                            #endregion
                        }
                        else if (_vcnName.ToLower() == "apiso")
                        {
                            #region "Apiso"
                            string GetAccessToken = Getaccesstokenstring(Convert.ToString(dtAPISOData.Rows[i]["UniqueId"]));

                            string parentaccount = "0";
                            string new_productID = Convert.ToString(_keySettings.PRODUCT_ID);

                            if (Convert.ToString(dtAPISOData.Rows[i]["vcCurrencyid"]) == Convert.ToString(_keySettings.UsdCurrencyId))
                            {
                                parentaccount = Convert.ToString(_keySettings.WITHINEARTHHOLIUSD);
                                iscurrentusdprice = 1;
                            }
                            else if (Convert.ToString(dtAPISOData.Rows[i]["vcCurrencyid"]) == Convert.ToString(_keySettings.SarCurrecnyId))
                            {
                                parentaccount = Convert.ToString(_keySettings.WITHINEARTHHOLISAR);
                                new_productID = Convert.ToString(_keySettings.PRODUCT_ID_SAR);
                            }
                            else if (Convert.ToString(dtAPISOData.Rows[i]["vcCurrencyid"]) == Convert.ToString(_keySettings.AEDCurrencyId))
                            {
                                parentaccount = Convert.ToString(_keySettings.WITHINEARTHHOLIAED);
                            }
                            else if (Convert.ToString(dtAPISOData.Rows[i]["vcCurrencyid"]) == Convert.ToString(_keySettings.EURCurrencyId))
                            {
                                parentaccount = Convert.ToString(_keySettings.WITHINEARTHHOLIEUR);
                            }
                            else
                            {

                            }

                            #region immidiate Request for VC

                            if (ACTIVE_DATE != "")
                            {
                                ACTIVE_DATE = ""; //Convert.ToDateTime(ACTIVE_DATE).ToString("MM/dd/yyyy");
                            }




                            //check vcn amount proper
                            //bool istrue = istrueVC(Convert.ToString(Convert.ToString(dtAPISOData.Rows[i]["PUB_TOK"])), GetAccessToken, utcdatetime, Convert.ToString(dtAPISOData.Rows[i]["UniqueId"]));
                            //end vcn amount check

                            RootObjectvc rbj = new RootObjectvc();
                            rbj.MethodName = "CREATE_VCN";
                            Input _cvc = new Input();


                            _cvc.PARENT_ACCOUNT = parentaccount;
                            _cvc.IMAGE_CREATE = "011111";
                            _cvc.EMBOSSED_NAME = Convert.ToString(dtAPISOData.Rows[i]["SupplierName"]);
                            _cvc.FIRST_NAME = Convert.ToString(dtAPISOData.Rows[i]["FirstName"]);
                            _cvc.LAST_NAME = Convert.ToString(dtAPISOData.Rows[i]["FirstName"]);
                            _cvc.PRODUCT_ID = new_productID;//Convert.ToString(ConfigurationSettings.AppSettings["PRODUCT_ID"]);
                            _cvc.REF_ID = Convert.ToString(dtAPISOData.Rows[i]["UniqueId"]);


                            _cvc.AMOUNT = totalamount;
                            _cvc.IS_ACTIVE = Convert.ToString(1);
                            _cvc.ACTIVE_DATE = Convert.ToString(ACTIVE_DATE);

                            rbj.Input = _cvc;

                            string jsonStringRequest = Newtonsoft.Json.JsonConvert.SerializeObject(rbj);

                            string pwsId = Convert.ToString(dtAPISOData.Rows[i]["UniqueId"]);

                            string jsonStringResponse = SendRequest("CREATE_VCN", jsonStringRequest, GetAccessToken, utcdatetime, pwsId);
                            RootObjectcreateVCResponse createVCN = Newtonsoft.Json.JsonConvert.DeserializeObject<RootObjectcreateVCResponse>(jsonStringResponse);
                            if (createVCN.Data.PUB_TOK == null)
                            {
                                SaveVCNresponse(jsonStringRequest, jsonStringResponse, "", "", apisobookingidlog, "0", "CREATE_VCNeroor", "CREATE_VCNeroor", 0,
                                "", "", "", 0, "", "", ROEcalculated, 0
                                );
                                continue;

                            }

                            Pan = DecryptAES(Developerkey, createVCN.Data.PAN);
                            CVV = Convert.ToString(DecryptAES(Developerkey, createVCN.Data.CVV));
                            Expiry = Convert.ToString(DecryptAES(Developerkey, createVCN.Data.EXPIRY));

                            int Bookingstatus = Convert.ToInt32(dtAPISOData.Rows[i]["Bookingstatus"]);

                            SaveVCNresponse(jsonStringRequest, "Success", createVCN.Data.PUB_TOK, createVCN.Data.VCN_ID, Convert.ToInt32(dtAPISOData.Rows[i]["apisoBookingId"]), totalamount, "CREATE_VCN", "CREATE_VCN", isfullamt,
                                "", "", "", Bookingstatus, "", "", ROEcalculated, 0
                                );


                            #endregion

                            try
                            {
                                SaveROEAPISO(Convert.ToInt32(dtAPISOData.Rows[i]["apisoBookingId"]), ROETOUSD, ROEUSDTOVCN, "CreateVCN");
                            }
                            catch (System.Exception ex)
                            {


                            }


                            SaveVCNresponse("ActualPriceofVCN", "ActualPriceofVCN", "", "", apisobookingidlog, "0", "ActualPriceofVCN", "ActualPriceofVCN", 0,
                              "", "", "", 0, "", "", 0, Convert.ToDecimal(totalamount)
                              );
                            #endregion
                        }



                    }
                }
            }
            catch (WebException ex)
            {
                SaveVCNresponse(ex.ToString(), "error", "", "", apisobookingidlog, "0", "CREATE_VCNeroor", "CREATE_VCNeroor", 0,
                           "", "", "", 0, "", "", 0, 0
                           );
                _ = _exception.ErrorBooking(ex, "", "CreateCard");
            }
        }

        public DataTable GetAPISO_mail_Services_Final(string ReferenceNo, int BookingId, int BookingDetailId, bool IsPruvo)
        {
            try
            {
                SqlParameter[] para = new SqlParameter[] {
                    new SqlParameter("@ReferenceNo",ReferenceNo),
                    new SqlParameter("@BookingId",BookingId),
                    new SqlParameter("@BookingDetailId",BookingDetailId),
                    new SqlParameter("@IsRebook",IsPruvo)//Added on 6-JUN-2024
                };


                DataTable dtAPISOData = _dlConnection.GetDataTable("GetAPISO_mail_Services_Final", para);
                return dtAPISOData;
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorNew(ex, _LogAPI.Book);
                return null;
            }

        }

        public decimal getrateofexchange(int currencyid, DateTime bookingdate)
        {
            try
            {
                SqlParameter[] para = new SqlParameter[] {

                    new SqlParameter ("@currencyid",currencyid),
                    new SqlParameter ("@bookingdate",bookingdate)
                };

                DataTable dtAPISOData = _dlConnection.GetDataTable("GetRoEpreviousdateforVC", para);
                return Convert.ToDecimal(dtAPISOData.Rows[0]["TotalAmtToExch"]);
            }
            catch (System.Exception ex)
            {
                 _ = _exception.ErrorBooking(ex, "");
                throw;
            }
        }

        public string getWebResponse(string payload, string apiname)
        {
            try
            {
                string weburl = Convert.ToString(_keySettings.VCNAPI_EndPoint) + "/" + apiname;
                WebRequest req = WebRequest.Create(weburl);
                req.ContentType = "application/json";
                req.Method = "POST";
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                string data = payload;

                Stream reqStr = req.GetRequestStream();
                StreamWriter sw = new StreamWriter(reqStr);
                sw.Write(data);
                sw.Close();
                string strResult = "";

                try
                {
                    WebResponse res = req.GetResponse();
                    Stream resStr = res.GetResponseStream();
                    StreamReader sr = new StreamReader(resStr);
                    strResult = sr.ReadToEnd();
                    sr.Close();
                    res.Close();
                }
                catch (WebException ex)
                {
                    string errMessage = ex.Message;
                    strResult = errMessage;
                    if (ex.Response != null)
                    {
                        System.IO.Stream resStr = ex.Response.GetResponseStream();
                        System.IO.StreamReader sr = new System.IO.StreamReader(resStr);
                        string soapResponse = sr.ReadToEnd();
                        sr.Close();
                        resStr.Close();
                        errMessage += Environment.NewLine + soapResponse;
                        strResult = errMessage;
                        ex.Response.Close();
                    }
                }
                return strResult;
            }
            catch (System.Exception ex)
            {
                // Common.Exception.LogException("getWebResponse", ex.ToString());
                return "";
            }
        }

        public DataSet GetDataSetFromResponse(string response)
        {
            string responseXML = Newtonsoft.Json.JsonConvert.DeserializeXmlNode(response, "root").InnerXml;
            DataSet Response = new DataSet();
            byte[] buffer = Encoding.UTF8.GetBytes(responseXML);
            using (MemoryStream stream = new MemoryStream(buffer))
            {
                System.Xml.XmlReader reader = System.Xml.XmlReader.Create(stream);
                Response.ReadXml(reader);
            }
            return Response;
        }
        public class cardPera
        {
            public int vcnBookingId { get; set; }
            public decimal amount { get; set; }
            public decimal minAuthAmount { get; set; }
            public decimal maxAuthAmount { get; set; }
        }

        #region load unload
        public class LOADUNLOADImmediate
        {
            public string AMOUNT { get; set; }
            public string AMOUNT_CCY { get; set; }
            public string INPUT_ID { get; set; }
            public string INPUT_TYPE { get; set; }
            public string SOURCE { get; set; }
            public string TXN_TYPE { get; set; }
        }

        public class RootObjectLOADUNLOADImmediate
        {
            public LOADUNLOADImmediate Input { get; set; }
            public string MethodName { get; set; }
        }
        #endregion

        #region load unload response

        public class Dataloadunloadresponse
        {
            public string TXN_ID { get; set; }
        }

        public class RootObjectloadunloadresponse
        {
            public Dataloadunloadresponse Data { get; set; }
            public List<object> DataTable { get; set; }
            public string Status { get; set; }
        }

        #endregion

        public void SaveVCNresponse(string request, string response, string PUB_TOK, string VCN_ID, int apisoBookingId, string totalamount, string apiname, string operationtype, int isfullamt, string PAN, string CVV, string EXpiry, int Bookingstatus, string CreditorDebit, string totalamountCreditorDebit, decimal ROEcalculated, decimal ActualPriceofVCN)
        {
            try
            {
                if (totalamount == "")
                {
                    totalamount = "0";
                }

                if (totalamountCreditorDebit == "")
                {
                    totalamountCreditorDebit = "0";
                }

                SqlParameter[] para = new SqlParameter[] {
                new SqlParameter("@request",request),
                    new SqlParameter("@response",response),
                     new SqlParameter("@PUB_TOK",PUB_TOK),
                      new SqlParameter("@VCN_ID",VCN_ID),
                      new SqlParameter("@apisoBookingId",apisoBookingId),
                      new SqlParameter("@totalamount",Convert.ToDecimal(totalamount)),
                      new SqlParameter("@apiname",Convert.ToString(apiname)),
                      new SqlParameter("@operationtype",Convert.ToString(operationtype)),
                      new SqlParameter("@isfullamt",Convert.ToString(isfullamt)),
                       new SqlParameter("@PAN",Convert.ToString(PAN)),
                      new SqlParameter("@CVV",Convert.ToString(CVV)),
                      new SqlParameter("@EXpiry",Convert.ToString(EXpiry)),
                       new SqlParameter("@Bookingstatus",Convert.ToInt32(Bookingstatus)),
                       new SqlParameter("@CreditorDebit",Convert.ToString(CreditorDebit)),
                       new SqlParameter("@totalamountCreditorDebit",Convert.ToDecimal(totalamountCreditorDebit)),
                        new SqlParameter("@ROEcalculated",Convert.ToDecimal(ROEcalculated)),
                         new SqlParameter("@ActualPriceofVCN",Convert.ToDecimal(ActualPriceofVCN)),
                };
                DataTable dtAPISOlog = _dlConnection.GetDataTable("SaveAPISOLOG", para);
            }
            catch (System.Exception ex)
            {
                _ = _exception.ErrorBooking(ex, "");
                //throw; need to catch log here
            }
        }

        public void SaveROEAPISO(int apisoBookingId, decimal ROETOUSD, decimal ROEUSDTOVCN, string isfrom)
        {
            try
            {
                SqlParameter[] para = new SqlParameter[] {
                     new SqlParameter("@apisoBookingId",apisoBookingId),
                    new SqlParameter("@ROETOUSD",ROETOUSD),
                    new SqlParameter("@ROEUSDTOVCN",ROEUSDTOVCN),
                    new SqlParameter("@isfrom",Convert.ToString(isfrom))
                };

                DataTable dtAPISOData = _dlConnection.GetDataTable("InsertROEForAPISO", para);
            }
            catch (Exception ex)
            {
                _ = _exception.ErrorBooking(ex, "");
            }
            
        }

        public string Getaccesstokenstring(string randno)
        {
            String utcdatetime = DateTime.UtcNow.ToString();
            string pwsId = randno;
            string inputvalue = (DateTime.Parse(utcdatetime) - DateTime.Parse("01 Jan 2015 00:00:00")).TotalSeconds + pwsId.ToString();
            string GetAccessTokenfinalstring = EncryptAES(Developerkey, inputvalue);
            return GetAccessTokenfinalstring;
        }

        public static string EncryptAES(string key, string input)
        {
            //if (input.Length % 2 == 1)
            //{
            // input = input + "0";
            //}
            byte[] keyBuffer = StringToByteArray(key);
            // byte[] inputBuffer = StringToByteArray(getHexString(input));
            byte[] inputBuffer = Encoding.ASCII.GetBytes(input);
            byte[] cipherbuffer = AESEncrypt(inputBuffer, keyBuffer);
            string s = BitConverter.ToString(cipherbuffer).Replace("-", "");
            return BitConverter.ToString(cipherbuffer).Replace("-", "");
        }

        private static byte[] StringToByteArray(string hex)
        {
            if ((hex.Length % 2) == 1)
            {
                throw new System.Exception("The binary key or input text cannot have an odd number of digits");
            }
            byte[] buffer = new byte[hex.Length >> 1];
            for (int i = 0; i < (hex.Length >> 1); i++)
            {
                buffer[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + GetHexVal(hex[(i << 1) + 1]));
            }
            return buffer;
        }

        private static byte[] AESEncrypt(byte[] original, byte[] key)
        {
            AesCryptoServiceProvider cryptoProvider = new AesCryptoServiceProvider();
            cryptoProvider.Key = key;
            cryptoProvider.Mode = CipherMode.ECB;
            cryptoProvider.Padding = PaddingMode.Zeros;
            ICryptoTransform transform = cryptoProvider.CreateEncryptor();
            return transform.TransformFinalBlock(original, 0, original.Length);
        }
        private static int GetHexVal(char hex)
        {
            int num = hex;
            return (num - ((num < 58) ? 48 : 55));
        }

        public string SendRequest(string apiName, string payload, string WRQCcode, string utcdatetime, string pwsId)
        {
            //string apiUrl = Convert.ToString(ConfigurationManager.AppSettings["APISOAPIURL"]);

            //WebRequest req = WebRequest.Create(apiUrl);

            //req.Headers.Add("APP_ID", "0E71D5DC9C4648EA976FBF3DF4697887");
            //req.Headers.Add("CHANNEL_ID", "01");
            //req.Headers.Add("PWS_ID", pwsId.ToString());
            //req.Headers.Add("KEY_ID", "5");
            //req.Headers.Add("UTC_DATETIME", utcdatetime);
            //req.Headers.Add("WRQC", WRQCcode);
            //req.Headers.Add("ContentType", "application/json");

            string apiUrl = Convert.ToString(_keySettings.APISOAPIURL);
            WebRequest req = WebRequest.Create(apiUrl);

            req.Headers.Add("APP_ID", Convert.ToString(_keySettings.APP_ID));
            req.Headers.Add("CHANNEL_ID", Convert.ToString(_keySettings.CHANNEL_ID));
            req.Headers.Add("PWS_ID", pwsId.ToString());
            req.Headers.Add("KEY_ID", Convert.ToString(_keySettings.KEY_ID));
            req.Headers.Add("UTC_DATETIME", utcdatetime);
            req.Headers.Add("WRQC", WRQCcode);

            req.ContentType = "application/json";
            req.Method = "POST";

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            string data = payload;



            Stream reqStr = req.GetRequestStream();
            StreamWriter sw = new StreamWriter(reqStr);
            sw.Write(data);
            sw.Close();
            string strResult = "";

            try
            {
                WebResponse res = req.GetResponse();
                Stream resStr = res.GetResponseStream();
                StreamReader sr = new StreamReader(resStr);
                strResult = sr.ReadToEnd();

                sr.Close();

                res.Close();
            }
            catch (WebException ex)
            {
                string errMessage = ex.Message;
                strResult = errMessage;
                if (ex.Response != null)
                {
                    System.IO.Stream resStr = ex.Response.GetResponseStream();
                    System.IO.StreamReader sr = new System.IO.StreamReader(resStr);
                    string soapResponse = sr.ReadToEnd();
                    sr.Close();
                    resStr.Close();

                    errMessage += Environment.NewLine + soapResponse;
                    strResult = errMessage;

                    ex.Response.Close();
                }
            }



            return strResult;
        }

        public string DecryptAES(string key, string input)
        {
            //if (input.Length % 2 == 1)
            //{
            //    input = input + "0";
            //}
            byte[] keyBuffer = StringToByteArray(key);
            byte[] inputBuffer = StringToByteArray(input);
            byte[] cipherbuffer = AESDecrypt(inputBuffer, keyBuffer);
            //return BitConverter.ToString(cipherbuffer).Replace("-", "");
            return System.Text.Encoding.ASCII.GetString(cipherbuffer).Trim('\0');

        }

        private static byte[] AESDecrypt(byte[] original, byte[] key)
        {
            AesCryptoServiceProvider cryptoProvider = new AesCryptoServiceProvider();
            cryptoProvider.Key = key;
            cryptoProvider.Mode = CipherMode.ECB;
            cryptoProvider.Padding = PaddingMode.Zeros;
            ICryptoTransform transform = cryptoProvider.CreateDecryptor();
            return transform.TransformFinalBlock(original, 0, original.Length);
        }

        #region create vc
        public class Input
        {
            public string PARENT_ACCOUNT { get; set; }
            public string AMOUNT { get; set; }
            public string IMAGE_CREATE { get; set; }
            public string EMBOSSED_NAME { get; set; }
            public string FIRST_NAME { get; set; }
            public string LAST_NAME { get; set; }
            public string PRODUCT_ID { get; set; }
            public string REF_ID { get; set; }
            public string IS_ACTIVE { get; set; }
            public string ACTIVE_DATE { get; set; }
        }

        public class RootObjectvc
        {
            public string MethodName { get; set; }
            public Input Input { get; set; }
        }

        public class createVCResponse
        {
            public string PUB_TOK { get; set; }
            public string VCN_IMAGE { get; set; }
            public string ACCOUNT_ID { get; set; }
            public string CUSTOMER_ID { get; set; }
            public string PAN { get; set; }
            public string CVV { get; set; }
            public string EXPIRY { get; set; }
            public string EMBOSSED_NAME { get; set; }
            public string VCN_ID { get; set; }
        }

        public class RootObjectcreateVCResponse
        {
            public createVCResponse Data { get; set; }
            public List<object> DataTable { get; set; }
            public string Status { get; set; }
        }

        #endregion


        public void updateCardOldData_ReBooking(string referenceNo, int bookingId, int detailId)
        {
            try
            { // Note: Uncomment from Master also if you uncomment below

                //DLConnection _db = new DLConnection();
                //SqlParameter[] para = new SqlParameter[] {
                //        new SqlParameter("@referenceNo",referenceNo),
                //        new SqlParameter("@bookingId",bookingId),
                //        new SqlParameter("@detailId",detailId),
                //    };
                //_db.ExecuteNonQuery("updateCardOldData_ReBooking", para);

            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
            }
        }

        public void updateCardOldData_Fail_ReBooking(string referenceNo, int bookingId, int detailId)
        {
            try
            {
                //DLConnection _db = new DLConnection();
                //SqlParameter[] para = new SqlParameter[] {
                //        new SqlParameter("@referenceNo",referenceNo),
                //        new SqlParameter("@bookingId",bookingId),
                //        new SqlParameter("@detailId",detailId),
                //    };
                //_db.ExecuteNonQuery("updateCardOldData_Fail_ReBooking", para);

            }
            catch (Exception ex)
            {
                _ = _exception.Error(ex);
            }
        }
    }
}
