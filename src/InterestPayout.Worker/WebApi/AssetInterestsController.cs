using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using InterestPayout.Common.Domain;
using InterestPayout.Common.Persistence;
using InterestPayout.Worker.WebApi.Models;
using Lykke.InterestPayout.ApiContract;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swisschain.Extensions.Idempotency;

namespace InterestPayout.Worker.WebApi
{
    [ApiController]
    [Route("api/assets")]
    public class AssetInterestsController : ControllerBase
    {
        private readonly IUnitOfWorkManager<UnitOfWork> _unitOfWorkManager;
        private readonly IIdGenerator _idGenerator;

        public AssetInterestsController(IUnitOfWorkManager<UnitOfWork> unitOfWorkManager,
            IIdGenerator idGenerator)
        {
            _unitOfWorkManager = unitOfWorkManager;
            _idGenerator = idGenerator;
        }
        
        [HttpGet]
        [ProducesResponseType(typeof(AssetInterestResponse[]), StatusCodes.Status200OK)]
        public async Task<ActionResult<AssetInterestResponse>> GetAll()
        {
            await using var roUnitOfWork = await _unitOfWorkManager.Begin();

            var entries = await roUnitOfWork.AssetInterests.GetAll();

            return Ok(entries.Select(x => ToResponse(x)).ToArray());
        }
        
        [HttpPost("create-or-update")]
        public async Task<ActionResult> CreateOrUpdate(
            [FromBody] AssetInterestCreateOrUpdateRequest request,
            [Required, FromHeader(Name = "X-Idempotency-ID")] string idempotencyId)
        {
            if (request == null)
                return BadRequest("Request is required.");
            if(string.IsNullOrWhiteSpace(request.AssetId))
                return BadRequest("AssetId is required.");
            if (request.InterestRate < -100m)
                return BadRequest("Interest rate cannot be lower than minus one hundred percent.");
            
            await using var unitOfWork = await _unitOfWorkManager.Begin(
                $"CreatingOrUpdateAssetInterest:{idempotencyId}");

            var existingEntry = await unitOfWork.AssetInterests.GetByAssetOrDefault(request.AssetId);
            if (existingEntry == null)
            {
                var newAssetInterestId = await _idGenerator.GetId(
                    Guid.NewGuid().ToString(),
                    IdGenerators.AssetInterests);
                var assetInterest = AssetInterest.Create(newAssetInterestId,
                    request.AssetId,
                    request.InterestRate);
                await unitOfWork.AssetInterests.Add(assetInterest);
            }
            else
            {
                var hasChanges = existingEntry.UpdateInterestRate(request.InterestRate);
                if (hasChanges)
                    await unitOfWork.AssetInterests.Update(existingEntry);
            }

            await unitOfWork.Commit();

            return Ok("success");
        }
        
        [HttpDelete("delete")]
        public async Task<ActionResult> Delete(
            [FromBody] IReadOnlyCollection<string> assetIds,
            [Required, FromHeader(Name = "X-Idempotency-ID")] string idempotencyId)
        {
            if (assetIds == null || assetIds.Count == 0 || assetIds.Any(x => string.IsNullOrWhiteSpace(x)))
                return BadRequest("Empty asset IDs.");

            await using var unitOfWork = await _unitOfWorkManager.Begin(
                $"DeletingAssetInterest:{idempotencyId}");
            
            await unitOfWork.AssetInterests.DeleteByAssetIds(assetIds.ToHashSet());

            await unitOfWork.Commit();
            
            return Ok("success");
        }

        private static AssetInterestResponse ToResponse(AssetInterest domainEntity)
        {
            return new AssetInterestResponse
            {
                Id = domainEntity.Id,
                AssetId = domainEntity.AssetId,
                InterestRate = domainEntity.InterestRate,
                CreatedAt = domainEntity.CreatedAt,
                UpdatedAt = domainEntity.UpdatedAt
            };
        }
    }
}
