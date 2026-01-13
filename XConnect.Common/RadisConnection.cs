using MathNet.Numerics.Distributions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XConnect.CommonUtilities.Interface;
using XConnect.Models;
using XConnect.Models.Hotel;

namespace XConnect.CommonUtilities
{
    public class RadisConnection
    {
        private readonly RedisSettings _settings;
        private readonly Lazy<ConnectionMultiplexer> _redis1;

        // ════════════════════════════════════════════════════════════════════════════════════════════
        // 3-NODE CLUSTER CONFIGURATION
        // ════════════════════════════════════════════════════════════════════════════════════════════
        // BEFORE: private static Lazy<ConnectionMultiplexer> _redis2;
        // AFTER:  Array of 3 connections for load balancing

        //private static Lazy<ConnectionMultiplexer>[] _redis2Cluster;
        //private static int _clusterIndex = 0;
        //private static readonly object _clusterLock = new object();

        // Static connections - initialized ONCE
        private static ConnectionMultiplexer[] _redis2Cluster;
        private static long _clusterIndex = 0;
        private static readonly object _initLock = new object();
        private static bool _isInitialized = false;

        private readonly IMemoryCache _cache;

        private readonly ICommonMethod _commonMethod;

        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _cacheLocks = new ConcurrentDictionary<string, SemaphoreSlim>(); //17-Dec-2025

        #region OLD Code
        //public RadisConnection(IOptions<RedisSettings> settings, IMemoryCache cache)
        //{
        //    _cache = cache;
        //    _settings = settings.Value;
        //    _redis1 = new Lazy<ConnectionMultiplexer>(() =>
        //   ConnectionMultiplexer.Connect(_settings.RedisDB_ConnectionStr1));

        //    // create array based on config count
        //    _redis2Cluster = new Lazy<ConnectionMultiplexer>[_settings.ClusterNodes.Count];

        //    for (int i = 0; i < _settings.ClusterNodes.Count; i++)
        //    {
        //        int index = i;
        //        _redis2Cluster[i] = new Lazy<ConnectionMultiplexer>(() =>
        //        {
        //            // 3-ClusterNodes Server IPs (Master + 2 Replicas)
        //            var options = ConfigurationOptions.Parse(_settings.ClusterNodes[index]);
        //            options.AbortOnConnectFail = false;
        //            options.ConnectRetry = 3;
        //            options.ConnectTimeout = 10000;
        //            options.SyncTimeout = 10000;
        //            options.AsyncTimeout = 10000;
        //            options.ReconnectRetryPolicy = new ExponentialRetry(5000);

        //            var mux = ConnectionMultiplexer.Connect(options);

        //            mux.ConnectionFailed += (_, e) =>
        //                Console.WriteLine($"❌ Redis cluster node failed: {_settings.ClusterNodes[index]}, {e.FailureType}");

        //            mux.ConnectionRestored += (_, e) =>
        //                Console.WriteLine($"✅ Redis cluster node restored: {_settings.ClusterNodes[index]}");

        //            return mux;
        //        });
        //    }
        //}
        #endregion
        public RadisConnection(IOptions<RedisSettings> settings, IMemoryCache cache, ICommonMethod commonMethod)
        {
            _cache = cache;
            _settings = settings.Value;

            _redis1 = new Lazy<ConnectionMultiplexer>(() =>
               ConnectionMultiplexer.Connect(_settings.RedisDB_ConnectionStr1));

            // Initialize ONLY ONCE using double-check locking
            if (!_isInitialized)
            {
                lock (_initLock)
                {
                    if (!_isInitialized)
                    {
                        InitializeClusterConnections(_settings.ClusterNodes);
                        _isInitialized = true;
                    }
                }
            }

            _commonMethod = commonMethod;
        }

        // Runs ONCE at startup - connections created here
        private static void InitializeClusterConnections(List<string> clusterNodes)
        {
            _redis2Cluster = new ConnectionMultiplexer[clusterNodes.Count];

            for (int i = 0; i < clusterNodes.Count; i++)
            {
                var options = ConfigurationOptions.Parse(clusterNodes[i]);
                options.AbortOnConnectFail = false;
                options.ConnectRetry = 3;
                options.ConnectTimeout = 3000;// 10000; //16/12/25
                options.SyncTimeout = 3000;// 5000;  //16/12/25
                options.AsyncTimeout = 3000; // 5000;  //16/12/25
                options.ReconnectRetryPolicy = new ExponentialRetry(3000);

                try
                {
                    _redis2Cluster[i] = ConnectionMultiplexer.Connect(options);
                    Console.WriteLine($"Redis node connected: {clusterNodes[i]}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Redis node failed: {clusterNodes[i]}, {ex.Message}");
                    _redis2Cluster[i] = null;
                }
            }
        }

