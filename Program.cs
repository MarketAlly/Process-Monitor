using marketally.processmonitor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

Console.WriteLine("Do you want to start processes in a new window? (yes/no)");
string userInput = Console.ReadLine().ToLower();
bool openInNewWindow = userInput == "yes";

double checkOnce = 0;

while (true)
{
	string jsonFilePath = "processlist.json"; // Relative path to the JSON file
	string json = File.ReadAllText(jsonFilePath);
	var jObject = JObject.Parse(json);
	var processInfos = jObject["processes"].ToObject<List<ProcessInfo>>();

	foreach (var processInfo in processInfos.Where(x => x.Enable))
	{
		if (checkOnce == 0 && processInfo.Interval.HasValue)
		{
			// Schedule repeated execution
			ScheduleRepeatedExecution(processInfo);
		}
		else if (string.IsNullOrEmpty(processInfo.Time))
		{
			// Run continuously
			EnsureProcessRunning(processInfo, processInfo.Count);
		}
		else if (checkOnce == 0) 
		{
			// Schedule for a specific time
			ScheduleProcessStart(processInfo);
		}
	}
	if (checkOnce == 0)
		checkOnce = 1;
	Thread.Sleep(10000); // Wait for 10 seconds before checking again
}

void EnsureProcessRunning(ProcessInfo processInfo, int desiredCount)
{
	var runningProcesses = Process.GetProcessesByName(processInfo.Name);
	int countToStart = desiredCount - runningProcesses.Length;

	for (int i = 0; i < countToStart; i++)
	{
		ProcessStartInfo startInfo = new ProcessStartInfo
		{
			FileName = processInfo.Path,
			CreateNoWindow = !openInNewWindow, // Controlled by the user's choice
			UseShellExecute = true, // Use the system shell to start the process
		};

		Process.Start(startInfo);
		Console.WriteLine($"Started {processInfo.Name} at {DateTime.Now}");
		AppendToLogFile(processInfo, "launched");
	}
}

void ScheduleProcessStart(ProcessInfo processInfo)
{
	DateTime scheduledTime = DateTime.Today.Add(TimeSpan.Parse(processInfo.Time));
	TimeSpan delay = scheduledTime - DateTime.Now;

	if (delay < TimeSpan.Zero)
	{
		// Scheduled time is in the past. Schedule for the next day or handle as needed.
		delay = delay.Add(TimeSpan.FromDays(1));
	}

	var timer = new System.Threading.Timer(_ =>
	{
		ProcessStartInfo startInfo = new ProcessStartInfo
		{
			FileName = processInfo.Path,
			CreateNoWindow = !openInNewWindow, // Controlled by the user's choice
			UseShellExecute = true, // Use the system shell to start the process
		};
		Process.Start(startInfo);
		AppendToLogFile(processInfo, "run");
	}, null, delay, Timeout.InfiniteTimeSpan); // Run only once

	Console.WriteLine($"Scheduled {processInfo.Name} to start at {scheduledTime}");
}

void ScheduleRepeatedExecution(ProcessInfo processInfo)
{
	var timer = new System.Threading.Timer(_ =>
	{
		var runningProcesses = Process.GetProcessesByName(processInfo.Name);
		if (runningProcesses.Length == 0)
		{
			ProcessStartInfo startInfo = new ProcessStartInfo
			{
				FileName = processInfo.Path,
				CreateNoWindow = !openInNewWindow, // Controlled by the user's choice
				UseShellExecute = true, // Use the system shell to start the process
			};
			Process.Start(startInfo);
			Console.WriteLine($"Started {processInfo.Name} at {DateTime.Now}");
			AppendToLogFile(processInfo, "run");
		} else
		{
			Console.WriteLine($"Process still running {processInfo.Name} at {DateTime.Now}, skipping for next interval");
			AppendToLogFile(processInfo, "skipped");
		}
	}, null, TimeSpan.Zero, TimeSpan.FromMinutes(processInfo.Interval.Value));

	Console.WriteLine($"Scheduled {processInfo.Name} to run every {processInfo.Interval.Value} minutes");
}

void AppendToLogFile(ProcessInfo processInfo, string state)
{
	string logFilePath = Path.Combine("Logs", GetLogFileName()); // Store logs in a "Logs" subfolder
	string logEntry = $"{DateTime.Now}: {processInfo.Name} was {state}.";

	if (!Directory.Exists("Logs"))
	{
		Directory.CreateDirectory("Logs");
	}

	File.AppendAllText(logFilePath, logEntry + Environment.NewLine);
}

string GetLogFileName()
{
	var cultureInfo = System.Globalization.CultureInfo.CurrentCulture;
	int weekNo = cultureInfo.Calendar.GetWeekOfYear(
		DateTime.Now,
		cultureInfo.DateTimeFormat.CalendarWeekRule,
		cultureInfo.DateTimeFormat.FirstDayOfWeek);

	return $"processLog_{DateTime.Now.Year}_Week{weekNo}.log";
}
