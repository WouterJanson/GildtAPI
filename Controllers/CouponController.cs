using System.Collections.Generic;
using System.Threading.Tasks;

using GildtAPI.DAO;
using GildtAPI.Model;

namespace GildtAPI.Controllers
{
    class CouponController : Singleton<CouponController>
    {
        public async Task<List<Coupon>> GetAllAsync()
        {
            return await CouponDAO.Instance.GetAll();
        }

        public async Task<Coupon> GetAsync(int id)
        {
            return await CouponDAO.Instance.Get(id);
        }

        public async Task<int> CreateAsync(Coupon coupon)
        {
            return await CouponDAO.Instance.CreateAsync(coupon);
        }

        public async Task<int> DeleteAsync(int id)
        {
            return await CouponDAO.Instance.DeleteAsync(id);
        }

        public async Task<int> EditAsync(Coupon coupon)
        {
            return await CouponDAO.Instance.EditAsync(coupon);
        }

        public async Task<int> SignUpAsync(int couponId, int userId)
        {
            return await CouponDAO.Instance.SignupAsync(couponId, userId);
        }

    }
}
