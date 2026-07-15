using PuppeteerSharp;
using System.Globalization;
using System.Text.Json;

namespace PSLDiscordBot.Core.Utility;
public static class CDPUtils
{
	extension(IPage self)
	{
		public async Task SetViewportAsync(int width, int height, double deviceScaleFactor = 1)
		{
			await self.SetViewportAsync(new()
			{
				Width = width,
				Height = height,
				DeviceScaleFactor = deviceScaleFactor
			});
		}
		public async Task<Stream> ScreenshotLowMemory(ScreenshotOptions options)
		{
			var screenMessage = new
			{
				Format = (options.Type ?? ScreenshotType.Png).ToString().ToLower(CultureInfo.CurrentCulture),
				options.CaptureBeyondViewport,
				options.FromSurface,
				options.OptimizeForSpeed,
				options.Clip,
				options.Quality
			};

			await self.BringToFrontAsync();
			await self.Client.SendAsync("Page.disable");
			JsonElement element = await self.Client.SendAsync("Page.captureScreenshot", screenMessage)
				?? throw new InvalidDataException("Failed to capture screenshot.");
			// unfortunately they don't provide a way to get the original memory<byte>, otherwise i could create a base64 decoding stream
			return new MemoryStream(element.GetProperty("data").GetBytesFromBase64());
		}
	}

	extension(ICDPSession self)
	{
		public async Task<JsonElement?> RuntimeEvaluate(string command)
		{
			return await self.SendAsync("Runtime.evaluate", new { Expression = command });
		}
		public async Task LogEnable()
		{
			await self.SendAsync("Log.enable");
		}
		public async Task LogClear()
		{
			await self.SendAsync("Log.clear");
		}
		public async Task DebuggerEnable()
		{
			await self.SendAsync("Debugger.enable");
		}
		public async Task DebuggerResume()
		{
			await self.SendAsync("Debugger.resume");
		}

		public Task RunUntilDebugger()
		{
			TaskCompletionSource tcs = new();
			self.MessageReceived += OnEvent;

			return tcs.Task;

			void OnEvent(object? sender, MessageEventArgs e)
			{
				if (e.MessageID != "Debugger.paused")
					return;

				self.MessageReceived -= OnEvent;
				tcs.SetResult();
			}
		}
		public async Task PageNavigate(string url)
		{
			await self.SendAsync("Page.navigate", new { Url = url });
		}
	}
}
