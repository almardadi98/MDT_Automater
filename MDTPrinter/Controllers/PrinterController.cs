using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using MDTPrinter.Filters;


namespace MDTPrinter.Controllers
{
    //http://codingsonata.com/secure-asp-net-core-web-api-using-api-key-authentication/
    [ApiKeyAuth]
    [Route("api/Printer")]
    [ApiController]
    public class PrinterController : ControllerBase
    {
        // requires using Microsoft.Extensions.Configuration;
        private readonly IConfiguration Configuration;
        public PrinterController(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        private void HandlePrinted(int status, object value)
        {
            Console.WriteLine("Printing completed");
        }

        private IActionResult PrintLabel(string Text)
        {
            if (String.IsNullOrEmpty(Text))
            {
                return BadRequest("Text is null");
            }
            try
            {
                string pathtoLBX = Configuration["PathToLBX"];
                bpac.Document doc = new bpac.Document();

                doc.Printed += new bpac.IPrintEvents_PrintedEventHandler(HandlePrinted);


                doc.Open(pathtoLBX);

                // Virkar þetta?
                //string PrinterName = doc.GetPrinterName();
                string PrinterName = Configuration["PrinterName"];


                bool textprinter = doc.SetPrinter(PrinterName, true);
                doc.GetObject("ComputerName").Text = Text;
                //doc.GetObject("ComputerName").Width = Text.Length;
                /*
                    LONG bpac::IDocument::Length [get, set] 

                    Sets or acquires the tape length 
                    Sets or acquires the tape length being printed.
                    The unit is 1440dpi.
                    When a value of 0 or below is specified, the length becomes unfixed and the length is automatically adjusted to fit the object in the print area. 
                    */
                doc.Length = 0;
                // Start print job setup
                doc.StartPrint("", bpac.PrintOptionConstants.bpoAutoCut);
                // Add print job
                doc.PrintOut(1, bpac.PrintOptionConstants.bpoAutoCut);
                // End of print job setup
                // Wait if printer is offline
                if(doc.Printer.IsPrinterOnline(PrinterName) == false)
                {
                    StatusCode(StatusCodes.Status500InternalServerError, $"Printer not connected");
                }
                doc.EndPrint();
                doc.Close();
                return Ok($"{Text} successfully printed");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error: {ex.Message}");
            }
        }



        private IActionResult MockPrintLabel(string Text)
        {
            if(string.IsNullOrEmpty(Text) == false)
            {
                Console.WriteLine(Text);
                return Ok($"{Text} successfully printed");
            }
            else
            {
                return BadRequest("Text is null");
            }
        }


        [HttpPost]
        public IActionResult Post(string Text)
        {
            return PrintLabel(Text);
        }
    }
}
