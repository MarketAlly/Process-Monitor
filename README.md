# Process Monitor Utility

## Overview

This utility is a process monitoring and management tool written in C#. It allows users to automate the starting of processes based on a predefined schedule and conditions. The program reads from a JSON file to determine which processes to manage and how to manage them.

## Features

- **Flexible Process Management**: Enables starting processes at specific times, continuously running them, or scheduling them at regular intervals.
- **User Input for Window Management**: Users can choose whether processes should start in a new window.
- **Logging**: Automatically logs process start and management actions in a weekly log file.

## Requirements

- .NET Framework or .NET Core
- Newtonsoft.Json package for JSON processing
- Marketally.ProcessMonitor library (if applicable)

## Installation

1. Ensure that .NET Framework or .NET Core is installed on your system.
2. Include the `Newtonsoft.Json` package in your project.
3. Add `marketally.processmonitor` library, if it's a separate dependency.

## Usage

1. **Configure Process List**: Edit the `processlist.json` file to include the list of processes you want to manage. The JSON structure should be as follows:
   
   ```json
   {
       "processes": [
           {
               "Name": "ProcessName",
               "Path": "ExecutablePath",
               "Enable": true/false,
               "Interval": Minutes (optional),
               "Time": "HH:mm" (optional),
               "Count": NumberOfInstances (optional)
           },
           ...
       ]
   }
   ```

2. **Run the Utility**: Execute the program. It will ask if processes should be started in a new window. Respond with `yes` or `no`.
3. **Monitor Logs**: Check the `Logs` folder for weekly log files detailing the process management actions.

## Functions Description

- `EnsureProcessRunning`: Ensures the specified number of process instances are running.
- `ScheduleProcessStart`: Schedules a process to start at a specific time.
- `ScheduleRepeatedExecution`: Schedules a process to start at regular intervals.
- `AppendToLogFile`: Logs actions to a weekly log file.
- `GetLogFileName`: Generates a filename for the log based on the current week of the year.

## Notes

- The program uses a 10-second loop to continuously check and manage processes.
- It is advisable to have error handling for reading the JSON file and managing processes.

## Contribution

Contributions to the project are welcome. Please ensure you follow the coding standards for new features.

---

This README provides a basic overview. Please read through the source code comments for more detailed information.