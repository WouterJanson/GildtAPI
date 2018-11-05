using System;
using System.Collections.Generic;
using System.Text;

namespace GildtAPI.Model
{
    public class Event
    {
        public int Id;
        public string Name;
        public DateTime StartDate;
        public DateTime EndDate;
        public string Image;
        public string Location;
        public bool IsActive;
        public string ShortDescription;
        public string LongDescription;
        public Tag[] Tags;
        
    }
}
