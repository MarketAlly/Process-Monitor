using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace marketally.processmonitor
{
	class ProcessInfo
	{
		//Name of process
		public string Name { get; set; }
		//Path to the executable process
		public string Path { get; set; }
		//Number of processes
		public Int32 Count { get; set; }
		//Time of day hour and minute to launch
		public string Time { get; set; }
		//Frequency in minutes to launch
		public int? Interval { get; set; }
		//Whether to enable process
		public bool Enable { get; set; }
	}
}