        // Lock-FREE round-robin using Interlocked
        private ConnectionMultiplexer GetNextClusterConnection()
        {
            int length = _redis2Cluster.Length;

            for (int attempts = 0; attempts < length; attempts++)
            {
                // Atomic increment - NO LOCK!
                long index = Interlocked.Increment(ref _clusterIndex);
                index = ((index % length) + length) % length;

                var conn = _redis2Cluster[index];

                // Check connection without blocking
                if (conn != null && conn.IsConnected)
                    return conn;

                // Try reconnect in background (non-blocking)
                TryReconnectAsync(index);
            }

            // Fallback: return any connected server
            for (int i = 0; i < length; i++)
            {
                if (_redis2Cluster[i] != null && _redis2Cluster[i].IsConnected)
                    return _redis2Cluster[i];
            }

            throw new RedisConnectionException(ConnectionFailureType.UnableToConnect,
                "All Redis cluster nodes unavailable!");
        }

        // Non-blocking reconnection
        private static void TryReconnectAsync(long index)
        {
            Task.Run(() =>
            {
                try
                {
                    if (_redis2Cluster[index] == null || !_redis2Cluster[index].IsConnected)
                    {
                        Console.WriteLine($"Attempting reconnect to node {index}");
                    }
                }
                catch { }
            });
        }

        #region OLD CODE
        //private ConnectionMultiplexer GetNextClusterConnection()
        //{
        //    lock (_clusterLock)
        //    {
        //        // Try each server until one works (failover)
        //        for (int attempts = 0; attempts < _redis2Cluster.Length; attempts++)
        //        {
        //            int index = _clusterIndex;
        //            _clusterIndex = (_clusterIndex + 1) % _redis2Cluster.Length;  // Round-robin: 0->1->2->0...

        //            var conn = _redis2Cluster[index].Value;
        //            if (conn != null && conn.IsConnected)
        //                return conn;
        //        }

        //        // If all failed, try first one anyway (will throw if truly unavailable)
        //        return _redis2Cluster[0].Value;
        //    }
        //}


        //public IDatabase GetDatabase(string? name, int dataBase)
        //{
        //    return name switch
        //    {
        //        "Main1" => _redis1.Value.GetDatabase(),
        //        // ════════════════════════════════════════════════════════════════════════════════
        //        // BEFORE: "Main2" => _redis2!.Value.GetDatabase(dataBase),
        //        // AFTER:  Use round-robin load balanced connection
        //        // ════════════════════════════════════════════════════════════════════════════════
        //        "Main2" => GetNextClusterConnection().GetDatabase(dataBase),
        //        _ => throw new ArgumentException("Invalid Redis database name", nameof(name))
        //    };
        //}

        //public IDatabase[] GetDatabases(string name, int startIndex, int endIndex)
        //{
        //    int count = endIndex - startIndex + 1;
        //    var dbs = new IDatabase[count];

        //    // ════════════════════════════════════════════════════════════════════════════════════
        //    // BEFORE: All DBs from same single connection
        //    // AFTER:  Get connection once via round-robin, then get all DBs from that connection
        //    //         (keeps batch operations on same server for consistency)
        //    // ════════════════════════════════════════════════════════════════════════════════════

        //    for (int i = 0; i < count; i++)
        //    {
        //        dbs[i] = name switch
        //        {
        //            "Main1" => _redis1!.Value.GetDatabase(startIndex + i),
        //            "Main2" => GetNextClusterConnection().GetDatabase(startIndex + i),
        //            _ => throw new ArgumentException("Invalid Redis database name")
        //        };
        //    }

        //    return dbs;
        //}

        //public IDatabase[] GetDatabasesNewBalancing(string name, int startIndex, int endIndex)
        //{
        //    int count = endIndex - startIndex + 1;
        //    var dbs = new IDatabase[count];

        //    if (name == "Main1")
        //    {
        //        for (int i = 0; i < count; i++)
        //            dbs[i] = _redis1.Value.GetDatabase(startIndex + i);

        //        return dbs;
        //    }

        //    if (name == "Main2")
        //    {
        //        // Get ONE connection only
        //        var conn = GetNextClusterConnection();

        //        for (int i = 0; i < count; i++)
        //            dbs[i] = conn.GetDatabase(startIndex + i);

        //        return dbs;
        //    }

