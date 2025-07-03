public interface IAnaHttpClientFactory
{
    Task<HttpClient> GetHttpClient();
}
