using System;

namespace GildtAPI.Model
{

    public class SongRequest
    {
        public int Id;
        public string Title;
        public string Artist;
        public DateTime DateTime;
        public int UserId;
        public int Upvotes;
        public int Downvotes;
    }
}