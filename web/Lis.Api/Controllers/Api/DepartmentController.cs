using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models;
using LIS.Logger;
using System;
using System.Collections.Generic;
using System.Web.Http;

namespace Lis.Api.Controllers.Api
{
    public class DepartmentController : ApiController
    {
        private IDepartmentManager manager;
        private ILogger logger;
        public DepartmentController(IDepartmentManager departmentManager, ILogger Logger)
        {
            manager = departmentManager;
            logger = Logger;
        }

        [AllowAnonymous]
        [HttpGet]
        public IEnumerable<Departments> Get()
        {
            try
            {
                var departments = manager.Get();
                return departments;
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return null;
            }
        }
    }
}