        //    throw new ArgumentException("Invalid Redis database name");
        //}
        #endregion
        public IDatabase GetDatabase(string? name, int dataBase)
        {
            return name switch
            {
                "Main1" => _redis1.Value.GetDatabase(),
                "Main2" => GetNextClusterConnection().GetDatabase(dataBase),
                _ => throw new ArgumentException("Invalid Redis database name", nameof(name))
            };
        }

        // FIXED: Gets ONE connection for ALL databases
        public IDatabase[] GetDatabasesNewBalancing(string name, int startIndex, int endIndex)
        {
            int count = endIndex - startIndex + 1;
            var dbs = new IDatabase[count];

            if (name == "Main1")
            {
                for (int i = 0; i < count; i++)
                    dbs[i] = _redis1.Value.GetDatabase(startIndex + i);
                return dbs;
            }

            if (name == "Main2")
            {
                // Get ONE connection only
                var conn = GetNextClusterConnection();
                for (int i = 0; i < count; i++)
                    dbs[i] = conn.GetDatabase(startIndex + i);
                return dbs;
            }

            throw new ArgumentException("Invalid Redis database name");
        }

        public async Task<string?> GetCachedValueAsync(string key, string dbName)
        {
            var db = _redis1.Value.GetDatabase();
            var value = await db.StringGetAsync(key);

            return value.HasValue ? value.ToString() : null;
        }

