using Castle.Core.Logging;
using Microsoft.Extensions.Logging;
using Moq;
using System.Runtime.CompilerServices;
using Worker.Application.Services;
using Worker.Domain.Entities;
using Worker.Domain.Enums;
using Worker.Domain.Repositories;

namespace Worker.Tests
{
    public class SyncProcessingServiceTests
    {
        readonly Mock<ISyncRecordRepository> _repo;
        readonly Mock<ILogger<SyncProcessingService>> _logger;
        readonly SyncProcessingService _service;

        public SyncProcessingServiceTests()
        {
            _repo = new Mock<ISyncRecordRepository>();
            _logger = new Mock<ILogger<SyncProcessingService>>();

            // Inject mocks into the service
            _service = new SyncProcessingService(_repo.Object, _logger.Object);
        }

        [Fact]
        public async Task ProcessPendingRecords_MustExecuteStrategyInsert_WhenOperationWereInsert()
        {
            // Arrange
            var record = new SyncRecord
            {
                OperationType = "INSERT",
                RecordId = "123",
                TableName = "TestTable"
            };

            // Simulates a queue returning 1 register and after that null (end of the queue)
            _repo.SetupSequence(r => r.GetNextPendingAsync())
                .ReturnsAsync(record)
                .ReturnsAsync((SyncRecord?)null);

            // Act
            await _service.ProcessPendingRecordsAsync(1, CancellationToken.None);

            // Assert
            // verify if it called UpdateStatus with "Completed" status
            _repo.Verify(r => r.UpdateStatusAsync(
                record.Id,
                ProcessingStatus.Completed,
                It.IsAny<string>(),
                null), Times.Once);
        }

        [Fact]
        public async Task ProcessPendingRecords_ItMustLogAnError_WhenStrategyDoesNotExist()
        {
            // Arrange
            var record = new SyncRecord
            {
                OperationType = "UNKNOWN",
                RecordId = "999"
            };

            _repo.SetupSequence(r => r.GetNextPendingAsync())
                .ReturnsAsync(record)
                .ReturnsAsync((SyncRecord?)null);

            // Act
            await _service.ProcessPendingRecordsAsync(1, CancellationToken.None);

            // Assert
            // it must fail because it doesn't exist delegate to "UNKNOWN" operation type, so it must log an error
            _repo.Verify(r => r.UpdateStatusAsync(
                record.Id,
                ProcessingStatus.Failed,
                It.Is<string>(s => s.Contains("not supported")),
                null), Times.Once);
        }
    }
}
