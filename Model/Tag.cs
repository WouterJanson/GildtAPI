using Newtonsoft.Json;

namespace GildtAPI.Model
{
    public class Tag
    {
        [JsonProperty]
        public int Id { get; set; }
        [JsonProperty]
        public string Name { get; set; }

        public Tag(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}