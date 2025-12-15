using NexusHome.IoT.Core.DTOs;

namespace NexusHome.IoT.Core.Services.Interfaces;

public interface IPredictiveMaintenanceService
{
    Task<MaintenancePredictionResult> PredictMaintenanceNeedsAsync(int deviceId);
    Task<AnomalyDetectionResult> DetectAnomaliesAsync(int deviceId);
    Task TrainModelAsync(string deviceType);
    Task<HealthScoreResult> CalculateDeviceHealthScoreAsync(int deviceId);
}
