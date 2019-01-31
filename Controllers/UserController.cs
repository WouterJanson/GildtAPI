using System.Collections.Generic;
using System.Threading.Tasks;

using GildtAPI.DAO;
using GildtAPI.Model;

namespace GildtAPI.Controllers
{
    class UserController : Singleton<UserController>
    {
        public async Task<List<User>> GetAllAsync()
        {
            return await UserDAO.Instance.GetAllAsync();
        }

        public async Task<User> GetAsync(int id)
        {
            return await UserDAO.Instance.GetAsync(id);
        }

        public async Task<int> DeleteAsync(int id)
        {
            return await UserDAO.Instance.DeleteAsync(id);
        }

        public async Task<int> CreateAsync(User user)
        {
            return await UserDAO.Instance.CreateAsync(user);
        }

        public async Task<int> EditAsync(User user)
        {
            return await UserDAO.Instance.EditAsync(user);
        }
    }
}
