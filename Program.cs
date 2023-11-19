using marketally.processmonitor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

// Asking the user if they want to open new processes in a separate window
Console.WriteLine("Do you want to start processes in a new window? (yes/no)");
string userInput = Console.ReadLine().ToLower();
bool openInNewWindow = userInput == "yes";

double checkOnce = 0;

// The main loop that continuously checks and manages processes
while (true)
{
	// Define the path to the JSON file containing process information
	string jsonFilePath = "processlist.json"; // Relative path to the JSON file
	string json = File.ReadAllText(jsonFilePath);
	var jObject = JObject.Parse(json);
	var processInfos = jObject["processes"].ToObject<List<ProcessInfo>>();

	foreach (var processInfo in processInfos.Where(x => x.Enable))
	{
		// Check if it's the first run and if the process has a specific interval set
		if (checkOnce == 0 && processInfo.Interval.HasValue)
		{
			// Schedule repeated execution
			ScheduleRepeatedExecution(processInfo);
		}
		// If no specific time is set, ensure the process is running continuously
		else if (string.IsNullOrEmpty(processInfo.Time))
		{
			// Run continuously
			EnsureProcessRunning(processInfo, processInfo.Count);
		}
		// If it's the first run and a specific time is set, schedule the process
		else if (checkOnce == 0) 
		{
			// Schedule for a specific time
			ScheduleProcessStart(processInfo);
		}
	}
	// After the first iteration, set checkOnce to 1 to avoid rescheduling
	if (checkOnce == 0)
		checkOnce = 1;
	Thread.Sleep(10000); // Wait for 10 seconds before checking again
}

// Ensures the desired number of instances of a process are running
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

// Schedules a process to start at a specific time
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

// Schedules a process for repeated execution
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

// Appends a log entry for a process action
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
