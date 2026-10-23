using System.Text.Json;

using ERP.Core.Database.Domain.Entities.Auth;
using ERP.Core.Database.Domain.Entities.Shopping;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos;
using ERP.Core.Warehouse.Api.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Helpers.Purchasing.Services
{
    public static class PurchaseRequestReturnHistory
    {
        public const string RequotationDescription = "Re-cotización";

        public const string AnnulmentDescription = "Anulación";

        public const string AccountingSource = "Accounting";

        public const string ManagementSource = "Management";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        };

        public static void Record(
            PurchaseRequest purchaseRequest,
            string reason,
            AnnulmentScope scope,
            string source,
            User? user,
            DateTime occurredAtUtc)
        {
            var trimmedReason = reason.Trim();
            purchaseRequest.AnnulmentComments = trimmedReason;

            if (!TryDeserializeHistory(purchaseRequest.AdditionalData, out var historyList))
            {
                return;
            }

            var entry = new PurchaseRequestAdditionalData
            {
                NewField = trimmedReason,
                OldFields = source,
                Description = scope == AnnulmentScope.QuotationOnly
                    ? RequotationDescription
                    : AnnulmentDescription,
                UpdatedAt = occurredAtUtc
            };

            if (user is not null)
            {
                entry.UserInformation.UserId = user.Id;
                entry.UserInformation.Email = user.Email;
                entry.UserInformation.Fullname = user.Fullname;
                entry.UserInformation.UserStatus = user.UserStatus;
            }

            historyList.Add(entry);
            purchaseRequest.AdditionalData = JsonSerializer.Serialize(historyList, JsonOptions);
        }

        public static int CountRequotations(string? additionalDataJson)
        {
            if (!TryDeserializeHistory(additionalDataJson, out var historyList))
            {
                return 0;
            }

            return historyList.Count(entry => entry.Description == RequotationDescription);
        }

        public static List<PurchaseRequestReturnEntryDto> ReadReturnHistory(string? additionalDataJson)
        {
            if (!TryDeserializeHistory(additionalDataJson, out var historyList))
            {
                return [];
            }

            return historyList
                .Where(entry =>
                    entry.Description == RequotationDescription ||
                    entry.Description == AnnulmentDescription)
                .Select(MapEntry)
                .ToList();
        }

        private static bool TryDeserializeHistory(string? json, out List<PurchaseRequestAdditionalData> historyList)
        {
            historyList = [];

            if (string.IsNullOrWhiteSpace(json))
            {
                return true;
            }

            try
            {
                historyList = JsonSerializer.Deserialize<List<PurchaseRequestAdditionalData>>(json, JsonOptions) ?? [];
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static PurchaseRequestReturnEntryDto MapEntry(PurchaseRequestAdditionalData entry)
        {
            return new PurchaseRequestReturnEntryDto
            {
                Scope = entry.Description == RequotationDescription
                    ? AnnulmentScope.QuotationOnly
                    : AnnulmentScope.FullProcess,
                Source = entry.OldFields ?? string.Empty,
                Reason = entry.NewField ?? string.Empty,
                ReturnedAt = entry.UpdatedAt,
                UserInformation = entry.UserInformation
            };
        }
    }
}
