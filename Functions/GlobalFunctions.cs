using System;

namespace GildtAPI
{
    public class GlobalFunctions
    {
        // Check if the Id of the object exists.
        public static bool CheckValidId(params string[] ids)
        {
            bool valid = false;
            try
            {
                foreach(string id in ids)
                {
                    // Checks if the id is not empty
                    if (id != null)
                    {
                        int Id = Convert.ToInt32(id);

                        // Checks if the id is a positive number
                        if (Id >= 0)
                        {
                            valid = true;
                        }
                        else
                        {
                            valid = false;
                        }
                    }
                }
                
            }
            catch
            {
                return false;
            }

            return valid;
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
