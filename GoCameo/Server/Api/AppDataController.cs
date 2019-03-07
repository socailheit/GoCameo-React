using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Authentication;
using GoCameo.Server.Business;

namespace GoCameo.Server.Api
{
    [Produces("application/json")]
    [Route("api/AppData")]
    public class AppDataController : Controller
    {
        private IHostingEnvironment _env;
        private ServiceBL _service;
        public AppDataController(IHostingEnvironment env)
        {
            _env = env;
            _service = new ServiceBL();
        }
        [DisableCors]
        [HttpGet("get")]
        public IActionResult Get(string id)
        {
            try
            {
                dynamic formUI = _service.GetMetadataAsync(id, _env.ContentRootPath, AzureAdB2COptions.Settings.tenant);
                return formUI;

            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Please try after sometime");
                return BadRequest();
            }

        }
        public string GetFileData(string fileName)
        {
            StreamReader oRead;
            string filepath = System.IO.Path.Combine(_env.ContentRootPath, "Forms");
            oRead = System.IO.File.OpenText(Path.Combine(filepath, fileName));  //For Local
            string ret = oRead.ReadToEnd();
            oRead.Close();
            return ret;
        }
    }
}