using System.Text;
using System.Text.Json;

using PaymentGateway.Api.Core;

namespace PaymentGateway.Api.Infrastructure
{
    public class AcquiringBankClient : IAcquiringBank
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AcquiringBankClient(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
        {
            using var httpClient = _httpClientFactory.CreateClient("AcquiringBankClient");

            try
            {
                var bankPaymentRequest = new AcquiringBankPaymentRequest(request);
                var jsonContent = JsonSerializer.Serialize(bankPaymentRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("/payments", content);

                if (!response.IsSuccessStatusCode)
                {
                    return new PaymentResult { IsSuccess = false };
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var paymentResponse = JsonSerializer.Deserialize<AcquiringBankPaymentResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (paymentResponse is null)
                {
                    return new PaymentResult { IsSuccess = false };
                }

                return new PaymentResult
                {
                    IsSuccess = true,
                    Authorized = paymentResponse.Authorized,
                    AuthorizationCode = paymentResponse.AuthorizationCode
                };

            }
            catch (Exception ex)
            {
                return new PaymentResult { IsSuccess = false };
            }
        }
    }
}
