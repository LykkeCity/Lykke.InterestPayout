using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InterestPayout.Common.Application;
using InterestPayout.Common.Configuration;
using InterestPayout.Common.Domain;
using InterestPayout.Common.Extensions;
using InterestPayout.Common.Persistence;
using InterestPayout.Worker.WebApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swisschain.Extensions.Idempotency;

namespace InterestPayout.Worker.WebApi
{
    [ApiController]
    [Route("api/schedules")]
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

        [HttpPost("create-or-update")]
        public async Task<ActionResult> CreateOrUpdate(
            [FromBody] AssetInterestCreateOrUpdateRequest createOrUpdateRequest)
        {
            if (createOrUpdateRequest == null)
                return BadRequest("Request is required.");
            if(string.IsNullOrWhiteSpace(createOrUpdateRequest.AssetId))
                return BadRequest("AssetId is required.");
            if (createOrUpdateRequest.InterestRate < 100m)
                return BadRequest("Interest rate cannot be lower than minus one hundred percent.");
            
            await using var unitOfWork = await _unitOfWorkManager.Begin(
                $"CreatingOrUpdateAssetInterest:{DateTimeOffset.UtcNow.Ticks}");

            var existingEntry =
                await unitOfWork.AssetInterests.GetLatestForDateOrDefault(createOrUpdateRequest.AssetId,
                    DateTimeOffset.UtcNow);

            if (existingEntry == null)
            {
                var newAssetInterestId = await _idGenerator.GetId(
                    Guid.NewGuid().ToString(),
                    IdGenerators.AssetInterests);
                var assetInterest = AssetInterest.Create(newAssetInterestId,
                    createOrUpdateRequest.AssetId,
                    createOrUpdateRequest.InterestRate,
                    createOrUpdateRequest.ValidUntil,
                    0);
                await unitOfWork.AssetInterests.Add(assetInterest);
            }
            else
            {
                var newAssetInterestId = await _idGenerator.GetId(
                    Guid.NewGuid().ToString(),
                    IdGenerators.AssetInterests);
                var assetInterest = AssetInterest.Create(newAssetInterestId,
                    createOrUpdateRequest.AssetId,
                    createOrUpdateRequest.InterestRate,
                    createOrUpdateRequest.ValidUntil,
                    existingEntry.Version + 1);
                await unitOfWork.AssetInterests.Add(assetInterest);
            }

            await unitOfWork.Commit();

            return Ok();
        }
    }
}
