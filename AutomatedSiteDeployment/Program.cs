using AutomatedSiteDeployment.Proxies;
using AutomatedSiteDeployment.Helpers;
using AutomatedSiteDeployment.Models;
using Microsoft.Extensions.Configuration;
using Models.Shared.Models.Domains;
using AutomatedSiteDeployment.Managers;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var settings = new SettingsHelper(configuration);

HttpClient httpClient = new HttpClient();
var manager = new ServiceManager(httpClient, settings);

Console.WriteLine("==== Automated Site Deployment ====");
Console.WriteLine("===================================");

bool exit = false;
while (!exit) {
    Console.WriteLine("Please choose an option to proceed:");
    Console.WriteLine("1. Display all domains");
    Console.WriteLine("2. Save a new Domain");
    Console.WriteLine("3. Delete a Domain");
    Console.WriteLine("4. Deploy a hosted site");
    Console.WriteLine("5. Exit");

    var input = Console.ReadLine();
    switch (input)
    {
        case "1":
            await manager.GetAllDoamins();
            break;
        case "2":
            await manager.SaveNewDomain();
            break;
        case "3":
             await manager.DeleteDomain();
            break;
        case "4":
            await manager.DeployHostedSite();
            break;
        case "5":
            exit = true;
            break;
        default:
            Console.WriteLine("Invalid selection.");
            break;
    }
}
