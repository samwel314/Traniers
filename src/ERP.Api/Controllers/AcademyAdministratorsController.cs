using ERP.Application.Common.Security;
using ERP.Application.Modules.AcademyAdmin;
using ERP.Application.Modules.AcademyAdmin.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Api.Controllers 
{ 


    [ApiController]
    [Route("api/academy-administrators")]
    [Produces("application/json")]
   
    public sealed class AcademyAdministratorsController(
    IAcademyAdministratorService service)
    : ApiControllerBase
    {
        [Authorize(Roles = Roles.Administrator)]
        [HttpPost]
        public async Task<IActionResult> Assign(
            AssignAcademyAdministratorRequest request,
            CancellationToken cancellationToken)
        {
            var result = await service.AssignAsync(
                request.AcademyId,
                request.UserId,
                cancellationToken);

            return ToActionResult(result);
        }

        //[HttpDelete("academies/{academyId:guid}/administrators/{userId:guid}")]
        //public async Task<IActionResult> Remove(
        //    Guid academyId,
        //    Guid userId,
        //    CancellationToken cancellationToken)
        //{
        //    var result = await service.RemoveAsync(
        //        academyId,
        //        userId,
        //        cancellationToken);

        //    return ToActionResult(result);
        //}

        [HttpGet("my-academies")]
        [Authorize(Roles = Roles.AcademyAdministrator)]
        public async Task<IActionResult> GetMyAcademies(
            CancellationToken cancellationToken)
        {
            var result = await service.GetMyAcademiesAsync(
                cancellationToken);

            return ToActionResult(result);
        }
    }
}
