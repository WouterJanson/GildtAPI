using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using GildtAPI.DAO;

namespace GildtAPI.Controllers
{
    class AttendanceController : Singleton<AttendanceController>
    {
        public async Task<bool> CheckVerification(int userId, int eventId)
        {
            return await AttendanceDAO.Instance.CheckVerification(userId, eventId);
        }
    }
}
