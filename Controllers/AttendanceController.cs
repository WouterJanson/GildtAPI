﻿using System.Threading.Tasks;

using GildtAPI.DAO;

namespace GildtAPI.Controllers
{
    class AttendanceController : Singleton<AttendanceController>
    {
        public async Task<bool> CheckVerificationAsync(int userId, int eventId)
        {
            return await AttendanceDAO.Instance.CheckVerificationAsync(userId, eventId);
        }
    }
}
