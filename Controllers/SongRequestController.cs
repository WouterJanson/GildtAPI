using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Data.SqlClient;
using GildtAPI.Model;
using System.Net.Http;
using System.Collections.Specialized;
using System.Net;
using System.Linq;
using System.Text.RegularExpressions;
using GildtAPI.Controllers;
using GildtAPI.DAO;

namespace GildtAPI.Controllers
{
    class SongRequestController : Singleton<SongRequestController>
    {

        public async Task<List<SongRequest>> GetAll()
        {
            return await SongRequestDAO.Instance.GetAll();
        }
        public async Task<SongRequest> Get(int id)
        {
            return await SongRequestDAO.Instance.Get(id);
        }
    }
}
