using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using XConnect.Models;

namespace XConnect.CommonUtilities
{
    

    public class MongoDbFactoryNew
    {
        private readonly MongoSettings _settings;

        private readonly MongoClient _client1;
        private readonly MongoClient _client2;
        private readonly MongoClient _client3;
        private readonly MongoClient _clientSuppLog;
        private readonly MongoClient _clientRoomMapping;
        private readonly MongoClient _clientRoomMapping2;
        private readonly MongoClient _clientUnMappedRoomMapping;
        private readonly MongoClient _clientQRSLog;
        private readonly MongoClient _clientSPR;
        private readonly MongoClient _clientExceptionLog; //Add On 31-DEC-25
        private static long _roomMappingCounter = 0;

        // --- Room Mapping Load Balancing ---
        private readonly MongoClient[] _roomMappingClients;
        private readonly bool[] _healthy;
        private long _counter = 0;

        private static readonly TimeSpan HealthCheckInterval = TimeSpan.FromSeconds(3);

        private static bool _healthMonitorStarted = false;
        private static readonly object _lock = new object();

        public MongoDbFactoryNew(IOptions<MongoSettings> settings)
        {
            _settings = settings.Value;

            _client1 = new MongoClient(_settings.MongoDB_ConnectionStr1);
            _client2 = new MongoClient(_settings.MongoDB_ConnectionStr2);
            _client3 = new MongoClient(_settings.MongoDB_ConnectionStr3);
            _clientSuppLog = new MongoClient(_settings.MongoDB_ConnectionStr_SupplierLog);
            _clientRoomMapping = new MongoClient(_settings.MongoDB_ConnectionStr_RoomMapping);
           // _clientRoomMapping2 = new MongoClient(_settings.MongoDB_ConnectionStr_RoomMapping2);
            _clientUnMappedRoomMapping = new MongoClient(_settings.MongoDB_ConnectionStr_UmMappedRoomMapping);
            _clientQRSLog = new MongoClient(_settings.MongoDB_ConStr_SaveAPIRQRSLog);
            _clientSPR = new MongoClient(_settings.MongoDB_ConStr_SPR);
            _clientExceptionLog = CreateFastClient(_settings.MongoDB_ConnectionStr_ExceptionLog); //Add On 31-DEC-25

            _roomMappingClients = new[]
            {
                 CreateFastClient(_settings.MongoDB_ConnectionStr_RoomMapping),
                 CreateFastClient(_settings.MongoDB_ConnectionStr_RoomMapping2)


             };
            _healthy = new bool[] { true, true };

            StartHealthMonitorOnce();
        }

        private MongoClient CreateClient(string connectionString)
        {
            var settings = MongoClientSettings.FromConnectionString(connectionString);

            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(2);
            settings.ConnectTimeout = TimeSpan.FromSeconds(2);
            settings.SocketTimeout = TimeSpan.FromSeconds(2);

            settings.RetryWrites = true;
            settings.DirectConnection = true; // Important for single-server ping

            return new MongoClient(settings);
        }

        private MongoClient CreateFastClient(string connectionString)
        {
            var settings = MongoClientSettings.FromConnectionString(connectionString);

            settings.ServerSelectionTimeout = TimeSpan.FromMilliseconds(200);
            settings.ConnectTimeout = TimeSpan.FromMilliseconds(200);
            settings.SocketTimeout = TimeSpan.FromMilliseconds(200);

            settings.RetryWrites = true;
            settings.DirectConnection = true; // Important for single-server ping

            return new MongoClient(settings);
        }

        //Start background monitor only once
        private void StartHealthMonitorOnce()
        {
            lock (_lock)
            {
                if (_healthMonitorStarted) return;
                _healthMonitorStarted = true;
                _ = StartHealthMonitorAsync();
            }
        }

        //Background health check
        private async Task StartHealthMonitorAsync()
        {
            while (true)
            {
                for (int i = 0; i < _roomMappingClients.Length; i++)
                {
                    try
                    {
                        var db = _roomMappingClients[i].GetDatabase(_settings.MongoDB_DatabaseName_RoomMapping);

                        using var cts = new CancellationTokenSource(600); // 300ms timeout


                        await db.RunCommandAsync<BsonDocument>(
                            new BsonDocument("ping", 1),
                            cancellationToken: cts.Token
                        );

                        _healthy[i] = true;
                    }
                    catch (Exception ex)
                    {
                        _healthy[i] = false;
                    }
                }

                await Task.Delay(HealthCheckInterval);
            }
        }
        public IMongoDatabase GetDatabase(string? name)
        {
            return name switch
            {
                "Main1" => _client1.GetDatabase(_settings.MongoDB_DatabaseName),
                "Main2" => _client2.GetDatabase(_settings.MongoDB_DatabaseName),
                "Main3" => _client3.GetDatabase(_settings.MongoDB_DatabaseName),
                "SuppLog" => _clientSuppLog.GetDatabase(_settings.MongoDB_DatabaseName),
                //"RoomMapping" => _clientRoomMapping.GetDatabase(_settings.MongoDB_DatabaseName_RoomMapping),
                //"RoomMapping" => _settings.MongoDB_Active_for_Loadbalance == "1" ? GetBalancedRoomMappingDatabase() : _clientRoomMapping.GetDatabase(_settings.MongoDB_DatabaseName_RoomMapping), //12NOV2025
                "RoomMapping" => _settings.MongoDB_Active_for_Loadbalance == "1"
                                ? GetBalancedRoomMappingDatabase()
                                : _clientRoomMapping.GetDatabase(_settings.MongoDB_DatabaseName_RoomMapping),

                "Row_RoomsForRoomMapping" => _clientUnMappedRoomMapping.GetDatabase(_settings.MongoDB_DatabaseName_UmMappedRoomMapping),
                "QRSLog" => _clientQRSLog.GetDatabase(_settings.MongoDB_DatabaseName_SaveAPIRQRSLog),
                "SPR" => _clientSPR.GetDatabase(_settings.MongoDB_DatabaseName_SPR),
                "BookingCRoomMapping" => _clientRoomMapping.GetDatabase(_settings.MongoDB_DatabaseName_BookingC), //130625
                "ExceptionLog" => _clientExceptionLog.GetDatabase(_settings.MongoDB_DatabaseName_ExceptionLog) //Add On 31-DEC-25
                // _ => throw new ArgumentException("Invalid database key", nameof(name))
            };
        }

        //Round-robin only among healthy servers
        private IMongoDatabase GetBalancedRoomMappingDatabase()
        {
            int len = _roomMappingClients.Length;
            for (int attempt = 0; attempt < len; attempt++)
            {
                long c = Interlocked.Increment(ref _counter);
                int index = (int)(c % len);

                if (_healthy[index])
                {
                    return _roomMappingClients[index]
                        .GetDatabase(_settings.MongoDB_DatabaseName_RoomMapping);
                }
            }

            // If all unhealthy → pick server 0
            return _roomMappingClients[0].GetDatabase(_settings.MongoDB_DatabaseName_RoomMapping);
        }



        public async Task WarmUpMongoAsync()
        {
            foreach (var client in _roomMappingClients)
            {
                try
                {
                    var db = client.GetDatabase(_settings.MongoDB_DatabaseName_RoomMapping);

                    // Allow LONG timeout ONCE
                    await db.RunCommandAsync<BsonDocument>(
                        new BsonDocument("ping", 1),
                        cancellationToken: new CancellationTokenSource(3000).Token // 3 sec
                    );
                }
                catch
                {
                    // Ignore – failover will handle it
                }
            }
        }

    }

}
