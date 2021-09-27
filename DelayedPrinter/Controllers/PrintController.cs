using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace DelayedPrinter.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PrintController : ControllerBase
    {
        private readonly IPrintScheduler _printScheduler;

        public PrintController(IPrintScheduler printScheduler)
        {
            _printScheduler = printScheduler;
        }

        [HttpPost]
        public async Task<ActionResult> PrintAt([FromBody] DelayedPrintRequest printRequest)
        {
            await _printScheduler.Schedule(printRequest);

            return Ok();
        }
    }
}
