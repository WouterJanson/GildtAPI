using System;
using Newtonsoft.Json;

namespace GildtAPI.Model
{
    public class Event
    {
        [JsonProperty]
        public int Id { get; set; }

        [JsonProperty]
        public string Name { get; set; }

        [JsonProperty]
        public DateTime StartDate { get; set; }

        [JsonProperty]
        public DateTime EndDate { get; set; }

        [JsonProperty]
        public string Image { get; set; }

        [JsonProperty]
        public string Location { get; set; }

        [JsonProperty]
        public bool IsActive { get; set; }

        [JsonProperty]
        public string ShortDescription { get; set; }

        [JsonProperty]
        public string LongDescription { get; set; }

        [JsonProperty]
        public Tag[] Tags { get; set; }

    }
}
