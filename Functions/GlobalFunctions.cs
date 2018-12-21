using System;
namespace GildtAPI
{
    public class GlobalFunctions
    {
        public static bool checkValidId(string id)
        {
            try
            {
                if (id != null)
                {
                    int Id = Convert.ToInt32(id);
                    return true;
                }
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
