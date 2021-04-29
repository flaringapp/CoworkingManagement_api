using System.Collections.Generic;
using System.Linq;
using Coursework2021DB.DB;
using Microsoft.AspNetCore.Mvc;

namespace Coursework2021Api.Controllers.Managers
{
    [ApiController]
    public class ManagersController : ControllerBase
    {
        private readonly CourseDBContext context;

        public ManagersController(CourseDBContext context)
        {
            this.context = context;
        }

        [HttpGet("/api/managers")]
        public ActionResult<List<ManagerResponse>> Get()
        {
            var managers = context.Managers.Select(manager => ResponseForModel(manager))
                .ToList();
            return managers;
        }

        [HttpGet("/api/manager")]
        public ActionResult<ManagerResponse?> Get([FromQuery] string id)
        {
            var manager = GetById(id);

            if (manager == null) return BadRequest("No manager found for id " + id);
            return ResponseForModel(manager);
        }

        [HttpPost("/api/manager")]
        public ActionResult<ManagerResponse> Post([FromBody] EditManagerRequest request)
        {
            var manager = GetById(request.Id);
            if (manager == null) return BadRequest("Cannot find manager for id " + request.Id);

            EditDBModel(manager, request);

            context.Managers.Update(manager);
            SaveChanges(manager);
            return ResponseForModel(manager);
        }

        [HttpPut("/api/manager")]
        public ActionResult<ManagerResponse> Put([FromBody] AddManagerRequest request)
        {
            var manager = CreateDBModel(request);
            context.Managers.Add(manager);
            SaveChanges(manager);
            return ResponseForModel(manager);
        }

        [HttpDelete("/api/manager")]
        public ActionResult Delete([FromQuery] string id)
        {
            var manager = GetById(id);
            if (manager == null) return BadRequest("Cannot find manager by given id");

            context.Managers.Remove(manager);
            context.SaveChanges();

            return Ok();
        }

        private Manager? GetById(string id)
        {
            var idInt = int.Parse(id);
            return context.Managers.FirstOrDefault(m => m.Id == idInt);
        }

        private void SaveChanges(Manager model)
        {
            context.SaveChanges();
            context.Entry(model.ManagerLocation).Reference(ml => ml.Location).Load();
            context.Entry(model.ManagerLocation).Reference(ml => ml.Manager).Load();
            context.Entry(model).Reference(r => r.ManagerLocation).Load();
            context.Entry(model).Collection(r => r.Transactions).Load();
        }

        private static ManagerResponse ResponseForModel(Manager manager)
        {
            return new()
            {
                Id = manager.Id.ToString(),
                FirstName = manager.FirstName,
                LastName = manager.LastName,
                Email = manager.Email,
                Description = manager.Description,
                LocationId = manager.ManagerLocation?.LocationId.ToString(),
                LocationName = manager.ManagerLocation?.Location?.Name,
                TimeCreated = manager.TimeCreated,
                TimeUpdated = manager.TimeUpdated
            };
        }

        private static Manager CreateDBModel(AddManagerRequest request)
        {
            var managerLocation = new ManagerLocation
            {
                LocationId = int.Parse(request.LocationId)
            };
            return new Manager
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                Description = request.Description,
                ManagerLocation = managerLocation,
                TimeCreated = request.TimeCreated,
                TimeUpdated = request.TimeUpdated
            };
        }

        private static void EditDBModel(Manager model, EditManagerRequest request)
        {
            var managerLocation = new ManagerLocation
            {
                LocationId = int.Parse(request.LocationId)
            };
            model.FirstName = request.FirstName;
            model.LastName = request.LastName;
            model.Email = request.Email;
            model.Description = request.Description;
            model.ManagerLocation = managerLocation;
            model.TimeCreated = request.TimeCreated;
            model.TimeUpdated = request.TimeUpdated;
        }
    }
}
