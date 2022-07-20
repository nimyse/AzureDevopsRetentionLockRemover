using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;

//Change this to true too perform the delete. If false it will only log the leases found.
const bool DryRun = false;

//The definition Id of the build pipeline you want to remove the locks from
const int BuildDefinitionId = 12;

//The Private Access Token to use to authenticate to Azure Devops. To create one, follow this article: https://docs.microsoft.com/en-us/azure/devops/organizations/accounts/use-personal-access-tokens-to-authenticate?view=azure-devops&tabs=Windows
const string AuthenticationPat = "";

//The name of your organization
const string OrganizatioName = "";

//The name of the project your build pipeline resides in
const string Projectname = "";

using (var client = new HttpClient())
{
    //Encode your personal access token
    var credentials = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(string.Format("{0}:{1}", "", AuthenticationPat)));
    
    client.BaseAddress = new Uri($"https://dev.azure.com/{OrganizatioName}/{ProjectName}/");
    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

    //Check that the build pipeline we are targeting exists  
    var response = await client.GetAsync($"_apis/build/builds?definitions={BuildDefinitionId}&statusFilter=completed&api-version=7.1-preview.7");
     
    if (response.IsSuccessStatusCode)
    {
        //Build pipeline exists. Go ahead and get all the leases connected to this pipeline
        var leasesUrl = $"_apis/build/retention/leases?api-version=6.1-preview&definitionId={BuildDefinitionId}";
        var leases = await client.GetAsync(leasesUrl);
        
        //Parse the resulting leases id to be used for additional API calls
        var leasesObject = JsonConvert.DeserializeObject<JObject>(await leases.Content.ReadAsStringAsync());

        var leaseIds =
            from p in leasesObject["value"]
            select (string)p["leaseId"];
        
        //Join the lease Ids to a string for use in the delete call
        var joinedIds = string.Join(", ", leaseIds.Select(x => x));
        
        Console.WriteLine($"Found {leaseIds.Count} leases. Proceding to delete: {DryRun}");

        //Delete all the leases in one call
        var deleteUrl = $"_apis/build/retention/leases?ids={joinedIds}&api-version=6.1-preview";
        var deleteResponse = await client.DeleteAsync(deleteUrl);
        
        if (deleteResponse.IsSuccessStatusCode)
        {
            Console.WriteLine($"Deleted all leases. Try to remove the build pipeline now.");
        }
        else
        {
            Console.WriteLine($"Deleted leases failed {deleteResponse.StatusCode}");
        }
    }
    Console.WriteLine($"No build pipeline found with id { BuildDefinitionId }");
}