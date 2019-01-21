using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net;
using System.Threading.Tasks;
using GildtAPI.Model;
using Microsoft.AspNetCore.Http;

namespace GildtAPI.DAO
{
    class EventDAO : Singleton<EventDAO>
    {
        private static List<Event> events = new List<Event>();

        public async Task<List<Event>> GetAllEvents()
        {
            // get all events Query
            var sqlStr = $"SELECT Events.Id as EventId, Events.Name, Events.EndDate, Events.StartDate, Events.Image, Events.Location, Events.IsActive, Events.ShortDescription, Events.LongDescription, Tag, TagId FROM Events " +
                $"LEFT JOIN (SELECT EventsTags.EventsId, Tags.Name AS Tag, Tags.Id AS TagId FROM EventsTags " +
                $"LEFT JOIN Tags ON EventsTags.TagsId = Tags.Id) as tags ON Events.Id = tags.EventsId ORDER BY Events.Id";

            SqlConnection conn = DBConnect.GetConnection();

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
            var sqlStr = $"DELETE Events WHERE Id = '{id}'";
            int rowsAffected;

            SqlConnection conn = DBConnect.GetConnection();

            using (SqlCommand cmd = new SqlCommand(sqlStr, conn))
            {
                rowsAffected = await cmd.ExecuteNonQueryAsync();
            }

            DBConnect.Dispose(conn);

            return rowsAffected;
        }

        public async Task<int> AddEvent(Event evenT)
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
            var sqlStr =
            $"INSERT INTO Events (Name, Location, StartDate, EndDate, ShortDescription, LongDescription, Image, IsActive) VALUES ('{evenT.Name}', '{evenT.Location}', '{evenT.StartDate}', '{evenT.EndDate}', '{evenT.ShortDescription}', '{evenT.LongDescription}', '{evenT.Image}', 'false')";

            //Connects with the database
            SqlConnection conn = DBConnect.GetConnection();

            using (SqlCommand cmd = new SqlCommand(sqlStr, conn))
            {
                RowsAffected = await cmd.ExecuteNonQueryAsync();
            }

            // Close the database connection
            DBConnect.Dispose(conn);

            return RowsAffected;
        }

        public async Task<int> EditEvent(Event evenT)
        {
            int EventDoesNotExist = 400;
            int RowsAffected;

            Event DesiredEvent = await GetTheEvent(evenT.Id);

            //check if event exist
            if (DesiredEvent == null)
            {
                return EventDoesNotExist;
            }

            // Queries
            var sqlStr = $"UPDATE Events SET " +
            $"Name = COALESCE({(evenT.Name == null ? "NULL" : $"\'{evenT.Name}\'")}, Name), " +
            $"Location = COALESCE({(evenT.Location == null ? "NULL" : $"\'{evenT.Location}\'")}, Location), " +
            $"StartDate = COALESCE({(evenT.StartDate == null ? "NULL" : $"\'{evenT.StartDate}\'")}, StartDate), " +
            $"EndDate = COALESCE({(evenT.EndDate == null ? "NULL" : $"\'{evenT.EndDate}\'")}, EndDate), " +
            $"ShortDescription = COALESCE({(evenT.ShortDescription == null ? "NULL" : $"\'{evenT.ShortDescription}\'")}, ShortDescription), " +
            $"LongDescription = COALESCE({(evenT.LongDescription == null ? "NULL" : $"\'{evenT.LongDescription}\'")}, LongDescription), " +
            $"Image = COALESCE({(evenT.Image == null ? "NULL" : $"\'{evenT.Image}\'")}, image), " +
            $"IsActive = COALESCE({(evenT.IsActive == null ? "NULL" : $"\'{evenT.IsActive}\'")}, IsActive) " +
            $" WHERE id = {evenT.Id};";

            //Connects with the database
            SqlConnection conn = DBConnect.GetConnection();

            using (SqlCommand cmd = new SqlCommand(sqlStr, conn))
            {
                RowsAffected = await cmd.ExecuteNonQueryAsync();
            }

            // Close the database connection
            DBConnect.Dispose(conn);

            return RowsAffected;
        }

