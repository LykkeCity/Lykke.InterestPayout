using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using InterestPayout.Worker.WebApi.Models;
using Lykke.InterestPayout.ApiContract;
using Newtonsoft.Json;

namespace Lykke.InterestPayout.ApiClient.WebApi
{
    public class AssetInterestsClient : IAssetInterestsClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public AssetInterestsClient(HttpClient httpClient, string baseUrl)
        {
            _httpClient = httpClient;
            _baseUrl = baseUrl;
        }

        public async Task<AssetInterestResponse[]> GetAll()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}get-all");

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<AssetInterestResponse[]>(content);
        }

        public async Task<bool> CreateOrUpdate(AssetInterestCreateOrUpdateRequest request, string idempotencyId)
        {
            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Add("X-Idempotency-ID", idempotencyId);

            var response = await _httpClient.PostAsync($"{_baseUrl}create-or-update", content);

            return response.IsSuccessStatusCode;
        }
    }
}
