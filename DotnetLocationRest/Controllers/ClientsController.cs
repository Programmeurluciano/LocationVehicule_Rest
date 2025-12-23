using Microsoft.AspNetCore.Mvc;
using DotnetLocationRest.Data;
using DotnetLocationRest.Models;

namespace DotnetLocationRest.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientsController : ControllerBase
    {
        private readonly ClientRepository _repo;

        public ClientsController(ClientRepository repo)
        {
            _repo = repo;
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok(_repo.GetAll());
        }


        //private IActionResult Ok(List<Client> clients)
        //{
        //    throw new NotImplementedException();
        //}

        [HttpPost]
        public IActionResult Post(Client client)
        {
            _repo.Add(client);
            return Ok("Client ajouté");
        }

        //private IActionResult Ok(string v)
        //{
        //    throw new NotImplementedException();
        //}
    }

}
