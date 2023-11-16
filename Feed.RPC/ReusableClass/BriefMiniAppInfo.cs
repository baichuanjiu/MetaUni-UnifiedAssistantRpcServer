using System.Text.Json.Serialization;

namespace Feed.RPC.ReusableClass
{
    public class BriefMiniAppInfo
    {
        public BriefMiniAppInfo(string id, string avatar, string type, string name)
        {
            Id = id;
            Avatar = avatar;
            Type = type;
            Name = name;
        }

        [JsonPropertyName("_id")]
        public string Id { get; set; }
        public string Avatar { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
    }
}
