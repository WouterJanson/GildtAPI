using System.Collections.Generic;
using System.Threading.Tasks;
using GildtAPI.DAO;
using GildtAPI.Model;

namespace GildtAPI.Controllers
{
    class TagController : Singleton<TagController>
    {
        public async Task<List<Tag>> GetAllTags()
        {
            return await TagDAO.Instance.GetAllTags();
        }

        public async Task<int> CreateTag(string tag)
        {
            return await TagDAO.Instance.CreateTag(tag);
        }

        public async Task<int> DeleteTag(int tagid)
        {
            return await TagDAO.Instance.DeleteTag(tagid);
        }

        public async Task<int> EditTag(string tag, string id)
        {
            return await TagDAO.Instance.EditTag(tag, id);
        }
    }
}
