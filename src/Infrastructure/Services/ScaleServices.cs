using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Application.Commons.Interfaces;

namespace ERP.Core.Warehouse.Api.Infrastructure.Services
{
    public partial class ScaleServices(HttpClient _httpClient, IConfiguration _configuration, IErrorManager _errorManager) : IScaleServices
    {
        [GeneratedRegex(@"name=""name12""\s+value=""([^""]+)""", RegexOptions.IgnoreCase)]
        private static partial Regex ScaleValueRegex();

        public async Task<decimal> GetWeightFromTheScale()
        {
            string baseUrl = _configuration["ScalesConfiguration:ScaleBaseUrl"] ?? "";

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                return _errorManager.ThrowBadRequest<decimal>("No se puedo obtener la cadena de conexión a la bascula", "ERP:01");
            }
            
            string htmlContent = await _httpClient.GetStringAsync(baseUrl);

            var match = ScaleValueRegex().Match(htmlContent);

            if (match.Success)
            {
                string rawValue = match.Groups[1].Value.Trim();
                
                if (decimal.TryParse(rawValue, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal weight))
                {
                    return weight;
                }
            }
            
            return 0.0m;
        }
    }
}