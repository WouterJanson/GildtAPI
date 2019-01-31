using System.Collections.Generic;
using System.Threading.Tasks;

using GildtAPI.DAO;
using GildtAPI.Model;

namespace GildtAPI.Controllers
{
    class EventController : Singleton<EventController>
    {
        public async Task<List<Event>> GetAllAsync()
        {
            return await EventDAO.Instance.GetAllEventsAsync();
        }

        public async Task<Event> GetEventAsync(int id)
        {
            return await EventDAO.Instance.GetTheEventAsync(id);
        }

        public async Task<int> DeleteEventAsync(int id)
        {
            return await EventDAO.Instance.DeleteEventAsync(id);
        }

        public async Task<int> CreateEventAsync(Event evenT)
        {
            return await EventDAO.Instance.CreateEventAsync(evenT);
        }

        public async Task<int> EditEventAsync(Event evenT)
        {
            return await EventDAO.Instance.EditEventAsync(evenT);
        }

        public async Task<int> AddTagToEventAsync(int eventId, int tagId)
        {
            return await EventDAO.Instance.AddTagToEventAsync(eventId, tagId);
        }

        public async Task<int> RemoveTagFromEventAsync(int eventId, int tagId)
        {
            return await EventDAO.Instance.RemoveTagFromEventAsync(eventId, tagId);
        }

    }
}
