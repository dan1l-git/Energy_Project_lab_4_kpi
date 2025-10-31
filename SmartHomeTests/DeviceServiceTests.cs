using Energy_Project.Services.Interfaces;
using Moq;
using Energy_Project.Models;
using Energy_Project.Services;
using Energy_Project.Services.Interfaces;
using Moq;

namespace SmartHomeTests
{
    public class DeviceServiceTests
    {
        private readonly Mock<IDeviceRepository> _deviceRepo = new();
        private readonly Mock<IEnergyPlanRepository> _planRepo = new();
        private readonly Mock<INotificationService> _notify = new();
        private readonly DeviceService _service;

        public DeviceServiceTests()
        {
            _deviceRepo =  new Mock<IDeviceRepository>();
            _service = new DeviceService(_deviceRepo.Object);
        }
        
        /// <summary>
        /// Tests that ToggleDevice throws an ArgumentException
        /// if a device with the specified ID is not found.
        /// </summary>
        [Fact]
        public void ToggleDevice_ShouldThrowArgumentException_WhenDeviceNotFound()
        {
            // Arrange
            _deviceRepo.Setup(r => r.GetById(It.IsAny<int>())).Returns((Device?)null);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _service.ToggleDevice(1, true));
        }
        
        /// <summary>
        /// Tests that ToggleDevice correctly turns a device on (IsOn = true)
        /// and returns true when 'turnOn' is true.
        /// </summary>
        [Fact]
        public void ToggleDevice_ShouldTurnDeviceOn_WhenCalledWithTrue()
        {
            // Arrange
            var device = new Device { Id = 1, IsOn = false };
            _deviceRepo.Setup(r => r.GetById(1)).Returns(device);

            // Act
            var result = _service.ToggleDevice(1, true);

            // Assert (Перевірка)
            Assert.True(result);
            Assert.True(device.IsOn);
        }

        /// <summary>
        /// Tests that ToggleDevice correctly turns a device off (IsOn = false)
        /// and returns false when 'turnOn' is false.
        /// </summary>
        [Fact]
        public void ToggleDevice_ShouldTurnDeviceOff_WhenCalledWithFalse()
        {
            // Arrange
            var device = new Device { Id = 1, IsOn = true };
            _deviceRepo.Setup(r => r.GetById(1)).Returns(device);
            
            //Act
            var result = _service.ToggleDevice(1, false);
            
            // Assert
            Assert.False(result);
            Assert.False(device.IsOn);
        }
        
        /// <summary>
        /// Tests that the repository's Update method is called exactly once
        /// when a device is successfully toggled.
        /// </summary>
        [Fact]
        public void ToggleDevice_ShouldCallUpdateExactlyOnce_WhenDeviceExists()
        {
            // Arrange
            var device = new Device { Id = 1, IsOn = false };
            _deviceRepo.Setup(r => r.GetById(1)).Returns(device);

            // Act
            _service.ToggleDevice(1, true);

            // Assert
            _deviceRepo.Verify(r => r.Update(device), Times.Exactly(1));
        }
        
        /// <summary>
        /// Tests that GetActiveDevices returns only devices that are 'IsOn = true'
        /// and that the list is not empty.
        /// </summary>
        [Fact]
        public void GetActiveDevices_ShouldReturnOnlyOnDevices()
        {
            // Arrange
            var devices = new List<Device>
            {
                new Device { Id = 1, Name = "Lamp", IsOn = true },
                new Device { Id = 2, Name = "TV", IsOn = false },
                new Device { Id = 3, Name = "PC", IsOn = true }
            };
            
            _deviceRepo.Setup(r => r.GetAll()).Returns(devices);

            // Act
            var result = _service.GetActiveDevices();

            // Assert
            Assert.NotNull(result); 
            Assert.NotEmpty(result); 
            Assert.Equal(2, result.Count()); 
            Assert.Contains(result, d => d.Id == 1); 
            Assert.DoesNotContain(result, d => d.Id == 2);
        }

        /// <summary>
        /// Tests that GetActiveDevices returns an empty list
        /// when no devices are currently on.
        /// </summary>
        [Fact]
        public void GetActiveDevices_ShouldReturnEmptyList_WhenNoDevicesAreOn()
        {
            // Arrange
            var devices = new List<Device>
            {
                new Device { Id = 1, Name = "Lamp", IsOn = false },
                new Device { Id = 2, Name = "TV", IsOn = false },
                new Device { Id = 3, Name = "PC", IsOn = false }
            };
            _deviceRepo.Setup(r => r.GetAll()).Returns(devices);
            
            // Act
            var result = _service.GetActiveDevices();
            
            // Assert
            Assert.Empty(result);
            Assert.NotNull(result);
        }
    }
}