        public async Task<(List<RaynaXML.Root> DB0, string? DB1, List<HashSet<int>> DB2, Dictionary<int, HashSet<int>> DB3, Dictionary<int, HashSet<int>> DB4)> GetCachedValueUsingKeysAsyncNew_Optimize(string[] keys, int agentId, HotelSearchParameter SearchPara, List<string> activeSupplier, DataTable AgtConfig, DataTable RedisCacheTime)
        {
         
            bool IsVervo = Convert.ToBoolean(AgtConfig.Rows[0]["IsVervo"]);
            bool IsGimmo = Convert.ToBoolean(AgtConfig.Rows[0]["IsGimmo"]);



            #region New Balancing Code
            // Get ONE connection selected by round robin
            // Get ONE connection selected by round robin
            var redisConn = GetNextClusterConnection();

            // Use THIS connection only for all DBs
            var dbs = new IDatabase[]
            {
                redisConn.GetDatabase(1),
                redisConn.GetDatabase(2),
                redisConn.GetDatabase(3),
                redisConn.GetDatabase(4),
                redisConn.GetDatabase(5),
                redisConn.GetDatabase(6)
            };

            #endregion


            //var dbs = GetDatabases("Main2", 1, 6);
            var db0 = IsVervo ? dbs[4] : dbs[5]; //5 -Vervo Mapping //6 - Gimmo Mapping
            var db1 = dbs[0]; //Supplier SpeedCategory (single JSON string)
            var db2 = dbs[1]; //0:AITO_Exclude_Agent, 1:AITO_Include_Agent, 2:BaseHotelID
            var db3 = dbs[2]; //Agt_SupTopselling_Base
            var db4 = dbs[3]; //SupTopselling

            int DB1CacheTime = Convert.ToInt32(RedisCacheTime.Rows[0]["DB1"]);
            int DB2CacheTime = Convert.ToInt32(RedisCacheTime.Rows[0]["DB2"]);
            int DB3CacheTime = Convert.ToInt32(RedisCacheTime.Rows[0]["DB3"]);
            int DB4CacheTime = Convert.ToInt32(RedisCacheTime.Rows[0]["DB4"]);

            int DBTimeout  = Convert.ToInt32(RedisCacheTime.Rows[0]["DB5N6Timeoutms"]);
            int DB1Timeout = Convert.ToInt32(RedisCacheTime.Rows[0]["DB1Timeoutms"]);
            int DB2Timeout = Convert.ToInt32(RedisCacheTime.Rows[0]["DB2Timeoutms"]);
            int DB3Timeout = Convert.ToInt32(RedisCacheTime.Rows[0]["DB3Timeoutms"]);
            int DB4Timeout = Convert.ToInt32(RedisCacheTime.Rows[0]["DB4Timeoutms"]);


            // ---------- DB0 keys ----------
            RedisKey[] db0Keys = keys.Select(k => (RedisKey)k).ToArray();
            //var task0 = FetchDb6HashSet_hashset(db0, keys, activeSupplier); //multiple hotelid in one call
            var task0 = FetchDb6HashSet_hashsetTimeout(db0, keys, activeSupplier, agentId, IsVervo ? 5 : 6, DBTimeout); //multiple hotelid in one call //16/12/25

            // ---------- DB1 fetch ----------
            string db1CacheKey = $"DB1_{agentId}";
            //Task<string> task1 = FetchDb1Async(agentId, db1, db1CacheKey, DB1CacheTime); //10 minutes cache
            //Task<string> task1 = FetchDb1AsyncTimeout(agentId, db1, db1CacheKey, DB1CacheTime, DB1Timeout); //10 minutes cache //16/12/25

            //17-DEC-2025
            Task<string?> task1 = GetOrCreateAsync(db1CacheKey,() => FetchDb1AsyncTimeout(agentId, db1, DB1Timeout), TimeSpan.FromMinutes(DB1CacheTime)); 

            // ---------- DB2 fetch ----------
            string db2CacheKey = $"DB2_{agentId}"; // 2 hours cache
            RedisKey[] db2Keys = {
                $"AITO_Exclude_Agent:{agentId}",
                $"AITO_Include_Agent:{agentId}",
                "AITO:BaseHotelID"
            };

            //Task<List<HashSet<int>>> task2 =
            //    _cache.TryGetValue(db2CacheKey, out List<HashSet<int>> cachedDb2)
            //    ? Task.FromResult(cachedDb2)
            //    //: FetchDb2Async(db2Keys, db2CacheKey, db2, DB2CacheTime);
            //    : FetchDb2AsyncTimeout(db2Keys, db2CacheKey, db2, DB2CacheTime,agentId, DB2Timeout); //16/12/25

            // 17-DEC-25
            Task<List<HashSet<int>>> task2 = GetOrCreateAsync(db2CacheKey,() => FetchDb2AsyncTimeout(db2Keys,db2CacheKey,db2,DB2CacheTime,agentId,DB2Timeout),TimeSpan.FromMinutes(DB2CacheTime)); //17/12/25


            // ---------- DB3 fetch ----------
            string db3CacheKey = $"DB3_{agentId}"; //Agent wise Supplier Top Selling //24 hours cache

            //Task<Dictionary<int, HashSet<int>>> task3 =
            //    _cache.TryGetValue(db3CacheKey, out Dictionary<int, HashSet<int>> cachedDb3)
            //        ? Task.FromResult(cachedDb3)
            //        //: FetchDbHashSetAsync(db3, agentId.ToString(), db3CacheKey, DB3CacheTime);
            //        : FetchDbHashSetAsyncTimeout(db3, agentId.ToString(), db3CacheKey, DB3CacheTime,agentId,3, DB3Timeout); //16/12/25

            // 17-DEC-25
            Task<Dictionary<int, HashSet<int>>> task3 = GetOrCreateAsync(db3CacheKey,() => FetchDbHashSetAsyncTimeout(db3, agentId.ToString(), db3CacheKey, DB3CacheTime, agentId,3,DB3Timeout),TimeSpan.FromHours(DB3CacheTime));

            // ---------- DB4 fetch ----------
            //string db4CacheKey = $"DB4_{agentId}";//Supplier Top Selling
            string db4CacheKey = "SupplierTopSelling";//Supplier Top Selling //17/12/25

            //Task<Dictionary<int, HashSet<int>>> task4 =
            //    _cache.TryGetValue(db4CacheKey, out Dictionary<int, HashSet<int>> cachedDb4)
            //        ? Task.FromResult(cachedDb4)
            //        //: FetchDbHashSetAsync(db4, "SupplierTopSelling", db4CacheKey, DB4CacheTime);
            //        : FetchDbHashSetAsyncTimeout(db4, "SupplierTopSelling", db4CacheKey, DB4CacheTime,agentId, 4, DB4Timeout); //16/12/25

            // 17-DEC-25
            Task<Dictionary<int, HashSet<int>>> task4 = GetOrCreateAsync(db4CacheKey, () => FetchDbHashSetAsyncTimeout(db4, "SupplierTopSelling", db4CacheKey, DB4CacheTime, agentId, 4, DB4Timeout), TimeSpan.FromHours(DB4CacheTime));



            System.Diagnostics.Stopwatch timer_AllTask = new System.Diagnostics.Stopwatch();
            timer_AllTask.Start();
            // ---------- Await all ----------
            await Task.WhenAll(task0, task1, task2, task3, task4).ConfigureAwait(false);
            timer_AllTask.Stop();
            SearchPara.SupplierTime += ",|MappingSP_Cid_AllTask:" + Convert.ToString(SearchPara.CountryID) + "" + "|Time :" + timer_AllTask.Elapsed.ToString();

            // ---------- Process DB0 ----------
            //var db0Values = task0.Result.Select(v => v.HasValue ? v.ToString()! : "").ToList();
            var db0Values = task0.Result;
            // ---------- Process DB1 ----------
            var db1Value = task1.Result;

            // ---------- Return tuple ----------
            //return (db0Values, db1Value, task2.Result, task3.Result, task4.Result);
            return (db0Values, db1Value, task2.Result, task3.Result, task4.Result);
        }

