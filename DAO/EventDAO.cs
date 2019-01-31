using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;

using GildtAPI.Model;

namespace GildtAPI.DAO
{
    class EventDAO : Singleton<EventDAO>
    {
        private static List<Event> events = new List<Event>();

        public async Task<List<Event>> GetAllEventsAsync()
        {
            // get all events Query
            string sqlStr = $"SELECT Events.Id as EventId, Events.Name, Events.EndDate, Events.StartDate, Events.Image, Events.Location, Events.IsActive, Events.ShortDescription, Events.LongDescription, Tag, TagId FROM Events " +
                $"LEFT JOIN (SELECT EventsTags.EventsId, Tags.Name AS Tag, Tags.Id AS TagId FROM EventsTags " +
                $"LEFT JOIN Tags ON EventsTags.TagsId = Tags.Id) as tags ON Events.Id = tags.EventsId ORDER BY Events.Id";

            using (var conn = DBConnect.GetConnection()) {
                await AddEventsToListAsync(sqlStr, conn);
            }

            return events;
        }

        public async Task<Event> GetTheEventAsync(int id)
        {
            var eventslist = await GetAllEventsAsync();

            foreach (var evenT in eventslist) {
                // start looking for the desired event by ID
                if (evenT.Id == id) {
                    return evenT;
                }
            }

            // if no results have been found return a null
            return null;
        }

        public async Task<int> DeleteEventAsync(int id)
        {
            //queries
            string sqlStr = $"DELETE Events WHERE Id = @id";
            int rowsAffected;

            using (var conn = DBConnect.GetConnection()) {
                using (var cmd = new SqlCommand(sqlStr, conn)) {
                    cmd.Parameters.AddWithValue("@Id", id);
                    rowsAffected = await cmd.ExecuteNonQueryAsync();
                }

            }

            return rowsAffected;
        }

        public async Task<int> CreateEventAsync(Event evenT)
        {
            int RowsAffected;

            var eventsList = await GetAllEventsAsync();

            //check if event already exist
            foreach (var e in eventsList) {
                if (e.Name == evenT.Name && e.StartDate == evenT.StartDate) {
                    return 0;
                }
            }

            // Queries
            string sqlStr =
            $"INSERT INTO Events (Name, Location, StartDate, EndDate, ShortDescription, LongDescription, Image, IsActive) VALUES (@Name, @Location, @StartDate, @EndDate, @ShortDescription, @LongDescription, @Image, 'false')";

            //Connects with the database
            using (var conn = DBConnect.GetConnection()) {
                using (var cmd = new SqlCommand(sqlStr, conn)) {
                    cmd.Parameters.AddWithValue("@Name", evenT.Name);
                    cmd.Parameters.AddWithValue("@Location", evenT.Location);
                    cmd.Parameters.AddWithValue("@StartDate", evenT.StartDate);
                    cmd.Parameters.AddWithValue("@EndDate", evenT.EndDate);
                    cmd.Parameters.AddWithValue("@ShortDescription", evenT.ShortDescription);
                    cmd.Parameters.AddWithValue("@LongDescription", evenT.LongDescription);
                    cmd.Parameters.AddWithValue("@Image", evenT.Image);

                    RowsAffected = await cmd.ExecuteNonQueryAsync();
                }
            }

            return RowsAffected;
        }

