using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GildtAPI.DAO;
using GildtAPI.Model;

namespace GildtAPI.Controllers
{
    class UserController : Singleton<UserController>
    {
        public async Task<List<User>> GetAll()
        {
            return await UserDAO.Instance.GetAll();
        }

        public async Task<User> Get(int id)
        {
            return await UserDAO.Instance.Get(id);
        }

        public async Task<int> Delete(int id)
        {
            return await UserDAO.Instance.Delete(id);
        }

        public async Task<int> Create(User user)
        {
            return await UserDAO.Instance.Create(user);
        }

        public async Task<int> Edit(User user)
        {
            return await UserDAO.Instance.Edit(user);
        }
    }
}
