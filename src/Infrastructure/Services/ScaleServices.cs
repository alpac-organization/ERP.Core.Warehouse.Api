using System.Text.RegularExpressions;
using System.Globalization;
using ERP.Core.Warehouse.Api.Application.Commons.Interfaces;

namespace ERP.Core.Warehouse.Api.Infrastructure.Services
{
    public class ScaleServices(HttpClient _httpClient) : IScaleServices
    {
        public async Task<decimal> GetWeightFromTheScale()
        {
            try
            {
                // 1. Hacer la petición HTTP a la báscula
                string url = "http://192.168.5.15/scaledata1.iws";
                string htmlContent = await _httpClient.GetStringAsync(url);

                var match = Regex.Match(htmlContent, @"name=""name12""\s+value=""([^""]+)""", RegexOptions.IgnoreCase);

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
            catch (Exception)
            {
                return 0.0m;
            }
        }
    }
}