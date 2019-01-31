using System.Collections.Generic;
using System.Threading.Tasks;

using GildtAPI.DAO;
using GildtAPI.Model;

namespace GildtAPI.Controllers
{
    class SongRequestController : Singleton<SongRequestController>
    {

        public async Task<List<SongRequest>> GetAllSongrequests()
        {
            return await SongRequestDAO.Instance.GetAllSongrequests();
        }
        public async Task<SongRequest> GetSongrequest(int id)
        {
            return await SongRequestDAO.Instance.GetSongrequest(id);
        }

        public async Task<int> DeleteSongrequest(int id)
        {
            return await SongRequestDAO.Instance.DeleteSongrequest(id);
        }

        public async Task<int> AddSongRequest(SongRequest song)
        {
            return await SongRequestDAO.Instance.AddSongRequest(song);

        }
        public async Task<int> UpVote(int RequestId, int UserId)
        {
            return await SongRequestDAO.Instance.Upvote(RequestId, UserId);
        }
        public async Task<int> Downvote(int RequestId, int UserId)
        {
            return await SongRequestDAO.Instance.Downvote(RequestId, UserId);
        }
      
    }
}