        private async Task<List<RaynaXML.Root>> FetchDb6HashSet_hashset(IDatabase db6, string[] SharedKeys, List<string> activeSupplier)
        {
            //string result = string.Join(",", activeSupplier);

            var tasks = new List<Task<RedisValue[]>>();
            var batch = db6.CreateBatch();

            //RedisValue[] fields = { "RaynaId", "HotelName", "Rating", "CountryId", "CityId", "giataid", "ReservationEmail", "HotelContactNo", "IsRecomondedHotel", "EAN", "IWTX2", "Agoda_V1" };

            // Fixed fields
            List<string> fixedFields = new List<string>
        {
            "RaynaId", "HotelName", "Rating", "CountryId", "CityId", "giataid",
            "ReservationEmail", "HotelContactNo", "IsRecomondedHotel"
        };

            // Combine fixed fields with dynamic suppliers
            RedisValue[] fields = fixedFields.Concat(activeSupplier).Select(f => (RedisValue)f).ToArray();

            foreach (var hotelId in SharedKeys)
            {
                // Correct method returns Task<RedisValue[]>
                var task = batch.HashGetAsync(hotelId, fields);
                tasks.Add(task);   // <-- This will work
            }

            batch.Execute();

            await Task.WhenAll(tasks).ConfigureAwait(false);

            List<RaynaXML.Root> hotelDatas = new List<RaynaXML.Root>();
            foreach (var t in tasks)
            {
                RedisValue[] values = t.Result;

                // -------- XMLMappings (Dynamic Suppliers) ----------
                List<RaynaXML.XMLMapping> suppliers = new List<RaynaXML.XMLMapping>();

                // Loop remaining fields dynamically (index 1 onward)
                for (int i = 9; i < fields.Length; i++)
                {
                    string supplierName = fields[i];  // "EAN", "IWTX2", etc.
                    string json = values[i];

                    if (values[i].IsNullOrEmpty)
                        continue;

                    var lstXmlMapping = System.Text.Json.JsonSerializer.Deserialize<List<RaynaXML.XMLMapping>>(json);
                    if (lstXmlMapping != null)
                    {
                        foreach (var s in lstXmlMapping)
                        {
                            s.supplierName = supplierName;
                        }
                        suppliers.AddRange(lstXmlMapping);
                    }

                    //// if (json.TrimStart().StartsWith("{"))
                    //{
                    //    // Single supplier object
                    //    var single = System.Text.Json.JsonSerializer.Deserialize<RaynaXML.XMLMapping>(json);
                    //    if (single != null)
                    //    {
                    //        single.supplierName = supplierName;  // ensure tag
                    //        suppliers.Add(single);
                    //    }
                    //}
                    ////else
                    ////{
                    ////    // List of suppliers
                    ////    var list = JsonSerializer.Deserialize<List<SupplierInfo>>(json);
                    ////    if (list != null)
                    ////    {
                    ////        foreach (var s in list)
                    ////        {
                    ////            s.supplierName = supplierName;
                    ////        }
                    ////        suppliers.AddRange(list);
                    ////    }
                    ////}
                }

                if (suppliers.Count > 0)
                {
                    // -------- Build Hotel model ----------
                    RaynaXML.Root model = new RaynaXML.Root
                    {
                        RaynaId = suppliers[0].RaynaId,
                        CityId = values[4].IsNull ? 0 : (int)values[4],
                        CountryId = values[3].IsNull ? 0 : (int)values[3],
                        giataid = suppliers[0].GiataId,
                        Rating = 0,
                        XMLMappings = suppliers
                    };

                    hotelDatas.Add(model);
                }

            }

            return hotelDatas;
        }

