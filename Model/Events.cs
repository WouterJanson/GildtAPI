using System;
using System.Collections.Generic;
using System.Text;

namespace GildtAPI.Model
{
    public class Event
    {
        public int EventId;
        public string name;
        public DateTime DateTime;
        public string image;
        public string location;
        public bool IsActive;
        public string ShortDescription;
        public string LongDescription;
        public List<Event> events;
    }
}
