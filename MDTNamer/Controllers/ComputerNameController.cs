using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.DirectoryServices.AccountManagement;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using System.Collections;
using Microsoft.Extensions.Configuration;
using MDTNamer.Filters;

namespace MDTNamer.Controllers
{
    [Route("api/GetComputerName")]
    public class ComputerNameController : ControllerBase
    {
        private readonly IConfiguration Configuration;

        private static Hashtable MACAddresses = new Hashtable();

        public ComputerNameController(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        // Býr til computer object til að taka frá nafnið í AD.
        private string CreateComputer(string ComputerName)
        {
            //https://docs.microsoft.com/en-us/dotnet/api/system.directoryservices.accountmanagement.principal?view=dotnet-plat-ext-5.0
            var ctx = new PrincipalContext(ContextType.Domain, Configuration["ADNetBiosName"], Configuration["ADComputerOUPath"]);
            ComputerPrincipal computerPrincipal = new ComputerPrincipal(ctx);
            //string ComputerName = (string)MACAddresses[MACAddress];
            computerPrincipal.Name = ComputerName;
            // Athuga hvort að tölva með þetta nafn sé þegar til í AD
            var searcher = new PrincipalSearcher();
            searcher.QueryFilter = computerPrincipal;
            var results = searcher.FindAll();
            if(results.Any() == true)
            {
                return $"{ComputerName} already exists in AD.";
            }
            else
            {                
                //Ef ekki þá má halda áfram
                computerPrincipal.Description = $"Created by MDT Web Service";
                computerPrincipal.Enabled = true;
                computerPrincipal.Save();
                return $"{ComputerName} created in AD.";
            }
        }

        private string GetComputerName(string isLaptop)
        {
            // Listi sem geymir raðnúmerin
            var ComputerNumbers = new List<int>();
            // AD context - Stilligildi í appsettings.json 
            var ctx = new PrincipalContext(ContextType.Domain, Configuration["ADNetBiosName"], Configuration["ADOUPath"]);
            
            // Leita að öllum tölvum sem heita OUTRUN-* í ad contextinu.
            ComputerPrincipal computerPrincipal = new ComputerPrincipal(ctx);
            computerPrincipal.Name = "OUTRUN-*";
            var searcher = new PrincipalSearcher();
            searcher.QueryFilter = computerPrincipal;
            var results = searcher.FindAll();
            foreach (Principal p in results)
            {
                //Bara tölvur sem byrja á OUTRUN- og enda á fjögurra stafa tölu auk optional bókstaf (F)
                // Ef tölva heitir t.d. OUTRUN-test20 þá verður hún filteruð út hérna. Kemur í veg fyrir villur.  
                if (Regex.IsMatch(p.Name, @"OUTRUN-\d\d\d\d?", RegexOptions.IgnoreCase))
                {
                    string Number = new string (p.Name.Where(Char.IsDigit).ToArray());
                    ComputerNumbers.Add(int.Parse(Number));
                }
            }

            //Ef listinn er tómur
            if(ComputerNumbers.Count == 0)
            {
                throw new ArgumentException("Computer numbers list is empty!");
            }

            // Finna hæstu tölu í listanum og bæta einum við.
            int NextNumber = ComputerNumbers.Max() + 1;
            // Bætir við núllum fyrir framan ef talan er ekki 4 tölustafir. T.d. ef talan er 99 þá verður strengurinn 0099
            string NextName = "OUTRUN-" + NextNumber.ToString().PadLeft(4,'0');

            // Bæta við F ef tölva er fartölva
            if (isLaptop == "True")
            {
                NextName += "F";
            }

            // Annað check. Passar að nafninu sem verður skilað sé örugglega rétt formattað
            if (Regex.IsMatch(NextName, @"OUTRUN-\d\d\d\d?"))
            {
                return NextName;
            }
            else
            {
                throw new ArgumentException("Name does not fit OUTRUN-\\d\\d\\d\\d? template");
            }
        }

        // API route sem er læst með key. 
        // MDT TS sendir staðfestingu þegar imaging er byrjað. (POST request)
        [ApiKeyAuth]
        [Route("/api/GetComputerName/ConfirmCreate")]
        [HttpPost]
        public IActionResult Create(string OSDComputerName)
        {


            string msg = CreateComputer(OSDComputerName);
            return Ok(msg);
            

        }

        /**********************************************************************************************************
        * Lýsing: Finnur næsta lausa nafn í AD og skilar því.
        *         Ef tölva (MAC) hefur fengið nafn áður þá er því skilað.
        * Færibreytur: 
        *   string isLaptop - Strengur sem er annaðhvort 'YES' eða 'NO'
        *   string MACAddress - MAC addressa tölvunar.
        *                       Geymt í minni til að tölva fái alltaf sama nafnið ef hún kallar aftur á þetta route
        * Skilar: IActionResult með næsta lausa nafni sem XML formatted strengur.
        **********************************************************************************************************/
        [Produces("application/xml")] // Svo það þurfi ekki að specifya content header í requestinu.
        [HttpPost]
        public IActionResult Get(string isLaptop, string MACAddress)
        {

            // MAC þarf að fylgja með.
            if (String.IsNullOrEmpty(MACAddress))
            {
                return StatusCode(StatusCodes.Status400BadRequest, "MAC address must be defined");
            }


            // Búa til nýtt nafn
            string ComputerName = GetComputerName(isLaptop);
                
                
            // Athuga hvort að þessi tölva (MAC) sé búin að fá nafn (Athugar hashtable í minni)
            if (MACAddresses.ContainsKey(MACAddress))
            {
                //Skila nafninu sem tölvan fékk áður
                return Ok(MACAddresses[MACAddress]);
            }
            else
            {
                // Bæta MAC í listann svo að tölva fái sama nafnið aftur
                MACAddresses.Add(MACAddress, ComputerName);
            }


             // Ef strengurinn er null
            if (String.IsNullOrEmpty(ComputerName))
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "ComputerName is null");
            }

            return Ok(ComputerName);
        }
        [HttpGet]
        public IActionResult TestApi()
        {
            return Ok("OUTRUN-0000");
        }
    }
}