        private async Task<string> FetchDb1Async(int agentId, IDatabase db1, string cacheKey, int DB1CacheTime)
        {
            // Check cache first
            if (_cache.TryGetValue(cacheKey, out string cachedDb1))
                return cachedDb1;

            // Fetch from Redis
            RedisValue value = await db1.StringGetAsync(agentId.ToString()).ConfigureAwait(false);

            string result = value.HasValue ? value.ToString() : null;

            // Store in cache only if value exists
            if (result != null)
                _cache.Set(cacheKey, result, TimeSpan.FromMinutes(DB1CacheTime));//10 

            return result;
        }
        private async Task<List<HashSet<int>>> FetchDb2Async(RedisKey[] db2Keys, string db2CacheKey, IDatabase db2, int DB2CacheTime)
        {
            int count = db2Keys.Length;

            // Fetch all redis sets concurrently (non-blocking)
            var tasks = new Task<RedisValue[]>[count];
            for (int i = 0; i < count; i++)
            {
                tasks[i] = db2.SetMembersAsync(db2Keys[i]);
            }

            RedisValue[][] results = await Task.WhenAll(tasks).ConfigureAwait(false);

            // Pre-allocate the outer list
            var list = new List<HashSet<int>>(count);

            // Process each redis set
            for (int i = 0; i < results.Length; i++)
            {
                RedisValue[] values = results[i];
                var set = new HashSet<int>(values.Length);

                for (int j = 0; j < values.Length; j++)
                {
                    if (values[j].TryParse(out int val))
                    {
                        set.Add(val);
                    }
                }

                list.Add(set);
            }

            _cache.Set(db2CacheKey, list, TimeSpan.FromMinutes(DB2CacheTime)); //120
            return list;
        }

        
        private async Task<Dictionary<int, HashSet<int>>> FetchDbHashSetAsync(IDatabase db, string redisKey, string cacheKey, int CacheTime)
        {
            // Fetch all hash entries from Redis
            HashEntry[] entries = await db.HashGetAllAsync(redisKey).ConfigureAwait(false);

            // Pre-allocate dictionary with expected capacity
            var dict = new Dictionary<int, HashSet<int>>(entries.Length);

            foreach (var entry in entries)
            {
                if (!int.TryParse(entry.Name, out int supplierId))
                    continue;

                try
                {
                    // Deserialize JSON list directly
                    var list = System.Text.Json.JsonSerializer.Deserialize<List<int>>(entry.Value);
                    dict[supplierId] = list?.ToHashSet() ?? new HashSet<int>();
                }
                catch
                {
                    dict[supplierId] = new HashSet<int>();
                }
            }

            _cache.Set(cacheKey, dict, TimeSpan.FromHours(CacheTime)); //24Hours

            return dict;
        }

        //16/12/25
        private async Task<T?> ExecuteWithTimeout<T>(Func<Task<T>> redisCall,string redisKey,string operationName, int AgentId, int DB,int timeoutMs = 700) 
        {
            var timeoutTask = Task.Delay(timeoutMs);
            var redisTask = redisCall();

            var completed = await Task.WhenAny(redisTask, timeoutTask)
                                      .ConfigureAwait(false);

            if (completed == timeoutTask)
            {
                // 🔴 TIMEOUT HIT → log
               _= LogRedisTimeout(operationName, redisKey, timeoutMs,AgentId,DB);
                return default;
            }

            return await redisTask.ConfigureAwait(false);
        }

        //16/12/25
        private async Task LogRedisTimeout(string operation, string key, int timeoutMs, int agentId, int DB)
        {
            try
            {
               _= _commonMethod.InsertRedisHotelSearchData("Timeout", key,agentId,DB, timeoutMs);
            }
            catch
            {
                // never throw from logging
            }
        }
        //16/12/25

        private async Task<string?> FetchDb1AsyncTimeoutOLD(int agentId,IDatabase db1,string cacheKey,int DB1CacheTime, int timeoutMs = 700)
        {
            if (_cache.TryGetValue(cacheKey, out string cached))
                return cached;

            string redisKey = agentId.ToString();

            RedisValue value = await ExecuteWithTimeout(
                () => db1.StringGetAsync(redisKey),
                redisKey,
                "DB1_StringGet", agentId, 1, timeoutMs
            );

            if (!value.HasValue)
                return null;

            string result = value.ToString();
            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(DB1CacheTime));

            return result;
        }

        private async Task<string?> FetchDb1AsyncTimeout(int agentId,IDatabase db1,int timeoutMs = 700) //17-DEC-2025
        {
            string redisKey = agentId.ToString();

            RedisValue value = await ExecuteWithTimeout(() => db1.StringGetAsync(redisKey), redisKey, "DB1_StringGet", agentId, 1,timeoutMs).ConfigureAwait(false);

            if (!value.HasValue)
                return null;

            return value.ToString();
        }


