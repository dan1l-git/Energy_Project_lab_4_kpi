using Energy_Project.Services.Interfaces;
using Moq;
using Energy_Project.Models;
using Energy_Project.Services;
using Energy_Project.Services.Interfaces;
using Moq;
namespace SmartHomeTests
{
    public class EnergyMonitorServiceTests
    {
        private readonly Mock<IDeviceRepository> _deviceRepo = new();
        private readonly Mock<IEnergyPlanRepository> _planRepo = new();
        private readonly Mock<INotificationService> _notify = new();
        private readonly EnergyMonitorService _service;

        public EnergyMonitorServiceTests()
        {
            _deviceRepo = new Mock<IDeviceRepository>();
            _planRepo = new Mock<IEnergyPlanRepository>();
            _notify = new Mock<INotificationService>();
            
            _service = new EnergyMonitorService(
                _deviceRepo.Object,
                _planRepo.Object,
                _notify.Object
            );
        }
        
        /// <summary>
        /// Tests that CalculateCurrentUsageKwh returns 0.0
        /// when the repository returns no active devices (or no devices at all).
        /// </summary>
        [Fact]
        public void CalculateCurrentUsageKwh_ShouldReturnZero_WhenNoActiveDevices()
        {
            // Arrange
            var devices = new List<Device>();
            _deviceRepo.Setup(r => r.GetAll()).Returns(devices);

            // Act
            var result = _service.CalculateCurrentUsageKwh();

            // Assert
            Assert.Equal(0.0, result);
        }
        
        /// <summary>
        /// Tests that CalculateCurrentUsageKwh correctly sums the PowerUsageWatts
        /// of 'On' devices and divides by 1000 to return kWh.
        /// </summary>
        [Fact]
        public void CalculateCurrentUsageKwh_ShouldReturnCorrectSum_WhenDevicesAreActive()
        {
            // Arrange
            var devices = new List<Device>
            {
                new Device { Id = 1, IsOn = true, PowerUsageWatts = 500 }, // 500W
                new Device { Id = 2, IsOn = false, PowerUsageWatts = 1000 }, // Off, should be ignored
                new Device { Id = 3, IsOn = true, PowerUsageWatts = 250 }  // 250W
            };
            
            // Expected calculation: (500 + 250) / 1000.0 = 0.75
            double expectedKwh = 0.75;
            
            _deviceRepo.Setup(r => r.GetAll()).Returns(devices);

            // Act
            var result = _service.CalculateCurrentUsageKwh();

            // Assert
            Assert.Equal(expectedKwh, result);
        }
        
        /// <summary>
        /// Tests that CheckForOverload does NOT send an alert
        /// when the calculated usage is below or equal to the plan's limit.
        /// </summary>
        [Fact]
        public void CheckForOverload_ShouldNotSendAlert_WhenUsageIsBelowLimit()
        {
            // Arrange
            var devices = new List<Device>
            {
                new Device { IsOn = true, PowerUsageWatts = 1000 }
            };
            _deviceRepo.Setup(r => r.GetAll()).Returns(devices);
            
            var plan = new EnergyPlan { DailyLimitKwh = 1.5 };
            _planRepo.Setup(p => p.GetCurrentPlan()).Returns(plan);

            // Act
            _service.CheckForOverload();

            // Assert
            _notify.Verify(n => n.SendAlert(It.IsAny<string>()), Times.Never);
        }
        
        /// <summary>
        /// Tests that CheckForOverload DOES send an alert
        /// when the calculated usage exceeds the plan's limit.
        /// </summary>
        [Fact]
        public void CheckForOverload_ShouldSendAlert_WhenUsageExceedsLimit()
        {
            // Arrange
            var devices = new List<Device>
            {
                new Device { IsOn = true, PowerUsageWatts = 2000 }
            };
            _deviceRepo.Setup(r => r.GetAll()).Returns(devices);
            
            var plan = new EnergyPlan { DailyLimitKwh = 1.5 };
            _planRepo.Setup(p => p.GetCurrentPlan()).Returns(plan);

            // Act
            _service.CheckForOverload();

            // Assert
            // Verify that the SendAlert method was called exactly ONCE
            // We use It.IsAny<string>() because we don't care about the exact message content,
            // only that an alert WAS sent.
            _notify.Verify(n => n.SendAlert(It.IsAny<string>()), Times.Once);
        }
        
        /// <summary>
        /// Tests that UpdateEnergyLimit correctly updates the DailyLimitKwh
        /// on the plan object and calls UpdatePlan with that object.
        /// </summary>
        [Fact]
        public void UpdateEnergyLimit_ShouldCallUpdatePlan_WithCorrectNewLimit()
        {
            // Arrange
            double newLimit = 10.5;
            
            var originalPlan = new EnergyPlan { DailyLimitKwh = 5.0 };
            _planRepo.Setup(p => p.GetCurrentPlan()).Returns(originalPlan);

            // Act
            _service.UpdateEnergyLimit(newLimit);

            // Assert
            _planRepo.Verify(
                p => p.UpdatePlan(It.Is<EnergyPlan>(plan => plan.DailyLimitKwh == newLimit)),
                Times.Once
            );
        }
        
        /// <summary>
        /// A parameterized test that checks the CheckForOverload logic
        /// with multiple scenarios.
        /// </summary>
        /// <param name="usageWatts">The power usage to simulate</param>
        /// <param name="limitKwh">The energy limit to set</param>
        /// <param name="shouldAlert">The expected outcome (true if alert should be sent)</param>
        [Theory]
        [InlineData(1000, 1.5, false)] // Scenario 1: Usage (1.0) < Limit (1.5) -> No Alert
        [InlineData(1500, 1.5, false)] // Scenario 2: Usage (1.5) == Limit (1.5) -> No Alert
        [InlineData(1501, 1.5, true)]  // Scenario 3: Usage (1.501) > Limit (1.5) -> Alert
        [InlineData(2000, 0.5, true)]  // Scenario 4: Usage (2.0) > Limit (0.5) -> Alert
        public void CheckForOverload_ShouldAlertBasedOnLimit(double usageWatts, double limitKwh, bool shouldAlert)
        {
            // Arrange
            var devices = new List<Device> { new Device { IsOn = true, PowerUsageWatts = usageWatts } };
            _deviceRepo.Setup(r => r.GetAll()).Returns(devices);

            var plan = new EnergyPlan { DailyLimitKwh = limitKwh };
            _planRepo.Setup(p => p.GetCurrentPlan()).Returns(plan);

            // Act
            _service.CheckForOverload();

            // Assert
            if (shouldAlert)
            {
                _notify.Verify(n => n.SendAlert(It.IsAny<string>()), Times.Once);
            }
            else
            {
                _notify.Verify(n => n.SendAlert(It.IsAny<string>()), Times.Never);
            }
        }
    }
}
