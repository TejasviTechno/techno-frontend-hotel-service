using MongoDB.Driver;
using XConnect.Models;

namespace XConnect.API.Middleware
{
    public class MongoDbClientFactory
    {
        private readonly Dictionary<string, IMongoClient> _mongoClients = new();

        public MongoDbClientFactory(IConfiguration configuration)
        {
            // Get MongoSettings from configuration
            var settings = configuration.GetSection("MongoSettings").Get<MongoSettings>();

            // Register multiple Mongo clients based on different connection strings
            if (settings != null)
            {
                if (!string.IsNullOrEmpty(settings.MongoDB_ConnectionStr1))
                {
                    _mongoClients["Connection1"] = new MongoClient(settings.MongoDB_ConnectionStr1);
                }
                if (!string.IsNullOrEmpty(settings.MongoDB_ConnectionStr2))
                {
                    _mongoClients["Connection2"] = new MongoClient(settings.MongoDB_ConnectionStr2);
                }
                // Add more connections if needed
            }
        }

        public IMongoClient GetClient(string connectionName)
        {
            // Return the MongoDB client for the given connection name
            return _mongoClients.TryGetValue(connectionName, out var client) ? client : null;
        }
    }

}