        public async Task<int> EditEventAsync(Event evenT)
        {
            int RowsAffected;

            var DesiredEvent = await GetTheEventAsync(evenT.Id);

            //check if event exist
            if (DesiredEvent == null) {
                return 0;
            }

            // Queries
            string sqlStr = $"UPDATE Events SET " +
                $"Name = COALESCE({(evenT.Name == null ? "NULL" : "@Name")}, Name), " +
                $"Location = COALESCE({(evenT.Location == null ? "NULL" : "@Location")}, Location), " +
                $"StartDate = COALESCE({(evenT.StartDate == DateTime.MinValue ? "NULL" : "@StartDate")}, StartDate), " +
                $"EndDate = COALESCE({(evenT.EndDate == DateTime.MinValue ? "NULL" : "@EndDate")}, EndDate), " +
                $"ShortDescription = COALESCE({(evenT.ShortDescription == null ? "NULL" : "@ShortDescription")}, ShortDescription), " +
                $"LongDescription = COALESCE({(evenT.LongDescription == null ? "NULL" : "@LongDescription")}, LongDescription), " +
                $"Image = COALESCE({(evenT.Image == null ? "NULL" : "@Image")}, image), " +
                $"IsActive = COALESCE({(evenT.IsActive == false ? "NULL" : "@IsActive")}, IsActive) " +
                $" WHERE id = @Id";

            //Connects with the database
            using (var conn = DBConnect.GetConnection()) {
                using (var cmd = new SqlCommand(sqlStr, conn)) {
                    cmd.Parameters.AddWithValue("@Id", ((object)evenT.Id) ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Name", ((object)evenT.Name) ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Location", ((object)evenT.Location) ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@StartDate", ((object)evenT.StartDate) ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@EndDate", ((object)evenT.EndDate) ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ShortDescription", ((object)evenT.ShortDescription) ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@LongDescription", ((object)evenT.LongDescription) ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Image", ((object)evenT.Image) ?? DBNull.Value);

                    RowsAffected = await cmd.ExecuteNonQueryAsync();
                }
            }

            return RowsAffected;
        }

        public async Task<int> AddTagToEventAsync(int eventId, int tagId)
       {
            int RowsAffected;

            // Queries
            string sqlStr = $"INSERT INTO EventsTags (EventsId, TagsId) VALUES (@eventId, @tagId)";
            // querry to validate Tag (does it exist?)
            string sqlTagCheckStr = $"SELECT Id FROM Tags WHERE id = @tagId";

            var DesiredEvent = await GetTheEventAsync(eventId);

            //check if event exist
            if (DesiredEvent == null) {
                return 0;
            }

            // check if tag is already assigned to the event to avoid duplicate tags
            for (int i = 0; i < DesiredEvent.Tags.Length; i++) {
                if (DesiredEvent.Tags[i].Id == tagId) {
                    return 0;
                }
            }

            //Connects with the database
            using (var conn = DBConnect.GetConnection()) {
                //check if given tag exist
                using (var cmd = new SqlCommand(sqlTagCheckStr, conn)) {
                    cmd.Parameters.AddWithValue("@tagId", tagId);

                    using (SqlDataReader reader = cmd.ExecuteReader()) {
                        // check if the query has found a tag with the given TagId, IF SO also close dbconnection otherwise keep it open
                        if (reader.HasRows == false) {
                            return 0;
                        }
                        reader.Close();
                    }
                }

                //execute operation if everything is OK
                using (var cmd2 = new SqlCommand(sqlStr, conn)) {
                    cmd2.Parameters.AddWithValue("@tagId", tagId);
                    cmd2.Parameters.AddWithValue("@eventId", eventId);

                    // insert in to the table EventsTags
                    RowsAffected = await cmd2.ExecuteNonQueryAsync();
                }
            }

            return RowsAffected;
        }

        public async Task<int> RemoveTagFromEventAsync(int eventId, int tagId)
        {
            int RowsAffected;

            // Queries
            string sqlStr = $"DELETE EventsTags WHERE EventsId = @eventId AND TagsId = @tagId";
            // querry to validate Tag (does it exist?)
            string sqlTagCheckStr = $"SELECT Id FROM Tags WHERE id = @tagId";

            var DesiredEvent = await GetTheEventAsync(eventId);

            //check if event exist
            if (DesiredEvent == null) {
                return 0;
            }

            //Connects with the database
            using (var conn = DBConnect.GetConnection()) {
                //check if given tag exist
                using (var cmd = new SqlCommand(sqlTagCheckStr, conn)) {
                    cmd.Parameters.AddWithValue("@tagId", tagId);

                    using (var reader = cmd.ExecuteReader()) {
                        // check if the query has found a tag with the given TagId,IF SO also close dbconnection otherwise keep it open
                        if (reader.HasRows == false) {
                            return 0;
                        }
                        reader.Close();
                    }
                }

                //Proceed to remove if everything is OK
                using (SqlCommand cmd2 = new SqlCommand(sqlStr, conn)) {
                    cmd2.Parameters.AddWithValue("@tagId", tagId);
                    cmd2.Parameters.AddWithValue("@eventId", eventId);

                    // insert in to the table EventsTags
                    RowsAffected = await cmd2.ExecuteNonQueryAsync();
                }
            }

            return RowsAffected;
        }

        public async Task AddEventsToListAsync(string sqlStr, SqlConnection conn)
        {
            events.Clear();
            using (var cmd = new SqlCommand(sqlStr, conn)) {
                var reader = await cmd.ExecuteReaderAsync();

                Event currentEvent = null;
                var currentEventTagsList = new List<Tag>();

                while (reader.Read()) {
                    var newEvent = new Event() {
                        //read event
                        Id = Convert.ToInt32(reader["EventId"]),
                        Name = reader["Name"].ToString(),
                        StartDate = DateTime.Parse(reader["StartDate"].ToString()),
                        EndDate = DateTime.Parse(reader["EndDate"].ToString()),
                        Image = reader["Image"].ToString(),
                        Location = reader["location"].ToString(),
                        IsActive = (bool)reader["IsActive"],
                        ShortDescription = reader["ShortDescription"].ToString(),
                        LongDescription = reader["LongDescription"].ToString()
                    };

                    //check if it is the first event from the reader
                    if (currentEvent == null) {
                        currentEvent = newEvent;
                    }


                    //check if event has tag if not give a 0
                    int.TryParse(reader["TagId"].ToString(), out int tagId);
                    string tagName = reader["Tag"].ToString();

                    //Add tag to current event tags List
                    currentEventTagsList.Add(new Tag(tagId, tagName));

                    // keep checking if a unique event has been read, if so save the gathered tags to the previous event and make a new list(tags) for the new event
                    if (currentEvent.Id != newEvent.Id) {
                        currentEvent.Tags = currentEventTagsList.ToArray(); //sla alle tags in de list van "currentEventTags" op in current event.tags zodra een nieuwe event binnenkomt.                       

                        events.Add(currentEvent); //add the event with its tags to the events list                       
                        currentEvent = newEvent; // the new event will now be current event

                        currentEventTagsList = new List<Tag>(); // make a new empty list of "currentEventTags" when a new event has been read
                    }
                }

                //add the last event from the reader 
                currentEvent.Tags = currentEventTagsList.ToArray();
                events.Add(currentEvent);
                reader.Close();
            }
        }

    }
}
