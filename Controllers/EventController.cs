using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using GildtAPI.DAO;
using GildtAPI.Model;

namespace GildtAPI.Controllers
{
    class EventController : Singleton<EventController>
    {
        public async Task<List<Event>> GetAll()
        {
            return await EventDAO.Instance.GetAllEvents();
        }

        public async Task<Event> GetEvent(int id)
        {
            return await EventDAO.Instance.GetTheEvent(id);
        }

        public async Task<int> DeleteEvent(int id)
        {
            return await EventDAO.Instance.DeleteEvent(id);
        }

        public async Task<int> CreateEvent(Event evenT)
        {
            return await EventDAO.Instance.CreateEvent(evenT);
        }

        public async Task<int> EditEvent(Event evenT)
        {
            return await EventDAO.Instance.EditEvent(evenT);
        }

        public async Task<int> AddTag(int eventId, int tagId)
        {
            return await EventDAO.Instance.AddTag(eventId, tagId);
        }

        public async Task<int> DeleteTag(int tagid)
        {
            return await EventDAO.Instance.DeleteTag(tagid);
        }

        public async Task<int> EditTag(string tag, string id)
        {
            return await EventDAO.Instance.EditTag(tag, id);
        }

        public async Task<int> CreateTag(string tag)
        {
            return await EventDAO.Instance.Createtag(tag);
        }
    }
}
