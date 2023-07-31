using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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

            return Ok();
        }
    }
}
