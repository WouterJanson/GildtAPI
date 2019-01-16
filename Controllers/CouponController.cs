using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GildtAPI.DAO;
using GildtAPI.Model;

namespace GildtAPI.Controllers
{
    class CouponController : Singleton<CouponController>
    {
        public async Task<List<Coupon>> GetAll()
        {
            return await CouponDAO.Instance.GetAll();
        }

        public async Task<Coupon> Get(int id)
        {
            return await CouponDAO.Instance.Get(id);
        }

        public async Task<int> Create(Coupon coupon)
        {
            return await CouponDAO.Instance.Create(coupon);
        }

        public async Task<int> Delete(int id)
        {
            return await CouponDAO.Instance.Delete(id);
        }

    }
}