        public async Task<int> AddTag(int eventId, int tagId)
        {
            int EventDoesNotExist = 400;
            int TagDoesNotExist = 401;
            int TagAlreadyAssigned = 402;
            int RowsAffected;

            // Queries
            var sqlStr = $"INSERT INTO EventsTags (EventsId, TagsId) VALUES ('{eventId}', '{tagId}')";
            // querry to validate Tag (does it exist?)
            var sqlTagCheckStr = $"SELECT Id FROM Tags WHERE id ='{tagId}'";
            // querry to check if the given tag is already assigned to a event
            var SqlCheckIfAssigned = $"SELECT TagsId, EventsId FROM EventsTags WHERE TagsId = '{tagId}' AND EventsId = '{eventId}'";

            //Connects with the database
            SqlConnection conn = DBConnect.GetConnection();

            //check if event exist
            Event DesiredEvent = await GetTheEvent(eventId);

            if (DesiredEvent == null)
            {
                return EventDoesNotExist;
            }

            //check if given tag exist
            using (SqlCommand cmd = new SqlCommand(sqlTagCheckStr, conn))
            {
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    // check if the query has found a tag with the given TagId
                    if (reader.HasRows == false)
                    {
                        reader.Close();
                        // Close the database connection
                        DBConnect.Dispose(conn);
                        return TagDoesNotExist;
                    }
                }
            }

            //check if a tag is already assigned to the event
            using (SqlCommand cmd2 = new SqlCommand(SqlCheckIfAssigned, conn))
            {
                using (SqlDataReader reader2 = cmd2.ExecuteReader())
                {
                    // check if the query has found a tag with the given TagId
                    if (reader2.HasRows == true)
                    {
                        reader2.Close();
                        // Close the database connection
                        DBConnect.Dispose(conn);
                        return TagAlreadyAssigned;
                    }
                }
            }

            //execute operation if everything is OK
            using (SqlCommand cmd3 = new SqlCommand(sqlStr, conn))
            {
                // insert in to the table EventsTags
                RowsAffected = await cmd3.ExecuteNonQueryAsync();
                DBConnect.Dispose(conn);
            }

            return RowsAffected;
        }

        public async Task<int> EditTag(Tag tag)
        {
            int RowsAffected;

            //query om te updaten
            string sqlStrUpdate = $"UPDATE Tags SET " +
                                  $"Name = COALESCE({(tag.Name == null ? "NULL" : $"'{tag.Name}'")}, Name)" +
                                  $"Where Id= {tag.Id}";

            //Connects with the database
            SqlConnection conn = DBConnect.GetConnection();

            using (SqlCommand cmd = new SqlCommand(sqlStrUpdate, conn))
            {
                RowsAffected = await cmd.ExecuteNonQueryAsync();
            }

            // Close the database connection
            DBConnect.Dispose(conn);

            return RowsAffected;
        }

        public async Task<int> DeleteTag(int tagId)
        {
            //queries
            var sqlStr = $"DELETE Tags WHERE Id = '{tagId}'";
            int rowsAffected;

            SqlConnection conn = DBConnect.GetConnection();

            using (SqlCommand cmd = new SqlCommand(sqlStr, conn))
            {
                rowsAffected = await cmd.ExecuteNonQueryAsync();
            }

            DBConnect.Dispose(conn);

            return rowsAffected;
        }

        public async Task<int> Createtag(string tag)
        {
            int TagAlreadyExist = 400;
            int RowsAffected;

            // Queries
            var sqlStr = $"INSERT INTO Tags (Name) VALUES ('{tag}')";
            var sqlTagCheckStr = $"SELECT Name FROM Tags WHERE Name ='{tag}'";

            //Connects with the database
            SqlConnection conn = DBConnect.GetConnection();

            // check if tag already exist in the database to avoid dublicate entries
            using (SqlCommand cmd2 = new SqlCommand(sqlTagCheckStr, conn))

            using (SqlDataReader reader = cmd2.ExecuteReader())
            {
                //check if tag already exist in the database
                if (reader.HasRows)
                {
                    // Close the database connection
                    DBConnect.Dispose(conn);

                    return TagAlreadyExist;
                }

                reader.Close();
            }

            using (SqlCommand cmd = new SqlCommand(sqlStr, conn))
            {
                RowsAffected = await cmd.ExecuteNonQueryAsync();
            }

            // Close the database connection
            DBConnect.Dispose(conn);
            return RowsAffected;
        }

    }
}
