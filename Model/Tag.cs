using System;
using System.Collections.Generic;
using System.Text;

namespace GildtAPI.Model
{
    public class Tag
    {
        //public int Id { get; set; }
        public string Name { get; set; }
        
    }

    public class TagsEvents
    {
        public Tag Tag;
        public Event Event;
    }
}
