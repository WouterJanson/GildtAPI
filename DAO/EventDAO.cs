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

        public async Task<List<Event>> GetAllEvents()
        {
            // get all events Query
            string sqlStr = $"SELECT Events.Id as EventId, Events.Name, Events.EndDate, Events.StartDate, Events.Image, Events.Location, Events.IsActive, Events.ShortDescription, Events.LongDescription, Tag, TagId FROM Events " +
                $"LEFT JOIN (SELECT EventsTags.EventsId, Tags.Name AS Tag, Tags.Id AS TagId FROM EventsTags " +
                $"LEFT JOIN Tags ON EventsTags.TagsId = Tags.Id) as tags ON Events.Id = tags.EventsId ORDER BY Events.Id";

            SqlConnection conn = DBConnect.GetConnection();

            await AddEventsToList(sqlStr, conn);

            DBConnect.Dispose(conn);

            return events;
        }

        // Get single user
        public async Task<Event> GetTheEvent(int id)
        {
            List<Event> eventslist = await GetAllEvents();

            foreach (Event evenT in eventslist)
            {
                // start looking for the desired event by ID
                if (evenT.Id == id)
                {
                    return evenT;
                }
            }

            // if no results have been found return a null
            return null;
        }

        public async Task<int> DeleteEvent(int id)
        {
            //queries
            string sqlStr = $"DELETE Events WHERE Id = @id";
            int rowsAffected;

            SqlConnection conn = DBConnect.GetConnection();

            using (SqlCommand cmd = new SqlCommand(sqlStr, conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);

                rowsAffected = await cmd.ExecuteNonQueryAsync();
            }

            DBConnect.Dispose(conn);

            return rowsAffected;
        }

        public async Task<int> CreateEvent(Event evenT)
        {
            int EventAlreadyExist = 400; // komt meerdere keren voor, kan dit in een methode ?
            int RowsAffected;

            List<Event> eventsList = await GetAllEvents();

            //check if event already exist
            foreach (Event e in eventsList)
            {
                if (e.Name == evenT.Name && e.StartDate == evenT.StartDate)
                {
                    return EventAlreadyExist;
                }
            }

            // Queries
            string sqlStr =
            $"INSERT INTO Events (Name, Location, StartDate, EndDate, ShortDescription, LongDescription, Image, IsActive) VALUES (@Name, @Location, @StartDate, @EndDate, @ShortDescription, @LongDescription, @Image, 'false')";

            //Connects with the database
            SqlConnection conn = DBConnect.GetConnection();

            using (SqlCommand cmd = new SqlCommand(sqlStr, conn))
            {
                cmd.Parameters.AddWithValue("@Name", evenT.Name);
                cmd.Parameters.AddWithValue("@Location", evenT.Location);
                cmd.Parameters.AddWithValue("@StartDate", evenT.StartDate);
                cmd.Parameters.AddWithValue("@EndDate", evenT.EndDate);
                cmd.Parameters.AddWithValue("@ShortDescription", evenT.ShortDescription);
                cmd.Parameters.AddWithValue("@LongDescription", evenT.LongDescription);
                cmd.Parameters.AddWithValue("@Image", evenT.Image);

                RowsAffected = await cmd.ExecuteNonQueryAsync();
            }

            // Close the database connection
            DBConnect.Dispose(conn);

            return RowsAffected;
        }

        public async Task<int> EditEvent(Event evenT)
        {
            int RowsAffected;

            Event DesiredEvent = await GetTheEvent(evenT.Id);

            //check if event exist
            if (DesiredEvent == null)
            {
                return 0;
            }

            // Queries
            string sqlStr = $"UPDATE Events SET " +
            $"Name = COALESCE({(evenT.Name == null ? "NULL" : "@Name")}, Name), " +
            $"Location = COALESCE({(evenT.Location == null ? "NULL" : "@Location")}, Location), " +
            $"StartDate = COALESCE({(evenT.StartDate == null ? "NULL" : "@StartDate")}, StartDate), " +
            $"EndDate = COALESCE({(evenT.EndDate == null ? "NULL" : "@EndDate")}, EndDate), " +
            $"ShortDescription = COALESCE({(evenT.ShortDescription == null ? "NULL" : "@ShortDescription")}, ShortDescription), " +
            $"LongDescription = COALESCE({(evenT.LongDescription == null ? "NULL" : "@LongDescription")}, LongDescription), " +
            $"Image = COALESCE({(evenT.Image == null ? "NULL" : "@Image")}, image), " +
            $"IsActive = COALESCE({(evenT.IsActive == false ? "NULL" : "@IsActive")}, IsActive) " +
            $" WHERE id = @Id";

            //Connects with the database
            SqlConnection conn = DBConnect.GetConnection();

            using (SqlCommand cmd = new SqlCommand(sqlStr, conn))
            {
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

            // Close the database connection
            DBConnect.Dispose(conn);

            return RowsAffected;
        }

        public async Task<int> EditTag(string tag, string id)
        {
            int RowsAffected;

            //query om te updaten
            string sqlStrUpdate = $"UPDATE Tags SET " +
                                  $"Name = COALESCE({(tag == null ? "NULL" : "@Tag")}, Name)" +
                                  $"Where Id= @Id";

            //Connects with the database
            SqlConnection conn = DBConnect.GetConnection();

            using (SqlCommand cmd = new SqlCommand(sqlStrUpdate, conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@Tag", tag);

                RowsAffected = await cmd.ExecuteNonQueryAsync();
            }

            // Close the database connection
            DBConnect.Dispose(conn);

            return RowsAffected;
        }

        public async Task<int> DeleteTag(int Id)
        {
            //queries
            string sqlStr = $"DELETE Tags WHERE Id = @Id";
            int rowsAffected;

            SqlConnection conn = DBConnect.GetConnection();

            using (SqlCommand cmd = new SqlCommand(sqlStr, conn))
            {
                cmd.Parameters.AddWithValue("@Id", Id);
                rowsAffected = await cmd.ExecuteNonQueryAsync();
            }

            DBConnect.Dispose(conn);

            return rowsAffected;
        }

        public async Task<int> AddTag(int eventId, int tagId)
        {
            int EventDoesNotExist = 400;
            int TagDoesNotExist = 401;
            int TagAlreadyAssigned = 402;
            int RowsAffected;

            // Queries
            string sqlStr = $"INSERT INTO EventsTags (EventsId, TagsId) VALUES (@eventId, @tagId)";
            // querry to validate Tag (does it exist?)
            string sqlTagCheckStr = $"SELECT Id FROM Tags WHERE id = @tagId";

            //Connects with the database
            SqlConnection conn = DBConnect.GetConnection();
            
            Event DesiredEvent = await GetTheEvent(eventId);

            //check if event exist
            if (DesiredEvent == null)
            {
                return EventDoesNotExist;
            }

            for (int i = 0; i < DesiredEvent.Tags.Length; i++)
            {
                if (DesiredEvent.Tags[i].Id == tagId)
                {
                    return TagAlreadyAssigned;
                }
            }

            //check if given tag exist
            using (SqlCommand cmd = new SqlCommand(sqlTagCheckStr, conn))
            {
                cmd.Parameters.AddWithValue("@tagId", tagId);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    // check if the query has found a tag with the given TagId
                    if (reader.HasRows == false)
                    {
                        DBConnect.Dispose(conn);
                        return TagDoesNotExist;                        
                    }
                    reader.Close();                    
                }
            }

            //execute operation if everything is OK
            using (SqlCommand cmd2 = new SqlCommand(sqlStr, conn))
            {
                cmd2.Parameters.AddWithValue("@tagId", tagId);
                cmd2.Parameters.AddWithValue("@eventId", eventId);

                // insert in to the table EventsTags
                RowsAffected = await cmd2.ExecuteNonQueryAsync();
                DBConnect.Dispose(conn);
            }

            return RowsAffected;
        }

        public async Task<int> Createtag(string tag)
        {
            int TagAlreadyExist = 400;
            int RowsAffected;

            // Queries
            string sqlStr = $"INSERT INTO Tags (Name) VALUES (@tag)";
            string sqlTagCheckStr = $"SELECT Name FROM Tags WHERE Name = @tag";

            //Connects with the database
            SqlConnection conn = DBConnect.GetConnection();

            // check if tag already exist in the database to avoid dublicate entries
            using (SqlCommand cmd2 = new SqlCommand(sqlTagCheckStr, conn))
            {
                cmd2.Parameters.AddWithValue("@tag", tag);

                using (SqlDataReader reader = cmd2.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        DBConnect.Dispose(conn);
                        return TagAlreadyExist;
                    }
                    reader.Close();
                }
            }
           
            using (SqlCommand cmd = new SqlCommand(sqlStr, conn))
            {
                cmd.Parameters.AddWithValue("@tag", tag);

                RowsAffected = await cmd.ExecuteNonQueryAsync();
            }

            DBConnect.Dispose(conn);
            return RowsAffected;
        }

        public async Task AddEventsToList(string sqlStr, SqlConnection conn)
        {
            events.Clear();
            using (SqlCommand cmd = new SqlCommand(sqlStr, conn))
            {
                SqlDataReader reader = await cmd.ExecuteReaderAsync();

                Event currentEvent = null;
                List<Tag> currentEventTagsList = new List<Tag>();

                while (reader.Read())
                {
                    Event newEvent = new Event()
                    {
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
                    if (currentEvent == null)
                    {
                        currentEvent = newEvent;
                    }

                    // check if reader got a new event
                    if (currentEvent.Id != newEvent.Id)
                    {
                        currentEvent.Tags = currentEventTagsList.ToArray(); //sla alle tags in de list van "currentEventTags" op in current event.tags zodra een nieuwe event binnenkomt.                       

                        events.Add(currentEvent); //add the event with its tags to the events list                       
                        currentEvent = newEvent; // the new event will now be current event

                        currentEventTagsList = new List<Tag>(); // make a new empty list of "currentEventTags" when a new event has been read
                    }

                    //check if event has tag if not give a 0
                    int tagId = 0;
                    int.TryParse(reader["TagId"].ToString(), out tagId);

                    string tagName = reader["Tag"].ToString();

                    //Add tag to current event tags List
                    currentEventTagsList.Add(new Tag(tagId, tagName));
                }

                //add the last event from the reader 
                currentEvent.Tags = currentEventTagsList.ToArray();
                events.Add(currentEvent);
                reader.Close();
            }
        }

    }
}
