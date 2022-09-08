// See https://aka.ms/new-console-template for more information
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;

//encode your personal access token                   
string credentials = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(string.Format("{0}:{1}", "", "")));
const int buildDefinitionId = 9;

//use the httpclient
using (var client = new HttpClient())
{
    client.BaseAddress = new Uri($"RepoUrl/");  //url of your organization
    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

    //Check that the build pipeline we are targeting exists  
    var response = client.GetAsync($"_apis/build/builds?definitions={buildDefinitionId}&statusFilter=completed&api-version=7.1-preview.7").Result;
     
    if (response.IsSuccessStatusCode)
    {
        //Get all leases on the current build pipeline
        var leasesUrl = $"_apis/build/retention/leases?api-version=6.1-preview&definitionId={buildDefinitionId}";
        var leases = client.GetAsync(leasesUrl).Result;
        var leasesObject = JsonConvert.DeserializeObject<JObject>(leases.Content.ReadAsStringAsync().Result);

        var leaseIds =
            from p in leasesObject["value"]
            select (string)p["leaseId"];

        //var joinedIds = string.Join(", ", leaseIds.Select(x => x));

        //Delete the leases
        foreach (var item in leaseIds)
        {
            var deleteUrl = $"_apis/build/retention/leases?ids={item}&api-version=6.1-preview";
            var deleteResponse = client.DeleteAsync(deleteUrl).Result;

            if (deleteResponse.IsSuccessStatusCode)
            {
                Console.WriteLine($"Deleted all leases. Try to remove the build pipeline now.");
            }
            else
            {
                Console.WriteLine($"Deleted leases failed {deleteResponse.StatusCode}");
            }
        }
    }
}
