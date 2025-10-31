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
    }
}
