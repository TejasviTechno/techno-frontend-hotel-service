using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using XConnect.CommonUtilities.Interface;
using XConnect.ErrorLogHandler.Interface;
using XConnect.Models;
using static XConnect.Models.CacheMaster;
using Options = XConnect.Models.Options;

namespace XConnect.CommonUtilities
{
    public class CacheMasterRepo : ICacheMaster
    {
        private readonly IException _exception;
        private readonly ICommonMethod _commonMethod;
        private readonly MongoSettings _mongoSettings;
        private readonly KeySettings _keySettings;

        private readonly IMongoDatabase _main1;
        private readonly IMongoDatabase _main2;
        private readonly IMongoDatabase _main3;
        //Below Comment on 12NOV2025
        //private readonly IMongoDatabase _supp;
        //private readonly IMongoDatabase _roomMapping;
        //private readonly IMongoDatabase _QRSLog;
        //private readonly IMongoDatabase _SPRLog;

        public CacheMasterRepo(IException exception, ICommonMethod commonMethod, IOptions<MongoSettings> mongoSettings, IOptions<KeySettings> keySettings, MongoDbFactoryNew factory)
        {
            _exception = exception;
            _commonMethod = commonMethod;
            _mongoSettings = mongoSettings.Value;
            _keySettings = keySettings.Value;
            _main1 = factory.GetDatabase("Main1");
            _main2 = factory.GetDatabase("Main2");
            _main3 = factory.GetDatabase("Main3");

            //Below Comment on 12NOV2025
            //_supp = factory.GetDatabase("SuppLog");
            //_roomMapping = factory.GetDatabase("RoomMapping");
            //_QRSLog = factory.GetDatabase("QRSLog");
            //_SPRLog = factory.GetDatabase("SPR");
        }



        public int UpdateCacheForHotelListMaster_mongodb(int giataSequence, string filepath, string Extension, string NewSearchKey, TokenData Tokendata, ref AvailHotelList finalResponse, int operation, CacheRoomToken cacheRoomToken)//#MongoDB
        {
            try
            {
                bool isError = false;
                bool error = false;
                string folderPath = GenerateCacheFilePath(Tokendata);
                int Group = (giataSequence / CacheKey_Devider);

                filepath = folderPath + "\\" + filepath + "_" + (Group + 1) + Extension;

                if (operation == Convert.ToInt32(_CacheOperation.update))
                {
                    if (!Directory.Exists(folderPath))
                        Directory.CreateDirectory(folderPath);
                    try
                    {
                        string json = Newtonsoft.Json.JsonConvert.SerializeObject(finalResponse);

                        if (_mongoSettings.MongoDB_Active_for_Cachefile == "1")
                        {
                            //Common.UpdateDataToFile_MongoDB(finalResponse, NewSearchKey, cacheRoomToken);//#MongoDB
                            var filter1 = Builders<AvailHotelList>.Filter;
                            var filternew = filter1.And(
                               filter1.Eq(x => x.Search_Key, NewSearchKey));


                            var update = Builders<AvailHotelList>.Update;
                            var updatenew = update.Set(x => x.AvailabilityRS.HotelResult[cacheRoomToken.giataid_Seq].HotelOption[cacheRoomToken.hotelid_Seq].HotelRooms[cacheRoomToken.Room_seqno][0], finalResponse.AvailabilityRS.HotelResult[cacheRoomToken.giataid_Seq].HotelOption[cacheRoomToken.hotelid_Seq].HotelRooms[cacheRoomToken.Room_seqno][0]);

                            #region Read from MongoDB1 //Added 25-feb-2025
                            try
                            {
                                var collection = _main1.GetCollection<AvailHotelList>(_mongoSettings.MongoDB_CollectionName); //300425
                                collection.UpdateOne(filternew, updatenew);//
                            }
                            catch (Exception x)
                            {
                                _exception.Error(x, NewSearchKey);
                            }
                            #endregion
                            #region Read from MongoDB2 //Added 25-feb-2025
                            try
                            {
                                //var client = new MongoClient(ConfigurationManager.AppSettings["MongoDB_ConnectionStr2"]);
                                //var database = client.GetDatabase(ConfigurationManager.AppSettings["MongoDB_DatabaseName"]);
                                var collection = _main2.GetCollection<AvailHotelList>(_mongoSettings.MongoDB_CollectionName);
                                collection.UpdateOne(filternew, updatenew);//
                            }
                            catch (Exception x)
                            {
                                _exception.Error(x, NewSearchKey);
                            }
                            #endregion
                            #region Read from MongoDB3 //Added 25-feb-2025
                            try
                            {
                                //var client = new MongoClient(ConfigurationManager.AppSettings["MongoDB_ConnectionStr3"]);
                                //var database = client.GetDatabase(ConfigurationManager.AppSettings["MongoDB_DatabaseName"]);
                                var collection = _main3.GetCollection<AvailHotelList>(_mongoSettings.MongoDB_CollectionName);
                                collection.UpdateOne(filternew, updatenew);//
                            }
                            catch (Exception x)
                            {
                                _exception.Error(x, NewSearchKey);
                            }
                            #endregion
                            #region Read from MongoDB old //Added 25-feb-2025
                            //try
                            //{
                            //    var client = new MongoClient(ConfigurationManager.AppSettings["MongoDB_ConnectionStr"]);
                            //    var database = client.GetDatabase(ConfigurationManager.AppSettings["MongoDB_DatabaseName"]);
                            //    var collection = database.GetCollection<AvailHotelList>(ConfigurationManager.AppSettings["MongoDB_CollectionName"]);
                            //    collection.UpdateOne(filternew, updatenew);//
                            //}
                            //catch (Exception x)
                            //{
                            //    Common.Error(x, NewSearchKey);
                            //}
                            #endregion
                        }
                        else
                        {
                            error = _commonMethod.WriteDataToFile(filepath, json);
                        }
                        if (error)
                            isError = true;
                    }
                    catch (Exception ex)
                    {
                        _exception.Error(ex);
                        isError = true;
                    }

                    //HttpRuntime.Cache.Insert(NewSearchKey + NewSearchKey, NewSearchKey, null, DateTime.Now.AddHours(24), TimeSpan.Zero);
                    return Convert.ToInt32(_CacheError.success);
                }


                if (isError)
                    return Convert.ToInt32(_CacheError.error);
                else
                    return Convert.ToInt32(_CacheError.success);

            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return Convert.ToInt32(_CacheError.error);
            }

        }

