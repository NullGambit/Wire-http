using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;

namespace Wire.Common;

public static class FrameWriter
{
	// most headers will likely be smaller than this
	const int STARTING_CAPACITY = 256;
	
	public static async Task<MemoryStream> WriteResponse(Response response, MemoryStream? ms = null)
	{
		ms ??= new MemoryStream();

		if (ms.Capacity < STARTING_CAPACITY)
		{
			ms.Capacity = STARTING_CAPACITY;
		}

		await ms.WriteAsync(HttpVersion.ONE_POINT_ONE);

		ms.WriteByte((byte)' ');

		var statusInt = (int)response.status;
		var statusBuffer = Encoding.UTF8.GetBytes(statusInt.ToString());
		
		await ms.WriteAsync(statusBuffer);
		
		ms.WriteByte((byte)' ');

		if (string.IsNullOrEmpty(response.message))
		{
			await ms.WriteAsync(Encoding.UTF8.GetBytes(response.status.ToString()));
		}
		else
		{
			await ms.WriteAsync(Encoding.UTF8.GetBytes(response.message));
		}
		
		await ms.WriteAsync("\r\n"u8.ToArray());

		foreach (var (key, value) in response.headers)
		{
			await ms.WriteAsync(Encoding.UTF8.GetBytes(key));
			await ms.WriteAsync(": "u8.ToArray());
			await ms.WriteAsync(Encoding.UTF8.GetBytes(value));
			await ms.WriteAsync("\r\n"u8.ToArray());
		}
		
		await ms.WriteAsync("\r\n"u8.ToArray());

		if (response.body != null)
		{
			await ms.WriteAsync(response.body);
		}

		return ms;
	}
}