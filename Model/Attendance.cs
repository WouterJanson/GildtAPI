namespace GildtAPI.Model
{
    class Attendance
    {
        public int UserId { get; set; }
        public int EventId { get; set; }
        public string UserName { get; set; }
        public Attendance(int userId, int eventId, string userName)
        {
            this.UserId = userId;
            this.EventId = eventId;
            this.UserName = userName;
        }
    }
}
