using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using XConnect.Models;

namespace XConnect.CommonUtilities
{
    public class MongoDbFactory
    {
        private readonly MongoSettings _settings;

        public MongoDbFactory(IOptions<MongoSettings> settings)
        {
            _settings = settings.Value;
        }

        public IMongoDatabase GetDatabase(string? name)
        {
            return name switch
            {
                "Main1" => new MongoClient(_settings.MongoDB_ConnectionStr1).GetDatabase(_settings.MongoDB_DatabaseName),
                "Main2" => new MongoClient(_settings.MongoDB_ConnectionStr2).GetDatabase(_settings.MongoDB_DatabaseName),
                "Main3" => new MongoClient(_settings.MongoDB_ConnectionStr3).GetDatabase(_settings.MongoDB_DatabaseName),
                "SuppLog" => new MongoClient(_settings.MongoDB_ConnectionStr_SupplierLog).GetDatabase(_settings.MongoDB_DatabaseName),
                "RoomMapping" => new MongoClient(_settings.MongoDB_ConnectionStr_RoomMapping).GetDatabase(_settings.MongoDB_DatabaseName_RoomMapping),
                "QRSLog" => new MongoClient(_settings.MongoDB_ConStr_SaveAPIRQRSLog).GetDatabase(_settings.MongoDB_DatabaseName_SaveAPIRQRSLog),
                "SPR" => new MongoClient(_settings.MongoDB_ConStr_SPR).GetDatabase(_settings.MongoDB_DatabaseName_SPR)
                //_ => throw new ArgumentException("Invalid database key", nameof(name))
            };
        }
    }
}