        public Options LoadHotelfromMainCache_mongodb(ref AvailHotelList finalResponse, string SearchKey, TokenData tokenData, string OwnsystemHotelId, string SupplierID, int giataSequence, int HotelSequence, ref int giataid_Seq, ref int hotelid_Seq, ref int status)
        {
            Options hoteloption = new Options();

            try
            {
                bool isFound = false;
                status = ReadCacheForHotelListMaster_mongodb(SearchKey, CacheKey_Extension, giataSequence, SearchKey, tokenData, ref finalResponse, Convert.ToInt32(_CacheOperation.read));
                //finalResponse = null;
                int Group = (giataSequence / CacheKey_Devider);
                int TempgiataSequence = giataSequence - ((Group + 1) * CacheMaster.CacheKey_Devider);
                int temp = Group * CacheMaster.CacheKey_Devider;
                if (finalResponse != null)
                {
                    if (finalResponse.AvailabilityRS != null)
                    {
                        if (finalResponse.AvailabilityRS.HotelResult != null)
                        {
                            for (int i = 0; i < finalResponse.AvailabilityRS.HotelResult.Count; i++)
                            {
                                if (isFound) break;
                                for (int j = 0; j < finalResponse.AvailabilityRS.HotelResult[i].HotelOption.Count; j++)
                                {
                                    if (isFound) break;
                                    _HotelList hotel = finalResponse.AvailabilityRS.HotelResult[i].HotelOption[j].HotelOptionId.DecryptHotelToken();
                                    string ownhotelid = hotel.OwnsystemHotelId;
                                    if (ownhotelid == OwnsystemHotelId && hotel.SupplierId == SupplierID && HotelSequence == j)
                                    {
                                        hoteloption = finalResponse.AvailabilityRS.HotelResult[i].HotelOption[j]; isFound = true;
                                        giataid_Seq = temp;
                                        hotelid_Seq = j;
                                        break;
                                    }
                                }
                                temp++;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
            return hoteloption;
        }

        public int ReadCacheForHotelListMaster_mongodb(string filepath, string Extension, int giataSequence, string NewSearchKey, TokenData Tokendata, ref AvailHotelList finalResponse, int operation)//#MongoDB
        {
            try
            {
                bool isError = false;
                string folderPath = GenerateCacheFilePath(Tokendata);
                int Group = (giataSequence / CacheKey_Devider);

                filepath = folderPath + "\\" + filepath + "_" + (Group + 1) + Extension;

                //filepath = folderPath + "\\" + NewSearchKey + ".json";
                long tickNow = DateTime.Now.Ticks;
                long tickOnkey = Convert.ToInt64(NewSearchKey.Split('_')[2].ReverseString());
                DateTime dtNow = new DateTime(tickNow);
                DateTime dtSearch = new DateTime(tickOnkey);

                TimeSpan timeSpan = dtNow - dtSearch;
                int cacheTimeout = Convert.ToInt32(_keySettings.CacheTimeOut);
                //using (new WNetConnection(folderPath, "administrator", "}50ojZAuCv>6LP}s^)xX"))
                {
                    if (timeSpan.TotalMinutes > cacheTimeout && operation == Convert.ToInt32(_CacheOperation.read))
                    {
                        if (File.Exists(filepath))
                        {
                            string str = _commonMethod.ReadCacheFile(filepath, ref isError);
                            // if (!isError)
                            //   File.Delete(filepath);
                        }
                        return Convert.ToInt32(_CacheError.expired);
                    }


                    if (operation == Convert.ToInt32(_CacheOperation.read))
                    {
                        if (_mongoSettings.MongoDB_Active_for_Cachefile == "1") //Read from MongoDB
                        {
                            //finalResponse = Common.ReadCacheFile_MongoDB(NewSearchKey);//#MongoDB
                            var Data = new AvailHotelList1();
                            try//check on MongoDB1 //Added 25-feb-2025
                            {
                                var collection = _main1.GetCollection<AvailHotelList1>(_mongoSettings.MongoDB_CollectionName); //added new
                                Data = collection.Find(x => x.Search_Key == NewSearchKey).FirstOrDefault();
                            }
                            catch (Exception x)
                            {
                                _exception.Error(x);
                            }
                            if (Data == null)//check on MongoDB2 //Added 25-feb-2025
                            {
                                try
                                {
                                    //var client = new MongoClient(_mongoSettings.MongoDB_ConnectionStr2);
                                    // var database = client.GetDatabase(_mongoSettings.MongoDB_DatabaseName);
                                    var collection = _main2.GetCollection<AvailHotelList1>(_mongoSettings.MongoDB_CollectionName);
                                    Data = collection.Find(x => x.Search_Key == NewSearchKey).FirstOrDefault();
                                }
                                catch (Exception x)
                                {
                                    _exception.Error(x);
                                }
                                //Data = _searchlogs_read2.Find(x => (x.Search_Key == NewSearchKey)).FirstOrDefault();
                            }
                            if (Data == null)//check on MongoDB3 //Added 25-feb-2025
                            {
                                try
                                {
                                    //var client = new MongoClient(_mongoSettings.MongoDB_ConnectionStr3);
                                    //var database = client.GetDatabase(_mongoSettings.MongoDB_DatabaseName);
                                    var collection = _main3.GetCollection<AvailHotelList1>(_mongoSettings.MongoDB_CollectionName);
                                    Data = collection.Find(x => x.Search_Key == NewSearchKey).FirstOrDefault();
                                }
                                catch (Exception x)
                                {
                                    _exception.Error(x);
                                }
                                //Data = _searchlogs_read.Find(x => (x.Search_Key == NewSearchKey)).FirstOrDefault();
                            }
                            if (Data == null)//check on MongoDB old //Added 25-feb-2025
                            {
                                try
                                {
                                    //var client = new MongoClient(ConfigurationManager.AppSettings["MongoDB_ConnectionStr"]);
                                    //var database = client.GetDatabase(ConfigurationManager.AppSettings["MongoDB_DatabaseName"]);
                                    //var collection = database.GetCollection<AvailHotelList1>(ConfigurationManager.AppSettings["MongoDB_CollectionName"]);
                                    //Data = collection.Find(x => x.Search_Key == NewSearchKey).FirstOrDefault();

                                    return Convert.ToInt32(_CacheError.expired);
                                }
                                catch (Exception x)
                                {
                                    _exception.Error(x);
                                }
                                //Data = _searchlogs_read.Find(x => (x.Search_Key == NewSearchKey)).FirstOrDefault();
                            }
                            AvailHotelList ListData = new AvailHotelList();
                            ListData.AvailabilityRS = Data.AvailabilityRS;
                            ListData.Search_Key = Data.Search_Key;
                            ListData.Time = Data.Time;
                            finalResponse = ListData;
                        }
                        else
                        {
                            if (File.Exists(filepath))
                            {
                                string str = _commonMethod.ReadCacheFile(filepath, ref isError);
                                if (isError)
                                    return Convert.ToInt32(_CacheError.error);
                                else
                                    finalResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<AvailHotelList>(str);

                            }
                            else
                            {
                                try
                                {
                                    string NewStr = "";
                                    string[] arr = filepath.Split('\\');
                                    int hour = Convert.ToInt32(arr[9]) - 1;

                                    for (int i = 0; i < arr.Length; i++)
                                    {
                                        if (i == 9)
                                        {
                                            NewStr = NewStr + "\\" + hour;
                                        }
                                        else if (i == 0)
                                        {
                                            NewStr = NewStr + arr[i];
                                        }
                                        else
                                            NewStr = NewStr + "\\" + arr[i];
                                    }
                                    filepath = NewStr;
                                }
                                catch (Exception ex)
                                {
                                    _exception.Error(ex);
                                }

                                if (File.Exists(filepath))
                                {

                                    string str = _commonMethod.ReadCacheFile(filepath, ref isError);
                                    if (isError)
                                        return Convert.ToInt32(_CacheError.error);
                                    else
                                        finalResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<AvailHotelList>(str);

                                }
                                else
                                {
                                    try
                                    {
                                        string NewStr = "";
                                        string[] arr = filepath.Split('\\');
                                        int Day = Convert.ToInt32(arr[8]) - 1;
                                        int hour = 23;

                                        for (int i = 0; i < arr.Length; i++)
                                        {
                                            if (i == 8)
                                            {
                                                NewStr = NewStr + "\\" + Day;
                                            }
                                            else if (i == 9)
                                            {
                                                NewStr = NewStr + "\\" + hour;
                                            }
                                            else if (i == 0)
                                            {
                                                NewStr = NewStr + arr[i];
                                            }
                                            else
                                                NewStr = NewStr + "\\" + arr[i];
                                        }
                                        filepath = NewStr;
                                    }
                                    catch (Exception ex)
                                    {
                                        _exception.Error(ex);
                                    }

                                    if (File.Exists(filepath))
                                    {

                                        string str = _commonMethod.ReadCacheFile(filepath, ref isError);
                                        if (isError)
                                            return Convert.ToInt32(_CacheError.error);
                                        else
                                            finalResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<AvailHotelList>(str);

                                    }
                                    else
                                        return Convert.ToInt32(_CacheError.expired);
                                }
                            }
                        }
                    }
                }
                if (isError)
                    return Convert.ToInt32(_CacheError.error);
                else
                    return Convert.ToInt32(_CacheError.success);

            }
            catch (Exception ex)
            {
                _exception.Error(ex);
                return Convert.ToInt32(_CacheError.error);
            }

        }

        public string GenerateCacheFilePath(TokenData tokenData)
        {
            try
            {
                string[] paths = { Convert.ToString(CacheMaster.WebConfigKey_Search),
                                   Convert.ToString(_keySettings.IsLive),
                                   DateTime.Now.Year.ToString(),
                                   DateTime.Now.Month.ToString() ,
                                   DateTime.Now.Day.ToString(),
                                   DateTime.Now.Hour.ToString(),
                                   tokenData.AgentCode
                                    };
                string fullPath = Path.Combine(paths);
                return fullPath;
            }
            catch (Exception ex)
            {
                _exception.Error(ex);
            }
            return "";
        }

    }
}
