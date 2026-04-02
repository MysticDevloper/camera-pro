using CameraPro.Core.Models;
using Windows.Devices.Enumeration;

namespace CameraPro.Camera;

public class DeviceEnumerator
{
    public Task<List<CameraDevice>> GetAllCamerasAsync()
    {
        return Task.Run(async () =>
        {
            var cameras = new List<CameraDevice>();
            
            var devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            
            foreach (var device in devices)
            {
                cameras.Add(new CameraDevice
                {
                    Id = device.Id,
                    Name = device.Name,
                    IsDefault = device.IsDefault
                });
            }
            
            return cameras;
        });
    }
}
