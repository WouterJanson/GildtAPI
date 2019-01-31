using System.Collections.Generic;
using System.Threading.Tasks;

using GildtAPI.DAO;
using GildtAPI.Model;

namespace GildtAPI.Controllers
{
    class SongRequestController : Singleton<SongRequestController>
    {

        public async Task<List<SongRequest>> GetAllSongrequestsAsync()
        {
            return await SongRequestDAO.Instance.GetAllSongrequestsAsync();
        }

        public async Task<SongRequest> GetSongrequestAsync(int id)
        {
            return await SongRequestDAO.Instance.GetSongrequestAsync(id);
        }

        public async Task<int> DeleteSongrequestAsync(int id)
        {
            return await SongRequestDAO.Instance.DeleteSongrequestAsync(id);
        }

        public async Task<int> AddSongRequestAsync(SongRequest song)
        {
            return await SongRequestDAO.Instance.AddSongRequestAsync(song);
        }

        public async Task<int> UpVoteAsync(int RequestId, int UserId)
        {
            return await SongRequestDAO.Instance.UpvoteAsync(RequestId, UserId);
        }

        public async Task<int> DownvoteAsync(int RequestId, int UserId)
        {
            return await SongRequestDAO.Instance.DownvoteAsync(RequestId, UserId);
        }

    }
}
