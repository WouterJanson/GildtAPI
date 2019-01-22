using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GildtAPI
{
    public class GlobalFunctions
    {
        // Check if the Id of the object exists.
        public static bool CheckValidId(string id)
        {
            try
            {
                // Checks if the id is not empty
                if (id != null)
                {
                    int Id = Convert.ToInt32(id);

                    // Checks if the id is 0 or larger
                    if(Id >= 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        //Check if all the inputs are filled in.
        public static bool CheckInputs(params string[] values)
        {
            foreach (string value in values)
            {
                if(value == null)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
