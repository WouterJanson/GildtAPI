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
            return await EventDAO.Instance.GetAll();
        }

        public async Task<Event> Get(int id)
        {
            return await EventDAO.Instance.Get(id);
        }

    }
}
