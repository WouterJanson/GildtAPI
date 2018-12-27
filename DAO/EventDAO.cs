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

        public async Task<List<Event>> GetAll()
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

                    //read the TagId + TagName of the corresponding Event
                    int tagId = Convert.ToInt32(reader["TagId"]);
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
        public async Task<Event> Get(int id)
        {
            List<Event> eventslist = await GetAll();

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

    }
}