        private async Task<List<HashSet<int>>> FetchDb2AsyncTimeout(RedisKey[] db2Keys, string db2CacheKey, IDatabase db2, int DB2CacheTime,int AgentId, int timeoutMs = 700)
        {
            int count = db2Keys.Length;

            // Fetch all redis sets concurrently with timeout
            var tasks = new Task<HashSet<int>>[count];
            for (int i = 0; i < count; i++)
            {
                var key = db2Keys[i];
                tasks[i] = ExecuteWithTimeout(
                    async () =>
                    {
                        RedisValue[] values = await db2.SetMembersAsync(key).ConfigureAwait(false);
                        var set = new HashSet<int>(values.Length);
                        foreach (var v in values)
                        {
                            if (v.TryParse(out int val))
                                set.Add(val);
                        }
                        return set;
                    },
                    key.ToString(),
                    "DB2_SetMembersAsync",AgentId,2,
                    timeoutMs
                ) ?? Task.FromResult(new HashSet<int>()); // Return empty if timeout
            }

            //var results = await Task.WhenAll(tasks).ConfigureAwait(false);
            HashSet<int>[] results = await Task.WhenAll(tasks).ConfigureAwait(false);
            // Pre-allocate list (same as original method)
            var list = new List<HashSet<int>>(count);

            for (int i = 0; i < results.Length; i++)
            {
                // 🔹 Always add something (never skip)
                list.Add(results[i] ?? new HashSet<int>());
            }



            // ❌ If any Redis call timed out → skip cache
            //if (results.Any(r => r == null))
            //{
            //    return results
            //        .Where(r => r != null)
            //        .ToList();
            //}

            ////var list = results.ToList();
            //var list = results.Cast<HashSet<int>>().ToList();

            // _cache.Set(db2CacheKey, list, TimeSpan.FromMinutes(DB2CacheTime)); // cache for DB2CacheTime

            return list;
        }


        private async Task<List<RaynaXML.Root>> FetchDb6HashSet_hashsetTimeout(IDatabase db6, string[] SharedKeys, List<string> activeSupplier, int AgentId, int DB, int timeoutMs = 700)
        {
            //string result = string.Join(",", activeSupplier);

            var tasks = new List<Task<RedisValue[]>>();
            var batch = db6.CreateBatch();

            //RedisValue[] fields = { "RaynaId", "HotelName", "Rating", "CountryId", "CityId", "giataid", "ReservationEmail", "HotelContactNo", "IsRecomondedHotel", "EAN", "IWTX2", "Agoda_V1" };

            // Fixed fields
            List<string> fixedFields = new List<string>
        {
            "RaynaId", "HotelName", "Rating", "CountryId", "CityId", "giataid",
            "ReservationEmail", "HotelContactNo", "IsRecomondedHotel"
        };

            // Combine fixed fields with dynamic suppliers
            RedisValue[] fields = fixedFields.Concat(activeSupplier).Select(f => (RedisValue)f).ToArray();

            foreach (var hotelId in SharedKeys)
            {
                //// Correct method returns Task<RedisValue[]>
                //var task = batch.HashGetAsync(hotelId, fields);
                //tasks.Add(task);   // <-- This will work

                var task = ExecuteWithTimeout(() => batch.HashGetAsync(hotelId, fields),hotelId, "HashGetAsync",AgentId,DB,timeoutMs);
                tasks.Add(task);
            }



            batch.Execute();

            await Task.WhenAll(tasks).ConfigureAwait(false);

            List<RaynaXML.Root> hotelDatas = new List<RaynaXML.Root>();
            foreach (var t in tasks)
            {
                if(t.Result == null)
                    continue;

                RedisValue[] values = t.Result;

                // -------- XMLMappings (Dynamic Suppliers) ----------
                List<RaynaXML.XMLMapping> suppliers = new List<RaynaXML.XMLMapping>();

                // Loop remaining fields dynamically (index 1 onward)
                for (int i = 9; i < fields.Length; i++)
                {
                    string supplierName = fields[i];  // "EAN", "IWTX2", etc.
                    string json = values[i];

                    if (values[i].IsNullOrEmpty)
                        continue;

                    var lstXmlMapping = System.Text.Json.JsonSerializer.Deserialize<List<RaynaXML.XMLMapping>>(json);
                    if (lstXmlMapping != null)
                    {
                        foreach (var s in lstXmlMapping)
                        {
                            s.supplierName = supplierName;
                        }
                        suppliers.AddRange(lstXmlMapping);
                    }

                    //// if (json.TrimStart().StartsWith("{"))
                    //{
                    //    // Single supplier object
                    //    var single = System.Text.Json.JsonSerializer.Deserialize<RaynaXML.XMLMapping>(json);
                    //    if (single != null)
                    //    {
                    //        single.supplierName = supplierName;  // ensure tag
                    //        suppliers.Add(single);
                    //    }
                    //}
                    ////else
                    ////{
                    ////    // List of suppliers
                    ////    var list = JsonSerializer.Deserialize<List<SupplierInfo>>(json);
                    ////    if (list != null)
                    ////    {
                    ////        foreach (var s in list)
                    ////        {
                    ////            s.supplierName = supplierName;
                    ////        }
                    ////        suppliers.AddRange(list);
                    ////    }
                    ////}
                }

                if (suppliers.Count > 0)
                {
                    // -------- Build Hotel model ----------
                    RaynaXML.Root model = new RaynaXML.Root
                    {
                        RaynaId = suppliers[0].RaynaId,
                        CityId = values[4].IsNull ? 0 : (int)values[4],
                        CountryId = values[3].IsNull ? 0 : (int)values[3],
                        giataid = suppliers[0].GiataId,
                        Rating = 0,
                        XMLMappings = suppliers
                    };

                    hotelDatas.Add(model);
                }

            }

            return hotelDatas;
        }

