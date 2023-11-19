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

## ProcessInfo Class

The `ProcessInfo` class is a key component of the utility, defining the structure and properties of each process that needs to be monitored or managed. Here is a detailed explanation of its properties:

### Properties

- **Name (`string`)**: 
  - **Description**: The name of the process. This is used to identify and manage the process within the system.
  - **Example**: `"ExampleProcess"`

- **Path (`string`)**: 
  - **Description**: The file path to the executable of the process. This path is used to start the process.
  - **Example**: `"C:\\Program Files\\ExampleProcess\\process.exe"`

- **Count (`Int32`)**: 
  - **Description**: The desired number of instances of this process to be running simultaneously. This is used to ensure that a specific number of process instances are active.
  - **Example**: `3`

- **Time (`string`)**: 
  - **Description**: The specific time of day when the process should be launched. It should be in the format of "HH:mm" (hour and minute).
  - **Example**: `"14:30"` (This would launch the process at 2:30 PM)

- **Interval (`int?`)**: 
  - **Description**: The frequency, in minutes, at which the process should be launched. This is used to schedule repeated execution of the process. If null, it indicates that the process does not need to be started at regular intervals.
  - **Example**: `15` (This would schedule the process to start every 15 minutes)

- **Enable (`bool`)**: 
  - **Description**: A flag indicating whether the process monitoring or management for this particular process should be enabled or disabled.
  - **Example**: `true` (This would enable the process monitoring and management)

### Usage in JSON Configuration

In the `processlist.json` file, each process to be managed should be defined as an object with the above properties. For example:

```json
{
    "processes": [
        {
            "Name": "ExampleProcess",
            "Path": "C:\\Program Files\\ExampleProcess\\process.exe",
            "Count": 3,
            "Time": "14:30",
            "Interval": 15,
            "Enable": true
        },
        // More processes can be added here
    ]
}
```

Each process object in the array represents a separate process configuration that the utility will manage based on the provided details.

## Notes

- The program uses a 10-second loop to continuously check and manage processes.
- It is advisable to have error handling for reading the JSON file and managing processes.

## Contribution

Contributions to the project are welcome. Please ensure you follow the coding standards for new features.

---

This README provides a basic overview. Please read through the source code comments for more detailed information.