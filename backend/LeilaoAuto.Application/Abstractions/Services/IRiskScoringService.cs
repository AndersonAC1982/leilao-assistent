using LeilaoAuto.Application.Contracts.Scoring;
using LeilaoAuto.Domain.Enums;

namespace LeilaoAuto.Application.Abstractions.Services;

public interface IRiskScoringService
{
    RiskScoreResult Score(string title, string? description, VehicleCondition condition, int year, bool hasValidLotUrl);
}
