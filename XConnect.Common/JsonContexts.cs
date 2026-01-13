using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using XConnect.Models;

namespace XConnect.CommonUtilities
{
    [JsonSourceGenerationOptions(WriteIndented = false)]
    [JsonSerializable(typeof(RaynaXML.Root), TypeInfoPropertyName = "RaynaRoot")]
    [JsonSerializable(typeof(SpeedCategory.Root), TypeInfoPropertyName = "SpeedCategoryRoot")]
    public partial class JsonContexts : JsonSerializerContext
    {
    }
}