        private async Task<Dictionary<int, HashSet<int>>> FetchDbHashSetAsyncTimeout(IDatabase db,string redisKey, string cacheKey,int CacheTime, int AgentId, int DB, int timeoutMs = 700)
        {
            HashEntry[]? entries = await ExecuteWithTimeout(
                () => db.HashGetAllAsync(redisKey),
                redisKey,
                "HashGetAllAsync",AgentId,DB,
                timeoutMs
            ).ConfigureAwait(false);

            // 🔴 If timeout → return empty dictionary
            if (entries == null || entries.Length == 0)
            {
                var empty = new Dictionary<int, HashSet<int>>();
               // _cache.Set(cacheKey, empty, TimeSpan.FromHours(CacheTime)); // if empty then no need to add in cache
                return empty;
            }

            var dict = new Dictionary<int, HashSet<int>>(entries.Length);

            foreach (var entry in entries)
            {
                if (!int.TryParse(entry.Name, out int supplierId))
                    continue;

                try
                {
                    var list = System.Text.Json.JsonSerializer
                        .Deserialize<List<int>>(entry.Value);

                    dict[supplierId] = list?.ToHashSet() ?? new HashSet<int>();
                }
                catch
                {
                    dict[supplierId] = new HashSet<int>();
                }
            }

            //_cache.Set(cacheKey, dict, TimeSpan.FromHours(CacheTime));
            return dict;
        }

        private static class NegativeCache<T>
        {
            public static readonly object Value = new object();
        }

        private async Task<T?> GetOrCreateAsync<T>(string cacheKey,Func<Task<T?>> factory,TimeSpan positiveTtl, TimeSpan? negativeTtl = null)
        {
            // 🔒 Type-safe cache key (prevents InvalidCastException bugs)
            string typedKey = $"{typeof(T).FullName}:{cacheKey}";

            // ⚡ Fast path
            if (_cache.TryGetValue(typedKey, out object cached))
            {
                if (ReferenceEquals(cached, NegativeCache<T>.Value))
                    return default;

                return (T)cached;
            }

            var semaphore = _cacheLocks.GetOrAdd(
                typedKey,
                _ => new SemaphoreSlim(1, 1));

            await semaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                // 🔁 Double-check after acquiring lock
                if (_cache.TryGetValue(typedKey, out cached))
                {
                    if (ReferenceEquals(cached, NegativeCache<T>.Value))
                        return default;

                    return (T)cached;
                }

                var result = await factory().ConfigureAwait(false);

                if (result != null)
                {
                    _cache.Set(typedKey, result, positiveTtl);
                    return result;
                }

                // 🔴 Negative cache (timeout / Redis failure / missing)
                var ttl = negativeTtl
                    ?? TimeSpan.FromSeconds(30 + Random.Shared.Next(0, 30)); // jitter

                _cache.Set(typedKey, NegativeCache<T>.Value, ttl);
                return default;
            }
            finally
            {
                semaphore.Release();

                // 🧹 Cleanup semaphore to prevent memory leak
                if (semaphore.CurrentCount == 1)
                    _cacheLocks.TryRemove(typedKey, out _);
            }
        }


        //private async Task<T> GetOrCreateAsync<T>(string cacheKey,Func<Task<T>> factory,TimeSpan cacheTime) //16-Dec-2025
        //{
        //    // Fast path – already cached
        //    if (_cache.TryGetValue(cacheKey, out T cached))
        //        return cached;

        //    var semaphore = _cacheLocks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));

        //    await semaphore.WaitAsync().ConfigureAwait(false);
        //    try
        //    {
        //        // Double-check inside lock
        //        if (_cache.TryGetValue(cacheKey, out cached))
        //            return cached;

        //        var result = await factory().ConfigureAwait(false);

        //        if (result != null)
        //            _cache.Set(cacheKey, result, cacheTime);

        //        return result;
        //    }
        //    finally
        //    {
        //        semaphore.Release();
        //    }
        //}

    }
}