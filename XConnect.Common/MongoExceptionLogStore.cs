using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.ErrorLogHandler.Interface;
using XConnect.Models;

namespace XConnect.CommonUtilities
{
    public class MongoExceptionLogStore : IExceptionLogStore
    {
        private readonly MongoDbFactoryNew _factory;
        private readonly IMongoDatabase _ExceptionLog;
        private readonly MongoSettings _mongoSettings;
        //private readonly ICommonMethod _commonMethod;//Added on 08-JAN-26 //circular dependency . not working this here
        private readonly RabbitMQPublisher _publisher; //Added on 09-JAN-26


        public MongoExceptionLogStore(MongoDbFactoryNew factory, IOptions<MongoSettings> mongoSettings, RabbitMQPublisher publisher)
        {
            _ExceptionLog = factory.GetDatabase("ExceptionLog");
            _mongoSettings = mongoSettings.Value;
            _publisher = publisher;

            //_commonMethod = commonMethod;
        }

        private string GetCollectionName(int apiName)
        {
            return apiName switch
            {
                1 => "Errorlog_AV",    // Availability
                2 => "Errorlog_RE",    // Recheck / CancellationPolicy
                3 => "Errorlog_PR",    // Prebook
                4 => "Errorlog_BO",    // Book / BookingDetail
                5 => "Errorlog_CA",    // CancelBooking / CheckHotelCancellationCharges
                _ => "Errorlog_OT"     // Other
            };
        }

        public async Task SaveAsync(ExceptionLogDocument doc)
        {
            string collectionName = GetCollectionName(doc.ApiName);

            //var collection = _ExceptionLog.GetCollection<BsonDocument>(_mongoSettings.MongoDB_CollectionName_ExceptionLog_AV);
            var collection = _ExceptionLog.GetCollection<BsonDocument>(collectionName);

            var bson = new BsonDocument
                {
                    { "servicename", doc.servicename },
                    { "title", doc.title },
                    { "status", doc.status },
                    { "details", doc.details },
                    { "datetime", doc.datetime },
                    { "source", doc.source },
                    { "WebsiteName", doc.WebsiteName },
                    { "SearchKey", doc.SearchKey },
                    { "Info", doc.SearchKey },
                    { "apiName", doc.ApiName }
                };

            await collection.InsertOneAsync(bson);
        }


        public async Task SaveLogAsync(ResLog log, string CollectionName)
        {
            var collection = _ExceptionLog.GetCollection<BsonDocument>(CollectionName);

            var bson = new BsonDocument
                {
                    { "_id", log.SearchKey},
                    { "XmlRequest", log.XmlRequest },
                    { "XmlResponse", log.XmlResponse },
                    { "Provider", log.Provider },
                    { "TypeRequest", log.TypeRequest },
                    { "ReqId", log.ReqId },
                    { "SearchKey", log.SearchKey },
                };

            await collection.InsertOneAsync(bson);
        }

        public async Task SaveRequestTrackingLogAsync(RequestTrackingLog log, string CollectionName)
        {
            var collection = _ExceptionLog.GetCollection<BsonDocument>(CollectionName);

            var bson = new BsonDocument
                {
                    { "_id", log.SearchKey},
                    { "SearchData", log.SearchData },
                    { "IP", log.IP },
                    { "Agentid", log.Agentid },
                    { "TotalRecord", log.TotalRecord },
                    { "CityID", log.CityID },
                    { "from", log.from },
                    { "SupplierTime", log.SupplierTime },
                    { "SearchKey", log.SearchKey },
                };

            await collection.InsertOneAsync(bson);
        }

        public async Task SaveLogRabbitMQAsync(ExceptionLogDocument doc)
        {
            string Queue = GetCollectionName(doc.ApiName);
            _ = _publisher.SendMessageAsync(doc, Queue);
        }

    }
}
