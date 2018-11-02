using System;
using System.Collections.Generic;
using System.Text;

namespace GildtAPI.Model
{
    public class Event
    {
        public int EventId;
        public string name;
        public DateTime StartDate;
        public DateTime EndDate;
        public string image;
        public string location;
        public bool IsActive;
        public string ShortDescription;
        public string LongDescription;

        public List<Event> events;
        public List<Tag> tags { get; set; }
    }
}
