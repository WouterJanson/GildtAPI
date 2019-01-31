using System.Collections.Generic;
using System.Threading.Tasks;

using GildtAPI.DAO;
using GildtAPI.Model;

namespace GildtAPI.Controllers
{
    class TagController : Singleton<TagController>
    {
        public async Task<List<Tag>> GetAllTagsAsync()
        {
            return await TagDAO.Instance.GetAllTagsAsync();
        }

        public async Task<int> CreateTagAsync(string tag)
        {
            return await TagDAO.Instance.CreateTagAsync(tag);
        }

        public async Task<int> DeleteTagAsync(int tagid)
        {
            return await TagDAO.Instance.DeleteTagAsync(tagid);
        }

        public async Task<int> EditTagAsync(string tag, string id)
        {
            return await TagDAO.Instance.EditTagAsync(tag, id);
        }
    }
}
